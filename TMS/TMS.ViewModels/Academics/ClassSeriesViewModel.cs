using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Common;
using TMS.ViewModels.Masters;

namespace TMS.ViewModels.Academics
{
    public class ClassSeriesViewModel : BaseViewModel
    {
        [MapToDTO, Required, StringLength(200)]
        public string Title { get; set; } = default!;

        [MapToDTO, StringLength(500)]
        public string? Description { get; set; }

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
        public DateTime StartDate { get; set; }

        [MapToDTO]
        public DateTime? EndDate { get; set; }

        [MapToDTO, Required]
        public TimeSpan StartTime { get; set; }

        [MapToDTO, Required]
        public int DurationMinutes { get; set; } = 60;

        // ─── Recurrence ───
        [MapToDTO]
        public bool IsRecurring { get; set; } = false;

        [MapToDTO, StringLength(500)]
        public string? RecurrenceRule { get; set; }

        // ─── Meeting Provider ───
        [MapToDTO, Required, StringLength(50)]
        public string MeetingProvider { get; set; } = "Zoom";

        // ─── Capacity ───
        [MapToDTO]
        public int MaxCapacity { get; set; } = 100;
        
        [MapToDTO, StringLength(500)]
        public string? ExternalMeetingLink { get; set; }

        // ─── Navigation (Read-only) ───
        public CourseMasterViewModel? Course { get; set; }
        public SubjectMasterViewModel? Subject { get; set; }
        public UnitMasterViewModel? Unit { get; set; }
        public UserViewModel? Faculty { get; set; }

        public List<ClassInstanceViewModel> Instances { get; set; } = new();

        // ─── Helper properties for UI ───
        [NotMapped]
        public string FacultyName => Faculty?.Name ?? "Unassigned";

        [NotMapped]
        public override string? CourseName => Course?.Name;

        [NotMapped]
        public string SubjectName => Subject?.Name ?? "General";

        // ─── Recurrence UI helpers ───
        [NotMapped]
        public List<string> SelectedDays { get; set; } = new(); // ["MO","WE","FR"]
    }
}
