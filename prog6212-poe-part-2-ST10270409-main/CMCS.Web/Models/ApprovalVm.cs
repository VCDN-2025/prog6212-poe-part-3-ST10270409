//Referencing list//
//https://learn.microsoft.com/en-us/aspnet/core/mvc/overview?view=aspnetcore-8.0//

using System;

namespace CMCS.Web.Models;
public class ApprovalVm
{
    public Guid ClaimId { get; set; }
    public string Lecturer { get; set; } = "";
    public string MonthLabel { get; set; } = "";
    public string Stage { get; set; } = "ProgrammeCoordinator";
    public string Status { get; set; } = "UnderReview";
}
