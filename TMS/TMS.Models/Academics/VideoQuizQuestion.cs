using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Academics
{
    /// <summary>
    /// A quiz question tied directly to a VideoMaster.
    /// Completely separate from the existing CourseId+QuadrantId quiz system.
    /// TriggerTimeSeconds: the playback position (in seconds) at which this question appears.
    /// CorrectOption stores "A", "B", "C", or "D" — NEVER exposed to frontend.
    /// </summary>
    [Table(nameof(VideoQuizQuestion))]
    public class VideoQuizQuestion : BaseModel
    {
        [Required]
        public int VideoId { get; set; }

        [ForeignKey(nameof(VideoId))]
        public virtual VideoMaster? Video { get; set; }

        [Required]
        public string QuestionText { get; set; } = default!;

        /// <summary>Option A text (always required).</summary>
        [Required, StringLength(500)]
        public string OptionA { get; set; } = default!;

        /// <summary>Option B text.</summary>
        [StringLength(500)]
        public string? OptionB { get; set; }

        /// <summary>Option C text.</summary>
        [StringLength(500)]
        public string? OptionC { get; set; }

        /// <summary>Option D text.</summary>
        [StringLength(500)]
        public string? OptionD { get; set; }

        /// <summary>
        /// The correct answer letter: "A", "B", "C", or "D".
        /// NEVER sent to the frontend in any API response.
        /// </summary>
        [Required, StringLength(1)]
        public string CorrectOption { get; set; } = default!;

        /// <summary>
        /// The video timestamp (seconds) at which this question is triggered.
        /// Multiple questions can share the same TriggerTimeSeconds (up to 2 shown at once).
        /// </summary>
        [Required]
        public int TriggerTimeSeconds { get; set; }

        /// <summary>Display order when multiple questions share the same trigger time.</summary>
        public int SortOrder { get; set; } = 0;
    }
}
