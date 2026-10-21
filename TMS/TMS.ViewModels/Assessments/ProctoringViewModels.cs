using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TMS.ViewModels.Assessments
{
    public class StartProctoringSessionRequestDto
    {
        [Required]
        public int AttemptId { get; set; }
        public bool CameraConsentGiven { get; set; } = true;
        public string VerificationMethod { get; set; } = "SystemProfile"; // SystemProfile, ManualFallback
    }

    public class LogProctoringIncidentRequestDto
    {
        [Required]
        public int SessionId { get; set; }
        public int? AttemptId { get; set; }
        [Required]
        public string EventType { get; set; } = default!; // TAB_SWITCH, WINDOW_BLUR, FULLSCREEN_EXIT, CAMERA_OFF, CONNECTION_INTERRUPTED
        public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical
        public string? Details { get; set; }
        public DateTime? ClientTimestamp { get; set; }
    }

    public class EndProctoringSessionRequestDto
    {
        [Required]
        public int SessionId { get; set; }
    }

    public class ReviewIncidentDto
    {
        [Required]
        public int IncidentId { get; set; }
        [Required]
        public string ReviewNotes { get; set; } = string.Empty;
    }

    public class ReviewSessionDto
    {
        [Required]
        public int SessionId { get; set; }
        [Required]
        public string Decision { get; set; } = "Reviewed"; // Reviewed, Flagged
        public string? Remarks { get; set; }
    }

    public class ProctoringSessionSummaryDto
    {
        public int SessionId { get; set; }
        public int AttemptId { get; set; }
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = string.Empty;
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public bool CameraConsentGiven { get; set; }
        public bool IdentityVerified { get; set; }
        public string VerificationMethod { get; set; } = string.Empty;
        public int TotalViolationsCount { get; set; }
        public string Status { get; set; } = "Active";
        public string? ReviewerRemarks { get; set; }
        public List<ProctoringIncidentItemDto> Incidents { get; set; } = new();
    }

    public class ProctoringIncidentItemDto
    {
        public int IncidentId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Severity { get; set; } = "Medium";
        public DateTime ServerTimestamp { get; set; }
        public string? Details { get; set; }
        public bool IsReviewed { get; set; }
        public string? ReviewNotes { get; set; }
    }
}
