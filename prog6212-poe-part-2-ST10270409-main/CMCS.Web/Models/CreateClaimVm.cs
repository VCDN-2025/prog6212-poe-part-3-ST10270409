using System.ComponentModel.DataAnnotations;

namespace CMCS.Web.Models
{
    public class CreateClaimVm
    {
        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        [Range(0.1, 24, ErrorMessage = "Hours must be between 0.1 and 24.")]
        public decimal HoursWorked { get; set; }

        // Set from HR – displayed as readonly
        public decimal HourlyRate { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
