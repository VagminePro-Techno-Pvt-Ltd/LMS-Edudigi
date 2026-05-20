using System.Collections.Generic;
using TMS.ViewModels.Masters;

namespace TMS.ViewModels.Academics
{
    // ══════════════════════════════════════════════════════
    //  4-QUADRANT LMS FLOW VIEW MODELS
    //  Student:  Semester → Subject → Quadrant → Unit → Content
    //  Faculty:  Course → Semester → Subject → Quadrant → Unit → Content Upload
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Page-level VM for the Subject listing page (Step 3 in student flow).
    /// Student selects a subject after picking a semester.
    /// </summary>
    public class SubjectListPageViewModel
    {
        public int SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public List<SubjectSummaryVM> Subjects { get; set; } = new();
    }

    public class SubjectSummaryVM
    {
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string? SubjectCode { get; set; }
        public int SortOrder { get; set; }
        public int TotalUnits { get; set; }
        public int TotalContent { get; set; }
    }

    // ══════════════════════════════════════════════════════
    //  Quadrant Page — 4 quadrant cards for a subject
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Page-level VM for the 4-Quadrant selection page.
    /// Shows 4 quadrant cards after student selects a subject.
    /// </summary>
    public class SubjectQuadrantPageViewModel
    {
        public int SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string? SubjectCode { get; set; }
        public List<QuadrantCardVM> Quadrants { get; set; } = new();
    }

    public class QuadrantCardVM
    {
        public int QuadrantId { get; set; }
        public int QuadrantNumber { get; set; }    // 1-4
        public string QuadrantName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "fa-folder";
        public string ColorClass { get; set; } = "#6366f1";
        public string BgClass { get; set; } = "#eff6ff";
        public int UnitCount { get; set; }
        public int ContentCount { get; set; }
    }

    // ══════════════════════════════════════════════════════
    //  Unit List Page — units inside a subject+quadrant
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Page-level VM for the Unit listing inside a Subject+Quadrant.
    /// </summary>
    public class QuadrantUnitPageViewModel
    {
        public int SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public int QuadrantId { get; set; }
        public int QuadrantNumber { get; set; }
        public string QuadrantName { get; set; } = string.Empty;
        public string QuadrantDescription { get; set; } = string.Empty;
        public string QuadrantIcon { get; set; } = "fa-folder";
        public List<UnitContentGroupVM> Units { get; set; } = new();
    }

    public class UnitContentGroupVM
    {
        public int UnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public string? UnitDescription { get; set; }
        public int SortOrder { get; set; }
        public int ContentCount { get; set; }
        public List<ContentItemVM> Contents { get; set; } = new();
    }

    public class ContentItemVM
    {
        public int MaterialId { get; set; }
        public int MappingId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string MaterialType { get; set; } = "Document";   // "Video", "Document", "VideoURL"
        public string? FilePath { get; set; }
        public string? ContentPath { get; set; }    // For VideoURL
        public string Source { get; set; } = "Local";
        public int SortOrder { get; set; }
    }

    // ══════════════════════════════════════════════════════
    //  Admin/Faculty Upload — extended upload VM
    // ══════════════════════════════════════════════════════

    /// <summary>
    /// Extended data needed when rendering the LectureMaterial upload form
    /// with Subject and Unit dropdowns (Admin/Faculty flow).
    /// </summary>
    public class QuadrantUploadContextVM
    {
        public int SelectedSubjectId { get; set; }
        public int SelectedUnitId { get; set; }
        public int SelectedQuadrantId { get; set; }
    }

    // ══════════════════════════════════════════════════════
    //  NEW UNIT-FIRST HIERARCHY MODELS
    // ══════════════════════════════════════════════════════

    public class SubjectUnitsPageViewModel
    {
        public int SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string? SubjectCode { get; set; }
        public List<UnitSummaryVM> Units { get; set; } = new();
    }

    public class UnitSummaryVM
    {
        public int UnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SortOrder { get; set; }
        public int TotalContent { get; set; }
    }

    public class UnitQuadrantPageViewModel
    {
        public int SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public int UnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public List<QuadrantCardVM> Quadrants { get; set; } = new();
    }

    public class UnitContentPageViewModel
    {
        public int SemesterId { get; set; }
        public string? SemesterName { get; set; }
        public int CourseId { get; set; }
        public string? CourseName { get; set; }
        public int SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public int UnitId { get; set; }
        public string? UnitName { get; set; }
        public int QuadrantId { get; set; }
        public int QuadrantNumber { get; set; }
        public string? QuadrantName { get; set; }
        public string? QuadrantIcon { get; set; }
        public string? EmptyMessage { get; set; }
        public List<ContentItemVM> Contents { get; set; } = new();
    }
}
