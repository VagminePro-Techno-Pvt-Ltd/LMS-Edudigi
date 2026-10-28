using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Account;
using TMS.Models.Masters;

namespace TMS.Models.Academics
{
    [Table("CertificationExams")]
    public class CertificationExam : BaseModel
    {
        [Required]
        [StringLength(250)]
        public string Title { get; set; } = default!;

        [Required]
        public int CourseId { get; set; }

        [ForeignKey(nameof(CourseId))]
        public virtual CourseMaster? Course { get; set; }

        public int? SubjectId { get; set; }

        [ForeignKey(nameof(SubjectId))]
        public virtual SubjectMaster? Subject { get; set; }

        [Required]
        [StringLength(250)]
        public string CertificationTopic { get; set; } = default!;

        public string? Description { get; set; }

        [Required]
        public int DurationMinutes { get; set; } = 60;

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalMarks { get; set; } = 100.00m;

        [Required]
        [Column(TypeName = "decimal(5, 2)")]
        public decimal PassingPercentage { get; set; } = 50.00m;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public int MaxAttempts { get; set; } = 1;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Draft"; // Draft, Published, Archived

        [Required]
        public int FacultyId { get; set; }

        [ForeignKey(nameof(FacultyId))]
        public virtual UserMaster? Faculty { get; set; }

        public virtual ICollection<CertificationExamQuestion> Questions { get; set; } = new List<CertificationExamQuestion>();
        public virtual ICollection<CertificationExamAttempt> Attempts { get; set; } = new List<CertificationExamAttempt>();
        public virtual ICollection<IssuedCertificate> Certificates { get; set; } = new List<IssuedCertificate>();
    }

    [Table("CertificationExamQuestions")]
    public class CertificationExamQuestion : BaseModel
    {
        [Required]
        public int CertificationExamId { get; set; }

        [ForeignKey(nameof(CertificationExamId))]
        public virtual CertificationExam? CertificationExam { get; set; }

        [Required]
        public string QuestionText { get; set; } = default!;

        [Required]
        [StringLength(50)]
        public string QuestionType { get; set; } = "SingleChoice"; // SingleChoice, MultipleChoice, TrueFalse

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Marks { get; set; } = 1.00m;

        public string? Explanation { get; set; }

        public int SortOrder { get; set; } = 0;

        public virtual ICollection<CertificationExamQuestionOption> Options { get; set; } = new List<CertificationExamQuestionOption>();
    }

    [Table("CertificationExamQuestionOptions")]
    public class CertificationExamQuestionOption : BaseModel
    {
        [Required]
        public int CertificationExamQuestionId { get; set; }

        [ForeignKey(nameof(CertificationExamQuestionId))]
        public virtual CertificationExamQuestion? Question { get; set; }

        [Required]
        public string OptionText { get; set; } = default!;

        public bool IsCorrect { get; set; } = false;

        public int SortOrder { get; set; } = 0;
    }

    [Table("CertificationExamAttempts")]
    public class CertificationExamAttempt : BaseModel
    {
        [Required]
        public int CertificationExamId { get; set; }

        [ForeignKey(nameof(CertificationExamId))]
        public virtual CertificationExam? CertificationExam { get; set; }

        [Required]
        public int StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public virtual UserMaster? Student { get; set; }

        public int AttemptNumber { get; set; } = 1;

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? SubmittedAt { get; set; }

        public bool IsSubmitted { get; set; } = false;

        public int TimeRemainingSeconds { get; set; } = 0;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalScore { get; set; } = 0.00m;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal MaxScore { get; set; } = 0.00m;

        [Column(TypeName = "decimal(5, 2)")]
        public decimal Percentage { get; set; } = 0.00m;

        [StringLength(10)]
        public string Grade { get; set; } = "F";

        public bool IsPassed { get; set; } = false;

        public virtual ICollection<CertificationExamStudentAnswer> Answers { get; set; } = new List<CertificationExamStudentAnswer>();
        public virtual IssuedCertificate? Certificate { get; set; }
    }

    [Table("CertificationExamStudentAnswers")]
    public class CertificationExamStudentAnswer : BaseModel
    {
        [Required]
        public int CertificationExamAttemptId { get; set; }

        [ForeignKey(nameof(CertificationExamAttemptId))]
        public virtual CertificationExamAttempt? Attempt { get; set; }

        [Required]
        public int QuestionId { get; set; }

        [ForeignKey(nameof(QuestionId))]
        public virtual CertificationExamQuestion? Question { get; set; }

        public string? SelectedOptionIds { get; set; }

        public bool IsCorrect { get; set; } = false;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal MarksAwarded { get; set; } = 0.00m;

        public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
    }

    [Table("IssuedCertificates")]
    public class IssuedCertificate : BaseModel
    {
        [Required]
        [StringLength(100)]
        public string CertificateNumber { get; set; } = default!;

        [Required]
        [StringLength(100)]
        public string VerificationToken { get; set; } = default!;

        [Required]
        public int CertificationExamAttemptId { get; set; }

        [ForeignKey(nameof(CertificationExamAttemptId))]
        public virtual CertificationExamAttempt? Attempt { get; set; }

        [Required]
        public int StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public virtual UserMaster? Student { get; set; }

        [Required]
        public int CertificationExamId { get; set; }

        [ForeignKey(nameof(CertificationExamId))]
        public virtual CertificationExam? CertificationExam { get; set; }

        [Required]
        [StringLength(200)]
        public string StudentFullName { get; set; } = default!;

        [Required]
        [StringLength(250)]
        public string CertificationTopic { get; set; } = default!;

        [Required]
        [StringLength(20)]
        public string FinalGrade { get; set; } = default!;

        public bool IsPassed { get; set; } = false;

        public DateTime IssueDate { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(500)]
        public string PdfFilePath { get; set; } = default!;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Valid"; // Valid, Revoked

        [StringLength(500)]
        public string? RevocationReason { get; set; }

        public int? RevokedBy { get; set; }

        [ForeignKey(nameof(RevokedBy))]
        public virtual UserMaster? RevokedByUser { get; set; }

        public DateTime? RevokedOn { get; set; }

        public bool GeneratedSuccessfully { get; set; } = true;

        public string? GenerationError { get; set; }
    }
}
