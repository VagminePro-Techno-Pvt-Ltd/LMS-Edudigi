using System;
using System.Collections.Generic;

namespace TMS.ViewModels.LearningAssistant
{
    public class LearningAssistantRequest
    {
        public string Message { get; set; } = string.Empty;
        public int? CourseId { get; set; }
        public int? UnitId { get; set; }
        public int? QuadrantId { get; set; }
        public int? MaterialId { get; set; }
        public string? PageUrl { get; set; }
        public string? PageTitle { get; set; }
        public List<LearningAssistantChatMessage>? ConversationHistory { get; set; }
    }

    public class LearningAssistantChatMessage
    {
        public string Role { get; set; } = "user"; // "user" or "model"
        public string Text { get; set; } = string.Empty;
    }

    public enum CompanionIntent
    {
        Tutoring,
        Attendance,
        Grades,
        EnrolledCourses,
        Assignments,
        Schedule,
        Profile
    }

    public class LearningAssistantResponse
    {
        public bool Success { get; set; }
        public string Reply { get; set; } = string.Empty;
        public CompanionIntent Intent { get; set; } = CompanionIntent.Tutoring;
        public bool IsDatabaseGrounded { get; set; }
        public LearningAssistantContextDto? DetectedContext { get; set; }
        public List<LearningAssistantActionDto>? SuggestedActions { get; set; }
        public List<string>? Sources { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsCached { get; set; }
        public int? TokensSaved { get; set; }
    }

    public class LearningAssistantMetricsDto
    {
        public long TotalQueries { get; set; }
        public long CacheHits { get; set; }
        public long CacheMisses { get; set; }
        public long GeminiApiCalls { get; set; }
        public long TotalTokensSaved { get; set; }
        public double CacheHitRatio => TotalQueries > 0 ? Math.Round((double)CacheHits / TotalQueries * 100, 2) : 0;
        public decimal EstimatedCostSavedUsd => Math.Round((decimal)TotalTokensSaved / 1_000_000m * 0.15m, 4);
    }

    public class LearningAssistantContextDto
    {
        public int? CourseId { get; set; }
        public string? CourseName { get; set; }
        public string? CourseCode { get; set; }
        public int? UnitId { get; set; }
        public string? UnitName { get; set; }
        public string? MaterialTitle { get; set; }
        public string? PageName { get; set; }
        public string? Role { get; set; }
        public int? ProgressPercentage { get; set; }
    }

    public class LearningAssistantActionDto
    {
        public string Label { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public string? Icon { get; set; }
    }

    public class EnrolledCourseSummaryDto
    {
        public int CourseId { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public int? SemesterId { get; set; }
    }

    public class StudentAttendanceSummaryDto
    {
        public int StudentId { get; set; }
        public int TotalClasses { get; set; }
        public int AttendedClasses { get; set; }
        public int AbsentClasses { get; set; }
        public int LateClasses { get; set; }
        public int ExcusedClasses { get; set; }
        public double? AttendancePercentage { get; set; }
        public int DailyLoginSessions { get; set; }
        public int ClassroomSessions { get; set; }
        public string? CourseName { get; set; }
        public bool HasRecords => TotalClasses > 0;
        public string SummaryText { get; set; } = string.Empty;
    }

    public class StudentGradesSummaryDto
    {
        public int StudentId { get; set; }
        public List<StudentCourseGradeItemDto> CourseGrades { get; set; } = new();
        public int TotalEvaluated { get; set; }
        public int TotalPassed { get; set; }
        public int TotalPending { get; set; }
        public bool HasRecords => CourseGrades.Any();
        public string SummaryText { get; set; } = string.Empty;
    }

    public class StudentCourseGradeItemDto
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public string AssessmentTitle { get; set; } = string.Empty;
        public decimal? MarksObtained { get; set; }
        public decimal MaxMarks { get; set; }
        public string ResultStatus { get; set; } = "PENDING"; // PASS, FAIL, PENDING
        public DateTime? EvaluatedOn { get; set; }
    }

    public class StudentAssignmentsSummaryDto
    {
        public int StudentId { get; set; }
        public List<StudentAssignmentItemDto> PendingAssignments { get; set; } = new();
        public List<StudentAssignmentItemDto> SubmittedAssignments { get; set; } = new();
        public bool HasRecords => PendingAssignments.Any() || SubmittedAssignments.Any();
        public string SummaryText { get; set; } = string.Empty;
    }

    public class StudentAssignmentItemDto
    {
        public int AssignmentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public decimal MaxMarks { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Submitted, Graded
        public decimal? Marks { get; set; }
        public bool IsOverdue => DueDate < DateTime.Now && Status == "Pending";
    }

    public class StudentScheduleSummaryDto
    {
        public int StudentId { get; set; }
        public List<StudentScheduleItemDto> UpcomingSessions { get; set; } = new();
        public bool HasRecords => UpcomingSessions.Any();
        public string SummaryText { get; set; } = string.Empty;
    }

    public class StudentScheduleItemDto
    {
        public string Title { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public DateTime ScheduledStart { get; set; }
        public DateTime? ScheduledEnd { get; set; }
        public string SessionType { get; set; } = "Live Lecture";
        public string Status { get; set; } = "Scheduled";
    }

    public class StudentProfileSummaryDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ContactNo { get; set; }
        public string? RollNumber { get; set; }
        public string? ProgramName { get; set; }
        public int? OverallProgressPercent { get; set; }
        public string SummaryText { get; set; } = string.Empty;
    }
}
