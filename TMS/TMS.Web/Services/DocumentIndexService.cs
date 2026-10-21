using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TMS.Models.LearningAssistant;
using TMS.Repository;
using TMS.ViewModels.LearningAssistant;

namespace TMS.Web.Services
{
    public class DocumentIndexService : IDocumentIndexService
    {
        private readonly ApplicationDBContext _db;
        private readonly IDocumentProcessingService _processingService;
        private readonly IStudentLearningService _learningService;
        private readonly IWebHostEnvironment? _env;
        private readonly ILogger<DocumentIndexService>? _logger;

        private static bool _schemaEnsured = false;
        private static readonly object SchemaLock = new();

        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "the", "and", "or", "but", "if", "then", "else", "when", "at", "by", "for",
            "with", "about", "against", "between", "into", "through", "during", "before", "after",
            "above", "below", "to", "from", "up", "down", "in", "out", "on", "off", "over", "under",
            "again", "further", "then", "once", "here", "there", "all", "any", "both", "each", "few",
            "more", "most", "other", "some", "such", "no", "nor", "not", "only", "own", "same", "so",
            "than", "too", "very", "can", "will", "just", "don", "should", "now", "is", "am", "are",
            "was", "were", "be", "been", "being", "have", "has", "had", "having", "do", "does", "did",
            "doing", "what", "which", "who", "whom", "this", "that", "these", "those", "how", "why",
            "where", "explain", "tell", "describe", "give", "please", "me", "my", "your", "its", "our",
            "their", "we", "you", "i", "he", "she", "it", "they"
        };

