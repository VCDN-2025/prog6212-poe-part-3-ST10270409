using CMCS.Web.Models;
using CMCS.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMCS.Web.Controllers;

[Authorize(Roles = "Coordinator")]
public sealed class CoordinatorController : Controller
{
    private readonly IClaimRepository _repo;
    private readonly ILogger<CoordinatorController> _log;

    public CoordinatorController(IClaimRepository repo, ILogger<CoordinatorController> log)
    {
        _repo = repo;
        _log = log;
    }

    public async Task<IActionResult> Index()
    {
        var pending = (await _repo.GetAllAsync()).Where(c => c.Status == ClaimStatus.Pending).ToList();
        return View(pending);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify(Guid id)
    {
        try
        {
            var claim = await _repo.GetAsync(id);
            if (claim is null) return NotFound();

            // Automated validation checks
            var validationResult = ValidateClaim(claim);
            if (!validationResult.IsValid)
            {
                TempData["err"] = $"Validation failed: {validationResult.ErrorMessage}";
                return RedirectToAction(nameof(Index));
            }

            claim.Status = ClaimStatus.VerifiedByCoordinator;
            claim.VerifiedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(claim);
            TempData["ok"] = "Claim verified successfully.";
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Verify failed");
            TempData["err"] = "Could not verify claim.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid id)
    {
        try
        {
            var claim = await _repo.GetAsync(id);
            if (claim is null) return NotFound();

            claim.Status = ClaimStatus.Rejected;
            await _repo.UpdateAsync(claim);
            TempData["ok"] = "Claim rejected.";
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Reject failed");
            TempData["err"] = "Could not reject claim.";
        }
        return RedirectToAction(nameof(Index));
    }

    private ValidationResult ValidateClaim(CMCS.Web.Models.Claim claim)
    {
        // Automated validation against predefined criteria
        if (claim.HoursWorked < 0.5 || claim.HoursWorked > 24)
            return ValidationResult.Fail("Hours worked must be between 0.5 and 24.");

        if (claim.HourlyRate < 50 || claim.HourlyRate > 5000)
            return ValidationResult.Fail("Hourly rate must be between R50 and R5000.");

        if (claim.Total > 100000) // Maximum claim amount
            return ValidationResult.Fail("Claim amount exceeds maximum limit.");

        return ValidationResult.Success();
    }

    private class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        public static ValidationResult Success() => new() { IsValid = true };
        public static ValidationResult Fail(string message) => new() { IsValid = false, ErrorMessage = message };
    }
}