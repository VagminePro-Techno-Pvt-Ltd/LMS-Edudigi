using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Account;

namespace TMS.Models.Academics
{
    [Table(nameof(AssignmentGrade))]
    public class AssignmentGrade : BaseModel
    {
        [Required]
        public int SubmissionId { get; set; }

        [ForeignKey(nameof(SubmissionId))]
        public virtual AssignmentSubmission? Submission { get; set; }

        [Required]
        public decimal Marks { get; set; }

        public string? Feedback { get; set; }

        [Required]
        public int EvaluatedBy { get; set; }

        [ForeignKey(nameof(EvaluatedBy))]
        public virtual UserMaster? Evaluator { get; set; }

        [Required]
        public DateTime EvaluatedOn { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
