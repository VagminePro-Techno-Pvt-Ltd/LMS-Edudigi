using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TMS.Models.Academics;
using TMS.Repository;

namespace TMS.Web.Services
{
    public class ContentModerationService : IContentModerationService
    {
        private readonly ApplicationDBContext _db;
        private readonly ILogger<ContentModerationService>? _logger;

        // Configurable list of inappropriate terms / regex patterns
        private static readonly string[] InappropriateWords = new[]
        {
            "abuse", "idiot", "stupid", "moron", "hate", "harass", "kill",
            "threat", "scam", "cheat", "bastard", "damn", "bullshit", "asshole",
            "bitch", "loser", "fraud", "shitty", "f**k", "fuck", "leak exam",
            "exam leak", "hack exam", "stfu", "wtf"
        };

        public ContentModerationService(ApplicationDBContext db, ILogger<ContentModerationService>? logger = null)
        {
            _db = db;
            _logger = logger;
        }

        public ModerationCheckResult CheckContent(string? text)
        {
            var result = new ModerationCheckResult();
            if (string.IsNullOrWhiteSpace(text)) return result;

            var detected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var word in InappropriateWords)
            {
                // Word boundary matching to prevent false positives like 'class' or 'pass'
                string pattern = $@"\b{Regex.Escape(word)}\b";
                if (Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase))
                {
                    detected.Add(word);
                }
            }

            if (detected.Any())
            {
                result.IsInappropriate = true;
                result.DetectedKeywords = detected.ToList();
                result.WarningMessage = "Your content contains potentially inappropriate language that violates the institution's conduct guidelines.";
            }

            return result;
        }

        public async Task<ContentModerationFlag> CreateFlagAsync(
            string contentType,
            int contentId,
            int authorUserId,
            string? authorName,
            int? programId,
            int? courseId,
            string flaggedText,
            List<string> detectedKeywords)
        {
            var flag = new ContentModerationFlag
            {
                ContentType = contentType,
                ContentId = contentId,
                AuthorUserId = authorUserId,
                AuthorName = authorName,
                ProgramId = programId,
                CourseId = courseId,
                FlaggedText = flaggedText.Length > 2000 ? flaggedText.Substring(0, 2000) : flaggedText,
                DetectedKeywords = string.Join(", ", detectedKeywords),
                Status = "PendingReview",
                CreatedBy = authorUserId,
                CreatedOn = DateTime.UtcNow,
                IsActive = true
            };

            _db.ContentModerationFlags.Add(flag);
            await _db.SaveChangesAsync();

            // Create a non-punitive warning alert for the student
            var alert = new StudentRiskAlert
            {
                StudentId = authorUserId,
                CourseId = courseId ?? 0,
                ActivityType = "Moderation",
                ActivityId = flag.Id,
                Title = "Community Conduct Notice",
                Message = $"Your discussion submission was flagged for review due to potentially inappropriate wording ({flag.DetectedKeywords}). Please maintain academic decorum.",
                RiskLevel = "Warning",
                DeadlineDate = null,
                IsResolved = false,
                IsRead = false,
                CreatedBy = authorUserId,
                CreatedOn = DateTime.UtcNow,
                ActionUrl = contentType == "DiscussionThread" ? $"/Discussion/Details/{contentId}" : "/Discussion/Index",
                IsActive = true
            };

            _db.StudentRiskAlerts.Add(alert);
            await _db.SaveChangesAsync();

            return flag;
        }

        public async Task<List<ContentModerationFlag>> GetFlagsAsync(string? status = null)
        {
            var query = _db.ContentModerationFlags
                .AsNoTracking()
                .Include(f => f.AuthorUser)
                .Include(f => f.Program)
                .Include(f => f.Course)
                .Include(f => f.ReviewedByUser)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(f => f.Status == status);
            }

            return await query.OrderByDescending(f => f.CreatedOn).ToListAsync();
        }

        public async Task<bool> ReviewFlagAsync(int flagId, string decision, string? remarks, int reviewerUserId)
        {
            var flag = await _db.ContentModerationFlags.FirstOrDefaultAsync(f => f.Id == flagId);
            if (flag == null) return false;

            flag.Status = decision.Equals("Confirm", StringComparison.OrdinalIgnoreCase) ? "Confirmed" : "Dismissed";
            flag.ReviewedByUserId = reviewerUserId;
            flag.ReviewedOn = DateTime.UtcNow;
            flag.ReviewRemarks = remarks ?? (flag.Status == "Confirmed" ? "Flag confirmed by reviewer" : "Flag dismissed by reviewer");
            flag.UpdatedBy = reviewerUserId;
            flag.UpdatedOn = DateTime.UtcNow;

            // If dismissed, resolve any linked student warning alert
            if (flag.Status == "Dismissed")
            {
                var linkedAlerts = await _db.StudentRiskAlerts
                    .Where(a => a.ActivityType == "Moderation" && a.ActivityId == flag.Id && !a.IsResolved)
                    .ToListAsync();

                foreach (var a in linkedAlerts)
                {
                    a.IsResolved = true;
                    a.ResolvedOn = DateTime.UtcNow;
                    a.UpdatedBy = reviewerUserId;
                    a.UpdatedOn = DateTime.UtcNow;
                }
            }

            await _db.SaveChangesAsync();
            return true;
        }
    }
}
