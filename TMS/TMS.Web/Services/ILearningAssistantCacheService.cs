using System;
using System.Threading;
using System.Threading.Tasks;
using TMS.ViewModels.LearningAssistant;

namespace TMS.Web.Services
{
    public interface ILearningAssistantCacheService
    {
        Task<LearningAssistantResponse?> GetCachedAnswerAsync(
            string cacheKey,
            int studentId,
            string userRole,
            int? courseId = null,
            int? materialId = null,
            CancellationToken ct = default);

        Task SetCachedAnswerAsync(
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
            CancellationToken ct = default);

        Task InvalidateMaterialCacheAsync(int lectureMaterialId, CancellationToken ct = default);

        Task InvalidateCourseCacheAsync(int courseId, CancellationToken ct = default);

        string GenerateCacheKey(
            string normalizedQuery,
            string language = "en",
            int? courseId = null,
            int? materialId = null,
            string? documentHash = null,
            string modelName = "gemini-1.5-flash",
            string promptVersion = "v1");

        string NormalizeQuery(string query);

        Task<LearningAssistantResponse> ExecuteSingleFlightAsync(
            string key,
            Func<Task<LearningAssistantResponse>> factory);

        LearningAssistantMetricsDto GetMetrics();

        void RecordGeminiCall(int promptTokens, int candidateTokens);

        void ClearMemoryCache();

        void ResetMetrics();
    }
}
