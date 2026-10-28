using System.Collections.Generic;
using System.Threading.Tasks;
using TMS.Models.Academics;

namespace TMS.Web.Services
{
    public class ModerationCheckResult
    {
        public bool IsInappropriate { get; set; }
        public List<string> DetectedKeywords { get; set; } = new();
        public string? WarningMessage { get; set; }
    }

    public interface IContentModerationService
    {
        ModerationCheckResult CheckContent(string? text);

        Task<ContentModerationFlag> CreateFlagAsync(
            string contentType,
            int contentId,
            int authorUserId,
            string? authorName,
            int? programId,
            int? courseId,
            string flaggedText,
            List<string> detectedKeywords);

        Task<List<ContentModerationFlag>> GetFlagsAsync(string? status = null);

        Task<bool> ReviewFlagAsync(int flagId, string decision, string? remarks, int reviewerUserId);
    }
}
