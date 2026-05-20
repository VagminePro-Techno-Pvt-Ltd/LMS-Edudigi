using System.Collections.Generic;
using TMS.ViewModels.Masters;

namespace TMS.ViewModels.Academics
{
    //public class CourseMaterialGroupedViewModel
    //{ 
    //    public CourseMasterViewModel Course { get; set; } = new();
    //    public Dictionary<string, List<LectureMaterialViewModel>> GroupedMaterials { get; set; } = new();
    //}
    public class CourseMaterialGroupedViewModel
    {
        public CourseMasterViewModel Course { get; set; } = new();
        public List<QuadrantMaterialGroupViewModel> GroupedMaterials { get; set; } = new();
        public List<InteractivePPTStudentViewModel> InteractivePPTs { get; set; } = new();
    }
    public class QuadrantMaterialGroupViewModel
    {
        public int QuadrantId { get; set; }
        public string QuadrantName { get; set; } = string.Empty;
        public List<LectureMaterialViewModel> Materials { get; set; } = new();
        public bool HasAttemptedQuiz { get; set; }
    }

    // ── Page-level ViewModel for Content Library UI ──
    public class ContentLibraryPageViewModel
    {
        public List<CourseGroupViewModel> CourseGroups { get; set; } = new();
        public List<CourseCategoryMasterViewModel> Categories { get; set; } = new();
        public int? SelectedCourseId { get; set; }
        public int? SelectedCategoryId { get; set; }
        public CourseMaterialGroupedViewModel? SelectedCourse { get; set; }
        public List<CourseQuadrantViewModel> AllQuadrants { get; set; } = new();

        // ─── Global Library ───
        public bool ShowGlobalLibrary { get; set; } = false;
        public int GlobalLibraryCount { get; set; }
    }

    public class CourseGroupViewModel
    {
        public string CourseName { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public List<int> CourseIds { get; set; } = new();
        public int TotalMaterials { get; set; }
    }

    // ── Global Library Item ViewModel ──
    public class GlobalLibraryItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string MaterialType { get; set; } = "Document";
        public string Source { get; set; } = "Local";
        public string? FilePath { get; set; }
        public string? OriginalFileName { get; set; }
        public long? FileSizeBytes { get; set; }
        public DateTime UploadedOn { get; set; }
        public int MappedCourseCount { get; set; }
        public string? EncryptedId { get; set; }

        // Assignment Details
        public List<ContentAssignmentDetailViewModel> Assignments { get; set; } = new();
        public string? AssignmentSummary { get; set; }
    }

    public class ContentAssignmentDetailViewModel
    {
        public int MappingId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public string SemesterName { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string AssignedBy { get; set; } = string.Empty;
        public DateTime AssignedDate { get; set; }
    }

    public class ContentLibraryCourseSummary
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public int MaterialCount { get; set; }
    }
}
