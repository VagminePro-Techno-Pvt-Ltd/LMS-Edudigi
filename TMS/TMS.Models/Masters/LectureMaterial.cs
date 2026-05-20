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

        public string? FileHash { get; set; }

        public DateTime UploadedOn { get; set; } = DateTime.UtcNow;

        // ─── Cloud Drive Integration ───
        [StringLength(20)]
        public string Source { get; set; } = "Local"; // "Local", "GoogleDrive", "OneDrive"

        [StringLength(500)]
        public string? ExternalFileId { get; set; }  // Cloud file ID (Google/OneDrive)

        [StringLength(100)]
        public string? MimeType { get; set; }

        [StringLength(500)]
        public string? OriginalFileName { get; set; }

        public long? FileSizeBytes { get; set; }
    }
}
