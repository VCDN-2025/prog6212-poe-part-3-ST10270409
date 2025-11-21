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

    // In-text reference: (Microsoft, 2024) ASP.NET Core MVC controllers and actions.
    // URL: https://learn.microsoft.com/aspnet/core/mvc/controllers/actions
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var pending = (await _repo.GetAllAsync())
                .Where(c => c.Status == ClaimStatus.Pending)
                .OrderByDescending(c => c.Date)
                .ToList();

            return View(pending);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to load coordinator dashboard.");
            TempData["err"] = "Unable to load pending claims.";
            return View(Enumerable.Empty<Claim>());
        }
    }

    // In-text reference: (Microsoft, 2024) Validation patterns and business rules.
    // URL: https://learn.microsoft.com/aspnet/core/mvc/models/validation
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify(Guid id)
    {
        try
        {
            var claim = await _repo.GetAsync(id);
            if (claim is null) return NotFound();

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
            _log.LogError(ex, "Verify failed for claim {ClaimId}", id);
            TempData["err"] = "Could not verify claim.";
        }

        return RedirectToAction(nameof(Index));
    }

    // In-text reference: (Microsoft, 2024) Handling form posts securely with anti-forgery.
    // URL: https://learn.microsoft.com/aspnet/core/security/anti-request-forgery
    [HttpPost]
    [ValidateAntiForgeryToken]
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
            _log.LogError(ex, "Reject failed for claim {ClaimId}", id);
            TempData["err"] = "Could not reject claim.";
        }

        return RedirectToAction(nameof(Index));
    }

    // In-text reference: (Microsoft, 2024) Encapsulating business rules in helper methods.
    // URL: https://learn.microsoft.com/aspnet/core/architecture/modern-web-apps-azure
    private ValidationResult ValidateClaim(Claim claim)
    {
        if (claim.HoursWorked < 0.5m || claim.HoursWorked > 24m)
            return ValidationResult.Fail("Hours worked must be between 0.5 and 24.");

        if (claim.HourlyRate < 50m || claim.HourlyRate > 5000m)
            return ValidationResult.Fail("Hourly rate must be between R50 and R5000.");

        if (claim.Total > 100_000m)
            return ValidationResult.Fail("Claim amount exceeds the maximum allowed (R100,000).");

        return ValidationResult.Success();
    }
}

/*
References

Microsoft. 2024. Controllers, actions and routing in ASP.NET Core MVC. Microsoft Learn.
Available at: https://learn.microsoft.com/aspnet/core/mvc/controllers/actions [Accessed 20 Nov 2025].

Microsoft. 2024. Model validation in ASP.NET Core. Microsoft Learn.
Available at: https://learn.microsoft.com/aspnet/core/mvc/models/validation [Accessed 20 Nov 2025].

Microsoft. 2024. Anti-forgery token configuration in ASP.NET Core. Microsoft Learn.
Available at: https://learn.microsoft.com/aspnet/core/security/anti-request-forgery [Accessed 20 Nov 2025].

Microsoft. 2024. Architect modern web applications with ASP.NET Core and Azure. Microsoft Learn.
Available at: https://learn.microsoft.com/aspnet/core/architecture/modern-web-apps-azure [Accessed 20 Nov 2025].
*/
