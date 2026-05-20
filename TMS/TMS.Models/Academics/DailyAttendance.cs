using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Account;

namespace TMS.Models.Academics
{
    /// <summary>
    /// Tracks student platform-level attendance based on login.
    /// </summary>
    [Table("DailyAttendances")]
    public class DailyAttendance : BaseModel
    {
        [Required]
        public int StudentId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public DateTime? LoginTime { get; set; }

        /// <summary>
        /// 0 = Absent, 1 = Present, 2 = Late, 3 = Excused
        /// </summary>
        public int AttendanceStatus { get; set; } = 0;

        [StringLength(500)]
        public string? Remarks { get; set; }

        // ─── Navigation ───
        [ForeignKey(nameof(StudentId))]
        public virtual UserMaster? Student { get; set; }
    }
}
