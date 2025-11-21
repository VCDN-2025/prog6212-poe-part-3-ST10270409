using CMCS.Web.Models;
using CMCS.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace CMCS.Web.Controllers;

[Authorize(Roles = "HR,Manager")]
public sealed class ReportsController : Controller
{
    private readonly IClaimRepository _repo;
    private readonly ILogger<ReportsController> _log;

    public ReportsController(IClaimRepository repo, ILogger<ReportsController> log)
    {
        _repo = repo;
        _log = log;
    }

    // In-text reference: (Microsoft, 2024) Querying and filtering with LINQ in ASP.NET Core.
    // URL: https://learn.microsoft.com/aspnet/core/data/ef-mvc/intro
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

    // In-text reference: (Microsoft, 2024) Returning file content in ASP.NET Core.
    // URL: https://learn.microsoft.com/aspnet/core/mvc/models/file-uploads
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

/*
References

Microsoft. 2024. Getting started with EF Core in ASP.NET Core MVC. Microsoft Learn.
Available at: https://learn.microsoft.com/aspnet/core/data/ef-mvc/intro [Accessed 20 Nov 2025].

Microsoft. 2024. File downloads and responses in ASP.NET Core. Microsoft Learn.
Available at: https://learn.microsoft.com/aspnet/core/mvc/models/file-uploads [Accessed 20 Nov 2025].

Microsoft. 2024. Logging in ASP.NET Core. Microsoft Learn.
Available at: https://learn.microsoft.com/aspnet/core/fundamentals/logging [Accessed 20 Nov 2025].
*/
