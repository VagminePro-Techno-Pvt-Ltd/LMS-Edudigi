using System.Collections.Generic;
using TMS.ViewModels; // adjust if CourseMeetingViewModel lives in another namespace
// using TMS.ViewModels.Academics; // uncomment/adjust if needed

namespace TMS.ViewModels.Academics
{
    public class VirtualClassroomViewModel
    {
        public List<CourseMeetingViewModel> UpcomingSessions { get; set; } = new();
        public List<CourseMeetingViewModel> PastSessions { get; set; } = new();
        public string? SearchTerm { get; set; }
        public bool IsFaculty { get; set; }
        public List<TMS.ViewModels.Masters.CourseMasterViewModel> AvailableCourses { get; set; } = new();
        public List<TMS.ViewModels.Masters.CourseQuadrantViewModel> AvailableQuadrants { get; set; } = new();
        public CourseMeetingViewModel CreateModel { get; set; } = new();
    }
}
