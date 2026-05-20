using System;
using System.ComponentModel.DataAnnotations;
using TMS.Common;

namespace TMS.ViewModels.Academics
{
    public class ClassAttendanceViewModel : BaseViewModel
    {
        [MapToDTO, Required]
        public int ClassInstanceId { get; set; }

        [MapToDTO, Required]
        public int StudentId { get; set; }

        /// <summary>
        /// 0 = Absent, 1 = Present, 2 = Late, 3 = Excused
        /// </summary>
        [MapToDTO]
        public int AttendanceStatus { get; set; } = 0;

        [MapToDTO]
        public DateTime? JoinedAt { get; set; }

        [MapToDTO]
        public DateTime? LeftAt { get; set; }

        [MapToDTO]
        public int DurationMinutes { get; set; } = 0;

        [MapToDTO, StringLength(50)]
        public string Source { get; set; } = "Manual";

        // ─── Navigation ───
        public ClassInstanceViewModel? ClassInstance { get; set; }
        public UserViewModel? Student { get; set; }

        // ─── Computed ───
        public string StudentName => Student?.Name ?? "Unknown";
        public string StatusText => AttendanceStatus switch
        {
            0 => "Absent",
            1 => "Present",
            2 => "Late",
            3 => "Excused",
            _ => "Unknown"
        };
    }
}
