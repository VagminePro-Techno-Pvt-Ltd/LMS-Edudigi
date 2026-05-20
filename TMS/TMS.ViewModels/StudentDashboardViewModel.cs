using System.Collections.Generic;
using TMS.ViewModels.Academics;

namespace TMS.ViewModels
{
    /// <summary>Row for “view in browser” on the student dashboard (course files + interactive decks).</summary>
    public class StudentDashboardDocumentItem
    {
        /// <summary>"Lecture" (mapped material) or "InteractivePpt".</summary>
        public string DocKind { get; set; } = "Lecture";
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string MaterialType { get; set; } = "Document";
        public string Source { get; set; } = "Local";
    }

    public class StudentDashboardViewModel
    {
        public string StudentName { get; set; }
        public int TotalCourses { get; set; }
        public int TotalExams { get; set; }
        public int TotalResults { get; set; }
        public double PassingRate { get; set; }

        public List<string> TodayExams { get; set; } = new();
        public List<string> TodayVirtualClasses { get; set; } = new();
        public List<AnnouncementViewModel> RecentAnnouncements { get; set; } = new();
        public List<TMS.ViewModels.Masters.CourseMasterViewModel> EnrolledCourses { get; set; } = new();
        public string LastLoginString { get; set; }

        /// <summary>Recent lecture files + interactive PPTs for enrolled courses (in-browser preview).</summary>
        public List<StudentDashboardDocumentItem> RecentDocuments { get; set; } = new();
    }
}
