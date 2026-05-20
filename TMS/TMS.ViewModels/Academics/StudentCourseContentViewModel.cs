using System.Collections.Generic;
using TMS.ViewModels.Masters;

namespace TMS.ViewModels.Academics
{
    public class StudentCourseContentViewModel:BaseViewModel
    {
        public int CourseId { get; set; }
        public string? CourseName { get; set; }

        public List<QuadrantContentViewModel> Quadrants { get; set; } = new();
    }

    public class QuadrantContentViewModel:BaseViewModel
    {
        public int QuadrantId { get; set; }
        public string QuadrantName { get; set; } = string.Empty;

        public List<LectureMaterialViewModel> Materials { get; set; } = new();
        public bool IsCompleted { get; set; }

        public QuizMasterViewModel? Quiz { get; set; }
    }
}
