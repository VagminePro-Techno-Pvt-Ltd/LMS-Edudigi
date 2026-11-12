using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TMS.Models.Academics;
using TMS.Repository;
using TMS.ViewModels.Academics;

namespace TMS.Web.Services
{
    public class LectureTranscriptionService : ILectureTranscriptionService
    {
        private readonly ApplicationDBContext _db;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<LectureTranscriptionService> _logger;
        private readonly IMemoryCache? _cache;

        private static bool _schemaEnsured = false;
        private static readonly object SchemaLock = new();

        private const string TranscriptionPrompt = @"You are an expert audio transcription system.
Transcribe all spoken dialogue in this lecture video with exact verbatim accuracy.
Support English, Hindi, Hinglish, and regional accents accurately.
Capture what the speaker actually says word-for-word, without summarizing, paraphrasing, omitting sentences, or adding fictional dialogue.
Divide the speech into timestamped segments matching the speaker's natural phrasing and pauses.

Format your response STRICTLY as a valid JSON object matching this structure:
{
  ""language"": ""English"",
  ""duration"": 120.0,
  ""segments"": [
    {
      ""start"": 0.0,
      ""end"": 5.5,
      ""text"": ""Verbatim spoken words here..."",
      ""speaker"": ""Instructor""
    }
  ]
}

If the video contains no spoken words or dialogue, output:
{
  ""language"": ""English"",
  ""duration"": 0.0,
  ""segments"": []
}

Do not include any explanation, conversational text, or markdown codeblocks outside the JSON.";

        public LectureTranscriptionService(
            ApplicationDBContext db,
            HttpClient httpClient,
            IConfiguration configuration,
            IWebHostEnvironment env,
            ILogger<LectureTranscriptionService> logger,
            IMemoryCache? cache = null)
        {
            _db = db;
            _httpClient = httpClient;
            _configuration = configuration;
            _env = env;
            _logger = logger;
            _cache = cache;
        }

