using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Account;
using TMS.Models.Masters;

namespace TMS.Models.Academics
{
    /// <summary>
    /// Represents a recurring or one-off class series definition.
    /// A series generates one or more ClassInstance records.
    /// </summary>
    [Table("ClassSeries")]
    public class ClassSeries : BaseModel
    {
        [Required, StringLength(200)]
        public string Title { get; set; } = default!;

        [StringLength(500)]
        public string? Description { get; set; }

        // ─── Academic Hierarchy ───
        [Required]
        public int CourseId { get; set; }

        public int? SubjectId { get; set; }

        public int? UnitId { get; set; }

        // ─── Faculty ───
        [Required]
        public int FacultyId { get; set; }

        // ─── Schedule ───
        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public int DurationMinutes { get; set; } = 60;

        // ─── Recurrence ───
        /// <summary>
        /// False = One-off class, True = Recurring series
        /// </summary>
        public bool IsRecurring { get; set; } = false;

        /// <summary>
        /// iCalendar RRULE string, e.g. "FREQ=WEEKLY;BYDAY=MO,WE,FR"
        /// Null for one-off classes.
        /// </summary>
        [StringLength(500)]
        public string? RecurrenceRule { get; set; }

        // ─── Meeting Provider ───
        /// <summary>
        /// "Zoom", "GoogleMeet", "MicrosoftTeams", "Jitsi"
        /// </summary>
        [Required, StringLength(50)]
        public string MeetingProvider { get; set; } = "Zoom";

        // ─── Capacity ───
        public int MaxCapacity { get; set; } = 100;

        // ─── Navigation ───
        [ForeignKey(nameof(CourseId))]
        public virtual CourseMaster? Course { get; set; }

        [ForeignKey(nameof(SubjectId))]
        public virtual SubjectMaster? Subject { get; set; }

        [ForeignKey(nameof(UnitId))]
        public virtual UnitMaster? Unit { get; set; }

        [ForeignKey(nameof(FacultyId))]
        public virtual UserMaster? Faculty { get; set; }

        public virtual List<ClassInstance> Instances { get; set; } = new();
    }
}
