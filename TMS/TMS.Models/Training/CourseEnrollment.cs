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

        // ─── Status-based Enrollment Lifecycle (NotMapped for LMS-SilverOak compatibility) ───
        /// <summary>
        /// 1 = Active, 2 = Pending, 3 = Dropped, 4 = Completed, 5 = Waitlisted
        /// </summary>
        [NotMapped]
        public int Status { get; set; } = 1;

        [NotMapped]
        public DateTime? ApprovedOn { get; set; }

        [NotMapped]
        public DateTime? DroppedOn { get; set; }

        [NotMapped]
        public DateTime? CompletedOn { get; set; }

        /// <summary>
        /// Source of enrollment: Admin, CSV, Auto, Payment
        /// </summary>
        [NotMapped]
        [StringLength(50)]
        public string? Source { get; set; } = "Admin";
    }
}
