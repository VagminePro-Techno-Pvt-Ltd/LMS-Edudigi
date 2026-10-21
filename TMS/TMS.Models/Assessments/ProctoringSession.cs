using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Account;

namespace TMS.Models.Assessments
{
    [Table("ProctoringSession")]
    public class ProctoringSession : BaseIdModel
    {
        [Required]
        public int AttemptId { get; set; }

        [ForeignKey(nameof(AttemptId))]
        public virtual StudentExamAttempt? Attempt { get; set; }

        [Required]
        public int ExamId { get; set; }

        [ForeignKey(nameof(ExamId))]
        public virtual ExamMaster? Exam { get; set; }

        [Required]
        public int StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public virtual UserMaster? Student { get; set; }

        [Required]
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? EndedAt { get; set; }

        public bool CameraConsentGiven { get; set; } = false;

        public DateTime? ConsentTimestamp { get; set; }

        public bool IdentityVerified { get; set; } = false;

        [StringLength(50)]
        public string VerificationMethod { get; set; } = "SystemProfile"; // SystemProfile, ManualFallback

        public int TotalViolationsCount { get; set; } = 0;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Active"; // Active, Completed, FlaggedForReview, Reviewed

        [StringLength(1000)]
        public string? ReviewerRemarks { get; set; }

        public int? ReviewedBy { get; set; }

        public DateTime? ReviewedOn { get; set; }

        public virtual ICollection<ProctoringIncident> Incidents { get; set; } = new List<ProctoringIncident>();
    }
}