        public async Task EnsureSchemaAsync(CancellationToken ct = default)
        {
            if (_schemaEnsured) return;

            lock (SchemaLock)
            {
                if (_schemaEnsured) return;
            }

            try
            {
                if (_db.Database.IsRelational())
                {
                    const string sql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LectureTranscript')
BEGIN
    CREATE TABLE [dbo].[LectureTranscript](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [LectureMaterialId] [int] NOT NULL,
        [Status] [nvarchar](30) NOT NULL DEFAULT 'Pending',
        [Language] [nvarchar](50) NULL,
        [DurationSeconds] [float] NOT NULL DEFAULT 0,
        [FullText] [nvarchar](max) NULL,
        [Provider] [nvarchar](50) NULL,
        [ErrorMessage] [nvarchar](1000) NULL,
        [RetryCount] [int] NOT NULL DEFAULT 0,
        [LastProcessedAt] [datetime2](7) NULL,
        [SegmentsJson] [nvarchar](max) NULL,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [CreatedBy] [int] NULL,
        [CreatedOn] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedOn] [datetime2](7) NULL,
        CONSTRAINT [PK_LectureTranscript] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE NONCLUSTERED INDEX [IX_LectureTranscript_LectureMaterialId] ON [dbo].[LectureTranscript]([LectureMaterialId]);
    CREATE NONCLUSTERED INDEX [IX_LectureTranscript_Status] ON [dbo].[LectureTranscript]([Status]);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LectureTranscriptSegment')
BEGIN
    CREATE TABLE [dbo].[LectureTranscriptSegment](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [LectureTranscriptId] [int] NOT NULL,
        [StartTimeSeconds] [float] NOT NULL DEFAULT 0,
        [EndTimeSeconds] [float] NOT NULL DEFAULT 0,
        [Text] [nvarchar](max) NOT NULL,
        [SequenceOrder] [int] NOT NULL DEFAULT 0,
        [Speaker] [nvarchar](100) NULL,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [CreatedBy] [int] NULL,
        [CreatedOn] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedOn] [datetime2](7) NULL,
        CONSTRAINT [PK_LectureTranscriptSegment] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE NONCLUSTERED INDEX [IX_LectureTranscriptSegment_LectureTranscriptId] ON [dbo].[LectureTranscriptSegment]([LectureTranscriptId]);
    CREATE NONCLUSTERED INDEX [IX_LectureTranscriptSegment_StartTimeSeconds] ON [dbo].[LectureTranscriptSegment]([StartTimeSeconds]);
END;

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LectureTranscript_Material_Active' AND object_id = OBJECT_ID('dbo.LectureTranscript'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_LectureTranscript_Material_Active] ON [dbo].[LectureTranscript]([LectureMaterialId], [IsActive]);
END;

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LectureTranscriptSegment_Transcript_Active' AND object_id = OBJECT_ID('dbo.LectureTranscriptSegment'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_LectureTranscriptSegment_Transcript_Active] ON [dbo].[LectureTranscriptSegment]([LectureTranscriptId], [IsActive], [SequenceOrder]);
END;";
                    await _db.Database.ExecuteSqlRawAsync(sql, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LectureTranscriptionService EnsureSchema note: {Message}", ex.Message);
            }
            finally
            {
                lock (SchemaLock)
                {
                    _schemaEnsured = true;
                }
            }
        }

        public async Task<TranscriptStatusDto> GetStatusAsync(int materialId, CancellationToken ct = default)
        {
            string cacheKey = $"TranscriptStatus_{materialId}";
            if (_cache != null && _cache.TryGetValue(cacheKey, out TranscriptStatusDto? cachedStatus) && cachedStatus != null && cachedStatus.Status == TranscriptStatus.Ready)
            {
                return cachedStatus;
            }

            await EnsureSchemaAsync(ct);

            var transcript = await _db.LectureTranscripts
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.LectureMaterialId == materialId && t.IsActive, ct);

            if (transcript == null)
            {
                return new TranscriptStatusDto
                {
                    LectureMaterialId = materialId,
                    HasTranscript = false,
                    Status = "None"
                };
            }

            int segmentCount = 0;
            if (transcript.Status == TranscriptStatus.Ready)
            {
                segmentCount = await _db.LectureTranscriptSegments
                    .CountAsync(s => s.LectureTranscriptId == transcript.Id && s.IsActive, ct);
            }

            var statusDto = new TranscriptStatusDto
            {
                LectureMaterialId = materialId,
                HasTranscript = transcript.Status == TranscriptStatus.Ready && segmentCount > 0,
                Status = transcript.Status,
                Language = transcript.Language,
                SegmentCount = segmentCount,
                DurationSeconds = transcript.DurationSeconds,
                LastProcessedAt = transcript.LastProcessedAt
            };

            if (_cache != null && statusDto.Status == TranscriptStatus.Ready)
            {
                _cache.Set(cacheKey, statusDto, TimeSpan.FromMinutes(10));
            }

            return statusDto;
        }

        public async Task<LectureTranscriptViewModel?> GetTranscriptAsync(int materialId, CancellationToken ct = default)
        {
            string cacheKey = $"Transcript_{materialId}";
            if (_cache != null && _cache.TryGetValue(cacheKey, out LectureTranscriptViewModel? cached) && cached != null && cached.Status == TranscriptStatus.Ready)
            {
                return cached;
            }

            await EnsureSchemaAsync(ct);

            var material = await _db.LectureMaterials
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == materialId && m.IsActive, ct);

            if (material == null) return null;

            var transcript = await _db.LectureTranscripts
                .AsNoTracking()
                .Include(t => t.Segments.Where(s => s.IsActive).OrderBy(s => s.SequenceOrder).ThenBy(s => s.StartTimeSeconds))
                .FirstOrDefaultAsync(t => t.LectureMaterialId == materialId && t.IsActive, ct);

            if (transcript == null)
            {
                return new LectureTranscriptViewModel
                {
                    LectureMaterialId = materialId,
                    VideoTitle = material.Title ?? "Lecture Video",
                    Status = "None"
                };
            }

            var segmentsList = transcript.Segments?
                .Where(s => s.IsActive)
                .OrderBy(s => s.SequenceOrder)
                .ThenBy(s => s.StartTimeSeconds)
                .Select(s => new LectureTranscriptSegmentViewModel
                {
                    Id = s.Id,
                    StartTimeSeconds = s.StartTimeSeconds,
                    EndTimeSeconds = s.EndTimeSeconds,
                    Text = s.Text,
                    SequenceOrder = s.SequenceOrder,
                    Speaker = s.Speaker
                }).ToList() ?? new List<LectureTranscriptSegmentViewModel>();

            // Resilient fallback: If relational segments collection is empty but SegmentsJson is present, deserialize SegmentsJson
            if (!segmentsList.Any() && !string.IsNullOrWhiteSpace(transcript.SegmentsJson))
            {
                try
                {
                    var jsonSegs = JsonSerializer.Deserialize<List<TranscriptSegmentModel>>(transcript.SegmentsJson);
                    if (jsonSegs != null && jsonSegs.Any())
                    {
                        int seq = 1;
                        segmentsList = jsonSegs.Select(s => new LectureTranscriptSegmentViewModel
                        {
                            StartTimeSeconds = s.StartTimeSeconds,
                            EndTimeSeconds = s.EndTimeSeconds,
                            Text = s.Text,
                            SequenceOrder = seq++,
                            Speaker = s.Speaker
                        }).ToList();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse SegmentsJson fallback for transcript {Id}", transcript.Id);
                }
            }

            var result = new LectureTranscriptViewModel
            {
                Id = transcript.Id,
                LectureMaterialId = transcript.LectureMaterialId,
                VideoTitle = material.Title ?? "Lecture Video",
                Status = transcript.Status,
                Language = transcript.Language ?? "en",
                DurationSeconds = transcript.DurationSeconds,
                FullText = transcript.FullText,
                Provider = transcript.Provider,
                ErrorMessage = transcript.ErrorMessage,
                RetryCount = transcript.RetryCount,
                LastProcessedAt = transcript.LastProcessedAt,
                Segments = segmentsList
            };

            if (_cache != null && result.Status == TranscriptStatus.Ready && result.Segments.Any())
            {
                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));
            }

