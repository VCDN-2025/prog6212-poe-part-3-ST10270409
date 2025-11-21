using System.Security.Claims;
using CMCS.Web.Data;
using CMCS.Web.Models;
using CMCS.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
// alias to avoid clash with System.Security.Claims.Claim
using DbClaim = CMCS.Web.Models.Claim;

namespace CMCS.Web.Controllers
{
    // Class-level: any authenticated user may access this controller,
    // but we lock down each action with role-specific attributes.
    [Authorize]
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

        // ------------------------------
        // LECTURER – MY CLAIMS LIST
        // ------------------------------
        // Only lecturers may see *their own* claims
        [Authorize(Roles = "Lecturer")]
        [HttpGet]
        public async Task<IActionResult> My()
        {
            try
            {
                // Use NameIdentifier (user.Id) as stored in LecturerId
                var lecturerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(lecturerId))
                {
                    TempData["err"] = "User not found.";
                    return View(new List<DbClaim>());
                }

                var claims = await _repo.GetClaimsByLecturerAsync(lecturerId);
                return View(claims);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to load claims for current user.");
                TempData["err"] = "Unable to load your claims.";
                return View(new List<DbClaim>());
            }
        }

        // ------------------------------
        // CLAIM DETAILS (SHARED)
        // ------------------------------
        // Lecturers, HR, Coordinator and Manager may all view details
        [Authorize(Roles = "Lecturer,HR,Coordinator,Manager")]
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var claim = await _repo.GetAsync(id);
                if (claim == null) return NotFound();

                return View(claim);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to load details for claim {ClaimId}", id);
                TempData["err"] = "Unable to load claim details.";
                return RedirectToAction("Dashboard", "HR");
            }
        }

        // ------------------------------
        // LECTURER – CREATE CLAIM
        // ------------------------------
        [Authorize(Roles = "Lecturer")]
        [HttpGet]
        public IActionResult Create()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["err"] = "User not found.";
                    return RedirectToAction(nameof(My));
                }

                var user = _userService.GetUserById(Guid.Parse(userId));
                if (user == null)
                {
                    TempData["err"] = "User not found.";
                    return RedirectToAction(nameof(My));
                }

                if (user.HourlyRate <= 0)
                {
                    TempData["err"] = "Your hourly rate has not been configured by HR yet.";
                    return RedirectToAction(nameof(My));
                }

                var model = new CreateClaimVm
                {
                    HourlyRate = user.HourlyRate
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

        [Authorize(Roles = "Lecturer")]
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
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["err"] = "User not found.";
                    return View(model);
                }

                var user = _userService.GetUserById(Guid.Parse(userId));
                if (user == null)
                {
                    TempData["err"] = "User not found.";
                    return View(model);
                }

                if (user.HourlyRate <= 0)
                {
                    ModelState.AddModelError(string.Empty,
                        "Your hourly rate has not been configured by HR. Please contact HR.");
                    return View(model);
                }

                var claim = new DbClaim
                {
                    Id = Guid.NewGuid(),
                    Date = model.Date,
                    HoursWorked = model.HoursWorked,
                    HourlyRate = user.HourlyRate, // Rate controlled by HR
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

        // ------------------------------
        // LECTURER – UPLOAD SUPPORTING DOC
        // ------------------------------
        [Authorize(Roles = "Lecturer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(Guid id, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    TempData["err"] = "Please select a file to upload.";
                    return RedirectToAction(nameof(My));
                }

                if (!_fileCrypto.IsAllowedExtension(file.FileName))
                {
                    TempData["err"] =
                        $"Invalid file type '{Path.GetExtension(file.FileName)}'. Allowed: PDF, Word, Excel, Images, Text files.";
                    return RedirectToAction(nameof(My));
                }

                if (!_fileCrypto.IsAllowedSize(file.Length))
                {
                    TempData["err"] =
                        $"File too large ({file.Length / 1024 / 1024}MB). Maximum size is 10MB.";
                    return RedirectToAction(nameof(My));
                }

                // Ensure claim belongs to current lecturer
                var lecturerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var claim = await _repo.GetAsync(id);
                if (claim == null || string.IsNullOrEmpty(lecturerId) || claim.LecturerId != lecturerId)
                {
                    TempData["err"] = "Access denied.";
                    return RedirectToAction(nameof(My));
                }

                var uploadsPath = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsPath);

                var storedFileName = await _fileCrypto.EncryptAndSaveAsync(
                    file.OpenReadStream(),
                    uploadsPath,
                    file.FileName);

                using var scope = HttpContext.RequestServices.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var document = new ClaimDocument
                {
                    Id = Guid.NewGuid(),
                    OriginalFileName = file.FileName,
                    StoredFileName = storedFileName,
                    SizeBytes = file.Length,
                    UploadedAt = DateTime.UtcNow,
                    ClaimId = id
                };

                dbContext.ClaimDocuments.Add(document);
                await dbContext.SaveChangesAsync();

                TempData["ok"] = $"File '{file.FileName}' uploaded successfully!";
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Upload failed for claim {ClaimId}", id);
                TempData["err"] = $"Upload failed: {ex.Message}";
            }

            return RedirectToAction(nameof(My));
        }

        // ------------------------------
        // DOWNLOAD DOCUMENT (SHARED)
        // ------------------------------
        // HR/Manager/Coordinator must also be able to download docs
        [Authorize(Roles = "Lecturer,HR,Coordinator,Manager")]
        [HttpGet]
        public async Task<IActionResult> Download(Guid claimId, Guid docId)
        {
            try
            {
                using var scope = HttpContext.RequestServices.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var document = await db.ClaimDocuments
                    .FirstOrDefaultAsync(d => d.Id == docId && d.ClaimId == claimId);

                if (document == null) return NotFound();

                // Decrypt to a temp file, then stream to browser
                var tempPath = Path.Combine(
                    Path.GetTempPath(),
                    $"{Guid.NewGuid():N}_{document.OriginalFileName}");

                await _fileCrypto.DecryptToAsync(document.StoredFileName, tempPath);
                var bytes = await System.IO.File.ReadAllBytesAsync(tempPath);
                System.IO.File.Delete(tempPath);

                return File(bytes, "application/octet-stream", document.OriginalFileName);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Download failed for document {DocId}", docId);
                TempData["err"] = "Unable to download document.";
                return RedirectToAction("Dashboard", "HR");
            }
        }

        // ------------------------------
        // LECTURER – DELETE CLAIM
        // ------------------------------
        [Authorize(Roles = "Lecturer")]
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

                var lecturerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(lecturerId) || claim.LecturerId != lecturerId)
                {
                    TempData["err"] = "Access denied.";
                    return RedirectToAction(nameof(My));
                }

                if (claim.Status != ClaimStatus.Pending)
                {
                    TempData["err"] = "Only pending claims can be deleted.";
                    return RedirectToAction(nameof(My));
                }

                await _repo.DeleteAsync(id);

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
}
