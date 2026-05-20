using System;
using System.ComponentModel.DataAnnotations;

namespace TMS.ViewModels.Academics
{
    public class AssignmentGradeViewModel : BaseViewModel
    {
        [Required]
        public int SubmissionId { get; set; }

        [Required]
        public decimal Marks { get; set; }

        public string? Feedback { get; set; }

        [Required]
        public int EvaluatedBy { get; set; }

        public string? EvaluatorName { get; set; }

        [Required]
        public DateTime EvaluatedOn { get; set; }

        public bool IsDeleted { get; set; }
    }
}