            return result;
        }

        public async Task<LectureTranscriptResult> TranscribeVideoAsync(int materialId, CancellationToken ct = default, bool forceRegenerate = false)
        {
            await EnsureSchemaAsync(ct);

            var material = await _db.LectureMaterials
                .FirstOrDefaultAsync(m => m.Id == materialId && m.IsActive, ct);

            if (material == null)
            {
                return new LectureTranscriptResult
                {
                    Success = false,
                    Message = "Lecture material not found."
                };
            }

            // Only transcribe video types
            bool isVideo = material.MaterialType == "Video" || material.MaterialType == "VideoURL";
            if (!isVideo)
            {
                return new LectureTranscriptResult
                {
                    Success = false,
                    Message = "Material is not a video format."
                };
            }

            // Check existing transcript record
            var transcript = await _db.LectureTranscripts
                .FirstOrDefaultAsync(t => t.LectureMaterialId == materialId && t.IsActive, ct);

            // Preserve faculty manual edits unless explicitly force-regenerated
            if (transcript != null && !forceRegenerate && transcript.Provider == "ManualEdit" && transcript.Status == TranscriptStatus.Ready)
            {
                int segCount = await _db.LectureTranscriptSegments.CountAsync(s => s.LectureTranscriptId == transcript.Id && s.IsActive, ct);
                return new LectureTranscriptResult
                {
                    Success = true,
                    Status = TranscriptStatus.Ready,
                    SegmentCount = segCount,
                    DurationSeconds = transcript.DurationSeconds,
                    Language = transcript.Language,
                    Message = "Preserved faculty manual edits."
                };
            }

            if (transcript == null)
            {
                transcript = new LectureTranscript
                {
                    LectureMaterialId = materialId,
                    Status = TranscriptStatus.Processing,
                    CreatedBy = material.CreatedBy,
                    CreatedOn = DateTime.UtcNow,
                    IsActive = true
                };
                _db.LectureTranscripts.Add(transcript);
                await _db.SaveChangesAsync(ct);
                _cache?.Remove($"Transcript_{materialId}");
                _cache?.Remove($"TranscriptStatus_{materialId}");
            }
            else
            {
                // Concurrency safeguard: if already processing recently (< 3 min ago), don't duplicate
                if (!forceRegenerate &&
                    transcript.Status == TranscriptStatus.Processing &&
                    transcript.LastProcessedAt.HasValue &&
                    DateTime.UtcNow - transcript.LastProcessedAt.Value < TimeSpan.FromMinutes(3))
                {
                    return new LectureTranscriptResult
                    {
                        Success = true,
                        Status = TranscriptStatus.Processing,
                        Message = "Transcription is already in progress."
                    };
                }

                transcript.Status = TranscriptStatus.Processing;
                transcript.ErrorMessage = null;
                transcript.LastProcessedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
                _cache?.Remove($"Transcript_{materialId}");
                _cache?.Remove($"TranscriptStatus_{materialId}");
            }

            try
            {
                // Execute speech-to-text with configured providers
                var (success, language, duration, segments, provider, error) = await ExecuteSpeechToTextAsync(material, ct);

                if (!success || segments == null || !segments.Any())
                {
                    transcript.Status = TranscriptStatus.Failed;
                    transcript.ErrorMessage = error ?? "Transcription provider could not transcribe audio.";
                    transcript.RetryCount++;
                    transcript.LastProcessedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync(ct);
                _db.ChangeTracker.Clear();
                _cache?.Remove($"Transcript_{materialId}");
                _cache?.Remove($"TranscriptStatus_{materialId}");

                    return new LectureTranscriptResult
                    {
                        Success = false,
                        Status = TranscriptStatus.Failed,
                        Message = transcript.ErrorMessage
                    };
                }

                // Success: persist segments and update status
                transcript.Status = TranscriptStatus.Ready;
                transcript.Language = language ?? "en";
                transcript.DurationSeconds = duration > 0 ? duration : (segments.Max(s => s.EndTimeSeconds));
                transcript.Provider = provider;
                transcript.ErrorMessage = null;
                transcript.LastProcessedAt = DateTime.UtcNow;

                var fullTextBuilder = new StringBuilder();
                foreach (var seg in segments)
                {
                    fullTextBuilder.AppendLine(seg.Text);
                }
                transcript.FullText = fullTextBuilder.ToString();
                transcript.SegmentsJson = JsonSerializer.Serialize(segments);

                // Remove existing segments if any
                var existingSegments = await _db.LectureTranscriptSegments
                    .Where(s => s.LectureTranscriptId == transcript.Id)
                    .ToListAsync(ct);
                _db.LectureTranscriptSegments.RemoveRange(existingSegments);

                // Insert new segments
                int seq = 1;
                foreach (var seg in segments.OrderBy(s => s.StartTimeSeconds))
                {
                    _db.LectureTranscriptSegments.Add(new LectureTranscriptSegment
                    {
                        LectureTranscriptId = transcript.Id,
                        StartTimeSeconds = seg.StartTimeSeconds,
                        EndTimeSeconds = seg.EndTimeSeconds,
                        Text = seg.Text,
                        SequenceOrder = seq++,
                        Speaker = seg.Speaker ?? "Speaker",
                        CreatedBy = material.CreatedBy,
                        CreatedOn = DateTime.UtcNow,
                        IsActive = true
                    });
                }

                await _db.SaveChangesAsync(ct);
                _db.ChangeTracker.Clear();
                _cache?.Remove($"Transcript_{materialId}");
                _cache?.Remove($"TranscriptStatus_{materialId}");

                _logger.LogInformation("Successfully generated real transcript for video material {Id} ({Count} segments, Provider={Provider}).", materialId, segments.Count, provider);

                return new LectureTranscriptResult
                {
                    Success = true,
                    Status = TranscriptStatus.Ready,
                    SegmentCount = segments.Count,
                    DurationSeconds = transcript.DurationSeconds,
                    Language = transcript.Language,
                    Message = "Transcript generated successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during transcription of material {Id}", materialId);

                transcript.Status = TranscriptStatus.Failed;
                transcript.ErrorMessage = "Transcription processing failed: " + ex.Message;
                transcript.RetryCount++;
                transcript.LastProcessedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
                _cache?.Remove($"Transcript_{materialId}");
                _cache?.Remove($"TranscriptStatus_{materialId}");

                return new LectureTranscriptResult
                {
                    Success = false,
                    Status = TranscriptStatus.Failed,
                    Message = transcript.ErrorMessage
                };
            }
        }

