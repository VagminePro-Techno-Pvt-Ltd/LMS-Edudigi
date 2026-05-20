using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Academics
{
    /// <summary>
    /// Tracks a student's watch progress for a specific video.
    /// Unique constraint enforced on (StudentId, VideoId) in DbContext.
    /// MaxAllowedSeconds controls how far the student can seek — anti-skip gate.
    /// </summary>
    [Table(nameof(VideoProgress))]
    public class VideoProgress : BaseModel
    {
        [Required]
        public int StudentId { get; set; }

        [Required]
        public int VideoId { get; set; }

        [ForeignKey(nameof(VideoId))]
        public virtual VideoMaster? Video { get; set; }

        /// <summary>Last watched position in seconds (never goes backwards).</summary>
        public int LastWatchedSeconds { get; set; } = 0;

        /// <summary>
        /// Maximum seconds the student is allowed to seek to.
        /// Set initially to first quiz trigger time (or full duration if no quiz).
        /// Unlocked progressively after each quiz attempt.
        /// </summary>
        public int MaxAllowedSeconds { get; set; } = 0;

        /// <summary>True when student reaches the last 5 seconds of the video.</summary>
        public bool IsCompleted { get; set; } = false;

        public DateTime LastUpdatedOn { get; set; } = DateTime.UtcNow;
    }
}
