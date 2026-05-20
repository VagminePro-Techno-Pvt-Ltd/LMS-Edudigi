namespace TMS.Web.Models.ViewModels
{
    public class CourseCategoryStatViewModel
    {
        public string CategoryName { get; set; }
        public int TotalCourses { get; set; }
        public int TotalStudents { get; set; }
    }

    public class DashboardViewModel
    {
        // Admin & General Stats
        public int TotalCourses { get; set; }
        public int TotalMaterials { get; set; }
        public int TotalStudents { get; set; }
        public int TotalFaculty { get; set; }
        public List<CourseCategoryStatViewModel> CourseCategoryStats { get; set; }
        
        // Faculty Specific Stats
        public string FacultyName { get; set; }
        public int MyCourses { get; set; }
        public int MyStudents { get; set; }
        public int PendingAssessments { get; set; }
        public List<string> RecentActivities { get; set; }
        public bool IsFaculty { get; set; }
        public bool IsAdmin { get; set; }
        public string LastLoginString { get; set; }
    }
}
