using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TMS.Models.Academics;
using TMS.Models.Masters;

namespace TMS.ViewModels.Academics
{
    public class CertificationExamFormModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Exam Title is required.")]
        [StringLength(250)]
        public string Title { get; set; } = default!;

        [Required(ErrorMessage = "Course selection is required.")]
        public int CourseId { get; set; }

        public int? SubjectId { get; set; }

        [Required(ErrorMessage = "Certification Topic is required.")]
        [StringLength(250)]
        public string CertificationTopic { get; set; } = default!;

        public string? Description { get; set; }

        [Required]
        [Range(5, 360, ErrorMessage = "Duration must be between 5 and 360 minutes.")]
        public int DurationMinutes { get; set; } = 60;

        [Required]
        [Range(1, 1000, ErrorMessage = "Total Marks must be between 1 and 1000.")]
        public decimal TotalMarks { get; set; } = 100.00m;

        [Required]
        [Range(1, 100, ErrorMessage = "Passing Percentage must be between 1 and 100.")]
        public decimal PassingPercentage { get; set; } = 50.00m;

        [Required(ErrorMessage = "Start Date/Time is required.")]
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "End Date/Time is required.")]
        public DateTime EndDate { get; set; } = DateTime.UtcNow.AddDays(7);

        [Required]
        [Range(1, 10, ErrorMessage = "Maximum Attempts must be at least 1.")]
        public int MaxAttempts { get; set; } = 1;
    }

    public class CertificationOptionModel
    {
        public int Id { get; set; }
        public string OptionText { get; set; } = default!;
        public bool IsCorrect { get; set; }
        public int SortOrder { get; set; }
    }

    public class CertificationQuestionFormModel
    {
        public int Id { get; set; }
        public int CertificationExamId { get; set; }

        [Required(ErrorMessage = "Question text is required.")]
        public string QuestionText { get; set; } = default!;

        [Required]
        public string QuestionType { get; set; } = "SingleChoice"; // SingleChoice, MultipleChoice, TrueFalse

        [Required]
        [Range(0.25, 100, ErrorMessage = "Marks must be between 0.25 and 100.")]
        public decimal Marks { get; set; } = 1.00m;

        public string? Explanation { get; set; }
        public int SortOrder { get; set; }

        public List<CertificationOptionModel> Options { get; set; } = new();
    }

    public class FacultyCertificationDashboardViewModel
    {
        public List<CertificationExam> Exams { get; set; } = new();
        public List<CourseMaster> Courses { get; set; } = new();
        public List<SubjectMaster> Subjects { get; set; } = new();
        public int TotalExams { get; set; }
        public int PublishedExams { get; set; }
        public int TotalAttempts { get; set; }
        public int TotalCertificates { get; set; }
        public Dictionary<int, int> QuestionCounts { get; set; } = new();
        public Dictionary<int, int> AttemptCounts { get; set; } = new();
        public Dictionary<int, int> CertificateCounts { get; set; } = new();
    }

    public class StudentCertificationExamCardViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = default!;
        public string CourseName { get; set; } = default!;
        public string? SubjectName { get; set; }
        public string CertificationTopic { get; set; } = default!;
        public string? Description { get; set; }
        public int TotalQuestions { get; set; }
        public int DurationMinutes { get; set; }
        public decimal PassingPercentage { get; set; }
        public decimal TotalMarks { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string ExamStatus { get; set; } = default!; // Active, Upcoming, Closed
        public string AttemptStatus { get; set; } = default!; // Not Attempted, In Progress, Completed
        public int AttemptsUsed { get; set; }
        public int MaxAttempts { get; set; }
        public bool CanAttempt { get; set; }
        public int? AttemptId { get; set; }
        public bool? IsPassed { get; set; }
        public string? Grade { get; set; }
        public decimal? Score { get; set; }
        public decimal? Percentage { get; set; }
        public int? CertificateId { get; set; }
        public string? CertificateNumber { get; set; }
    }

    public class ExamTakingOptionViewModel
    {
        public int OptionId { get; set; }
        public string OptionText { get; set; } = default!;
        public int SortOrder { get; set; }
    }

    public class ExamTakingQuestionViewModel
    {
        public int QuestionId { get; set; }
        public int QuestionIndex { get; set; }
        public string QuestionText { get; set; } = default!;
        public string QuestionType { get; set; } = "SingleChoice"; // SingleChoice, MultipleChoice, TrueFalse
        public decimal Marks { get; set; }
        public List<ExamTakingOptionViewModel> Options { get; set; } = new();
        public List<int> SelectedOptionIds { get; set; } = new();
    }

    public class StudentExamTakingViewModel
    {
        public int AttemptId { get; set; }
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = default!;
        public string CertificationTopic { get; set; } = default!;
        public int DurationMinutes { get; set; }
        public int RemainingSeconds { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime DueAt { get; set; }
        public List<ExamTakingQuestionViewModel> Questions { get; set; } = new();
    }

    public class CertificationExamResultViewModel
    {
        public int AttemptId { get; set; }
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = default!;
        public string CertificationTopic { get; set; } = default!;
        public string CourseName { get; set; } = default!;
        public string? SubjectName { get; set; }
        public string StudentName { get; set; } = default!;
        public DateTime SubmittedAt { get; set; }
        public decimal TotalScore { get; set; }
        public decimal MaxScore { get; set; }
        public decimal Percentage { get; set; }
        public decimal PassingPercentage { get; set; }
        public string Grade { get; set; } = default!;
        public bool IsPassed { get; set; }
        public int? CertificateId { get; set; }
        public string? CertificateNumber { get; set; }
        public string? VerificationToken { get; set; }
    }

    public class PublicCertificateVerificationViewModel
    {
        public string CertificateNumber { get; set; } = default!;
        public string StudentFullName { get; set; } = default!;
        public string CertificationTopic { get; set; } = default!;
        public string FinalGrade { get; set; } = default!;
        public bool IsPassed { get; set; }
        public DateTime IssueDate { get; set; }
        public string Status { get; set; } = "Valid"; // Valid, Revoked
        public string? RevocationReason { get; set; }
        public DateTime? RevokedOn { get; set; }
        public string InstitutionName { get; set; } = "EduDigiClass University";
        public bool IsFound { get; set; } = true;
    }

    public class AdminCertificatesFilterModel
    {
        public string? SearchTerm { get; set; }
        public string? StatusFilter { get; set; }
        public int? ExamId { get; set; }
    }

    public class FacultyAttemptsViewModel
    {
        public CertificationExam Exam { get; set; } = default!;
        public List<CertificationExamAttempt> Attempts { get; set; } = new();
        public Dictionary<int, IssuedCertificate> CertificatesByAttemptId { get; set; } = new();
    }

    public class AiQuestionGenerateRequestModel
    {
        public int ExamId { get; set; }
        public string Subject { get; set; } = default!;
        public string CertificationTopic { get; set; } = default!;
        public string? AdditionalInstructions { get; set; }
        public int NumberOfQuestions { get; set; } = 5;
        public string Difficulty { get; set; } = "Mixed"; // Easy, Medium, Hard, Mixed
        public string QuestionType { get; set; } = "SingleChoice"; // SingleChoice, MultipleChoice, TrueFalse, Mixed
        public decimal MarksPerQuestion { get; set; } = 1.00m;
    }

    public class AiGeneratedQuestionModel
    {
        public string TempId { get; set; } = Guid.NewGuid().ToString("N");
        public string QuestionText { get; set; } = default!;
        public string QuestionType { get; set; } = "SingleChoice";
        public string Difficulty { get; set; } = "Medium";
        public decimal Marks { get; set; } = 1.00m;
        public string? Explanation { get; set; }
        public List<CertificationOptionModel> Options { get; set; } = new();
        public bool IsSelected { get; set; } = true;
    }

    public class AiQuestionGenerateResponseModel
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<AiGeneratedQuestionModel> Questions { get; set; } = new();
    }

    public class AiQuestionBatchSaveRequestModel
    {
        public int ExamId { get; set; }
        public List<CertificationQuestionFormModel> Questions { get; set; } = new();
    }
}
