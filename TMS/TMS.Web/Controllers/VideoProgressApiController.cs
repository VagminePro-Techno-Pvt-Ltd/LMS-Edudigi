using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TMS.Repository.Managers;
using TMS.ViewModels.Academics;
using TMS.ViewModels.Masters;
using TMS.Web.Services;

namespace TMS.Web.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/video-progress")]
    public class VideoProgressApiController : BaseController
    {
        private readonly IMasterBaseManager<VideoProgressViewModel> _progressManager;
        private readonly IMasterBaseManager<LectureMaterialViewModel> _materialManager;
        private readonly IStudentLearningService _learningService;

        public VideoProgressApiController(
            IMasterBaseManager<VideoProgressViewModel> progressManager,
            IMasterBaseManager<LectureMaterialViewModel> materialManager,
            IStudentLearningService learningService)
        {
            _progressManager = progressManager;
            _materialManager = materialManager;
            _learningService = learningService;
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  POST: /api/video-progress/validate - Validate and save video progress
        // ═══════════════════════════════════════════════════════════════════════
        [HttpPost("validate")]
        public async Task<IActionResult> ValidateProgress([FromBody] VideoProgressValidationRequest request)
        {
            try
            {
                int studentId = GetUserId();
                
                // Validate request
                if (request == null || request.MaterialId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid request data." });
                }

                // Verify material exists and student has access
                var material = await _materialManager.GetAsync(request.MaterialId);
                if (material == null)
                {
                    return NotFound(new { success = false, message = "Material not found." });
                }

                bool isStaff = User.IsInRole("Admin") || User.IsInRole("Super Admin") || User.IsInRole("Faculty") || User.IsInRole("Exam Coordinator");
                if (!isStaff)
                {
                    bool enrolled = await _learningService.IsStudentEnrolledInMaterialAsync(studentId, request.MaterialId);
                    if (!enrolled)
                        return StatusCode(403, new { success = false, message = "Access denied. Not enrolled in this course." });
                }

                // Get existing progress
                var existingProgress = await _progressManager.GetAsync(
                    predicate: p => p.StudentId == studentId && p.VideoId == request.MaterialId
                );
                var currentProgress = existingProgress?.FirstOrDefault();

                // Validate progress integrity
                var validationResult = ValidateProgressIntegrity(request, currentProgress);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new { 
                        success = false, 
                        message = validationResult.ErrorMessage,
                        resetToTime = validationResult.ResetToTime
                    });
                }

                // Save or update progress
                if (currentProgress == null)
                {
                    // Create new progress record
                    var newProgress = new VideoProgressViewModel
                    {
                        StudentId = studentId,
                        VideoId = request.MaterialId,
                        CurrentTimeSeconds = request.CurrentTimeSeconds,
                        MaxWatchedSeconds = request.MaxAllowedSeconds,
                        TotalWatchTimeSeconds = request.TotalWatchTime,
                        SkipViolations = request.SkipViolations,
                        SessionStartTime = request.SessionStartTime,
                        LastUpdated = DateTime.UtcNow,
                        IsActive = true
                    };

                    await _progressManager.AddUpdateAsync(newProgress, studentId);
                }
                else
                {
                    // Update existing progress
                    currentProgress.CurrentTimeSeconds = request.CurrentTimeSeconds;
                    currentProgress.MaxWatchedSeconds = Math.Max(currentProgress.MaxWatchedSeconds, request.MaxAllowedSeconds);
                    currentProgress.TotalWatchTimeSeconds = request.TotalWatchTime;
                    currentProgress.SkipViolations = request.SkipViolations;
                    currentProgress.LastUpdated = DateTime.UtcNow;

                    await _progressManager.AddUpdateAsync(currentProgress, studentId);
                }

                return Ok(new { 
                    success = true, 
                    message = "Progress validated and saved successfully.",
                    maxWatchedSeconds = currentProgress?.MaxWatchedSeconds ?? request.MaxAllowedSeconds
                });
            }
            catch (Exception ex)
            {
                // Log error (implement logging as needed)
                return StatusCode(500, new { 
                    success = false, 
                    message = "An error occurred while validating progress." 
                });
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  GET: /api/video-progress/{materialId} - Get student's progress for material
        // ═══════════════════════════════════════════════════════════════════════
        [HttpGet("{materialId}")]
        public async Task<IActionResult> GetProgress(int materialId)
        {
            try
            {
                int studentId = GetUserId();

                bool isStaff = User.IsInRole("Admin") || User.IsInRole("Super Admin") || User.IsInRole("Faculty") || User.IsInRole("Exam Coordinator");
                if (!isStaff)
                {
                    bool enrolled = await _learningService.IsStudentEnrolledInMaterialAsync(studentId, materialId);
                    if (!enrolled)
                        return StatusCode(403, new { success = false, message = "Access denied. Not enrolled in this course." });
                }

                var progress = await _progressManager.GetAsync(
                    predicate: p => p.StudentId == studentId && p.VideoId == materialId && p.IsActive
                );

                var studentProgress = progress?.FirstOrDefault();

                if (studentProgress == null)
                {
                    return Ok(new { 
                        success = true, 
                        progress = new { 
                            currentTimeSeconds = 0, 
                            maxWatchedSeconds = 0,
                            totalWatchTime = 0,
                            skipViolations = 0
                        } 
                    });
                }

                return Ok(new { 
                    success = true, 
                    progress = new { 
                        currentTimeSeconds = studentProgress.CurrentTimeSeconds, 
                        maxWatchedSeconds = studentProgress.MaxWatchedSeconds,
                        totalWatchTime = studentProgress.TotalWatchTimeSeconds,
                        skipViolations = studentProgress.SkipViolations,
                        lastUpdated = studentProgress.LastUpdated
                    } 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    message = "An error occurred while retrieving progress." 
                });
            }
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  Private: Validate progress integrity
        // ═══════════════════════════════════════════════════════════════════════
        private ProgressValidationResult ValidateProgressIntegrity(
            VideoProgressValidationRequest request, 
            VideoProgressViewModel? currentProgress)
        {
            // Check for suspicious time jumps
            if (currentProgress != null)
            {
                var timeSinceLastUpdate = DateTime.UtcNow - currentProgress.LastUpdated;
                var maxAllowedJump = Math.Min(timeSinceLastUpdate.TotalSeconds * 1.2, 60); // Max 60 seconds jump

                if (request.CurrentTimeSeconds > currentProgress.MaxWatchedSeconds + maxAllowedJump)
                {
                    return new ProgressValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Suspicious progress jump detected. Please watch the video sequentially.",
                        ResetToTime = currentProgress.MaxWatchedSeconds
                    };
                }
            }

            // Check for excessive skip violations
            if (request.SkipViolations > 20)
            {
                return new ProgressValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Too many skip attempts detected. Please contact your instructor.",
                    ResetToTime = currentProgress?.MaxWatchedSeconds ?? 0
                };
            }

            // Validate session duration vs watch time ratio
            var sessionDuration = DateTime.UtcNow - request.SessionStartTime;
            if (request.TotalWatchTime > sessionDuration.TotalSeconds * 1.5)
            {
                return new ProgressValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Invalid watch time detected. Please refresh and try again.",
                    ResetToTime = currentProgress?.MaxWatchedSeconds ?? 0
                };
            }

            return new ProgressValidationResult { IsValid = true };
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Request/Response Models
    // ═══════════════════════════════════════════════════════════════════════
    public class VideoProgressValidationRequest
    {
        public int MaterialId { get; set; }
        public int CurrentTimeSeconds { get; set; }
        public int MaxAllowedSeconds { get; set; }
        public int TotalWatchTime { get; set; }
        public int SkipViolations { get; set; }
        public DateTime SessionStartTime { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ProgressValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int ResetToTime { get; set; }
    }
}