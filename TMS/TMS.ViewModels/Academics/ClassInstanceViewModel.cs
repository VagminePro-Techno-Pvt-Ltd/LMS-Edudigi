using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Common;
using TMS.ViewModels.Masters;

namespace TMS.ViewModels.Academics
{
    public class ClassInstanceViewModel : BaseViewModel
    {
        [MapToDTO]
        public int? SeriesId { get; set; }

        [MapToDTO, Required, StringLength(200)]
        public string Title { get; set; } = default!;

        // ─── Academic Hierarchy ───
        [MapToDTO, Required]
        public int CourseId { get; set; }

        [MapToDTO]
        public int? SubjectId { get; set; }

        [MapToDTO]
        public int? UnitId { get; set; }

        // ─── Faculty ───
        [MapToDTO, Required]
        public int FacultyId { get; set; }

        // ─── Schedule ───
        [MapToDTO, Required]
        public DateTime ScheduledStart { get; set; }

        [MapToDTO, Required]
        public DateTime ScheduledEnd { get; set; }

        [MapToDTO]
        public DateTime? ActualStart { get; set; }

        [MapToDTO]
        public DateTime? ActualEnd { get; set; }

        // ─── Meeting Info ───
        [MapToDTO, Required, StringLength(50)]
        public string MeetingProvider { get; set; } = "Zoom";

        [MapToDTO, StringLength(200)]
        public string? ProviderMeetingId { get; set; }

        [MapToDTO, StringLength(500)]
        public string? JoinUrl { get; set; }

        [MapToDTO, StringLength(500)]
        public string? HostUrl { get; set; }

        [MapToDTO, StringLength(500)]
        public string? RecordingUrl { get; set; }

        // ─── Status ───
        [MapToDTO]
        public int Status { get; set; } = 0;

        [MapToDTO, StringLength(500)]
        public string? CancellationReason { get; set; }

        [MapToDTO]
        public int MaxCapacity { get; set; } = 100;

        // ─── Navigation (Read-only) ───
        public ClassSeriesViewModel? Series { get; set; }
        public CourseMasterViewModel? Course { get; set; }
        public SubjectMasterViewModel? Subject { get; set; }
        public UnitMasterViewModel? Unit { get; set; }
        public UserViewModel? Faculty { get; set; }

        public List<ClassAttendanceViewModel> Attendances { get; set; } = new();

        // ─── Computed Properties for UI ───
        [NotMapped]
        public override string? CourseName => Course?.Name;

        [NotMapped]
        public string FacultyName => Faculty?.Name ?? "Unassigned";

        [NotMapped]
        public string SubjectName => Subject?.Name ?? "General";

        [NotMapped]
        public string StatusText => Status switch
        {
            0 => "Scheduled",
            1 => "Live",
            2 => "Completed",
            3 => "Cancelled",
            4 => "Rescheduled",
            _ => "Unknown"
        };

        [NotMapped]
        public string StatusColor => Status switch
        {
            0 => "#3b82f6", // blue
            1 => "#ef4444", // red (live)
            2 => "#22c55e", // green
            3 => "#6b7280", // gray
            4 => "#f59e0b", // amber
            _ => "#6b7280"
        };

        [NotMapped]
        public int DurationMinutes => (int)(ScheduledEnd - ScheduledStart).TotalMinutes;

        [NotMapped]
        public bool IsUpcoming => ScheduledStart > DateTime.Now && Status == 0;

        [NotMapped]
        public bool IsLive => Status == 1 ||
            (ScheduledStart <= DateTime.Now && ScheduledEnd >= DateTime.Now && Status == 0);

        [NotMapped]
        public bool CanJoin => IsLive || (ScheduledStart <= DateTime.Now.AddMinutes(15) && Status == 0);

        [NotMapped]
        public int AttendeeCount => Attendances?.Count(a => a.AttendanceStatus == 1) ?? 0;
    }
}
