// References:
// Microsoft 2024, Overview of ASP.NET Core MVC, Microsoft Learn, viewed 16 November 2025.
// Stack Overflow 2023, 'Group and project data for a dashboard in ASP.NET Core MVC', viewed 16 November 2025.

using CMCS.Web.Models;
using CMCS.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace CMCS.Web.Controllers;

public sealed class ApprovalsController : Controller
{
    private readonly IClaimRepository _repo;
    private readonly ILogger<ApprovalsController> _log;

    public ApprovalsController(IClaimRepository repo, ILogger<ApprovalsController> log)
    {
        _repo = repo;
        _log = log;
    }

    /// <summary>
    /// Staff-only summary dashboard that shows each claim,
    /// which stage of the workflow it is currently in, and
    /// its overall status.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        try
        {
            var claims = await _repo.GetAllAsync();

            // Project JSON claims into a staff-friendly view model
            var items = claims
                .OrderByDescending(c => c.Date)
                .Select(c => new ApprovalVm
                {
                    ClaimId = c.Id,
                    Lecturer = "Demo lecturer", // single-lecturer prototype
                    MonthLabel = c.Date.ToString("MMMM yyyy"),
                    Stage = StageFor(c.Status),
                    Status = StatusFor(c.Status)
                })
                .ToList();

            return View(items);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to build approvals dashboard.");
            TempData["err"] = "Unable to load approvals dashboard.";
            return View(Enumerable.Empty<ApprovalVm>());
        }
    }

    private static string StageFor(ClaimStatus status) => status switch
    {
        ClaimStatus.Pending => "ProgrammeCoordinator",
        ClaimStatus.VerifiedByCoordinator => "AcademicManager",
        ClaimStatus.ApprovedByManager => "Complete",
        ClaimStatus.Rejected => "Complete",
        _ => "Unknown"
    };

    private static string StatusFor(ClaimStatus status) => status switch
    {
        ClaimStatus.Pending => "UnderReview",
        ClaimStatus.VerifiedByCoordinator => "UnderReview",
        ClaimStatus.ApprovedByManager => "Approved",
        ClaimStatus.Rejected => "Rejected",
        _ => "Unknown"
    };
}
