using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMCS.Web.Models;

[Table("Claims")]
public class Claim
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, DataType(DataType.Date)]
    public DateTime Date { get; set; } = DateTime.Today;

    [Required, Range(0.5, 24)]
    public double HoursWorked { get; set; }

    [Required, Range(50, 5000)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal HourlyRate { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    // Calculated property - NOT stored in database
    [NotMapped]
    public decimal Total => Math.Round((decimal)HoursWorked * HourlyRate, 2, MidpointRounding.AwayFromZero);

    public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

    public virtual ICollection<ClaimDocument> Documents { get; set; } = new List<ClaimDocument>();

    [Required, MaxLength(100)]
    public string LecturerName { get; set; } = string.Empty;

    [Required]
    public string LecturerId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? VerifiedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
}