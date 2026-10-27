using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Masters;

namespace TMS.Models.LearningAssistant
{
    public enum DocumentProcessingStatus
    {
        Pending = 0,
        Indexed = 1,
        ScannedOcrRequired = 2,
        Encrypted = 3,
        Empty = 4,
        Corrupted = 5,
        Unsupported = 6,
        FileNotFound = 7,
        Failed = 8
    }

    [Table("DocumentIndexStatuses")]
    public class DocumentIndexStatus : BaseModel
    {
        [Required]
        public int LectureMaterialId { get; set; }

        [ForeignKey(nameof(LectureMaterialId))]
        public virtual LectureMaterial? LectureMaterial { get; set; }

        [Required, StringLength(50)]
        public string Status { get; set; } = DocumentProcessingStatus.Pending.ToString();

        [StringLength(100)]
        public string? FileHash { get; set; }

        public int PageCount { get; set; }

        public int TotalChunks { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTime LastIndexedOn { get; set; } = DateTime.UtcNow;
    }
}
