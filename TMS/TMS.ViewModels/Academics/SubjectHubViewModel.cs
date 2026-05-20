using System.Collections.Generic;
using TMS.ViewModels.Masters;

namespace TMS.ViewModels.Academics
{
    // ══════════════════════════════════════════════════════
    //  SUBJECT HUB — Unit-First Hierarchy
    //  Subject → Unit Grid (with progress)
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Top-level ViewModel for the Subject Hub page.
    /// Displays course info + a grid of units with progress.
    /// </summary>
    public class SubjectHubViewModel
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string? CourseCode { get; set; }
        public string? CourseDescription { get; set; }
        public string CategoryName { get; set; } = "General";
        public int Credits { get; set; }
        public int? DurationMonths { get; set; }
        public int SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;

        public List<UnitCardViewModel> Units { get; set; } = new();

        // Summary stats
        public int TotalUnits => Units.Count;
        public int TotalMaterials => Units.Count > 0 ? Units.Sum(u => u.TotalMaterials) : 0;
        public int OverallProgressPercent
        {
            get
            {
                if (TotalMaterials == 0) return 0;
                int completed = Units.Sum(u => u.CompletedMaterials);
                return (int)Math.Round((double)completed / TotalMaterials * 100);
            }
        }
    }

    /// <summary>
    /// Represents a single Unit card on the Subject Hub grid.
    /// </summary>
    public class UnitCardViewModel
    {
        public int UnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public string? UnitDescription { get; set; }
        public int SortOrder { get; set; }
        public int TotalMaterials { get; set; }
        public int CompletedMaterials { get; set; }

        public int ProgressPercent
        {
            get
            {
                if (TotalMaterials == 0) return 0;
                return (int)Math.Round((double)CompletedMaterials / TotalMaterials * 100);
            }
        }
    }

    // ══════════════════════════════════════════════════════
    //  UNIT DETAILS — 4 Quadrant Cards
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// ViewModel for the Unit Details page.
    /// Shows unit info + exactly 4 quadrant cards.
    /// </summary>
    public class UnitHubViewModel
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string? CourseCode { get; set; }
        public int SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;

        public int UnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public string? UnitDescription { get; set; }

        public List<QuadrantCardSummaryViewModel> Quadrants { get; set; } = new();

        // Progress
        public int TotalMaterials => Quadrants.Sum(q => q.MaterialCount);
        public int CompletedMaterials => Quadrants.Sum(q => q.CompletedCount);
        public int ProgressPercent
        {
            get
            {
                if (TotalMaterials == 0) return 0;
                return (int)Math.Round((double)CompletedMaterials / TotalMaterials * 100);
            }
        }
    }

    /// <summary>
    /// Summary of a single quadrant within a unit.
    /// </summary>
    public class QuadrantCardSummaryViewModel
    {
        public int QuadrantId { get; set; }
        public int QuadrantNumber { get; set; }
        public string QuadrantName { get; set; } = string.Empty;
        public string Icon { get; set; } = "fa-folder";
        public string Color { get; set; } = "#6366f1";
        public string BgColor { get; set; } = "#eff6ff";
        public string Description { get; set; } = string.Empty;
        public int MaterialCount { get; set; }
        public int CompletedCount { get; set; }
        public bool HasQuizAttempt { get; set; }
    }

    // ══════════════════════════════════════════════════════
    //  QUADRANT CONTENT — Material List
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// ViewModel for the Quadrant Content page.
    /// Shows a filtered list of materials for a specific unit + quadrant.
    /// </summary>
    public class QuadrantMaterialListViewModel
    {
        // Breadcrumb context
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public int SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        public int UnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;

        // Quadrant info
        public int QuadrantId { get; set; }
        public int QuadrantNumber { get; set; }
        public string QuadrantName { get; set; } = string.Empty;
        public string QuadrantIcon { get; set; } = "fa-folder";
        public string QuadrantColor { get; set; } = "#6366f1";
        public string QuadrantBgColor { get; set; } = "#eff6ff";
        public string QuadrantDescription { get; set; } = string.Empty;

        public List<MaterialItemViewModel> Materials { get; set; } = new();

        public int TotalItems => Materials.Count;
    }

    /// <summary>
    /// Represents a single material item in the Quadrant Content list.
    /// </summary>
    public class MaterialItemViewModel
    {
        public int MaterialId { get; set; }
        public int MappingId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string MaterialType { get; set; } = "Document";
        public string? FilePath { get; set; }
        public string? ContentPath { get; set; }
        public string? OriginalFileName { get; set; }
        public string Source { get; set; } = "Local";
        public long? FileSizeBytes { get; set; }
        public int SortOrder { get; set; }
        public DateTime UploadedOn { get; set; }
        public bool IsCompleted { get; set; }

        // Computed helpers
        public string FileExtension => !string.IsNullOrEmpty(FilePath)
            ? System.IO.Path.GetExtension(FilePath).ToLowerInvariant()
            : string.Empty;

        public bool IsVideo => MaterialType == "Video" || MaterialType == "VideoURL"
            || new[] { ".mp4", ".webm", ".ogg", ".avi", ".mov" }.Contains(FileExtension)
            || (!string.IsNullOrEmpty(FilePath) && (FilePath.Contains("youtu") || FilePath.Contains("vimeo")));

        public bool IsDocument => MaterialType == "Document"
            || new[] { ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx", ".txt" }.Contains(FileExtension);
    }
}
