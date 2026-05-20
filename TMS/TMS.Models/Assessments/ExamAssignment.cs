using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Account;

namespace TMS.Models.Assessments
{
    [Table("ExamAssignment")]
    public class ExamAssignment : BaseIdModel
    {
        [Required]
        public int ExamId { get; set; }

        [ForeignKey(nameof(ExamId))]
        public virtual ExamMaster? Exam { get; set; }

        public int? BatchId { get; set; }

        [Required]
        public int StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public virtual UserMaster? Student { get; set; }

        [Required]
        public int AssignedBy { get; set; }

        [ForeignKey(nameof(AssignedBy))]
        public virtual UserMaster? Assigner { get; set; }

        [Required]
        public DateTime AssignedOn { get; set; } = DateTime.UtcNow;
    }
}
