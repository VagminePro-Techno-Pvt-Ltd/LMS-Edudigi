using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TMS.Models.LearningAssistant;
using TMS.Repository;
using TMS.ViewModels.LearningAssistant;

namespace TMS.Web.Services
{
    public class LearningAssistantCacheService : ILearningAssistantCacheService
    {
        private readonly ApplicationDBContext _db;
        private readonly IStudentLearningService _learningService;
        private readonly IConfiguration _config;
        private readonly ILogger<LearningAssistantCacheService>? _logger;

        // L1 In-Memory Cache
        private static readonly ConcurrentDictionary<string, MemoryCachedItem> MemoryCache = new();

        // In-Flight Request Coalescing (Singleflight)
        private static readonly ConcurrentDictionary<string, Task<LearningAssistantResponse>> InFlightRequests = new();

        // Thread-safe Metrics Counters
        private static long _totalQueries;
        private static long _cacheHits;
        private static long _cacheMisses;
        private static long _geminiApiCalls;
        private static long _tokensSaved;

        // Auto-DDL schema initialization flag and semaphore
        private static readonly SemaphoreSlim _tableInitLock = new(1, 1);
        private static bool _tableChecked;

        // Common spelling fixes for academic keywords
        private static readonly Dictionary<string, string> TypoDictionary = new(StringComparer.OrdinalIgnoreCase)
        {
            { "bussiness", "business" },
            { "buisness", "business" },
            { "bussines", "business" },
            { "attendence", "attendance" },
            { "attendace", "attendance" },
            { "atendance", "attendance" },
            { "accouting", "accounting" },
            { "acounting", "accounting" },
            { "mangement", "management" },
            { "managment", "management" },
            { "finanical", "financial" },
            { "statment", "statement" },
            { "calender", "calendar" },
            { "asignment", "assignment" },
            { "asignments", "assignments" },
            { "skedule", "schedule" },
            { "progres", "progress" },
            { "computr", "computer" },
            { "programing", "programming" },
            { "bookeping", "bookkeeping" },
            { "bookeeping", "bookkeeping" }
        };

        // Educational prefixes to normalize (so "what is business" and "define business" share the same cache entry)
        private static readonly string[] EducationalPrefixes = new[]
        {
            "what is the concept of ",
            "what is the definition of ",
            "what is the meaning of ",
            "what do you mean by ",
            "can you explain to me ",
            "could you explain to me ",
            "give me information on ",
            "give an overview of ",
            "tell me something about ",
            "tell me about ",
            "please explain ",
            "can you explain ",
            "could you explain ",
            "help me understand ",
            "what is ",
            "whats is ",
            "what's ",
            "what are ",
            "whats are ",
            "define ",
            "explain ",
            "describe ",
            "summary of ",
            "summarize ",
            "concept of "
        };

        private static readonly string[] EducationalSuffixes = new[]
        {
            " please",
            " in detail",
            " step by step",
            " briefly",
            " with example",
            " with examples"
        };

        public LearningAssistantCacheService(
            ApplicationDBContext db,
            IStudentLearningService learningService,
            IConfiguration config,
            ILogger<LearningAssistantCacheService>? logger = null)
        {
            _db = db;
            _learningService = learningService;
            _config = config;
            _logger = logger;
        }

        public string NormalizeQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return string.Empty;

            var text = query.Trim().ToLowerInvariant();

            // 1. Remove all common punctuation
            var sb = new StringBuilder();
            foreach (var ch in text)
            {
                if (!char.IsPunctuation(ch) && ch != '؟')
                {
                    sb.Append(ch);
                }
                else
                {
                    sb.Append(' ');
                }
            }
            text = sb.ToString();

            // 2. Normalize whitespace
            text = Regex.Replace(text, @"\s+", " ").Trim();

            // 3. Fix unambiguous typos word-by-word
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                if (TypoDictionary.TryGetValue(words[i], out var corrected))
                {
                    words[i] = corrected;
                }
            }
            text = string.Join(" ", words);

