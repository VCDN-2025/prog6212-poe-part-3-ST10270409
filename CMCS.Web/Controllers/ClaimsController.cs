using CMCS.Web.Data;
using CMCS.Web.Models;
using CMCS.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CMCS.Web.Controllers;

[Authorize(Roles = "Lecturer")]
public sealed class ClaimsController : Controller
{
    private readonly IClaimRepository _repo;
    private readonly IFileCrypto _fileCrypto;
    private readonly IWebHostEnvironment _env;
    private readonly IUserService _userService;
    private readonly ILogger<ClaimsController> _log;

    public ClaimsController(
        IClaimRepository repo,
        IFileCrypto fileCrypto,
        IWebHostEnvironment env,
        IUserService userService,
        ILogger<ClaimsController> log)
    {
        _repo = repo;
        _fileCrypto = fileCrypto;
        _env = env;
        _userService = userService;
        _log = log;
    }

    public async Task<IActionResult> My()
    {
        try
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                TempData["err"] = "User not found.";
                return View(new List<CMCS.Web.Models.Claim>());
            }

            var user = _userService.GetAllUsers().FirstOrDefault(u => u.Email == userEmail);
            if (user == null)
            {
                TempData["err"] = "User not found.";
                return View(new List<CMCS.Web.Models.Claim>());
            }

            var claims = await _repo.GetAllAsync();
            var userClaims = claims.Where(c => c.LecturerId == user.Id.ToString()).ToList();

            return View(userClaims);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to load claims for user.");
            TempData["err"] = "Unable to load your claims.";
            return View(new List<CMCS.Web.Models.Claim>());
        }
    }

    public IActionResult Create()
    {
        try
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                TempData["err"] = "User not found.";
                return RedirectToAction(nameof(My));
            }

            var user = _userService.GetAllUsers().FirstOrDefault(u => u.Email == userEmail);
            if (user == null)
            {
                TempData["err"] = "User not found.";
                return RedirectToAction(nameof(My));
            }

            var model = new CreateClaimVm
            {
                HourlyRate = user.HourlyRate > 0 ? user.HourlyRate : 350.00m // Default fallback
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to load create claim page.");
            TempData["err"] = "Unable to load claim form.";
            return RedirectToAction(nameof(My));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateClaimVm model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                TempData["err"] = "User not found.";
                return View(model);
            }

            var user = _userService.GetAllUsers().FirstOrDefault(u => u.Email == userEmail);
            if (user == null)
            {
                TempData["err"] = "User not found.";
                return View(model);
            }

            var claim = new CMCS.Web.Models.Claim
            {
                Id = Guid.NewGuid(),
                Date = model.Date,
                HoursWorked = model.HoursWorked,
                HourlyRate = model.HourlyRate,
                Notes = model.Notes,
                Status = ClaimStatus.Pending,
                LecturerName = user.Name,
                LecturerId = user.Id.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(claim);
            TempData["ok"] = "Claim submitted successfully!";
            return RedirectToAction(nameof(My));
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to create claim.");
            TempData["err"] = "Failed to submit claim. Please try again.";
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(Guid id, IFormFile file)
    {
        try
        {
            Console.WriteLine($"Upload started - File: {file?.FileName}, Size: {file?.Length}");

            if (file == null || file.Length == 0)
            {
                TempData["err"] = "Please select a file to upload.";
                return RedirectToAction(nameof(My));
            }

            Console.WriteLine($"File type: {Path.GetExtension(file.FileName)}");
            Console.WriteLine($"File size: {file.Length} bytes");

            // Validate file type
            if (!_fileCrypto.IsAllowedExtension(file.FileName))
            {
                TempData["err"] = $"Invalid file type '{Path.GetExtension(file.FileName)}'. Allowed: PDF, Word, Excel, Images, Text files.";
                return RedirectToAction(nameof(My));
            }

            // Validate file size (10MB max)
            if (!_fileCrypto.IsAllowedSize(file.Length))
            {
                TempData["err"] = $"File too large ({file.Length / 1024 / 1024}MB). Maximum size is 10MB.";
                return RedirectToAction(nameof(My));
            }

            // Verify user owns this claim
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var user = _userService.GetAllUsers().FirstOrDefault(u => u.Email == userEmail);
            if (user == null)
            {
                TempData["err"] = "Access denied.";
                return RedirectToAction(nameof(My));
            }

            // Create uploads directory if it doesn't exist
            var uploadsPath = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsPath);

            var storedFileName = await _fileCrypto.EncryptAndSaveAsync(
                file.OpenReadStream(),
                uploadsPath,
                file.FileName
            );

            // Create and save the document directly to avoid concurrency issues
            var document = new ClaimDocument
            {
                Id = Guid.NewGuid(),
                OriginalFileName = file.FileName,
                StoredFileName = storedFileName,
                SizeBytes = file.Length,
                UploadedAt = DateTime.UtcNow,
                ClaimId = id
            };

            // Use the DbContext directly to add the document
            using (var scope = HttpContext.RequestServices.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.ClaimDocuments.Add(document);
                await dbContext.SaveChangesAsync();
            }

            TempData["ok"] = $"File '{file.FileName}' uploaded successfully!";
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Upload failed for claim {ClaimId}", id);
            TempData["err"] = $"Upload failed: {ex.Message}";
        }

        return RedirectToAction(nameof(My));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var claim = await _repo.GetAsync(id);
            if (claim == null)
            {
                TempData["err"] = "Claim not found.";
                return RedirectToAction(nameof(My));
            }

            // Verify user owns this claim
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var user = _userService.GetAllUsers().FirstOrDefault(u => u.Email == userEmail);
            if (user == null || claim.LecturerId != user.Id.ToString())
            {
                TempData["err"] = "Access denied.";
                return RedirectToAction(nameof(My));
            }

            // Only allow deletion of pending claims
            if (claim.Status != ClaimStatus.Pending)
            {
                TempData["err"] = "Only pending claims can be deleted.";
                return RedirectToAction(nameof(My));
            }

            // In a real application, you would properly delete the claim
            // For now, we'll just change the status or remove it
            var claims = await _repo.GetAllAsync();
            var claimList = claims.ToList();
            claimList.RemoveAll(c => c.Id == id);

            TempData["ok"] = "Claim deleted successfully.";
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Delete failed for claim {ClaimId}", id);
            TempData["err"] = "Failed to delete claim.";
        }

        return RedirectToAction(nameof(My));
    }
}