        public DocumentIndexService(
            ApplicationDBContext db,
            IDocumentProcessingService processingService,
            IStudentLearningService learningService,
            IWebHostEnvironment? env = null,
            ILogger<DocumentIndexService>? logger = null)
        {
            _db = db;
            _processingService = processingService;
            _learningService = learningService;
            _env = env;
            _logger = logger;
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
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DocumentIndexStatuses')
BEGIN
    CREATE TABLE [dbo].[DocumentIndexStatuses](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [LectureMaterialId] [int] NOT NULL,
        [Status] [nvarchar](50) NOT NULL,
        [FileHash] [nvarchar](100) NULL,
        [PageCount] [int] NOT NULL DEFAULT 0,
        [TotalChunks] [int] NOT NULL DEFAULT 0,
        [ErrorMessage] [nvarchar](max) NULL,
        [LastIndexedOn] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [CreatedBy] [int] NOT NULL DEFAULT 0,
        [CreatedOn] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedOn] [datetime2](7) NULL,
        CONSTRAINT [PK_DocumentIndexStatuses] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE UNIQUE NONCLUSTERED INDEX [IX_DocumentIndexStatuses_LectureMaterialId]
    ON [dbo].[DocumentIndexStatuses]([LectureMaterialId]);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DocumentChunks')
BEGIN
    CREATE TABLE [dbo].[DocumentChunks](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [LectureMaterialId] [int] NOT NULL,
        [ChunkIndex] [int] NOT NULL,
        [PageNumber] [int] NULL,
        [Heading] [nvarchar](300) NULL,
        [Content] [nvarchar](max) NOT NULL,
        [WordCount] [int] NOT NULL DEFAULT 0,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [CreatedBy] [int] NOT NULL DEFAULT 0,
        [CreatedOn] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedOn] [datetime2](7) NULL,
        CONSTRAINT [PK_DocumentChunks] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE NONCLUSTERED INDEX [IX_DocumentChunks_LectureMaterialId]
    ON [dbo].[DocumentChunks]([LectureMaterialId]);
END;";
                    await _db.Database.ExecuteSqlRawAsync(sql, ct);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "DocumentIndexService EnsureSchema note: {Message}", ex.Message);
            }
            finally
            {
                lock (SchemaLock)
                {
                    _schemaEnsured = true;
                }
            }
        }

        public async Task<DocumentIndexResult> IndexMaterialAsync(int lectureMaterialId, bool force = false, CancellationToken ct = default)
        {
            await EnsureSchemaAsync(ct);

            var material = await _db.LectureMaterials
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == lectureMaterialId && m.IsActive, ct);

            if (material == null)
            {
                return new DocumentIndexResult
                {
                    Success = false,
                    LectureMaterialId = lectureMaterialId,
                    Status = DocumentProcessingStatus.FileNotFound,
                    ErrorMessage = $"LectureMaterial ID {lectureMaterialId} was not found or is inactive."
                };
            }

            var physicalPath = ResolvePhysicalPath(material.FilePath);
            if (!File.Exists(physicalPath))
            {
                _logger?.LogWarning("Material file not found at path: {Path}", physicalPath);
                await UpdateStatusAsync(lectureMaterialId, DocumentProcessingStatus.FileNotFound, null, 0, 0, $"File not found on server disk: {physicalPath}", ct);
                return new DocumentIndexResult
                {
                    Success = false,
                    LectureMaterialId = lectureMaterialId,
                    Status = DocumentProcessingStatus.FileNotFound,
                    ErrorMessage = $"File does not exist on disk: {physicalPath}"
                };
            }

            // Compute current file hash to detect unchanged files
            var currentHash = ComputeFileHash(physicalPath);

            var existingStatus = await _db.DocumentIndexStatuses
                .FirstOrDefaultAsync(s => s.LectureMaterialId == lectureMaterialId, ct);

            if (!force && existingStatus != null &&
                string.Equals(existingStatus.FileHash, currentHash, StringComparison.OrdinalIgnoreCase) &&
                existingStatus.Status == DocumentProcessingStatus.Indexed.ToString())
            {
                // Unchanged document already indexed
                return new DocumentIndexResult
                {
                    Success = true,
                    LectureMaterialId = lectureMaterialId,
                    Status = DocumentProcessingStatus.Indexed,
                    PageCount = existingStatus.PageCount,
                    TotalChunks = existingStatus.TotalChunks,
                    IsReprocessed = false
                };
            }

            // Process & extract document text
            var title = !string.IsNullOrWhiteSpace(material.Title) ? material.Title : material.OriginalFileName;
            var extraction = _processingService.Extract(physicalPath, title, lectureMaterialId);

            // Transactionally update chunks and status
            var strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                // Remove existing chunks for this material
                var oldChunks = await _db.DocumentChunks
                    .Where(c => c.LectureMaterialId == lectureMaterialId)
                    .ToListAsync(ct);

                if (oldChunks.Any())
                {
                    _db.DocumentChunks.RemoveRange(oldChunks);
                }

                // Update or insert status record
                var statusRecord = await _db.DocumentIndexStatuses
                    .FirstOrDefaultAsync(s => s.LectureMaterialId == lectureMaterialId, ct);

                if (statusRecord == null)
                {
                    statusRecord = new DocumentIndexStatus
                    {
                        LectureMaterialId = lectureMaterialId,
                        CreatedOn = DateTime.UtcNow,
                        CreatedBy = material.CreatedBy,
                        IsActive = true
                    };
                    _db.DocumentIndexStatuses.Add(statusRecord);
                }

                statusRecord.Status = extraction.Status.ToString();
                statusRecord.FileHash = currentHash;
                statusRecord.PageCount = extraction.PageCount;
                statusRecord.TotalChunks = extraction.Chunks.Count;
                statusRecord.ErrorMessage = extraction.ErrorMessage;
                statusRecord.LastIndexedOn = DateTime.UtcNow;
                statusRecord.UpdatedOn = DateTime.UtcNow;
                statusRecord.IsActive = true;

                if (extraction.Success && extraction.Chunks.Any())
                {
                    foreach (var c in extraction.Chunks)
                    {
                        _db.DocumentChunks.Add(new DocumentChunk
                        {
                            LectureMaterialId = lectureMaterialId,
                            ChunkIndex = c.ChunkIndex,
                            PageNumber = c.PageNumber,
                            Heading = c.Heading,
                            Content = c.Content,
                            WordCount = c.WordCount,
                            IsActive = true,
                            CreatedOn = DateTime.UtcNow,
                            CreatedBy = material.CreatedBy
                        });
                    }
                }

                await _db.SaveChangesAsync(ct);

                return new DocumentIndexResult
                {
                    Success = extraction.Success,
                    LectureMaterialId = lectureMaterialId,
                    Status = extraction.Status,
                    TotalChunks = extraction.Chunks.Count,
                    PageCount = extraction.PageCount,
                    ErrorMessage = extraction.ErrorMessage,
                    IsReprocessed = true
                };
            });
        }

        public async Task<int> EnsureCourseMaterialsIndexedAsync(int courseId, CancellationToken ct = default)
        {
            if (courseId <= 0) return 0;
            await EnsureSchemaAsync(ct);

            // Find all lecture material IDs linked to this course
            var directMaterialIds = await _db.LectureMaterials
                .AsNoTracking()
                .Where(m => m.CourseId == courseId && m.IsActive)
                .Select(m => m.Id)
                .ToListAsync(ct);

            var mappedMaterialIds = await _db.CourseMaterialMappings
                .AsNoTracking()
                .Where(m => m.CourseId == courseId && m.IsActive)
                .Select(m => m.LectureMaterialId)
                .ToListAsync(ct);

            var allMaterialIds = directMaterialIds.Concat(mappedMaterialIds).Distinct().ToList();
            if (!allMaterialIds.Any()) return 0;

            // Find which ones are not yet indexed
            var indexedIds = await _db.DocumentIndexStatuses
                .AsNoTracking()
                .Where(s => allMaterialIds.Contains(s.LectureMaterialId) && s.Status == DocumentProcessingStatus.Indexed.ToString())
                .Select(s => s.LectureMaterialId)
                .ToListAsync(ct);

            var unindexedIds = allMaterialIds.Except(indexedIds).ToList();
            var indexedCount = 0;

            foreach (var matId in unindexedIds)
            {
                try
                {
                    var res = await IndexMaterialAsync(matId, force: false, ct);
                    if (res.Success) indexedCount++;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to auto-index material {MaterialId} for course {CourseId}", matId, courseId);
                }
            }

            return indexedCount;
        }

        public async Task<bool> RemoveIndexForMaterialAsync(int lectureMaterialId, CancellationToken ct = default)
        {
            if (lectureMaterialId <= 0) return false;
            await EnsureSchemaAsync(ct);

            try
            {
                var chunks = await _db.DocumentChunks
                    .Where(c => c.LectureMaterialId == lectureMaterialId)
                    .ToListAsync(ct);

                if (chunks.Any())
                {
                    _db.DocumentChunks.RemoveRange(chunks);
                }

                var status = await _db.DocumentIndexStatuses
                    .FirstOrDefaultAsync(s => s.LectureMaterialId == lectureMaterialId, ct);

                if (status != null)
                {
                    _db.DocumentIndexStatuses.Remove(status);
                }

                await _db.SaveChangesAsync(ct);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to remove index for material {MaterialId}", lectureMaterialId);
                return false;
            }
        }

        public async Task<List<DocumentChunkSearchResult>> SearchPassagesAsync(
            int userId,
            string userRole,
            int? courseId,
            int? materialId,
            string query,
            int maxResults = 5,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<DocumentChunkSearchResult>();
            await EnsureSchemaAsync(ct);

            bool isStaff = IsStaffRole(userRole);

            // ─── 1. RESOLVE & VERIFY AUTHORIZATION ───
            List<int> authorizedCourseIds = new();

            if (!isStaff)
            {
                var enrolledCourseIds = await _learningService.GetEnrolledCourseIdsAsync(userId);
                if (!enrolledCourseIds.Any())
                {
                    return new List<DocumentChunkSearchResult>();
                }

                if (courseId.HasValue && courseId.Value > 0)
                {
                    if (!enrolledCourseIds.Contains(courseId.Value))
                    {
                        // Student is asking about an unauthorized course -> Strictly return empty
                        _logger?.LogWarning("Security rejection: Student {UserId} attempted to search unauthorized course {CourseId}", userId, courseId.Value);
                        return new List<DocumentChunkSearchResult>();
                    }
                    authorizedCourseIds = new List<int> { courseId.Value };
                }
                else
                {
                    authorizedCourseIds = enrolledCourseIds;
                }

                if (materialId.HasValue && materialId.Value > 0)
                {
                    bool enrolledInMat = await _learningService.IsStudentEnrolledInMaterialAsync(userId, materialId.Value);
                    if (!enrolledInMat)
                    {
                        _logger?.LogWarning("Security rejection: Student {UserId} attempted to search unauthorized material {MaterialId}", userId, materialId.Value);
                        return new List<DocumentChunkSearchResult>();
                    }
                }
            }
            else
            {
                // Staff member
                if (courseId.HasValue && courseId.Value > 0)
                {
                    authorizedCourseIds = new List<int> { courseId.Value };
                }
            }

            try
            {
                // ─── 2. RESOLVE AUTHORIZED LECTURE MATERIAL IDs ───
                IQueryable<int> materialQuery;

                if (materialId.HasValue && materialId.Value > 0)
                {
                    materialQuery = _db.LectureMaterials
                        .AsNoTracking()
                        .Where(m => m.Id == materialId.Value && m.IsActive)
                        .Select(m => m.Id);
                }
                else if (authorizedCourseIds.Any())
                {
                    // Materials directly in course OR mapped to course
                    var directQuery = _db.LectureMaterials
                        .AsNoTracking()
                        .Where(m => m.IsActive && m.CourseId.HasValue && authorizedCourseIds.Contains(m.CourseId.Value))
                        .Select(m => m.Id);

                    var mappedQuery = _db.CourseMaterialMappings
                        .AsNoTracking()
                        .Where(m => m.IsActive && authorizedCourseIds.Contains(m.CourseId))
                        .Select(m => m.LectureMaterialId);

                    materialQuery = directQuery.Union(mappedQuery);
                }
                else
                {
                    // Staff searching across LMS
                    materialQuery = _db.LectureMaterials
                        .AsNoTracking()
                        .Where(m => m.IsActive)
                        .Select(m => m.Id);
                }

                var authorizedMaterialIds = await materialQuery.Distinct().ToListAsync(ct);
                if (!authorizedMaterialIds.Any())
                {
                    return new List<DocumentChunkSearchResult>();
                }

                // Trigger background indexing if course has unindexed materials
                if (courseId.HasValue && courseId.Value > 0)
                {
                    _ = EnsureCourseMaterialsIndexedAsync(courseId.Value, ct);
                }

                // ─── 3. RETRIEVE CANDIDATE CHUNKS ───
                var chunks = await (from c in _db.DocumentChunks.AsNoTracking()
                                    join m in _db.LectureMaterials.AsNoTracking() on c.LectureMaterialId equals m.Id
                                    where authorizedMaterialIds.Contains(c.LectureMaterialId) && c.IsActive && m.IsActive
                                    select new
                                    {
                                        Chunk = c,
                                        Material = m
                                    }).ToListAsync(ct);

                if (!chunks.Any())
                {
                    return new List<DocumentChunkSearchResult>();
                }

                // ─── 4. RELEVANCE RANKING ───
                var keywords = ExtractKeywords(query);
                var normalizedFullQuery = query.Trim().ToLowerInvariant();

                var scoredResults = new List<DocumentChunkSearchResult>();

                foreach (var item in chunks)
                {
                    var content = item.Chunk.Content ?? string.Empty;
                    var heading = item.Chunk.Heading ?? string.Empty;
                    var title = item.Material.Title ?? string.Empty;

                    var score = CalculateRelevanceScore(normalizedFullQuery, keywords, content, heading, title);

                    if (score > 0)
                    {
                        scoredResults.Add(new DocumentChunkSearchResult
                        {
                            ChunkId = item.Chunk.Id,
                            LectureMaterialId = item.Chunk.LectureMaterialId,
                            DocumentTitle = title,
                            OriginalFileName = item.Material.OriginalFileName,
                            PageNumber = item.Chunk.PageNumber,
                            Heading = heading,
                            Content = content,
                            Score = score,
                            CourseId = item.Material.CourseId
                        });
                    }
                }

                return scoredResults
                    .OrderByDescending(r => r.Score)
                    .Take(maxResults)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Document passage search failed to query database: {Message}", ex.Message);
                return new List<DocumentChunkSearchResult>();
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // RELEVANCE SCORING
        // ═════════════════════════════════════════════════════════════════════
        private static double CalculateRelevanceScore(
            string normalizedQuery,
            List<string> keywords,
            string content,
            string heading,
            string title)
        {
            if (string.IsNullOrWhiteSpace(content) && string.IsNullOrWhiteSpace(heading))
                return 0;

            double score = 0;
            var lowerContent = content.ToLowerInvariant();
            var lowerHeading = heading.ToLowerInvariant();
            var lowerTitle = title.ToLowerInvariant();

            // 1. Exact full query phrase match
            if (normalizedQuery.Length > 3)
            {
                if (lowerContent.Contains(normalizedQuery))
                {
                    score += 60.0;
                }
                if (lowerHeading.Contains(normalizedQuery))
                {
                    score += 45.0;
                }
                if (lowerTitle.Contains(normalizedQuery))
                {
                    score += 30.0;
                }
            }

            // 2. Individual keyword matching
            int matchedKeywords = 0;

            foreach (var kw in keywords)
            {
                bool kwMatched = false;

                // Match in heading
                if (lowerHeading.Contains(kw))
                {
                    score += 20.0;
                    kwMatched = true;
                }

                // Match in document title
                if (lowerTitle.Contains(kw))
                {
                    score += 15.0;
                    kwMatched = true;
                }

                // Match in content text
                if (lowerContent.Contains(kw))
                {
                    // Count frequency capped at 5
                    int count = CountOccurrences(lowerContent, kw);
                    score += Math.Min(count * 5.0, 25.0);
                    kwMatched = true;
                }

                if (kwMatched) matchedKeywords++;
            }

            // 3. Keyword density / coverage bonus
            if (keywords.Count > 1 && matchedKeywords > 0)
            {
                double coverageRatio = (double)matchedKeywords / keywords.Count;
                score += coverageRatio * 25.0;

                // Extra reward if 100% of keywords matched
                if (matchedKeywords == keywords.Count)
                {
                    score += 35.0;
                }
            }

            return score;
        }

        private static int CountOccurrences(string text, string pattern)
        {
            int count = 0;
            int i = 0;
            while ((i = text.IndexOf(pattern, i, StringComparison.Ordinal)) != -1)
            {
                i += pattern.Length;
                count++;
                if (count >= 5) break;
            }
            return count;
        }

        private static List<string> ExtractKeywords(string query)
        {
            var cleaned = Regex.Replace(query, @"[^\w\s]", " ");
            var words = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var keywords = new List<string>();
            foreach (var w in words)
            {
                var lower = w.ToLowerInvariant().Trim();
                if (lower.Length > 2 && !StopWords.Contains(lower))
                {
                    keywords.Add(lower);
                }
            }

            return keywords.Distinct().ToList();
        }

        private string ResolvePhysicalPath(string virtualOrRelativePath)
        {
            if (string.IsNullOrWhiteSpace(virtualOrRelativePath)) return string.Empty;

            if (Path.IsPathRooted(virtualOrRelativePath) && File.Exists(virtualOrRelativePath))
            {
                return virtualOrRelativePath;
            }

            var webRoot = _env?.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "TMS", "TMS.Web", "wwwroot");
            var trimmed = virtualOrRelativePath.TrimStart('~', '/').Replace('/', Path.DirectorySeparatorChar);
            var candidate = Path.Combine(webRoot, trimmed);

            if (File.Exists(candidate)) return candidate;

            // Also check current directory relative uploads
            var altCandidate = Path.Combine(Directory.GetCurrentDirectory(), "TMS", "TMS.Web", "wwwroot", trimmed);
            if (File.Exists(altCandidate)) return altCandidate;

            return candidate;
        }

        private static string ComputeFileHash(string physicalFilePath)
        {
            try
            {
                using var md5 = MD5.Create();
                using var stream = File.OpenRead(physicalFilePath);
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
            catch
            {
                return string.Empty;
            }
        }

        private async Task UpdateStatusAsync(
            int lectureMaterialId,
            DocumentProcessingStatus status,
            string? hash,
            int pageCount,
            int totalChunks,
            string? error,
            CancellationToken ct)
        {
            var record = await _db.DocumentIndexStatuses
                .FirstOrDefaultAsync(s => s.LectureMaterialId == lectureMaterialId, ct);

            if (record == null)
            {
                record = new DocumentIndexStatus
                {
                    LectureMaterialId = lectureMaterialId,
                    CreatedOn = DateTime.UtcNow,
                    IsActive = true
                };
                _db.DocumentIndexStatuses.Add(record);
            }

            record.Status = status.ToString();
            record.FileHash = hash;
            record.PageCount = pageCount;
            record.TotalChunks = totalChunks;
            record.ErrorMessage = error;
            record.LastIndexedOn = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
        }

        private static bool IsStaffRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role)) return false;
            var r = role.Trim().ToLowerInvariant();
            return r == "admin" || r == "super admin" || r == "management" || r == "faculty" ||
                   r == "teacher" || r == "co-faculty" || r == "co faculty" || r == "exam coordinator" ||
                   r == "instructor";
        }
    }
}
