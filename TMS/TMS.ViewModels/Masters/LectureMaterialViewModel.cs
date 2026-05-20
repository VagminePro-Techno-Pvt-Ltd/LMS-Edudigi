using TMS;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Common;

namespace TMS.ViewModels.Masters
{
    public class LectureMaterialViewModel : BaseViewModel
    {
        [MapToDTO]
        [Display(Name = "Course")]
        public int? CourseId { get; set; }  // Nullable — central library support

        [MapToDTO, Display(Name = "Quadrant")]
        public int? CourseQuadrantId { get; set; }  // Nullable

        [MapToDTO, Required, Display(Name = "Material Type")]
        public string? MaterialType { get; set; }  // e.g., "Video", "Document", "VideoURL"

        [MapToDTO, Required, Display(Name = "File Path")]
        public string? FilePath { get; set; }

        [MapToDTO, Display(Name = "Title"), MaxLength(200)]
        public string? Title { get; set; }

        [MapToDTO, Display(Name = "Description"), MaxLength(500)]
        public string? Description { get; set; }

        public CourseMasterViewModel? Course { get; set; }
        public CourseQuadrantViewModel? CourseQuadrant { get; set; }

        [Display(Name = "Content URL")]
        public string? ContentPath { get; set; }

        [NotMapped]
        public bool MeetingExists { get; set; }

        [MapToDTO]
        public string? FileHash { get; set; }

        // ─── Cloud Drive Integration ───
        [MapToDTO, Display(Name = "Upload Source")]
        public string Source { get; set; } = "Local"; // "Local", "GoogleDrive", "OneDrive"

        [MapToDTO]
        public string? ExternalFileId { get; set; }

        [MapToDTO]
        public string? MimeType { get; set; }

        [MapToDTO]
        public string? OriginalFileName { get; set; }

        [MapToDTO]
        public long? FileSizeBytes { get; set; }

        // ─── UI-Only Properties ───
        public List<int>? SelectedCourseIds { get; set; }

        [NotMapped]
        public bool AssignToCourse { get; set; } = true; // Toggle for course assignment

        [NotMapped]
        public int? SemesterId { get; set; }
        [NotMapped]
        public int? SubjectId { get; set; }
        [NotMapped]
        public int? UnitId { get; set; }

        [NotMapped]
        public DateTime UploadedOn { get; set; } = DateTime.UtcNow;
    }
}
