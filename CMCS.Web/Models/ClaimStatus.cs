//Referencing list//
//https://learn.microsoft.com/en-us/aspnet/core/mvc/overview?view=aspnetcore-8.0//

namespace CMCS.Web.Models;

public enum ClaimStatus
{
    Pending = 0,
    VerifiedByCoordinator = 1,
    ApprovedByManager = 2,
    Rejected = 3
}
