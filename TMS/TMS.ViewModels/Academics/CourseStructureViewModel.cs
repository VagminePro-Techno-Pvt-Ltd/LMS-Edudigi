using System.Collections.Generic;
using TMS.ViewModels.Masters;

namespace TMS.ViewModels.Academics
{
    // ═══════════════════════════════════════════════════
    //  LEGACY: Kept for backward compatibility
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// Full hierarchy view for the Course Structure Designer UI.
    /// Course > Semesters > Units > Subjects
    /// </summary>
    public class CourseStructureViewModel
    {
        public CourseMasterViewModel Course { get; set; } = new();
        public List<SemesterNodeViewModel> Semesters { get; set; } = new();
    }

    public class SemesterNodeViewModel
    {
        public int SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        public List<SubjectNodeViewModel> Subjects { get; set; } = new();
    }

    public class SubjectNodeViewModel
    {
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string? SubjectCode { get; set; }
        public int SortOrder { get; set; }
        public List<UnitNodeViewModel> Units { get; set; } = new();
    }

    public class UnitNodeViewModel
    {
        public int UnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public string? UnitDescription { get; set; }
        public int SortOrder { get; set; }
        public int ContentCount { get; set; }
    }

    /// <summary>
    /// Page-level ViewModel for the Course Structure Designer (Legacy)
    /// </summary>
    public class CourseStructurePageViewModel
    {
        public List<CourseGroupViewModel> CourseGroups { get; set; } = new();
        public List<CourseCategoryMasterViewModel> Categories { get; set; } = new();
        public List<SemesterMasterViewModel> AllSemesters { get; set; } = new();
        public int? SelectedCourseId { get; set; }
        public int SelectedCategoryId { get; set; }
        public CourseStructureViewModel? SelectedCourseStructure { get; set; }
    }

    // ═══════════════════════════════════════════════════
    //  NEW: Program → Semester → Course → Content
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// Page-level ViewModel for the new Curriculum Builder
    /// </summary>
    public class CurriculumBuilderPageViewModel
    {
        public List<CourseCategoryMasterViewModel> Programs { get; set; } = new();
        public int SelectedProgramId { get; set; }
        public ProgramStructureViewModel? SelectedProgramStructure { get; set; }
        public List<CourseQuadrantViewModel> AllQuadrants { get; set; } = new();
        public List<CourseMasterViewModel> AllCourses { get; set; } = new();
        public List<SemesterMasterViewModel> AllSemesters { get; set; } = new();
        public string ActiveView { get; set; } = "tree";
    }

    /// <summary>
    /// Program → Semester Groups → Courses
    /// </summary>
    public class ProgramStructureViewModel
    {
        public int ProgramId { get; set; }
        public string ProgramName { get; set; } = string.Empty;
        public string? ProgramDescription { get; set; }
        public int NoOfSemester { get; set; }
        public List<SemesterCourseGroupNode> SemesterGroups { get; set; } = new();
    }

    /// <summary>
    /// A semester containing its courses
    /// </summary>
    public class SemesterCourseGroupNode
    {
        public int SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        public List<CourseNode> Courses { get; set; } = new();
    }

    /// <summary>
    /// An individual course within a semester
    /// </summary>
    public class CourseNode
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public int Credits { get; set; }
        public int DurationMonths { get; set; }
        public string? Description { get; set; }
        public int? SemesterId { get; set; }
        public int ContentCount { get; set; }
    }

    /// <summary>
    /// Course creation & structure definition model with Modules and Topics
    /// </summary>
    public class CourseFullStructureDto
    {
        public int CourseId { get; set; }
        public int ProgramId { get; set; }
        public int SemesterId { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Credits { get; set; } = 4;
        public int DurationMonths { get; set; } = 6;
        public List<ModuleStructureDto> Modules { get; set; } = new();
    }

    public class ModuleStructureDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public List<TopicStructureDto> Topics { get; set; } = new();
    }

    public class TopicStructureDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }
}
