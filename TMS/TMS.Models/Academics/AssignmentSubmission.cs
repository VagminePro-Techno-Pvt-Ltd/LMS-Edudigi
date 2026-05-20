using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Account;

namespace TMS.Models.Academics
{
    [Table(nameof(AssignmentSubmission))]
    public class AssignmentSubmission : BaseModel
    {
        [Required]
        public int AssignmentId { get; set; }

        [ForeignKey(nameof(AssignmentId))]
        public virtual AssignmentMaster? Assignment { get; set; }

        [Required]
        public int StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public virtual UserMaster? Student { get; set; }

        public string? SubmissionText { get; set; }

        [StringLength(500)]
        public string? AttachmentPath { get; set; }

        [Required]
        public DateTime SubmittedOn { get; set; }

        public int AttemptNumber { get; set; } = 1;

        /// <summary>
        /// Draft, Submitted, LateSubmitted, UnderReview, Graded, Resubmitted
        /// </summary>
        [Required, StringLength(50)]
        public string Status { get; set; } = "Draft";

        public bool IsDeleted { get; set; } = false;
    }
}