            // 4. Strip conversational carrier prefixes
            var core = text;
            foreach (var prefix in EducationalPrefixes)
            {
                if (core.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    core = core.Substring(prefix.Length).Trim();
                    break;
                }
            }

            // 5. Strip conversational carrier suffixes (loop to catch compound suffixes like "please in detail")
            bool strippedSuffix;
            do
            {
                strippedSuffix = false;
                foreach (var suffix in EducationalSuffixes)
                {
                    if (core.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    {
                        core = core.Substring(0, core.Length - suffix.Length).Trim();
                        strippedSuffix = true;
                        break;
                    }
                }
            } while (strippedSuffix);

            // If stripping leaves nothing, keep the normalized text
            return string.IsNullOrWhiteSpace(core) ? text : core;
        }

        public string GenerateCacheKey(
            string normalizedQuery,
            string language = "en",
            int? courseId = null,
            int? materialId = null,
            string? documentHash = null,
            string modelName = "gemini-1.5-flash",
            string promptVersion = "v1")
        {
            var cid = courseId.HasValue && courseId.Value > 0 ? courseId.Value.ToString() : "0";
            var mid = materialId.HasValue && materialId.Value > 0 ? materialId.Value.ToString() : "0";
            var dhash = !string.IsNullOrWhiteSpace(documentHash) ? documentHash.Trim() : "none";
            var lang = !string.IsNullOrWhiteSpace(language) ? language.Trim().ToLowerInvariant() : "en";
            var model = !string.IsNullOrWhiteSpace(modelName) ? modelName.Trim().ToLowerInvariant() : "gemini-1.5-flash";

            var rawKey = $"{promptVersion}:{model}:{lang}:{cid}:{mid}:{dhash}:{normalizedQuery.Trim()}";

            using var sha = SHA256.Create();
            var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(rawKey));
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        public async Task<LearningAssistantResponse?> GetCachedAnswerAsync(
            string cacheKey,
            int studentId,
            string userRole,
            int? courseId = null,
            int? materialId = null,
            CancellationToken ct = default)
        {
            Interlocked.Increment(ref _totalQueries);

            // 1. Check L1 In-Memory Cache
            if (MemoryCache.TryGetValue(cacheKey, out var memItem))
            {
                if (memItem.ExpiresOn > DateTime.UtcNow)
                {
                    if (IsRefusalOrUnhelpfulResponse(memItem.Response?.Reply))
                    {
                        MemoryCache.TryRemove(cacheKey, out _);
                    }
                    else
                    {
                        bool authorized = await VerifyAuthorizationAsync(studentId, userRole, memItem.CourseId, memItem.LectureMaterialId);
                        if (authorized)
                        {
                            Interlocked.Increment(ref _cacheHits);
                            Interlocked.Add(ref _tokensSaved, memItem.TotalTokens);
                            _logger?.LogDebug("Cache HIT (L1 Memory) for key {CacheKey}", cacheKey);
                            return CloneResponse(memItem.Response, memItem.TotalTokens);
                        }
                        else
                        {
                            _logger?.LogWarning("Security check failed for cached answer {CacheKey} by student {StudentId}", cacheKey, studentId);
                            return null;
                        }
                    }
                }
                else
                {
                    MemoryCache.TryRemove(cacheKey, out _);
                }
            }

            // 2. Check L2 Persistent Database Cache
            try
            {
                await EnsureTableExistsAsync(ct);

                var dbItem = await _db.LearningAssistantAnswerCaches
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CacheKey == cacheKey && c.IsActive && c.ExpiresOn > DateTime.UtcNow, ct);

                if (dbItem != null)
                {
                    if (IsRefusalOrUnhelpfulResponse(dbItem.Reply))
                    {
                        _logger?.LogInformation("Discarding unhelpful cached refusal for key {CacheKey}", cacheKey);
                        _ = InvalidateDbCacheEntryAsync(dbItem.Id);
                    }
                    else
                    {
                        bool authorized = await VerifyAuthorizationAsync(studentId, userRole, dbItem.CourseId, dbItem.LectureMaterialId);
                        if (authorized)
                        {
                            Interlocked.Increment(ref _cacheHits);
                            Interlocked.Add(ref _tokensSaved, dbItem.TotalTokens);

                            var resp = DeserializeResponse(dbItem);
                            // Promote to L1
                            MemoryCache[cacheKey] = new MemoryCachedItem
                            {
                                Response = resp,
                                CourseId = dbItem.CourseId,
                                LectureMaterialId = dbItem.LectureMaterialId,
                                TotalTokens = dbItem.TotalTokens,
                                ExpiresOn = dbItem.ExpiresOn
                            };

                            // Increment HitCount in DB (fire-and-forget safe)
                            _ = UpdateHitCountAsync(dbItem.Id);

                            _logger?.LogDebug("Cache HIT (L2 Database) for key {CacheKey}", cacheKey);
                            return CloneResponse(resp, dbItem.TotalTokens);
                        }
                        else
                        {
                            _logger?.LogWarning("Security check failed for cached answer {CacheKey} by student {StudentId}", cacheKey, studentId);
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Database lookup for cache key {CacheKey} note: {Message}", cacheKey, ex.Message);
            }

            Interlocked.Increment(ref _cacheMisses);
            _logger?.LogDebug("Cache MISS for key {CacheKey}", cacheKey);
            return null;
        }

        public async Task SetCachedAnswerAsync(
            string cacheKey,
            string normalizedQuery,
            string intent,
            LearningAssistantResponse response,
            int? courseId = null,
            int? materialId = null,
            string? documentHash = null,
            string modelName = "gemini-1.5-flash",
            string language = "en",
            int promptTokens = 0,
            int candidateTokens = 0,
            TimeSpan? ttl = null,
            CancellationToken ct = default)
        {
            // SAFETY RULE: Never cache personal records
            if (intent != "Tutoring" && intent != CompanionIntent.Tutoring.ToString())
            {
                _logger?.LogDebug("Ignoring caching for personal intent {Intent}", intent);
                return;
            }

            if (!response.Success || string.IsNullOrWhiteSpace(response.Reply) || IsRefusalOrUnhelpfulResponse(response.Reply))
            {
                _logger?.LogDebug("Skipping caching for unsuccessful or unhelpful/refusal response");
                return;
            }

            var expirationTtl = ttl ?? TimeSpan.FromMinutes(_config.GetValue<int>("Gemini:CacheTtlMinutes", 1440));
            var expiresOn = DateTime.UtcNow.Add(expirationTtl);
            var totalTokens = promptTokens + candidateTokens;

            // 1. Store in L1 Memory Cache
            MemoryCache[cacheKey] = new MemoryCachedItem
            {
                Response = response,
                CourseId = courseId,
                LectureMaterialId = materialId,
                TotalTokens = totalTokens,
                ExpiresOn = expiresOn
            };

            // 2. Store in L2 Persistent Database Cache
            try
            {
                await EnsureTableExistsAsync(ct);

                var existing = await _db.LearningAssistantAnswerCaches
                    .FirstOrDefaultAsync(c => c.CacheKey == cacheKey, ct);

                if (existing != null)
                {
                    existing.Reply = response.Reply;
                    existing.SourcesJson = response.Sources != null ? JsonSerializer.Serialize(response.Sources) : null;
                    existing.SuggestedActionsJson = response.SuggestedActions != null ? JsonSerializer.Serialize(response.SuggestedActions) : null;
                    existing.PromptTokens = promptTokens;
                    existing.CandidateTokens = candidateTokens;
                    existing.TotalTokens = totalTokens;
                    existing.ExpiresOn = expiresOn;
                    existing.IsActive = true;
                    existing.UpdatedOn = DateTime.UtcNow;
                }
                else
                {
                    var cacheEntry = new LearningAssistantAnswerCache
                    {
                        CacheKey = cacheKey,
                        NormalizedQuery = normalizedQuery.Length > 500 ? normalizedQuery.Substring(0, 500) : normalizedQuery,
                        Intent = intent,
                        CourseId = courseId,
                        LectureMaterialId = materialId,
                        DocumentHash = documentHash,
                        ModelName = modelName,
                        Language = language,
                        Reply = response.Reply,
                        SourcesJson = response.Sources != null ? JsonSerializer.Serialize(response.Sources) : null,
                        SuggestedActionsJson = response.SuggestedActions != null ? JsonSerializer.Serialize(response.SuggestedActions) : null,
                        PromptTokens = promptTokens,
                        CandidateTokens = candidateTokens,
                        TotalTokens = totalTokens,
                        ExpiresOn = expiresOn,
                        CreatedOn = DateTime.UtcNow,
                        IsActive = true
                    };
                    _db.LearningAssistantAnswerCaches.Add(cacheEntry);
                }

                await _db.SaveChangesAsync(ct);
                _logger?.LogDebug("Saved answer to L2 cache for key {CacheKey}", cacheKey);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to persist answer to DB cache: {Message}", ex.Message);
            }
        }

        public async Task InvalidateMaterialCacheAsync(int lectureMaterialId, CancellationToken ct = default)
        {
            if (lectureMaterialId <= 0) return;

            // Invalidate L1
            var keysToRemove = MemoryCache
                .Where(kv => kv.Value.LectureMaterialId == lectureMaterialId)
                .Select(kv => kv.Key)
                .ToList();

            foreach (var k in keysToRemove)
            {
                MemoryCache.TryRemove(k, out _);
            }

            // Invalidate L2
            try
            {
                var entries = await _db.LearningAssistantAnswerCaches
                    .Where(c => c.LectureMaterialId == lectureMaterialId && c.IsActive)
                    .ToListAsync(ct);

                if (entries.Any())
                {
                    foreach (var e in entries)
                    {
                        e.IsActive = false;
                        e.ExpiresOn = DateTime.UtcNow.AddMinutes(-1);
                    }
                    await _db.SaveChangesAsync(ct);
                }
                _logger?.LogInformation("Invalidated {Count} cache entries for material {MaterialId}", entries.Count, lectureMaterialId);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to invalidate material cache in DB for {MaterialId}: {Message}", lectureMaterialId, ex.Message);
            }
        }

        public async Task InvalidateCourseCacheAsync(int courseId, CancellationToken ct = default)
        {
            if (courseId <= 0) return;

            // Invalidate L1
            var keysToRemove = MemoryCache
                .Where(kv => kv.Value.CourseId == courseId)
                .Select(kv => kv.Key)
                .ToList();

            foreach (var k in keysToRemove)
            {
                MemoryCache.TryRemove(k, out _);
            }

            // Invalidate L2
            try
            {
                var entries = await _db.LearningAssistantAnswerCaches
                    .Where(c => c.CourseId == courseId && c.IsActive)
                    .ToListAsync(ct);

                if (entries.Any())
                {
                    foreach (var e in entries)
                    {
                        e.IsActive = false;
                        e.ExpiresOn = DateTime.UtcNow.AddMinutes(-1);
                    }
                    await _db.SaveChangesAsync(ct);
                }
                _logger?.LogInformation("Invalidated {Count} cache entries for course {CourseId}", entries.Count, courseId);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to invalidate course cache in DB for {CourseId}: {Message}", courseId, ex.Message);
            }
        }

        public async Task<LearningAssistantResponse> ExecuteSingleFlightAsync(
            string key,
            Func<Task<LearningAssistantResponse>> factory)
        {
            while (true)
            {
                if (InFlightRequests.TryGetValue(key, out var existingTask))
                {
                    _logger?.LogDebug("Coalescing duplicate concurrent request for key {Key}", key);
                    return await existingTask;
                }

                var tcs = new TaskCompletionSource<LearningAssistantResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
                if (InFlightRequests.TryAdd(key, tcs.Task))
                {
                    try
                    {
                        var result = await factory();
                        tcs.TrySetResult(result);
                        return result;
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                        throw;
                    }
                    finally
                    {
                        InFlightRequests.TryRemove(key, out _);
                    }
                }
            }
        }

        public LearningAssistantMetricsDto GetMetrics()
        {
            return new LearningAssistantMetricsDto
            {
                TotalQueries = Interlocked.Read(ref _totalQueries),
                CacheHits = Interlocked.Read(ref _cacheHits),
                CacheMisses = Interlocked.Read(ref _cacheMisses),
                GeminiApiCalls = Interlocked.Read(ref _geminiApiCalls),
                TotalTokensSaved = Interlocked.Read(ref _tokensSaved)
            };
        }

        public void RecordGeminiCall(int promptTokens, int candidateTokens)
        {
            Interlocked.Increment(ref _geminiApiCalls);
        }

        public void ClearMemoryCache()
        {
            MemoryCache.Clear();
        }

        public void ResetMetrics()
        {
            Interlocked.Exchange(ref _totalQueries, 0);
            Interlocked.Exchange(ref _cacheHits, 0);
            Interlocked.Exchange(ref _cacheMisses, 0);
            Interlocked.Exchange(ref _geminiApiCalls, 0);
            Interlocked.Exchange(ref _tokensSaved, 0);
        }

        private async Task<bool> VerifyAuthorizationAsync(int studentId, string userRole, int? courseId, int? materialId)
        {
            bool isStaff = string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(userRole, "Staff", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(userRole, "Faculty", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(userRole, "Teacher", StringComparison.OrdinalIgnoreCase);

            if (isStaff) return true;

            // If general education query not tied to course/material, it is public to all enrolled students
            if (!courseId.HasValue && !materialId.HasValue) return true;

            if (courseId.HasValue && courseId.Value > 0)
            {
                bool enrolled = await _learningService.IsStudentEnrolledAsync(studentId, courseId.Value);
                if (!enrolled)
                {
                    return false;
                }
            }

            if (materialId.HasValue && materialId.Value > 0)
            {
                var enrolledInMat = await _learningService.IsStudentEnrolledInMaterialAsync(studentId, materialId.Value);
                if (!enrolledInMat)
                {
                    return false;
                }
            }

            return true;
        }

        private static LearningAssistantResponse CloneResponse(LearningAssistantResponse source, int tokensSaved)
        {
            return new LearningAssistantResponse
            {
                Success = source.Success,
                Reply = source.Reply,
                Intent = source.Intent,
                IsDatabaseGrounded = source.IsDatabaseGrounded,
                DetectedContext = source.DetectedContext,
                SuggestedActions = source.SuggestedActions != null ? new List<LearningAssistantActionDto>(source.SuggestedActions) : null,
                Sources = source.Sources != null ? new List<string>(source.Sources) : null,
                ErrorMessage = source.ErrorMessage,
                IsCached = true,
                TokensSaved = tokensSaved
            };
        }

        private static LearningAssistantResponse DeserializeResponse(LearningAssistantAnswerCache item)
        {
            var sources = !string.IsNullOrWhiteSpace(item.SourcesJson)
                ? JsonSerializer.Deserialize<List<string>>(item.SourcesJson)
                : new List<string>();

            var actions = !string.IsNullOrWhiteSpace(item.SuggestedActionsJson)
                ? JsonSerializer.Deserialize<List<LearningAssistantActionDto>>(item.SuggestedActionsJson)
                : new List<LearningAssistantActionDto>();

            return new LearningAssistantResponse
            {
                Success = true,
                Reply = item.Reply,
                Intent = CompanionIntent.Tutoring,
                IsDatabaseGrounded = false,
                Sources = sources,
                SuggestedActions = actions,
                IsCached = true,
                TokensSaved = item.TotalTokens
            };
        }

        private async Task UpdateHitCountAsync(int cacheEntryId)
        {
            try
            {
                var entry = await _db.LearningAssistantAnswerCaches.FindAsync(cacheEntryId);
                if (entry != null)
                {
                    entry.HitCount++;
                    await _db.SaveChangesAsync();
                }
            }
            catch
            {
                // Ignore background metric update failure
            }
        }

        private async Task InvalidateDbCacheEntryAsync(int cacheEntryId)
        {
            try
            {
                var entry = await _db.LearningAssistantAnswerCaches.FindAsync(cacheEntryId);
                if (entry != null)
                {
                    entry.IsActive = false;
                    entry.ExpiresOn = DateTime.UtcNow.AddMinutes(-1);
                    await _db.SaveChangesAsync();
                }
            }
            catch
            {
                // Ignore background deactivation failure
            }
        }

        public static bool IsRefusalOrUnhelpfulResponse(string? reply)
        {
            if (string.IsNullOrWhiteSpace(reply)) return true;
            return reply.Contains("not covered in the current syllabus", StringComparison.OrdinalIgnoreCase) ||
                   reply.Contains("temporarily unavailable", StringComparison.OrdinalIgnoreCase) ||
                   reply.Contains("unable to formulate an explanation", StringComparison.OrdinalIgnoreCase);
        }

        private async Task EnsureTableExistsAsync(CancellationToken ct)
        {
            if (_tableChecked) return;
            try
            {
                if (!_db.Database.IsRelational())
                {
                    _tableChecked = true;
                    return;
                }
            }
            catch
            {
                _tableChecked = true;
                return;
            }

            await _tableInitLock.WaitAsync(ct);
            try
            {
                if (_tableChecked) return;
                string sql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LearningAssistantAnswerCaches')
BEGIN
    CREATE TABLE [dbo].[LearningAssistantAnswerCaches](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [CacheKey] [nvarchar](128) NOT NULL,
        [NormalizedQuery] [nvarchar](500) NOT NULL,
        [Intent] [nvarchar](50) NOT NULL DEFAULT 'Tutoring',
        [CourseId] [int] NULL,
        [LectureMaterialId] [int] NULL,
        [DocumentHash] [nvarchar](100) NULL,
        [ModelName] [nvarchar](50) NOT NULL DEFAULT 'gemini-1.5-flash',
        [Language] [nvarchar](20) NOT NULL DEFAULT 'en',
        [Reply] [nvarchar](max) NOT NULL,
        [SourcesJson] [nvarchar](max) NULL,
        [SuggestedActionsJson] [nvarchar](max) NULL,
        [HitCount] [int] NOT NULL DEFAULT 0,
        [PromptTokens] [int] NOT NULL DEFAULT 0,
        [CandidateTokens] [int] NOT NULL DEFAULT 0,
        [TotalTokens] [int] NOT NULL DEFAULT 0,
        [ExpiresOn] [datetime2](7) NOT NULL,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [CreatedBy] [int] NOT NULL DEFAULT 0,
        [CreatedOn] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedOn] [datetime2](7) NULL,
        CONSTRAINT [PK_LearningAssistantAnswerCaches] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE UNIQUE NONCLUSTERED INDEX [IX_LearningAssistantAnswerCaches_CacheKey] ON [dbo].[LearningAssistantAnswerCaches]([CacheKey]);
    CREATE NONCLUSTERED INDEX [IX_LearningAssistantAnswerCaches_LectureMaterialId] ON [dbo].[LearningAssistantAnswerCaches]([LectureMaterialId]);
    CREATE NONCLUSTERED INDEX [IX_LearningAssistantAnswerCaches_ExpiresOn] ON [dbo].[LearningAssistantAnswerCaches]([ExpiresOn]);
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.LearningAssistantAnswerCaches') AND name = 'CreatedBy')
        ALTER TABLE [dbo].[LearningAssistantAnswerCaches] ADD [CreatedBy] [int] NOT NULL DEFAULT 0;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.LearningAssistantAnswerCaches') AND name = 'UpdatedBy')
        ALTER TABLE [dbo].[LearningAssistantAnswerCaches] ADD [UpdatedBy] [int] NULL;
END";
                await _db.Database.ExecuteSqlRawAsync(sql, ct);
                _tableChecked = true;
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "Could not verify or auto-create LearningAssistantAnswerCaches table.");
                _tableChecked = true;
            }
            finally
            {
                _tableInitLock.Release();
            }
        }

        private sealed class MemoryCachedItem
        {
            public LearningAssistantResponse Response { get; set; } = null!;
            public int? CourseId { get; set; }
            public int? LectureMaterialId { get; set; }
            public int TotalTokens { get; set; }
            public DateTime ExpiresOn { get; set; }
        }
    }
}
