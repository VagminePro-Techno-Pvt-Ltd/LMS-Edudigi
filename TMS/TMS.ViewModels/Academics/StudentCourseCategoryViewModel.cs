using System.Collections.Generic;
using TMS.ViewModels.Masters;

namespace TMS.ViewModels.Academics
{
    public class StudentCourseCategoryViewModel
    {
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryDescription { get; set; } = string.Empty;
        public List<SemesterCourseGroupViewModel> Semesters { get; set; } = new();
        public List<CourseQuadrantViewModel> Quadrants { get; set; } = new();
    }

    public class SemesterCourseGroupViewModel
    {
        public int Semester { get; set; }
        public List<CourseSummaryViewModel> Courses { get; set; } = new();
    }

    public class CourseSummaryViewModel
    {
        public int CourseId { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        
        public int CategoryId { get; set; }
    }



}
