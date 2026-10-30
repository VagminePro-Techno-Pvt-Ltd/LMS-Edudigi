using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Account;
using TMS.Models.Masters;

namespace TMS.Models.Academics
{
    [Table(nameof(CourseActivityDeadline))]
    public class CourseActivityDeadline : BaseModel
    {
        [Required]
        public int CourseId { get; set; }

        [ForeignKey(nameof(CourseId))]
        public virtual CourseMaster? Course { get; set; }

        [Required, StringLength(50)]
        public string ActivityType { get; set; } = default!; // "Video", "Pdf", "Assignment", "Quiz", "Discussion"

        [Required]
        public int ActivityId { get; set; }

        [Required, StringLength(255)]
        public string ActivityTitle { get; set; } = default!;

        [Required]
        public DateTime DeadlineDate { get; set; }

        public int ReminderDaysBefore { get; set; } = 2;

        public bool IsActive { get; set; } = true;
    }
}
