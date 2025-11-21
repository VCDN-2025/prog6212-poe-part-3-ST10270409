using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMCS.Web.Models
{
   
    public class Claim
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Range(0.1, 24)]
        public decimal HoursWorked { get; set; }

        [Range(0, double.MaxValue)]
        public decimal HourlyRate { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [Required]
        public string LecturerId { get; set; } = string.Empty;

        [Required]
        public string LecturerName { get; set; } = string.Empty;

        public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? VerifiedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public ICollection<ClaimDocument> Documents { get; set; } = new List<ClaimDocument>();

        [NotMapped]
        public decimal Total => HoursWorked * HourlyRate;
    }
}
