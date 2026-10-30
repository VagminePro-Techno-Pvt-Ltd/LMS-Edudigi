using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.ContentSync
{
    [Table("ContentSyncOutboxJob")]
    public class ContentSyncOutboxJob
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string EntityType { get; set; } = default!; // "Program", "Semester", "Course"

        public int EntityId { get; set; }

        [Required]
        [StringLength(50)]
        public string Action { get; set; } = "CreateOrUpdate"; // "CreateOrUpdate", "Rename", "Move"

        [Required]
        [StringLength(50)]
        public string StorageProvider { get; set; } = default!; // "LocalDrive", "GoogleDrive"

        public string? Payload { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // "Pending", "Processing", "Completed", "Failed"

        public int RetryCount { get; set; } = 0;

        public int MaxRetries { get; set; } = 5;

        public DateTime? NextRetryUtc { get; set; }

        public string? LastError { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessedAt { get; set; }
    }
}
