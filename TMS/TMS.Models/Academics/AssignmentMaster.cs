using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Account;
using TMS.Models.Masters;

namespace TMS.Models.Academics
{
    [Table(nameof(AssignmentMaster))]
    public class AssignmentMaster : BaseModel
    {
        [Required, StringLength(250)]
        public string Title { get; set; } = default!;

        public string? Description { get; set; }

        [Required]
        public int ProgramId { get; set; }

        [ForeignKey(nameof(ProgramId))]
        public virtual CourseCategoryMaster? Program { get; set; }

        public int? CourseId { get; set; }

        [ForeignKey(nameof(CourseId))]
        public virtual CourseMaster? Course { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        [Required]
        public decimal MaxMarks { get; set; }

        [Required]
        public decimal PassMarks { get; set; }

        public bool AllowLateSubmission { get; set; } = false;

        public decimal LatePenalty { get; set; } = 0;

        [StringLength(500)]
        public string? AttachmentPath { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
