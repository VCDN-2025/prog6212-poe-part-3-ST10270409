using System;
using System.Collections.Generic;

namespace CMCS.Web.Models
{
    public sealed class HrDashboardVm
    {
        public decimal TotalPayments { get; set; }

        public int PendingClaims { get; set; }

        public int VerifiedClaims { get; set; }

        public int TotalLecturers { get; set; }

        // Recent claims for the table at the bottom
        public List<Claim> RecentClaims { get; set; } = new();
    }
}
