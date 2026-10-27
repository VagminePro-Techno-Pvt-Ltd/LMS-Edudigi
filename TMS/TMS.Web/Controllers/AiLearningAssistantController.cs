using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TMS.ViewModels.LearningAssistant;
using TMS.Web.Services;

namespace TMS.Web.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class AiLearningAssistantController : BaseController
    {
        private readonly ILearningAssistantService _assistantService;
        private readonly ILearningAssistantCacheService? _cacheService;
        private readonly ILogger<AiLearningAssistantController> _logger;

        // Thread-safe in-memory sliding window rate limiter: Max 12 requests per user per 60 seconds
        private static readonly ConcurrentDictionary<int, List<DateTime>> UserRequestTimestamps = new();
        private const int MaxRequestsPerMinute = 12;
        private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(1);

        public AiLearningAssistantController(
            ILearningAssistantService assistantService,
            ILogger<AiLearningAssistantController> logger,
            ILearningAssistantCacheService? cacheService = null)
        {
            _assistantService = assistantService;
            _logger = logger;
            _cacheService = cacheService;
        }

        [HttpPost("Ask")]
        public async Task<IActionResult> Ask([FromBody] LearningAssistantRequest request)
        {
            var userId = GetUserId();
            var role = GetUserRole();

            if (userId <= 0)
            {
                return Unauthorized(new LearningAssistantResponse
                {
                    Success = false,
                    ErrorMessage = "User authentication required."
                });
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new LearningAssistantResponse
                {
                    Success = false,
                    ErrorMessage = "Query message cannot be empty."
                });
            }

            // Cap student input message length to protect token consumption
            if (request.Message.Length > 2000)
            {
                request.Message = request.Message.Substring(0, 2000);
            }

            // ── Rate Limiting Check ──
            if (!CheckRateLimit(userId))
            {
                _logger.LogWarning("User {UserId} exceeded rate limit for AI Assistant.", userId);
                return Json(new LearningAssistantResponse
                {
                    Success = false,
                    Reply = "You are asking questions quite rapidly! Please wait a few moments before sending your next question.",
                    ErrorMessage = "Rate limit reached (max 12 requests per minute)."
                });
            }

            try
            {
                var response = await _assistantService.AskAssistantAsync(
                    userId,
                    role,
                    request,
                    HttpContext.RequestAborted);

                return Json(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing AI learning assistant request for user {UserId}", userId);
                return Json(new LearningAssistantResponse
                {
                    Success = false,
                    Reply = "I encountered an unexpected issue while formulating your explanation. Please try again.",
                    ErrorMessage = ex.Message
                });
            }
        }

        [HttpGet("EnrolledCourses")]
        public async Task<IActionResult> GetEnrolledCourses()
        {
            var userId = GetUserId();
            if (userId <= 0) return Unauthorized();

            var courses = await _assistantService.GetStudentEnrolledCoursesAsync(userId);
            return Json(new { success = true, courses });
        }

        [HttpGet("Context")]
        public async Task<IActionResult> GetContext(string? pageUrl, int? courseId, int? unitId, int? materialId)
        {
            var userId = GetUserId();
            var role = GetUserRole();
            if (userId <= 0) return Unauthorized();

            var context = await _assistantService.ResolveContextAsync(userId, role, pageUrl, courseId, unitId, materialId);
            return Json(new { success = true, context });
        }

        [HttpGet("Metrics")]
        public IActionResult GetMetrics()
        {
            var userId = GetUserId();
            if (userId <= 0) return Unauthorized();

            var metrics = _cacheService?.GetMetrics() ?? new LearningAssistantMetricsDto();
            return Json(new { success = true, metrics });
        }

        private static bool CheckRateLimit(int userId)
        {
            var now = DateTime.UtcNow;
            var timestamps = UserRequestTimestamps.GetOrAdd(userId, _ => new List<DateTime>());

            lock (timestamps)
            {
                // Prune older than 1 minute
                timestamps.RemoveAll(t => now - t > RateLimitWindow);

                if (timestamps.Count >= MaxRequestsPerMinute)
                {
                    return false;
                }

                timestamps.Add(now);
                return true;
            }
        }
    }
}
