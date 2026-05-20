using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Account;
using TMS.Models.Masters;

namespace TMS.Models.Academics
{
    /// <summary>
    /// Represents a single scheduled class session.
    /// Can belong to a ClassSeries (recurring) or be standalone (one-off).
    /// </summary>
    [Table("ClassInstances")]
    public class ClassInstance : BaseModel
    {
        // ─── Link to Series ───
        /// <summary>
        /// Null for standalone/instant classes, set for recurring series instances.
        /// </summary>
        public int? SeriesId { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = default!;

        // ─── Academic Hierarchy (denormalized for query performance) ───
        [Required]
        public int CourseId { get; set; }

        public int? SubjectId { get; set; }

        public int? UnitId { get; set; }

        // ─── Faculty ───
        [Required]
        public int FacultyId { get; set; }

        // ─── Schedule ───
        [Required]
        public DateTime ScheduledStart { get; set; }

        [Required]
        public DateTime ScheduledEnd { get; set; }

        public DateTime? ActualStart { get; set; }

        public DateTime? ActualEnd { get; set; }

        // ─── Meeting Info ───
        [Required, StringLength(50)]
        public string MeetingProvider { get; set; } = "Zoom";

        [StringLength(200)]
        public string? ProviderMeetingId { get; set; }

        [StringLength(500)]
        public string? JoinUrl { get; set; }

        [StringLength(500)]
        public string? HostUrl { get; set; }

        [StringLength(500)]
        public string? RecordingUrl { get; set; }

        // ─── Status ───
        /// <summary>
        /// 0 = Scheduled, 1 = Live, 2 = Completed, 3 = Cancelled, 4 = Rescheduled
        /// </summary>
        public int Status { get; set; } = 0;

        [StringLength(500)]
        public string? CancellationReason { get; set; }

        // ─── Capacity ───
        public int MaxCapacity { get; set; } = 100;

        // ─── Navigation ───
        [ForeignKey(nameof(SeriesId))]
        public virtual ClassSeries? Series { get; set; }

        [ForeignKey(nameof(CourseId))]
        public virtual CourseMaster? Course { get; set; }

        [ForeignKey(nameof(SubjectId))]
        public virtual SubjectMaster? Subject { get; set; }

        [ForeignKey(nameof(UnitId))]
        public virtual UnitMaster? Unit { get; set; }

        [ForeignKey(nameof(FacultyId))]
        public virtual UserMaster? Faculty { get; set; }

        public virtual List<ClassAttendance> Attendances { get; set; } = new();
    }
}
