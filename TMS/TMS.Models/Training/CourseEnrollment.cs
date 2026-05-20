using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Account;
using TMS.Models.Masters;
 

namespace TMS.Models.Training
{
    [Table(nameof(CourseEnrollment))]
    public class CourseEnrollment : BaseModel
    {
        public int CourseId { get; set; }
        [ForeignKey(nameof(CourseId))]
        public virtual CourseMaster? Course { get; set; }

        public int StudentId { get; set; }
        [ForeignKey(nameof(StudentId))]
        public virtual UserMaster? Student { get; set; }

        public DateTime EnrolledOn { get; set; } = DateTime.UtcNow;

        // ─── NEW: Status-based Enrollment Lifecycle ───
        /// <summary>
        /// 1 = Active, 2 = Pending, 3 = Dropped, 4 = Completed, 5 = Waitlisted
        /// </summary>
        public int Status { get; set; } = 1;

        public DateTime? ApprovedOn { get; set; }
        public DateTime? DroppedOn { get; set; }
        public DateTime? CompletedOn { get; set; }

        /// <summary>
        /// Source of enrollment: Admin, CSV, Auto, Payment
        /// </summary>
        [StringLength(50)]
        public string? Source { get; set; } = "Admin";
    }
}
