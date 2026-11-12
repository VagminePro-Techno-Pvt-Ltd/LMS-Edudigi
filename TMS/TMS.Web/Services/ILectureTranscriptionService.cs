using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMS.ViewModels.Academics;

namespace TMS.Web.Services
{
    public class LectureTranscriptResult
    {
        public bool Success { get; set; }
        public string? Status { get; set; }
        public string? Message { get; set; }
        public int SegmentCount { get; set; }
        public double DurationSeconds { get; set; }
        public string? Language { get; set; }
    }

    public interface ILectureTranscriptionService
    {
        Task EnsureSchemaAsync(CancellationToken ct = default);
        Task<TranscriptStatusDto> GetStatusAsync(int materialId, CancellationToken ct = default);
        Task<LectureTranscriptViewModel?> GetTranscriptAsync(int materialId, CancellationToken ct = default);
        Task<LectureTranscriptResult> TranscribeVideoAsync(int materialId, CancellationToken ct = default, bool forceRegenerate = false);
        Task<LectureTranscriptResult> RegenerateTranscriptAsync(int materialId, CancellationToken ct = default);
        Task<bool> SaveTranscriptEditsAsync(SaveTranscriptEditDto dto, int userId, CancellationToken ct = default);
    }
}

