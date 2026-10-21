using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TMS.ViewModels.Academics
{
    public class ResultCalculationSummaryDto
    {
        public int AssessmentId { get; set; }
        public string AssessmentTitle { get; set; } = string.Empty;
        public string AssessmentType { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public decimal MaximumMarks { get; set; }
        public int TotalEnrolled { get; set; }
        public int SubmittedCount { get; set; }
        public int EvaluatedCount { get; set; }
        public int AbsentCount { get; set; }
        public int PendingCount { get; set; }
        public int PassCount { get; set; }
        public int FailCount { get; set; }
        public int CompartmentEligibleCount { get; set; }
        public decimal AveragePercentage { get; set; }
        public bool IsCalculated { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? CalculatedOn { get; set; }
        public DateTime? PublishedOn { get; set; }
        public string? PublishedByName { get; set; }
        public bool IsCompartmental { get; set; }
        public int AttemptNumber { get; set; } = 1;
        public List<StudentProcessedResultDto> StudentResults { get; set; } = new();
    }

    public class StudentProcessedResultDto
    {
        public int ResultId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int AttemptNumber { get; set; } = 1;
        public decimal MaximumMarks { get; set; }
        public decimal? ObtainedMarks { get; set; }
        public decimal Percentage { get; set; }
        public decimal GraceMarksAwarded { get; set; } = 0;
        public bool IsGraceApplied { get; set; } = false;
        public string? AuditRemark { get; set; }
        public string Grade { get; set; } = "F";
        public decimal GradePoint { get; set; }
        public string Status { get; set; } = "PENDING"; // PASS, FAIL, ABSENT, COMPARTMENT_ELIGIBLE, PENDING
        public bool IsPublished { get; set; }
        public DateTime? EvaluatedOn { get; set; }
        public string SubmissionStatus { get; set; } = "NOT SUBMITTED";
    }

    public class CalculateResultsRequestDto
    {
        [Required]
        public int AssessmentId { get; set; }
        public int? PolicyId { get; set; }
    }

    public class CalculateExamResultsRequestDto
    {
        [Required]
        public int ExamId { get; set; }
        public int? PolicyId { get; set; }
    }

    public class ApplyGraceMarksRequestDto
    {
        [Required]
        public int ResultId { get; set; }
        [Range(0.25, 20)]
        public decimal GraceMarks { get; set; }
        [Required]
        public string ApprovalReason { get; set; } = string.Empty;
    }

    public class PublishProcessedResultsRequestDto
    {
        [Required]
        public int AssessmentId { get; set; }
        public bool Publish { get; set; } = true;
    }

    public class CompartmentalAssessmentCreateDto
    {
        [Required]
        public int OriginalAssessmentId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public DateTime DueDate { get; set; }

        public bool AllowLateSubmission { get; set; } = false;
        public List<int> EligibleStudentIds { get; set; } = new();
    }

    public class InstitutionalGradingPolicyViewModel
    {
        public int Id { get; set; }
        public string PolicyCode { get; set; } = string.Empty;
        public string PolicyName { get; set; } = string.Empty;
        public decimal PassingPercentage { get; set; } = 33m;
        public decimal CompartmentThresholdPercentage { get; set; } = 20m;
        public int MaxCompartmentSubjects { get; set; } = 2;
        public decimal MaxGraceMarks { get; set; } = 0.00m;
        public bool EnableGraceMarks { get; set; } = false;
        public string RoundingMethod { get; set; } = "HalfUp";
        public int RoundingDecimals { get; set; } = 2;
        public bool IsActive { get; set; } = true;
        public string GradeBoundariesJson { get; set; } = string.Empty;
    }
}
