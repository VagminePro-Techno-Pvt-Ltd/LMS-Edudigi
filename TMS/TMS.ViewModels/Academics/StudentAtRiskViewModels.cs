using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TMS.ViewModels.Academics
{
    public class CourseActivityItemViewModel
    {
        public string ActivityType { get; set; } = default!; // "Video", "Pdf", "Assignment", "Quiz", "Discussion"
        public int ActivityId { get; set; }
        public string Title { get; set; } = default!;
        public string UnitOrQuadrant { get; set; } = "General";
        public DateTime? DeadlineDate { get; set; }
        public int ReminderDaysBefore { get; set; } = 2;
        public bool HasExistingDeadline { get; set; }
        public string RemainingText { get; set; } = string.Empty;
        public bool IsOverdue { get; set; }
        public int TotalEnrolled { get; set; }
        public int CompletedCount { get; set; }
        public int PendingCount { get; set; }
        public int OverdueCount { get; set; }
    }

    public class FacultyCourseActivitiesViewModel
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = default!;
        public string? CourseCode { get; set; }
        public List<CourseActivityItemViewModel> Activities { get; set; } = new();
        public string SelectedActivityType { get; set; } = "All";
    }

    public class FacultyStudentRiskItemViewModel
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = default!;
        public string StudentEmail { get; set; } = default!;
        public string RiskLevel { get; set; } = "LOW"; // "LOW", "MEDIUM", "HIGH", "CRITICAL"
        public int PendingCount { get; set; }
        public int OverdueCount { get; set; }
        public int CompletedCount { get; set; }
        public int TotalActivities { get; set; }
    }

    public class FacultyStudentRiskRosterViewModel
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = default!;
        public string? CourseCode { get; set; }
        public List<(int Id, string Name, string Code)> FacultyCourses { get; set; } = new();
        public List<FacultyStudentRiskItemViewModel> Students { get; set; } = new();
        public List<CourseActivityItemViewModel> Activities { get; set; } = new();
        public string RiskFilter { get; set; } = "All";
        public string? SearchTerm { get; set; }
        public int CriticalCount { get; set; }
        public int HighCount { get; set; }
        public int MediumCount { get; set; }
        public int LowCount { get; set; }
    }

    public class StudentRiskActivityDetailViewModel
    {
        public string ActivityType { get; set; } = default!;
        public int ActivityId { get; set; }
        public string Title { get; set; } = default!;
        public DateTime? DeadlineDate { get; set; }
        public string RemainingText { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // "Completed", "Approaching", "Overdue", "OnTrack"
        public string? ActionUrl { get; set; }
    }

    public class StudentRiskDetailViewModel
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = default!;
        public string StudentEmail { get; set; } = default!;
        public int CourseId { get; set; }
        public string CourseName { get; set; } = default!;
        public string RiskLevel { get; set; } = "LOW";
        public List<StudentRiskActivityDetailViewModel> Activities { get; set; } = new();
    }

    public class StudentAlertItemViewModel
    {
        public int AlertId { get; set; }
        public int CourseId { get; set; }
        public string CourseName { get; set; } = default!;
        public string ActivityType { get; set; } = default!;
        public int ActivityId { get; set; }
        public string Title { get; set; } = default!;
        public string Message { get; set; } = default!;
        public string RiskLevel { get; set; } = "Low";
        public DateTime? DeadlineDate { get; set; }
        public string RemainingText { get; set; } = string.Empty;
        public bool IsResolved { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? ActionUrl { get; set; }
    }

    public class StudentAlertsPageViewModel
    {
        public List<StudentAlertItemViewModel> Alerts { get; set; } = new();
        public int TotalCount { get; set; }
        public int OverdueCount { get; set; }
        public int ApproachingCount { get; set; }
        public int UnreadCount { get; set; }
        public int WarningCount { get; set; }
        public string Filter { get; set; } = "All";
    }

    public class AdminStudentRiskItemViewModel
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = default!;
        public string StudentEmail { get; set; } = default!;
        public string ProgramName { get; set; } = default!;
        public int CourseId { get; set; }
        public string CourseName { get; set; } = default!;
        public string RiskLevel { get; set; } = "LOW";
        public int PendingCount { get; set; }
        public int OverdueCount { get; set; }
        public int CompletedCount { get; set; }
    }

    public class ModerationFlagItemViewModel
    {
        public int Id { get; set; }
        public string ContentType { get; set; } = default!;
        public int ContentId { get; set; }
        public int AuthorUserId { get; set; }
        public string AuthorName { get; set; } = default!;
        public string? ProgramName { get; set; }
        public string? CourseName { get; set; }
        public string FlaggedText { get; set; } = default!;
        public string DetectedKeywords { get; set; } = default!;
        public string Status { get; set; } = "PendingReview";
        public DateTime CreatedOn { get; set; }
        public string? ReviewedByName { get; set; }
        public DateTime? ReviewedOn { get; set; }
        public string? ReviewRemarks { get; set; }
    }

    public class AdminAtRiskAnalyticsViewModel
    {
        public int TotalStudents { get; set; }
        public int CriticalCount { get; set; }
        public int HighCount { get; set; }
        public int MediumCount { get; set; }
        public int LowCount { get; set; }
        public int TotalPendingActivities { get; set; }
        public int TotalOverdueActivities { get; set; }
        public int TotalCompletedActivities { get; set; }
        public int PendingModerationCount { get; set; }

        public List<(int Id, string Name)> Programs { get; set; } = new();
        public List<(int Id, string Name, int ProgramId)> Courses { get; set; } = new();
        public int? SelectedProgramId { get; set; }
        public int? SelectedCourseId { get; set; }
        public string SelectedRiskFilter { get; set; } = "All";
        public string? SearchTerm { get; set; }

        public List<AdminStudentRiskItemViewModel> Students { get; set; } = new();
        public List<ModerationFlagItemViewModel> ModerationFlags { get; set; } = new();
    }

    public class SaveDeadlineRequest
    {
        [Required]
        public int CourseId { get; set; }

        [Required]
        public string ActivityType { get; set; } = default!;

        [Required]
        public int ActivityId { get; set; }

        [Required]
        public string ActivityTitle { get; set; } = default!;

        [Required]
        public DateTime DeadlineDate { get; set; }

        public int ReminderDaysBefore { get; set; } = 2;
    }

    public class ReviewModerationRequest
    {
        [Required]
        public int FlagId { get; set; }

        [Required]
        public string Decision { get; set; } = default!; // "Confirm" or "Dismiss"

        public string? Remarks { get; set; }
    }
}
