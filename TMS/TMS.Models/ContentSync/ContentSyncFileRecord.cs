using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.ContentSync
{
    [Table("ContentSyncFileRecord")]
    public class ContentSyncFileRecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string StorageProvider { get; set; } = default!; // "LocalDrive", "GoogleDrive"

        public int CourseId { get; set; }

        [Required]
        [StringLength(50)]
        public string SubfolderType { get; set; } = default!; // "Videos", "PDFs", "Assignments", "Forum_Discussion"

        [Required]
        [StringLength(500)]
        public string ProviderFileId { get; set; } = default!;

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = default!;

        public long FileSize { get; set; }

        [StringLength(100)]
        public string? FileHash { get; set; }

        [Required]
        [StringLength(50)]
        public string LmsRecordType { get; set; } = "LectureMaterial"; // "LectureMaterial", "Assignment", "DiscussionThread"

        public int LmsRecordId { get; set; }

        [Required]
        [StringLength(50)]
        public string SyncStatus { get; set; } = "Synced"; // "Synced", "Failed", "Skipped"

        public DateTime SyncedAt { get; set; } = DateTime.UtcNow;

        public string? ErrorMessage { get; set; }
    }
}
