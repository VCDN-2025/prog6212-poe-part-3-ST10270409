using CMCS.Web.Models;
using CMCS.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMCS.Web.Controllers;

[Authorize(Roles = "Manager")]
public sealed class ManagerController : Controller
{
    private readonly IClaimRepository _repo;
    private readonly ILogger<ManagerController> _log;

    public ManagerController(IClaimRepository repo, ILogger<ManagerController> log)
    {
        _repo = repo;
        _log = log;
    }

    // In-text reference: (Microsoft, 2024) Role-based authorization in ASP.NET Core.
    // URL: https://learn.microsoft.com/aspnet/core/security/authorization/roles
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var verified = (await _repo.GetAllAsync())
                .Where(c => c.Status == ClaimStatus.VerifiedByCoordinator)
                .OrderByDescending(c => c.Date)
                .ToList();

            return View(verified);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to load manager dashboard.");
            TempData["err"] = "Unable to load verified claims.";
            return View(Enumerable.Empty<Claim>());
        }
    }

    // In-text reference: (Microsoft, 2024) Implementing approval workflows in web apps.
    // URL: https://learn.microsoft.com/aspnet/core/architecture/modern-web-apps-azure
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id)
    {
        try
        {
            var claim = await _repo.GetAsync(id);
            if (claim is null) return NotFound();

            if (claim.Total > 50_000m)
            {
                TempData["warn"] =
                    "High-value claim detected (over R50,000). Please confirm offline before final approval.";
            }

            claim.Status = ClaimStatus.ApprovedByManager;
            claim.ApprovedAt = DateTime.UtcNow;

            await _repo.UpdateAsync(claim);
            TempData["ok"] = "Claim approved successfully.";
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Approve failed for claim {ClaimId}", id);
            TempData["err"] = "Could not approve claim.";
        }

        return RedirectToAction(nameof(Index));
    }

    // In-text reference: (Microsoft, 2024) Posting form data and handling commands.
    // URL: https://learn.microsoft.com/aspnet/core/mvc/controllers/actions
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
}

/*
References

Microsoft. 2024. Role-based authorization in ASP.NET Core. Microsoft Learn.
Available at: https://learn.microsoft.com/aspnet/core/security/authorization/roles [Accessed 20 Nov 2025].

Microsoft. 2024. Controllers and actions in ASP.NET Core MVC. Microsoft Learn.
Available at: https://learn.microsoft.com/aspnet/core/mvc/controllers/actions [Accessed 20 Nov 2025].

Microsoft. 2024. Architect modern web applications with ASP.NET Core and Azure. Microsoft Learn.
Available at: https://learn.microsoft.com/aspnet/core/architecture/modern-web-apps-azure [Accessed 20 Nov 2025].
*/
