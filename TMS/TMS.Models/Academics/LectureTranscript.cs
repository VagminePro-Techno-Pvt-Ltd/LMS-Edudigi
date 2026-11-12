using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Masters;

namespace TMS.Models.Academics
{
    public static class TranscriptStatus
    {
        public const string Pending = "Pending";
        public const string Processing = "Processing";
        public const string Ready = "Ready";
        public const string Failed = "Failed";
    }

    /// <summary>
    /// Holds the AI-generated or faculty-edited transcript for a video lecture.
    /// Linked directly to LectureMaterial via LectureMaterialId.
    /// </summary>
    [Table(nameof(LectureTranscript))]
    public class LectureTranscript : BaseModel
    {
        [Required]
        public int LectureMaterialId { get; set; }

        [ForeignKey(nameof(LectureMaterialId))]
        public virtual LectureMaterial? LectureMaterial { get; set; }

        /// <summary>Processing state: Pending, Processing, Ready, Failed</summary>
        [Required, StringLength(30)]
        public string Status { get; set; } = TranscriptStatus.Pending;

        [StringLength(50)]
        public string? Language { get; set; } = "en";

        public double DurationSeconds { get; set; } = 0;

        /// <summary>Aggregated plain text for full-text search, AI indexing and quick export.</summary>
        public string? FullText { get; set; }

        /// <summary>Speech-to-text provider name (e.g. Gemini, Whisper, MockFallback).</summary>
        [StringLength(50)]
        public string? Provider { get; set; }

        /// <summary>Internal processing error message (sanitized for students, visible to faculty/admin).</summary>
        [StringLength(1000)]
        public string? ErrorMessage { get; set; }

        public int RetryCount { get; set; } = 0;

        public DateTime? LastProcessedAt { get; set; }

        /// <summary>Cached JSON array of segments for efficient retrieval.</summary>
        public string? SegmentsJson { get; set; }

        public virtual ICollection<LectureTranscriptSegment> Segments { get; set; } = new List<LectureTranscriptSegment>();
    }
}

