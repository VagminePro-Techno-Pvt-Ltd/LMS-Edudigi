using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Masters
{
    [Table(nameof(LectureMaterial))]
    public class LectureMaterial : BaseModel
    {
        // Nullable — content can exist in Central Library without a course
        public int? CourseId { get; set; }   

        [ForeignKey(nameof(CourseId))]
        public virtual CourseMaster? Course { get; set; }  

        public int? CourseQuadrantId { get; set; }

        [ForeignKey(nameof(CourseQuadrantId))]
        public virtual CourseQuadrantMaster? CourseQuadrant { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = default!;

        [Required, StringLength(50)]
        public string MaterialType { get; set; } = "Document"; // "Video", "Document", "VideoURL"

        [Required]
        public string FilePath { get; set; } = default!;

        [NotMapped]
        public string? FileHash { get; set; }

        public DateTime UploadedOn { get; set; } = DateTime.UtcNow;

        // ─── Cloud Drive Integration (NotMapped for LMS-SilverOak compatibility) ───
        [NotMapped]
        [StringLength(20)]
        public string Source { get; set; } = "Local"; // "Local", "GoogleDrive", "OneDrive"

        [NotMapped]
        [StringLength(500)]
        public string? ExternalFileId { get; set; }  // Cloud file ID (Google/OneDrive)

        [NotMapped]
        [StringLength(100)]
        public string? MimeType { get; set; }

        [NotMapped]
        [StringLength(500)]
        public string? OriginalFileName { get; set; }

        [NotMapped]
        public long? FileSizeBytes { get; set; }
    }
}