        public async Task<LectureTranscriptResult> RegenerateTranscriptAsync(int materialId, CancellationToken ct = default)
        {
            await EnsureSchemaAsync(ct);

            var transcript = await _db.LectureTranscripts
                .FirstOrDefaultAsync(t => t.LectureMaterialId == materialId && t.IsActive, ct);

            if (transcript != null)
            {
                transcript.Status = TranscriptStatus.Pending;
                transcript.ErrorMessage = null;
                transcript.Provider = null;
                await _db.SaveChangesAsync(ct);
                _cache?.Remove($"Transcript_{materialId}");
                _cache?.Remove($"TranscriptStatus_{materialId}");
            }

            return await TranscribeVideoAsync(materialId, ct, forceRegenerate: true);
        }

        public async Task<bool> SaveTranscriptEditsAsync(SaveTranscriptEditDto dto, int userId, CancellationToken ct = default)
        {
            if (dto == null || dto.LectureMaterialId <= 0 || dto.Segments == null)
                return false;

            await EnsureSchemaAsync(ct);

            var transcript = await _db.LectureTranscripts
                .Include(t => t.Segments)
                .FirstOrDefaultAsync(t => t.LectureMaterialId == dto.LectureMaterialId && t.IsActive, ct);

            if (transcript == null)
            {
                // Create transcript if none exists yet
                transcript = new LectureTranscript
                {
                    LectureMaterialId = dto.LectureMaterialId,
                    Status = TranscriptStatus.Ready,
                    Language = dto.Language ?? "en",
                    Provider = "ManualEdit",
                    CreatedBy = userId,
                    CreatedOn = DateTime.UtcNow,
                    IsActive = true
                };
                _db.LectureTranscripts.Add(transcript);
                await _db.SaveChangesAsync(ct);
                _cache?.Remove($"Transcript_{dto.LectureMaterialId}");
                _cache?.Remove($"TranscriptStatus_{dto.LectureMaterialId}");
            }

            transcript.Language = dto.Language ?? transcript.Language ?? "en";
            transcript.Status = TranscriptStatus.Ready;
            transcript.Provider = "ManualEdit";
            transcript.UpdatedBy = userId;
            transcript.UpdatedOn = DateTime.UtcNow;

            // Remove existing segments
            var oldSegments = await _db.LectureTranscriptSegments
                .Where(s => s.LectureTranscriptId == transcript.Id)
                .ToListAsync(ct);
            _db.LectureTranscriptSegments.RemoveRange(oldSegments);

            // Add updated segments
            var fullTextBuilder = new StringBuilder();
            int seq = 1;
            var segmentsList = new List<TranscriptSegmentEditItemDto>();

            foreach (var item in dto.Segments.OrderBy(s => s.StartTimeSeconds))
            {
                if (string.IsNullOrWhiteSpace(item.Text)) continue;

                var seg = new LectureTranscriptSegment
                {
                    LectureTranscriptId = transcript.Id,
                    StartTimeSeconds = Math.Max(0, item.StartTimeSeconds),
                    EndTimeSeconds = Math.Max(item.StartTimeSeconds, item.EndTimeSeconds),
                    Text = item.Text.Trim(),
                    SequenceOrder = seq++,
                    Speaker = string.IsNullOrWhiteSpace(item.Speaker) ? "Speaker" : item.Speaker.Trim(),
                    CreatedBy = userId,
                    CreatedOn = DateTime.UtcNow,
                    IsActive = true
                };

                _db.LectureTranscriptSegments.Add(seg);
                fullTextBuilder.AppendLine(seg.Text);
                segmentsList.Add(item);
            }

            transcript.FullText = fullTextBuilder.ToString();
            transcript.SegmentsJson = JsonSerializer.Serialize(segmentsList);
            if (dto.Segments.Any())
            {
                transcript.DurationSeconds = dto.Segments.Max(s => s.EndTimeSeconds);
            }

            await _db.SaveChangesAsync(ct);
            _db.ChangeTracker.Clear();
            _cache?.Remove($"Transcript_{dto.LectureMaterialId}");
            _cache?.Remove($"TranscriptStatus_{dto.LectureMaterialId}");
            return true;
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  SPEECH-TO-TEXT PROVIDER LOGIC
        // ═══════════════════════════════════════════════════════════════════════

        private async Task<(bool Success, string Language, double Duration, List<TranscriptSegmentModel> Segments, string Provider, string? Error)>
            ExecuteSpeechToTextAsync(TMS.Models.Masters.LectureMaterial material, CancellationToken ct)
        {
            var configuredProvider = (_configuration["LectureTranscription:Provider"] ?? "Gemini").Trim();

            // 1. Try Gemini Multimodal STT (supports YouTube URLs and local video files up to 2GB)
            if (configuredProvider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
            {
                var geminiResult = await TryGeminiSpeechToTextAsync(material, ct);
                if (geminiResult.Success)
                    return geminiResult;

                _logger.LogWarning("Gemini speech-to-text attempt failed for material {Id}: {Error}.", material.Id, geminiResult.Error);
                return geminiResult;
            }

            // 2. Try Whisper STT if configured
            if (configuredProvider.Equals("Whisper", StringComparison.OrdinalIgnoreCase))
            {
                var whisperResult = await TryWhisperSpeechToTextAsync(material, ct);
                if (whisperResult.Success)
                    return whisperResult;

                _logger.LogWarning("Whisper speech-to-text attempt failed for material {Id}: {Error}.", material.Id, whisperResult.Error);
                return whisperResult;
            }

            return (false, "en", 0, new List<TranscriptSegmentModel>(), "None", $"Provider '{configuredProvider}' is not supported or failed.");
        }

        private async Task<(bool Success, string Language, double Duration, List<TranscriptSegmentModel> Segments, string Provider, string? Error)>
            TryGeminiSpeechToTextAsync(TMS.Models.Masters.LectureMaterial material, CancellationToken ct)
        {
            try
            {
                var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    apiKey = _configuration["Gemini:ApiKey"] ?? _configuration["GEMINI_API_KEY"];
                }

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return (false, "en", 0, new List<TranscriptSegmentModel>(), "Gemini", "Gemini API key is not configured.");
                }

                var model = _configuration["LectureTranscription:GeminiModel"]
                    ?? _configuration["Gemini:Model"]
                    ?? "gemini-3.8-flash";

                // Check if this material is a web video URL (YouTube, external link)
                string? youtubeUrl = NormalizeVideoUrl(material.FilePath);
                if (material.MaterialType == "VideoURL" || !string.IsNullOrEmpty(youtubeUrl))
                {
                    string targetUrl = youtubeUrl ?? material.FilePath ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(targetUrl))
                    {
                        return (false, "en", 0, new List<TranscriptSegmentModel>(), "Gemini", "Video URL is empty.");
                    }

                    _logger.LogInformation("Processing video URL transcription via Gemini for material {Id}: {Url}", material.Id, targetUrl);
                    string? ytMimeType = !string.IsNullOrEmpty(youtubeUrl) ? "video/youtube" : "video/mp4";
                    return await ExecuteGeminiGenerateContentWithFileUriAsync(apiKey, model, targetUrl, ytMimeType, ct);
                }

                // Otherwise, resolve physical local file
                string? physicalPath = null;
                if (!string.IsNullOrWhiteSpace(material.FilePath))
                {
                    string relPath = material.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                    physicalPath = Path.Combine(_env.WebRootPath, relPath);
                }

                if (physicalPath == null || !File.Exists(physicalPath))
                {
                    return (false, "en", 0, new List<TranscriptSegmentModel>(), "Gemini", "Local video file not found on server.");
                }

                var fileInfo = new FileInfo(physicalPath);
                string mimeType = GetVideoMimeType(physicalPath, material.MimeType);

                // For small files <= 15MB: inline base64 is fastest
                if (fileInfo.Length <= 15 * 1024 * 1024)
                {
                    _logger.LogInformation("Processing small video inline ({Size} bytes) via Gemini for material {Id}", fileInfo.Length, material.Id);
                    byte[] fileBytes = await File.ReadAllBytesAsync(physicalPath, ct);
                    string base64Data = Convert.ToBase64String(fileBytes);

                    var requestPayload = new
                    {
                        contents = new[]
                        {
                            new
                            {
                                parts = new object[]
                                {
                                    new
                                    {
                                        inline_data = new
                                        {
                                            mime_type = mimeType,
                                            data = base64Data
                                        }
                                    },
                                    new
                                    {
                                        text = TranscriptionPrompt
                                    }
                                }
                            }
                        },
                        generationConfig = new
                        {
                            response_mime_type = "application/json",
                            temperature = 0.1
                        }
                    };

                    string requestUri = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent?key={apiKey}";
                    using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri);
                    httpRequest.Content = new StringContent(JsonSerializer.Serialize(requestPayload), Encoding.UTF8, "application/json");

                    var response = await _httpClient.SendAsync(httpRequest, ct);
                    if (!response.IsSuccessStatusCode)
                    {
                        var errBody = await response.Content.ReadAsStringAsync(ct);
                        return (false, "en", 0, new List<TranscriptSegmentModel>(), "Gemini", $"Gemini API HTTP {(int)response.StatusCode}: {errBody}");
                    }

                    var responseJson = await response.Content.ReadAsStringAsync(ct);
                    return ParseGeminiResponse(responseJson);
                }

