//Referencing list//
//https://learn.microsoft.com/en-us/aspnet/core/mvc/overview?view=aspnetcore-8.0//

namespace CMCS.Web.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
