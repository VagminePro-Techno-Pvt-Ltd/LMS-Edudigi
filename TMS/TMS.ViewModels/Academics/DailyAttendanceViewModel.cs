using System;
using System.ComponentModel.DataAnnotations;
using TMS.Common;
using TMS.ViewModels.Masters;

namespace TMS.ViewModels.Academics
{
    public class DailyAttendanceViewModel : BaseViewModel
    {
        [MapToDTO, Required]
        public int StudentId { get; set; }

        [MapToDTO, Required]
        public DateTime Date { get; set; }

        [MapToDTO]
        public DateTime? LoginTime { get; set; }

        /// <summary>
        /// 0 = Absent, 1 = Present, 2 = Late, 3 = Excused
        /// </summary>
        [MapToDTO]
        public int AttendanceStatus { get; set; } = 0;

        [MapToDTO, StringLength(500)]
        public string? Remarks { get; set; }

        // ─── Navigation ───
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
