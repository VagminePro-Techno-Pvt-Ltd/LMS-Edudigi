using System;
using System.Collections.Generic;

namespace TMS.ViewModels.Academics
{
    public class LectureTranscriptViewModel
    {
        public int Id { get; set; }
        public int LectureMaterialId { get; set; }
        public string VideoTitle { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string? Language { get; set; } = "en";
        public double DurationSeconds { get; set; }
        public string? FullText { get; set; }
        public string? Provider { get; set; }
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; }
        public DateTime? LastProcessedAt { get; set; }
        public List<LectureTranscriptSegmentViewModel> Segments { get; set; } = new();
    }

    public class LectureTranscriptSegmentViewModel
    {
        public int Id { get; set; }
        public double StartTimeSeconds { get; set; }
        public double EndTimeSeconds { get; set; }
        public string FormattedStartTime => FormatTimestamp(StartTimeSeconds);
        public string FormattedEndTime => FormatTimestamp(EndTimeSeconds);
        public string Text { get; set; } = string.Empty;
        public int SequenceOrder { get; set; }
        public string? Speaker { get; set; }

        public static string FormatTimestamp(double seconds)
        {
            if (seconds < 0) seconds = 0;
            var ts = TimeSpan.FromSeconds(seconds);
            return ts.TotalHours >= 1
                ? $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}"
                : $"{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
    }

    public class SaveTranscriptEditDto
    {
        public int LectureMaterialId { get; set; }
        public string? Language { get; set; }
        public List<TranscriptSegmentEditItemDto> Segments { get; set; } = new();
    }

    public class TranscriptSegmentEditItemDto
    {
        public int? Id { get; set; }
        public double StartTimeSeconds { get; set; }
        public double EndTimeSeconds { get; set; }
        public string Text { get; set; } = string.Empty;
        public int SequenceOrder { get; set; }
        public string? Speaker { get; set; }
    }

    public class TranscriptStatusDto
    {
        public int LectureMaterialId { get; set; }
        public bool HasTranscript { get; set; }
        public string Status { get; set; } = "None"; // None, Pending, Processing, Ready, Failed
        public string? Language { get; set; }
        public int SegmentCount { get; set; }
        public double DurationSeconds { get; set; }
        public DateTime? LastProcessedAt { get; set; }
    }
}

