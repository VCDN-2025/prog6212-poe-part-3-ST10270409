using System.ComponentModel.DataAnnotations;

namespace CMCS.Web.Models
{
    public class ClaimDocument
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        public string StoredFileName { get; set; } = string.Empty;

        public long SizeBytes { get; set; }

        public DateTime UploadedAt { get; set; }

        [Required]
        public Guid ClaimId { get; set; }

        public Claim Claim { get; set; } = null!;
    }
}
