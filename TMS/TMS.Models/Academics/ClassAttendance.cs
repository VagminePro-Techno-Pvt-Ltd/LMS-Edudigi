using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Account;

namespace TMS.Models.Academics
{
    /// <summary>
    /// Tracks student attendance for each class instance.
    /// Can be populated via Zoom webhooks or manual marking.
    /// </summary>
    [Table("ClassAttendances")]
    public class ClassAttendance : BaseModel
    {
        [Required]
        public int ClassInstanceId { get; set; }

        [Required]
        public int StudentId { get; set; }

        /// <summary>
        /// 0 = Absent, 1 = Present, 2 = Late, 3 = Excused
        /// </summary>
        public int AttendanceStatus { get; set; } = 0;

        public DateTime? JoinedAt { get; set; }

        public DateTime? LeftAt { get; set; }

        /// <summary>
        /// Duration in minutes the student was present.
        /// </summary>
        public int DurationMinutes { get; set; } = 0;

        /// <summary>
        /// Source of the attendance record: "Manual", "ZoomWebhook", "AutoDetect"
        /// </summary>
        [StringLength(50)]
        public string Source { get; set; } = "Manual";

        // ─── Navigation ───
        [ForeignKey(nameof(ClassInstanceId))]
        public virtual ClassInstance? ClassInstance { get; set; }

        [ForeignKey(nameof(StudentId))]
        public virtual UserMaster? Student { get; set; }
    }
}
