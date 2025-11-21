namespace CMCS.Web.Models
{
    public class NewUserVm
    {
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Role { get; set; }
        public decimal? HourlyRate { get; set; }
    }
}