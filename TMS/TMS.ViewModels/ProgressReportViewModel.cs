using System.Collections.Generic;

namespace TMS.ViewModels
{
    public class ProgressReportViewModel
    {
        public string StudentName { get; set; } = "";
        public string ReportType { get; set; } = "all"; // all, quiz, assignment, study-material, e-content
        
        // Overall Progress
        public decimal OverallProgress { get; set; }
        
        // Quiz Progress
        public QuizProgressData QuizProgress { get; set; } = new QuizProgressData();
        
        // Assignment Progress
        public AssignmentProgressData AssignmentProgress { get; set; } = new AssignmentProgressData();
        
        // Study Material Progress
        public StudyMaterialProgressData StudyMaterialProgress { get; set; } = new StudyMaterialProgressData();
        
        // E-Content Progress
        public EContentProgressData EContentProgress { get; set; } = new EContentProgressData();
    }
    
    public class QuizProgressData
    {
        public int TotalQuizzes { get; set; }
        public int AttemptedQuizzes { get; set; }
        public int PendingQuizzes { get; set; }
        public decimal CompletionPercentage { get; set; }
        public List<UnitProgress> UnitWiseProgress { get; set; } = new List<UnitProgress>();
    }
    
    public class AssignmentProgressData
    {
        public int TotalAssignments { get; set; }
        public int SubmittedAssignments { get; set; }
        public int PendingAssignments { get; set; }
        public decimal CompletionPercentage { get; set; }
        public List<UnitProgress> UnitWiseProgress { get; set; } = new List<UnitProgress>();
    }
    
    public class StudyMaterialProgressData
    {
        public int TotalMaterials { get; set; }
        public int ReadMaterials { get; set; }
        public int PendingMaterials { get; set; }
        public decimal CompletionPercentage { get; set; }
        public List<UnitProgress> UnitWiseProgress { get; set; } = new List<UnitProgress>();
    }
    
    public class EContentProgressData
    {
        public int TotalContent { get; set; }
        public int CompletedContent { get; set; }
        public int PendingContent { get; set; }
        public decimal CompletionPercentage { get; set; }
        public List<UnitProgress> UnitWiseProgress { get; set; } = new List<UnitProgress>();
        
        // Individual video-by-video breakdown
        public List<VideoProgressItem> VideoItems { get; set; } = new List<VideoProgressItem>();
    }
    
    public class VideoProgressItem
    {
        public string VideoTitle { get; set; } = "";
        public string CourseName { get; set; } = "";
        public decimal WatchedPercentage { get; set; }
        public int WatchedSeconds { get; set; }
        public int DurationSeconds { get; set; }
        public bool IsCompleted { get; set; }
        public System.DateTime? LastWatchedAt { get; set; }
    }
    
    public class UnitProgress
    {
        public string UnitName { get; set; } = "";
        public int Total { get; set; }
        public int Completed { get; set; }
        public decimal Percentage { get; set; }
    }
}