                // For large files (> 15MB up to 2GB): use Google Gemini Resumable File Upload API
                _logger.LogInformation("Processing large video ({Size} bytes) via Gemini Resumable File API for material {Id}", fileInfo.Length, material.Id);
                return await UploadAndTranscribeViaGeminiFileApiAsync(apiKey, model, physicalPath, fileInfo.Length, mimeType, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in TryGeminiSpeechToTextAsync for material {Id}", material.Id);
                return (false, "en", 0, new List<TranscriptSegmentModel>(), "Gemini", ex.Message);
            }
        }

        private async Task<(bool Success, string Language, double Duration, List<TranscriptSegmentModel> Segments, string Provider, string? Error)>
            UploadAndTranscribeViaGeminiFileApiAsync(string apiKey, string model, string physicalPath, long fileSizeBytes, string mimeType, CancellationToken ct)
        {
            string? fileResourceName = null;
            try
            {
                // Step 1: Initiate resumable upload session
                string initUrl = $"https://generativelanguage.googleapis.com/upload/v1beta/files?key={apiKey}";
                using var initReq = new HttpRequestMessage(HttpMethod.Post, initUrl);
                initReq.Headers.Add("X-Goog-Upload-Protocol", "resumable");
                initReq.Headers.Add("X-Goog-Upload-Command", "start");
                initReq.Headers.Add("X-Goog-Upload-Header-Content-Length", fileSizeBytes.ToString());
                initReq.Headers.Add("X-Goog-Upload-Header-Content-Type", mimeType);

                var metadata = new { file = new { display_name = Path.GetFileName(physicalPath) } };
                initReq.Content = new StringContent(JsonSerializer.Serialize(metadata), Encoding.UTF8, "application/json");

                var initRes = await _httpClient.SendAsync(initReq, ct);
                if (!initRes.IsSuccessStatusCode)
                {
                    var initErr = await initRes.Content.ReadAsStringAsync(ct);
                    return (false, "en", 0, new List<TranscriptSegmentModel>(), "Gemini", $"Gemini upload initialization failed: {initErr}");
                }

                if (!initRes.Headers.TryGetValues("X-Goog-Upload-URL", out var uploadUrlHeaders))
                {
                    return (false, "en", 0, new List<TranscriptSegmentModel>(), "Gemini", "Gemini did not return an upload URL.");
                }

                string uploadUrl = uploadUrlHeaders.First();

                // Step 2: Upload file stream directly to upload URL
                using var fileStream = new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
                using var uploadReq = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
                uploadReq.Headers.Add("X-Goog-Upload-Offset", "0");
                uploadReq.Headers.Add("X-Goog-Upload-Command", "upload, finalize");
                uploadReq.Content = new StreamContent(fileStream);
                uploadReq.Content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
                uploadReq.Content.Headers.ContentLength = fileSizeBytes;

                var uploadRes = await _httpClient.SendAsync(uploadReq, ct);
                if (!uploadRes.IsSuccessStatusCode)
                {
                    var uploadErr = await uploadRes.Content.ReadAsStringAsync(ct);
                    return (false, "en", 0, new List<TranscriptSegmentModel>(), "Gemini", $"Gemini file upload failed: {uploadErr}");
                }

                var uploadJson = await uploadRes.Content.ReadAsStringAsync(ct);
                var uploadDoc = JsonNode.Parse(uploadJson);
                fileResourceName = uploadDoc?["file"]?["name"]?.GetValue<string>(); // e.g. "files/xyz123"
                string? fileUri = uploadDoc?["file"]?["uri"]?.GetValue<string>();
                string fileState = uploadDoc?["file"]?["state"]?.GetValue<string>() ?? "PROCESSING";

                if (string.IsNullOrWhiteSpace(fileResourceName))
                {
                    return (false, "en", 0, new List<TranscriptSegmentModel>(), "Gemini", "Gemini file upload did not return a resource name.");
                }

                // Step 3: Wait for file to become ACTIVE
                int maxWaitSeconds = 240;
                var sw = Stopwatch.StartNew();
                while (fileState == "PROCESSING" && sw.Elapsed.TotalSeconds < maxWaitSeconds)
                {
                    await Task.Delay(3000, ct);
                    string statusUrl = $"https://generativelanguage.googleapis.com/v1beta/{fileResourceName}?key={apiKey}";
                    var statusRes = await _httpClient.GetAsync(statusUrl, ct);
                    if (statusRes.IsSuccessStatusCode)
                    {
                        var statusJson = await statusRes.Content.ReadAsStringAsync(ct);
                        var statusDoc = JsonNode.Parse(statusJson);
                        fileState = statusDoc?["state"]?.GetValue<string>() ?? "PROCESSING";
                        fileUri = statusDoc?["uri"]?.GetValue<string>() ?? fileUri;
                    }
                }

                if (fileState != "ACTIVE" || string.IsNullOrWhiteSpace(fileUri))
                {
                    return (false, "en", 0, new List<TranscriptSegmentModel>(), "Gemini", $"Gemini video processing did not complete (state={fileState}).");
                }

                // Step 4: Generate transcript with active file URI
                return await ExecuteGeminiGenerateContentWithFileUriAsync(apiKey, model, fileUri, mimeType, ct);
            }
            finally
            {
                // Purge temporary remote file in finally block
                if (!string.IsNullOrWhiteSpace(fileResourceName))
                {
                    try
                    {
                        string deleteUrl = $"https://generativelanguage.googleapis.com/v1beta/{fileResourceName}?key={apiKey}";
                        using var delReq = new HttpRequestMessage(HttpMethod.Delete, deleteUrl);
                        await _httpClient.SendAsync(delReq, CancellationToken.None);
                        _logger.LogInformation("Cleaned up Gemini remote file resource: {Resource}", fileResourceName);
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogWarning(cleanupEx, "Failed to purge Gemini file {Resource}: {Msg}", fileResourceName, cleanupEx.Message);
                    }
                }
            }
        }

        private async Task<(bool Success, string Language, double Duration, List<TranscriptSegmentModel> Segments, string Provider, string? Error)>
            ExecuteGeminiGenerateContentWithFileUriAsync(string apiKey, string model, string fileUri, string? mimeType, CancellationToken ct)
        {
            var fileDataPart = new JsonObject
            {
                ["file_uri"] = fileUri
            };
            if (!string.IsNullOrWhiteSpace(mimeType))
            {
                fileDataPart["mime_type"] = mimeType;
            }

            var requestPayload = new JsonObject
            {
                ["contents"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["parts"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["file_data"] = fileDataPart
                            },
                            new JsonObject
                            {
                                ["text"] = TranscriptionPrompt
                            }
                        }
                    }
                },
                ["generationConfig"] = new JsonObject
                {
                    ["response_mime_type"] = "application/json",
                    ["temperature"] = 0.1
                }
            };

            string requestUri = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent?key={apiKey}";
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri);
            httpRequest.Content = new StringContent(requestPayload.ToJsonString(), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(httpRequest, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errBody = await response.Content.ReadAsStringAsync(ct);
                return (false, "en", 0, new List<TranscriptSegmentModel>(), "Gemini", $"Gemini API HTTP {(int)response.StatusCode}: {errBody}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            return ParseGeminiResponse(responseJson);
        }

        private (bool Success, string Language, double Duration, List<TranscriptSegmentModel> Segments, string Provider, string? Error)
            ParseGeminiResponse(string rawJson)
        {
            try
            {
                var doc = JsonNode.Parse(rawJson);
                var candidates = doc?["candidates"]?.AsArray();
                var textContent = candidates?.FirstOrDefault()?["content"]?["parts"]?.AsArray()?.FirstOrDefault()?["text"]?.GetValue<string>();

                if (string.IsNullOrWhiteSpace(textContent))
                {
                    return (false, "en", 0, new List<TranscriptSegmentModel>(), "Gemini", "Empty response text from Gemini.");
                }

                // Clean potential markdown wrappers
                string cleanJson = textContent.Trim();
                if (cleanJson.StartsWith("```json", StringComparison.OrdinalIgnoreCase)) cleanJson = cleanJson[7..];
                if (cleanJson.StartsWith("```", StringComparison.OrdinalIgnoreCase)) cleanJson = cleanJson[3..];
                if (cleanJson.EndsWith("```", StringComparison.OrdinalIgnoreCase)) cleanJson = cleanJson[..^3];
                cleanJson = cleanJson.Trim();

                var parsed = JsonNode.Parse(cleanJson);
                string language = parsed?["language"]?.GetValue<string>() ?? "English";
                double duration = parsed?["duration"]?.GetValue<double>() ?? 0;
                var rawSegments = parsed?["segments"]?.AsArray();

                var segments = new List<TranscriptSegmentModel>();
                if (rawSegments != null)
                {
                    foreach (var s in rawSegments)
                    {
                        double start = s?["start"]?.GetValue<double>() ?? 0;
                        double end = s?["end"]?.GetValue<double>() ?? (start + 4);
                        string text = s?["text"]?.GetValue<string>() ?? "";
                        string speaker = s?["speaker"]?.GetValue<string>() ?? "Speaker";

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            segments.Add(new TranscriptSegmentModel
                            {
                                StartTimeSeconds = Math.Round(start, 2),
                                EndTimeSeconds = Math.Round(Math.Max(start, end), 2),
                                Text = text.Trim(),
                                Speaker = speaker
                            });
                        }
                    }
                }

                // Silent video with no spoken dialogue
                if (!segments.Any())
                {
                    segments.Add(new TranscriptSegmentModel
                    {
                        StartTimeSeconds = 0,
                        EndTimeSeconds = duration > 0 ? duration : 5.0,
                        Text = "(No spoken dialogue detected in this video.)",
                        Speaker = "System"
                    });
                }

                if (duration <= 0 && segments.Any())
                {
                    duration = segments.Max(s => s.EndTimeSeconds);
                }

                return (true, language, duration, segments, "Gemini", null);
            }
            catch (Exception ex)
            {
                return (false, "en", 0, new List<TranscriptSegmentModel>(), "Gemini", "Error parsing Gemini STT JSON: " + ex.Message);
            }
        }

        private async Task<(bool Success, string Language, double Duration, List<TranscriptSegmentModel> Segments, string Provider, string? Error)>
            TryWhisperSpeechToTextAsync(TMS.Models.Masters.LectureMaterial material, CancellationToken ct)
        {
            var apiKey = _configuration["Transcription:WhisperApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return (false, "en", 0, new List<TranscriptSegmentModel>(), "Whisper", "Whisper API key not configured.");
            }

            return (false, "en", 0, new List<TranscriptSegmentModel>(), "Whisper", "Whisper STT not activated.");
        }

        private static string? NormalizeVideoUrl(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            input = input.Trim();

            // Match YouTube URLs
            var ytMatch = Regex.Match(input, @"(?:youtu\.be\/|youtube\.com\/(?:embed\/|v\/|watch\?v=|watch\?.+&v=))([\w\-]{11})", RegexOptions.IgnoreCase);
            if (ytMatch.Success)
            {
                string videoId = ytMatch.Groups[1].Value;
                return $"https://www.youtube.com/watch?v={videoId}";
            }

            if (input.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                input.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return input;
            }

            return null;
        }

        private static string GetVideoMimeType(string filePath, string? fallbackMimeType)
        {
            if (!string.IsNullOrWhiteSpace(fallbackMimeType) && fallbackMimeType.StartsWith("video/"))
            {
                return fallbackMimeType;
            }

            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext switch
            {
                ".mp4" => "video/mp4",
                ".webm" => "video/webm",
                ".mov" => "video/quicktime",
                ".m4v" => "video/x-m4v",
                ".avi" => "video/x-msvideo",
                ".mkv" => "video/x-matroska",
                ".mp3" => "audio/mp3",
                ".m4a" => "audio/m4a",
                _ => "video/mp4"
            };
        }
    }

    public class TranscriptSegmentModel
    {
        public double StartTimeSeconds { get; set; }
        public double EndTimeSeconds { get; set; }
        public string Text { get; set; } = string.Empty;
        public string? Speaker { get; set; }
    }
}
