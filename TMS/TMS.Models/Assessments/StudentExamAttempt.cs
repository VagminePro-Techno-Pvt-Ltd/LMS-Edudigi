using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Account;

namespace TMS.Models.Assessments
{
    [Table("StudentExamAttempt")]
    public class StudentExamAttempt : BaseIdModel
    {
        [Required]
        public int ExamId { get; set; }

        [ForeignKey(nameof(ExamId))]
        public virtual ExamMaster? Exam { get; set; }

        [Required]
        public int StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public virtual UserMaster? Student { get; set; }

        [Required]
        public int AttemptNumber { get; set; } = 1;

        [Required]
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? SubmittedAt { get; set; }

        [Required]
        public int RemainingSeconds { get; set; }

        [Required]
        public bool IsSubmitted { get; set; } = false;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Score { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Percentage { get; set; }

        [StringLength(50)]
        public string? ResultStatus { get; set; } // Pass, Fail, Pending

        public virtual ICollection<StudentExamAnswer> Answers { get; set; } = new List<StudentExamAnswer>();
        public virtual ICollection<FacultyEvaluation> FacultyEvaluations { get; set; } = new List<FacultyEvaluation>();
        public virtual ICollection<ExamResult> Results { get; set; } = new List<ExamResult>();
    }
}
