using System;
using System.ComponentModel.DataAnnotations;

namespace TMS.ViewModels.Academics
{
    /// <summary>
    /// ViewModel for displaying Interactive PPT list in Faculty management page.
    /// </summary>
    public class InteractivePPTListViewModel
    {
        public int Id { get; set; }

        [Display(Name = "PPT Title")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "File Path")]
        public string FilePath { get; set; } = string.Empty;

        [Display(Name = "Original File Name")]
        public string? OriginalFileName { get; set; }

        [Display(Name = "File Size")]
        public long? FileSizeBytes { get; set; }

        // ── Related Entity Names ──
        [Display(Name = "Program")]
        public string ProgramName { get; set; } = string.Empty;

        [Display(Name = "Course")]
        public string CourseName { get; set; } = string.Empty;

        [Display(Name = "Unit")]
        public string UnitName { get; set; } = string.Empty;

        [Display(Name = "Topic")]
        public string TopicName { get; set; } = string.Empty;

        // ── IDs for Edit ──
        public int ProgramId { get; set; }
        public int CourseId { get; set; }
        public int UnitId { get; set; }
        public int TopicId { get; set; }

        // ── Metadata ──
        [Display(Name = "Created Date")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Uploaded By")]
        public int UploadedBy { get; set; }

        [Display(Name = "Status")]
        public bool IsActive { get; set; }

        // ── Computed Properties ──
        public string UploadedDateStr => CreatedAt.ToString("MMM dd, yyyy");
        public string StatusText => IsActive ? "Active" : "Inactive";
        public string StatusBadgeClass => IsActive ? "ppt-status-active" : "ppt-status-inactive";
        public string FileSizeFormatted
        {
            get
            {
                if (!FileSizeBytes.HasValue || FileSizeBytes.Value == 0)
                    return "Unknown";

                var bytes = FileSizeBytes.Value;
                if (bytes < 1024) return $"{bytes} B";
                if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F2} KB";
                return $"{bytes / (1024.0 * 1024.0):F2} MB";
            }
        }
    }
}
