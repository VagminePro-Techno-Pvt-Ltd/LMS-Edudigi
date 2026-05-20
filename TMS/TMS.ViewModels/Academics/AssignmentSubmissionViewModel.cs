using System;
using System.ComponentModel.DataAnnotations;

namespace TMS.ViewModels.Academics
{
    public class AssignmentSubmissionViewModel : BaseViewModel
    {
        [Required]
        public int AssignmentId { get; set; }

        public string? AssignmentTitle { get; set; }

        public string? AssignmentDescription { get; set; }

        public decimal MaxMarks { get; set; }

        public decimal PassMarks { get; set; }

        public DateTime DueDate { get; set; }

        [Required]
        public int StudentId { get; set; }

        public string? StudentName { get; set; }

        public string? SubmissionText { get; set; }

        public string? AttachmentPath { get; set; }

        [Required]
        public DateTime SubmittedOn { get; set; }

        public int AttemptNumber { get; set; } = 1;

        /// <summary>
        /// Draft, Submitted, LateSubmitted, UnderReview, Graded, Resubmitted
        /// </summary>
        [Required, StringLength(50)]
        public string Status { get; set; } = "Draft";

        public decimal? MarksObtained { get; set; }

        public string? Feedback { get; set; }

        public int? EvaluatedBy { get; set; }

        public string? EvaluatorName { get; set; }

        public DateTime? EvaluatedOn { get; set; }

        public bool IsDeleted { get; set; }
    }
}
