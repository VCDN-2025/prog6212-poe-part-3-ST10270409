// References:
// Microsoft 2024, 'Query data in ASP.NET Core MVC', Microsoft Learn, viewed 16 November 2025.
// Microsoft 2024, 'File downloads in ASP.NET Core', Microsoft Learn, viewed 16 November 2025.

using System.Text;
using CMCS.Web.Models;
using CMCS.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace CMCS.Web.Controllers;

public sealed class ReportsController : Controller
{
    private readonly IClaimRepository _repo;
    private readonly ILogger<ReportsController> _log;

    public ReportsController(IClaimRepository repo, ILogger<ReportsController> log)
    {
        _repo = repo;
        _log = log;
    }

    /// <summary>
    /// HR / Management report view with filters over status, date range and total amount.
    /// </summary>
    public async Task<IActionResult> Index(
        ClaimStatus? status,
        DateTime? from,
        DateTime? to,
        decimal? minTotal,
        decimal? maxTotal)
    {
        var claims = await _repo.GetAllAsync();
        var query = claims.AsQueryable();

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        if (from.HasValue)
            query = query.Where(c => c.Date >= from.Value.Date);

        if (to.HasValue)
            query = query.Where(c => c.Date <= to.Value.Date);

        if (minTotal.HasValue)
            query = query.Where(c => c.Total >= minTotal.Value);

        if (maxTotal.HasValue)
            query = query.Where(c => c.Total <= maxTotal.Value);

        var filtered = query
            .OrderByDescending(c => c.Date)
            .ToList();

        var totalApproved = filtered
            .Where(c => c.Status == ClaimStatus.ApprovedByManager)
            .Sum(c => c.Total);

        ViewBag.TotalApproved = totalApproved;
        ViewBag.FilterStatus = status;
        ViewBag.From = from?.ToString("yyyy-MM-dd");
        ViewBag.To = to?.ToString("yyyy-MM-dd");
        ViewBag.MinTotal = minTotal;
        ViewBag.MaxTotal = maxTotal;

        return View(filtered);
    }

    /// <summary>
    /// Export the same filtered dataset to CSV so HR can open in Excel.
    /// </summary>
    public async Task<IActionResult> ExportCsv(
        ClaimStatus? status,
        DateTime? from,
        DateTime? to,
        decimal? minTotal,
        decimal? maxTotal)
    {
        try
        {
            var claims = await _repo.GetAllAsync();
            var query = claims.AsQueryable();

            if (status.HasValue)
                query = query.Where(c => c.Status == status.Value);

            if (from.HasValue)
                query = query.Where(c => c.Date >= from.Value.Date);

            if (to.HasValue)
                query = query.Where(c => c.Date <= to.Value.Date);

            if (minTotal.HasValue)
                query = query.Where(c => c.Total >= minTotal.Value);

            if (maxTotal.HasValue)
                query = query.Where(c => c.Total <= maxTotal.Value);

            var filtered = query
                .OrderByDescending(c => c.Date)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("Date,Hours,HourlyRate,Total,Status");

            foreach (var c in filtered)
            {
                var line = string.Join(',',
                    c.Date.ToString("yyyy-MM-dd"),
                    c.HoursWorked.ToString("0.##"),
                    c.HourlyRate.ToString("0.##"),
                    c.Total.ToString("0.00"),
                    c.Status.ToString());
                sb.AppendLine(line);
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"cmcs-claims-report-{DateTime.Now:yyyyMMddHHmmss}.csv";

            return File(bytes, "text/csv", fileName);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to export claims report.");
            TempData["err"] = "Could not export CSV report.";
            return RedirectToAction(nameof(Index), new { status, from, to, minTotal, maxTotal });
        }
    }
}
