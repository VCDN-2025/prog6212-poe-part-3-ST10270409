using System;

namespace CMCS.Web.Models
{
    public class ResetPasswordVm
    {
        public Guid UserId { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string NewPassword { get; set; } = string.Empty;
    }
}
