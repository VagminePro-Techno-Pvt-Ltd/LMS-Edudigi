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

    public class StudentCourseProgressItem
    {
        public int CourseId { get; set; }
        public int TotalMaterials { get; set; }
        public int CompletedMaterials { get; set; }
        public int ProgressPercent { get; set; }
    }

    public class StudentDashboardViewModel
    {
        public string StudentName { get; set; }
        public int TotalCourses { get; set; }
        public int TotalExams { get; set; }
        public int TotalQuizzes { get; set; }
        public int PendingQuizzes { get; set; }
        public int TotalResults { get; set; }
        public double PassingRate { get; set; }

        public List<string> TodayExams { get; set; } = new();
        public List<string> TodayVirtualClasses { get; set; } = new();
        public List<AnnouncementViewModel> RecentAnnouncements { get; set; } = new();
        public List<TMS.ViewModels.Masters.CourseMasterViewModel> EnrolledCourses { get; set; } = new();
        public string LastLoginString { get; set; }

        /// <summary>Recent lecture files + interactive PPTs for enrolled courses (in-browser preview).</summary>
        public List<StudentDashboardDocumentItem> RecentDocuments { get; set; } = new();

        /// <summary>Course ID to progress metrics map</summary>
        public Dictionary<int, StudentCourseProgressItem> CourseProgress { get; set; } = new();
        public int OverallProgressPercent { get; set; }
        public int TotalMaterials { get; set; }
        public int CompletedMaterials { get; set; }

        // Dynamic Student Profile & Stats
        public string RollNumber { get; set; } = "2201640130001";
        public string Section { get; set; } = "LMS-SEC-A";
        public string LibraryCode { get; set; } = "2211072";
        public string ContactNo { get; set; } = "8604538435";
        public string Email { get; set; } = "";
        public string BirthDate { get; set; } = "21-07-2005";
        public string Address { get; set; } = "80/A BARRA-6 KANPUR-208027";
        public string ProgramName { get; set; } = "EduDigi-STUDENT-IV-A";

        // Attendance stats
        public double AttendancePercentage { get; set; } = 100.0;
        public int AttendanceTotal { get; set; } = 0;
        public int AttendancePresent { get; set; } = 0;
        public int AttendanceAbsent { get; set; } = 0;
    }
}
