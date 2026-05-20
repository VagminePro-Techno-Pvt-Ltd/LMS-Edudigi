using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Account;

namespace TMS.Models.Assessments
{
    [Table("FacultyEvaluation")]
    public class FacultyEvaluation : BaseIdModel
    {
        [Required]
        public int AttemptId { get; set; }

        [ForeignKey(nameof(AttemptId))]
        public virtual StudentExamAttempt? Attempt { get; set; }

        [Required]
        public int FacultyId { get; set; }

        [ForeignKey(nameof(FacultyId))]
        public virtual UserMaster? Faculty { get; set; }

        public string? Feedback { get; set; }

        [Required]
        public DateTime EvaluatedOn { get; set; } = DateTime.UtcNow;

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalMarksAwarded { get; set; }
    }
}
