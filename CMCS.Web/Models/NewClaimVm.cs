using System.Collections.Generic;

namespace CMCS.Web.Models;
public class NewClaimVm
{
    public string LecturerName { get; set; } = "Lonwabo Wabo (demo)";
    public string MonthLabel { get; set; } = "September 2025";
    public string Status { get; set; } = "Draft";
    public List<ClaimItemVm> Items { get; set; } = new();
}
