using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Account;
using TMS.Models.Masters;

namespace TMS.Models.Academics
{
    [Table(nameof(StudentRiskAlert))]
    public class StudentRiskAlert : BaseModel
    {
        [Required]
        public int StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public virtual UserMaster? Student { get; set; }

        [Required]
        public int CourseId { get; set; }

        [ForeignKey(nameof(CourseId))]
        public virtual CourseMaster? Course { get; set; }

        [Required, StringLength(50)]
        public string ActivityType { get; set; } = default!; // "Video", "Pdf", "Assignment", "Quiz", "Discussion", "Moderation"

        public int ActivityId { get; set; } = 0;

        [Required, StringLength(255)]
        public string Title { get; set; } = default!;

        [Required]
        public string Message { get; set; } = default!;

        [Required, StringLength(50)]
        public string RiskLevel { get; set; } = default!; // "Low", "Medium", "High", "Critical", "Warning"

        public DateTime? DeadlineDate { get; set; }

        public bool IsResolved { get; set; } = false;

        public DateTime? ResolvedOn { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime? ReadOn { get; set; }

        [StringLength(500)]
        public string? ActionUrl { get; set; }
    }
}
