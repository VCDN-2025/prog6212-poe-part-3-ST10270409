using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMCS.Web.Models;

[Table("ClaimDocuments")]
public class ClaimDocument
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(255)]
    public string OriginalFileName { get; set; } = default!;

    [Required, MaxLength(255)]
    public string StoredFileName { get; set; } = default!;

    public long SizeBytes { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Guid ClaimId { get; set; }

    [ForeignKey("ClaimId")]
    public virtual Claim Claim { get; set; } = default!;
}