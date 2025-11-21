using System;
using System.Linq;
using System.Threading.Tasks;
using CMCS.Web.Models;
using CMCS.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMCS.Web.Controllers
{
    // In-text reference: (Microsoft, 2024) Role-based authorization in ASP.NET Core.
    // URL: https://learn.microsoft.com/aspnet/core/security/authorization/roles
    [Authorize(Roles = "HR")]
    public sealed class HRController : Controller
    {
        private readonly IClaimRepository _claims;
        private readonly IUserService _users;

        public HRController(IClaimRepository claims, IUserService users)
        {
            _claims = claims;
            _users = users;
        }

        // In-text reference: (Microsoft, 2024) Using LINQ to query collections in C#.
        // URL: https://learn.microsoft.com/dotnet/csharp/programming-guide/concepts/linq
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            // Load all claims once
            var allClaims = (await _claims.GetAllAsync()).ToList();

            // PENDING = still waiting for any action (only Pending in your enum)
            var pending = allClaims.Where(c => c.Status == ClaimStatus.Pending);

            // VERIFIED = checked by Coordinator or already approved by Manager
            var verified = allClaims.Where(c =>
                c.Status == ClaimStatus.VerifiedByCoordinator ||
                c.Status == ClaimStatus.ApprovedByManager);

            // FINAL PAYMENTS = only manager-approved claims
            var approved = allClaims.Where(c => c.Status == ClaimStatus.ApprovedByManager);

            // In-text reference: (Microsoft, 2024) Aggregate operations with LINQ.
            // URL: https://learn.microsoft.com/dotnet/csharp/programming-guide/concepts/linq/aggregation-operations
            var totalPayments = approved.Sum(c => c.HoursWorked * c.HourlyRate);

            // HR manages all users – count lecturers for the "Total Lecturers" card
            var allUsers = _users.GetAllUsers();
            var lecturerCount = allUsers.Count(u => u.Role == "Lecturer");

            // Recent 5 claims, newest first
            var recentClaims = allClaims
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .ToList();

            var vm = new HrDashboardVm
            {
                TotalPayments = totalPayments,
                PendingClaims = pending.Count(),
                VerifiedClaims = verified.Count(),
                TotalLecturers = lecturerCount,
                RecentClaims = recentClaims
            };

            return View("Dashboard", vm);
        }

        // Optional extra HR views (e.g. reports) can stay here if you have them

        [HttpGet]
        public IActionResult Reports()
        {
            return View();
        }
    }
}

/*
References

Microsoft. 2024. Role-based authorization in ASP.NET Core. Microsoft Learn.
Available at: https://learn.microsoft.com/aspnet/core/security/authorization/roles [Accessed 20 Nov 2025].

Microsoft. 2024. LINQ in C# – Query expressions and aggregate operations. Microsoft Learn.
Available at: https://learn.microsoft.com/dotnet/csharp/programming-guide/concepts/linq [Accessed 20 Nov 2025].
*/
