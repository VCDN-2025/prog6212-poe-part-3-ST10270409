using CMCS.Web.Models;
using CMCS.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace CMCS.Web.Controllers;

[Authorize(Roles = "HR")]
public sealed class HRController : Controller
{
    private readonly IClaimRepository _claimRepo;
    private readonly IUserService _userService;
    private readonly ILogger<HRController> _log;

    public HRController(IClaimRepository claimRepo, IUserService userService, ILogger<HRController> log)
    {
        _claimRepo = claimRepo;
        _userService = userService;
        _log = log;
    }

    public async Task<IActionResult> Dashboard()
    {
        var claims = await _claimRepo.GetAllAsync();
        var users = _userService.GetAllUsers();

        var approvedClaims = claims.Where(c => c.Status == ClaimStatus.ApprovedByManager).ToList();
        var totalPayments = approvedClaims.Sum(c => c.Total);
        var pendingClaims = claims.Count(c => c.Status == ClaimStatus.Pending);
        var verifiedClaims = claims.Count(c => c.Status == ClaimStatus.VerifiedByCoordinator);

        ViewBag.TotalPayments = totalPayments;
        ViewBag.PendingClaims = pendingClaims;
        ViewBag.VerifiedClaims = verifiedClaims;
        ViewBag.TotalLecturers = users.Count(u => u.Role == "Lecturer");

        return View(claims);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GeneratePaymentReport()
    {
        try
        {
            var claims = await _claimRepo.GetAllAsync();
            var approvedClaims = claims.Where(c => c.Status == ClaimStatus.ApprovedByManager).ToList();

            var report = new StringBuilder();
            report.AppendLine("Payment Report - Generated on: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            report.AppendLine("==============================================");
            report.AppendLine("Lecturer Name|Date|Hours|Rate|Total Amount|Status");
            report.AppendLine("----------------------------------------------");

            foreach (var claim in approvedClaims)
            {
                report.AppendLine($"{claim.LecturerName}|{claim.Date:yyyy-MM-dd}|{claim.HoursWorked}|{claim.HourlyRate:C}|{claim.Total:C}|Approved");
            }

            report.AppendLine("==============================================");
            report.AppendLine($"Total Approved Amount: {approvedClaims.Sum(c => c.Total):C}");
            report.AppendLine($"Total Claims: {approvedClaims.Count}");

            var bytes = Encoding.UTF8.GetBytes(report.ToString());
            var fileName = $"payment-report-{DateTime.Now:yyyyMMddHHmmss}.txt";

            return File(bytes, "text/plain", fileName);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to generate payment report");
            TempData["err"] = "Failed to generate payment report.";
            return RedirectToAction(nameof(Dashboard));
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateInvoice(Guid claimId)
    {
        try
        {
            var claim = await _claimRepo.GetAsync(claimId);
            if (claim == null || claim.Status != ClaimStatus.ApprovedByManager)
            {
                TempData["err"] = "Claim not found or not approved.";
                return RedirectToAction(nameof(Dashboard));
            }

            var invoice = new StringBuilder();
            invoice.AppendLine("INVOICE");
            invoice.AppendLine("=========");
            invoice.AppendLine($"Invoice Date: {DateTime.Now:yyyy-MM-dd}");
            invoice.AppendLine($"Claim Date: {claim.Date:yyyy-MM-dd}");
            invoice.AppendLine($"Lecturer: {claim.LecturerName}");
            invoice.AppendLine("----------------------------------");
            invoice.AppendLine($"Hours Worked: {claim.HoursWorked}");
            invoice.AppendLine($"Hourly Rate: {claim.HourlyRate:C}");
            invoice.AppendLine($"Total Amount: {claim.Total:C}");
            invoice.AppendLine("----------------------------------");
            invoice.AppendLine("Status: APPROVED FOR PAYMENT");
            invoice.AppendLine("=========");

            var bytes = Encoding.UTF8.GetBytes(invoice.ToString());
            var fileName = $"invoice-{claim.LecturerName}-{claim.Date:yyyyMMdd}.txt";

            return File(bytes, "text/plain", fileName);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to generate invoice for claim {ClaimId}", claimId);
            TempData["err"] = "Failed to generate invoice.";
            return RedirectToAction(nameof(Dashboard));
        }
    }
}