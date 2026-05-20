using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Masters;

namespace TMS.Models.Academics
{
    /// <summary>
    /// Tracks a student's sequential watch progress for a LectureMaterial video.
    /// Unique constraint enforced on (StudentId, LectureMaterialId) in DbContext.
    /// WatchedSeconds = furthest point student watched sequentially (anti-skip gate).
    /// 95%+ watched → IsCompleted = true → report shows 100%.
    /// </summary>
    [Table("LectureVideoProgress")]
    public class LectureVideoProgress : BaseModel
    {
        [Required]
        public int StudentId { get; set; }

        /// <summary>LectureMaterial.Id — the video the student is watching.</summary>
        [Required]
        public int LectureMaterialId { get; set; }

        [ForeignKey(nameof(LectureMaterialId))]
        public virtual LectureMaterial? LectureMaterial { get; set; }

        /// <summary>Total duration of the video in seconds (set on first save).</summary>
        public int DurationSeconds { get; set; } = 0;

        /// <summary>
        /// Maximum sequential seconds the student has watched.
        /// This is the anti-skip gate: student cannot seek beyond this point.
        /// </summary>
        public int WatchedSeconds { get; set; } = 0;

        /// <summary>
        /// Percentage of video watched (WatchedSeconds / DurationSeconds * 100).
        /// Capped at 100.
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal ProgressPercentage { get; set; } = 0;

        /// <summary>True when ProgressPercentage >= 95 (student watched 95%+).</summary>
        public bool IsCompleted { get; set; } = false;

        /// <summary>Last position the student was at when they closed the video.</summary>
        public int LastPositionSeconds { get; set; } = 0;

        /// <summary>UTC timestamp of last progress update.</summary>
        public DateTime LastWatchedAt { get; set; } = DateTime.UtcNow;

        // ─── Computed helpers (not mapped) ───────────────────────────────────
        [NotMapped]
        public decimal DisplayPercentage => IsCompleted ? 100m : ProgressPercentage;
    }
}
