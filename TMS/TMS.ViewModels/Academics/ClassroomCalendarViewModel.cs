using System;
using System.Collections.Generic;
using TMS.ViewModels.Masters;

namespace TMS.ViewModels.Academics
{
    /// <summary>
    /// Page-level ViewModel for the Classroom Calendar views (Student, Faculty, Admin).
    /// </summary>
    public class ClassroomCalendarViewModel
    {
        // ─── Upcoming & Live Sessions ───
        public List<ClassInstanceViewModel> UpcomingSessions { get; set; } = new();
        public List<ClassInstanceViewModel> LiveSessions { get; set; } = new();
        public List<ClassInstanceViewModel> PastSessions { get; set; } = new();
        public List<ClassInstanceViewModel> AllSessions { get; set; } = new();

        // ─── Series (Faculty/Admin) ───
        public List<ClassSeriesViewModel> MySeries { get; set; } = new();

        // ─── Filters ───
        public List<CourseMasterViewModel> Courses { get; set; } = new();
        public List<SubjectMasterViewModel> Subjects { get; set; } = new();

        // ─── Current User Info ───
        public string UserRole { get; set; } = "Student";
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;

        // ─── Stats ───
        public int TotalUpcoming { get; set; }
        public int TotalCompleted { get; set; }
        public int TotalCancelled { get; set; }
        public double AverageAttendancePercent { get; set; }

        // ─── Next Class (for Dashboard "Join Now" widget) ───
        public ClassInstanceViewModel? NextClass { get; set; }
        public bool HasLiveClass => LiveSessions.Count > 0;
    }
}
