using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Academics
{
    /// <summary>
    /// A single timestamped segment within a video lecture transcript.
    /// StartTimeSeconds and EndTimeSeconds map directly to video playback position.
    /// </summary>
    [Table(nameof(LectureTranscriptSegment))]
    public class LectureTranscriptSegment : BaseModel
    {
        [Required]
        public int LectureTranscriptId { get; set; }

        [ForeignKey(nameof(LectureTranscriptId))]
        public virtual LectureTranscript? LectureTranscript { get; set; }

        /// <summary>Start time in seconds within the video.</summary>
        [Required]
        public double StartTimeSeconds { get; set; }

        /// <summary>End time in seconds within the video.</summary>
        [Required]
        public double EndTimeSeconds { get; set; }

        /// <summary>The spoken text content during this segment.</summary>
        [Required]
        public string Text { get; set; } = default!;

        /// <summary>Sequential order of this segment in the transcript.</summary>
        public int SequenceOrder { get; set; } = 0;

        /// <summary>Optional identified speaker label (e.g. "Instructor", "Speaker 1").</summary>
        [StringLength(100)]
        public string? Speaker { get; set; }
    }
}

