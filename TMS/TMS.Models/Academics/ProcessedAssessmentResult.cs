using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Account;
using TMS.Models.Assessments;
using TMS.Models.Masters;

namespace TMS.Models.Academics
{
    [Table("ProcessedAssessmentResult")]
    public class ProcessedAssessmentResult
    {
        [Key]
        public int Id { get; set; }

        public int? AssessmentId { get; set; }

        [ForeignKey(nameof(AssessmentId))]
        public virtual InternalAssessment? Assessment { get; set; }

        public int? ExamMasterId { get; set; }

        [ForeignKey(nameof(ExamMasterId))]
        public virtual ExamMaster? ExamMaster { get; set; }

        public int? StudentExamAttemptId { get; set; }

        [ForeignKey(nameof(StudentExamAttemptId))]
        public virtual StudentExamAttempt? StudentExamAttempt { get; set; }

        [Required]
        public int StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public virtual UserMaster? Student { get; set; }

        [Required]
        public int CourseId { get; set; }

        [ForeignKey(nameof(CourseId))]
        public virtual CourseMaster? Course { get; set; }

        public int? SemesterId { get; set; }

        [ForeignKey(nameof(SemesterId))]
        public virtual SemesterMaster? Semester { get; set; }

        public int? PolicyId { get; set; }

        [ForeignKey(nameof(PolicyId))]
        public virtual InstitutionalGradingPolicy? Policy { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal MaxMarks { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal ObtainedMarks { get; set; } = 0;

        [Column(TypeName = "decimal(5, 2)")]
        public decimal Percentage { get; set; } = 0;

        [Column(TypeName = "decimal(5, 2)")]
        public decimal GraceMarksAwarded { get; set; } = 0;

        public bool IsGraceApplied { get; set; } = false;

        [Required]
        [StringLength(10)]
        public string Grade { get; set; } = "F";

        [Column(TypeName = "decimal(4, 2)")]
        public decimal GradePoint { get; set; } = 0;

        [Required]
        [StringLength(50)]
        public string ResultStatus { get; set; } = "PENDING"; // PASS, FAIL, ABSENT, COMPARTMENT_ELIGIBLE

        public bool IsCompartmental { get; set; } = false;

        public int AttemptNumber { get; set; } = 1;

        public int? OriginalResultId { get; set; }

        [ForeignKey(nameof(OriginalResultId))]
        public virtual ProcessedAssessmentResult? OriginalResult { get; set; }

        public bool IsEvaluated { get; set; } = false;

        public bool IsPublished { get; set; } = false;

        public DateTime? PublishedOn { get; set; }

        public int? PublishedBy { get; set; }

        public DateTime CalculatedOn { get; set; } = DateTime.UtcNow;

        public int CalculatedBy { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        [StringLength(500)]
        public string? AuditRemark { get; set; }
    }
}
