using System;
using System.ComponentModel.DataAnnotations;
using TMS.ViewModels.Masters;

namespace TMS.ViewModels.Academics
{
    public class AssignmentMasterViewModel : BaseViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(250)]
        public string Title { get; set; } = default!;

        public string? Description { get; set; }

        [Required(ErrorMessage = "Program is required")]
        public int ProgramId { get; set; }

        public string? ProgramName { get; set; }

        public int? CourseId { get; set; }

        public string? CourseName { get; set; }

        [Required(ErrorMessage = "Due date is required")]
        public DateTime DueDate { get; set; }

        [Required(ErrorMessage = "Max marks is required")]
        public decimal MaxMarks { get; set; }

        [Required(ErrorMessage = "Pass marks is required")]
        public decimal PassMarks { get; set; }

        public bool AllowLateSubmission { get; set; }

        public decimal LatePenalty { get; set; }

        public string? AttachmentPath { get; set; }

        public bool IsDeleted { get; set; }
    }
}
