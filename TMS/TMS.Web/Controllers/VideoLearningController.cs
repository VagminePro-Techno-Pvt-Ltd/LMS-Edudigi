using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.Models.Academics;
using TMS.Repository;
using TMS.Web.Models.Learning;

namespace TMS.Web.Controllers
{
    /// <summary>
    /// Production-level API controller for LMS student video learning.
    /// Flow: Dashboard → Courses → Subject → Unit → Topic → Video → In-video Quiz
    ///
    /// SECURITY RULES:
    ///   - StudentId ALWAYS comes from authenticated session (GetUserId()), never from frontend.
    ///   - Every resource is validated against the student's enrolled course/semester chain.
    ///   - CorrectOption is NEVER exposed in any response.
    ///   - Anti-skip is enforced server-side with a 5-second buffer.
    /// </summary>
    [Route("api/learning")]
    [ApiController]
    [Authorize]
    public class VideoLearningController : BaseController
    {
        private readonly ApplicationDBContext _db;
        private readonly ILogger<VideoLearningController> _logger;

        // Buffer (seconds) allowed beyond MaxAllowedSeconds before clamping
        private const int SKIP_BUFFER = 5;

        public VideoLearningController(
            ApplicationDBContext db,
            ILogger<VideoLearningController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns all semester IDs the student is enrolled in (via CourseEnrollment → Course).
        /// </summary>
        private async Task<List<int>> GetStudentSemesterIdsAsync(int studentId)
        {
            return await _db.CourseEnrollments
                .Where(e => e.StudentId == studentId && e.IsActive)
                .Include(e => e.Course)
                .Where(e => e.Course != null && e.Course.SemesterId.HasValue)
                .Select(e => e.Course!.SemesterId!.Value)
                .Distinct()
                .ToListAsync();
        }

        /// <summary>
        /// Validates that a video belongs to the logged-in student's allowed
        /// semester chain: Video → Topic → Unit → Subject → Semester.
        /// Returns the VideoMaster with Topic included, or null if unauthorized.
        /// </summary>
        private async Task<VideoMaster?> GetAuthorizedVideoAsync(int videoId, int studentId)
        {
            var semesterIds = await GetStudentSemesterIdsAsync(studentId);
            if (!semesterIds.Any()) return null;

            return await _db.VideoMasters
                .Include(v => v.Topic)
                    .ThenInclude(t => t!.Unit)
                        .ThenInclude(u => u!.Subject)
                .FirstOrDefaultAsync(v =>
                    v.Id == videoId &&
                    v.IsActive &&
                    v.Topic != null &&
                    v.Topic.Unit != null &&
                    v.Topic.Unit.Subject != null &&
                    semesterIds.Contains(v.Topic.Unit.Subject.SemesterId));
        }

        /// <summary>
        /// Calculates the initial MaxAllowedSeconds for a new VideoProgress record.
        /// = first quiz trigger time, or full video duration if no quizzes exist.
        /// </summary>
        private async Task<int> GetInitialMaxAllowedSecondsAsync(int videoId, int videoDuration)
        {
            var firstTrigger = await _db.VideoQuizQuestions
                .Where(q => q.VideoId == videoId && q.IsActive)
                .OrderBy(q => q.TriggerTimeSeconds)
                .Select(q => (int?)q.TriggerTimeSeconds)
                .FirstOrDefaultAsync();

            return firstTrigger ?? videoDuration;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 1. GET /api/learning/subjects
        //    Returns all subjects available to the logged-in student.
        //    Chain: Student → CourseEnrollment → Course → Semester → SubjectMaster
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("subjects")]
        public async Task<IActionResult> GetSubjects()
        {
            try
            {
                int studentId = GetUserId();
                if (studentId == 0)
                    return Unauthorized(ApiResponse<object>.Fail("Not authenticated."));

                var semesterIds = await GetStudentSemesterIdsAsync(studentId);
                if (!semesterIds.Any())
                    return Ok(ApiResponse<List<SubjectDto>>.Success(
                        new List<SubjectDto>(), "No enrolled courses found."));

                var subjects = await _db.SubjectMasters
                    .Where(s => semesterIds.Contains(s.SemesterId) && s.IsActive)
                    .OrderBy(s => s.SortOrder).ThenBy(s => s.Name)
                    .Select(s => new SubjectDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Description = s.Description,
                        SubjectCode = s.SubjectCode,
                        SortOrder = s.SortOrder
                    })
                    .ToListAsync();

                return Ok(ApiResponse<List<SubjectDto>>.Success(subjects));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSubjects");
                return StatusCode(500, ApiResponse<object>.Fail("Error fetching subjects."));
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 2. GET /api/learning/subjects/{subjectId}/units
        //    Returns units for a subject. Validates subject belongs to student.
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("subjects/{subjectId}/units")]
        public async Task<IActionResult> GetUnits(int subjectId)
        {
            try
            {
                int studentId = GetUserId();
                if (studentId == 0)
                    return Unauthorized(ApiResponse<object>.Fail("Not authenticated."));

                var semesterIds = await GetStudentSemesterIdsAsync(studentId);

                // Ownership check: subject must belong to student's enrolled semester
                var subjectOk = await _db.SubjectMasters
                    .AnyAsync(s => s.Id == subjectId &&
                                   semesterIds.Contains(s.SemesterId) &&
                                   s.IsActive);
                if (!subjectOk)
                    return NotFound(ApiResponse<object>.Fail(
                        "Subject not found or not accessible."));

                var units = await _db.UnitMasters
                    .Where(u => u.SubjectId == subjectId && u.IsActive)
                    .OrderBy(u => u.SortOrder).ThenBy(u => u.Name)
                    .Select(u => new UnitDto
                    {
                        Id = u.Id,
                        Name = u.Name,
                        Description = u.Description,
                        SortOrder = u.SortOrder
                    })
                    .ToListAsync();

                return Ok(ApiResponse<List<UnitDto>>.Success(units));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUnits subjectId={Id}", subjectId);
                return StatusCode(500, ApiResponse<object>.Fail("Error fetching units."));
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 3. GET /api/learning/units/{unitId}/topics
        //    Returns topics for a unit. Validates unit → subject → semester chain.
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("units/{unitId}/topics")]
        public async Task<IActionResult> GetTopics(int unitId)
        {
            try
            {
                int studentId = GetUserId();
                if (studentId == 0)
                    return Unauthorized(ApiResponse<object>.Fail("Not authenticated."));

                var semesterIds = await GetStudentSemesterIdsAsync(studentId);

                // Ownership check: unit → subject → semester
                var unitOk = await _db.UnitMasters
                    .Include(u => u.Subject)
                    .AnyAsync(u => u.Id == unitId &&
                                   u.IsActive &&
                                   u.Subject != null &&
                                   semesterIds.Contains(u.Subject.SemesterId));
                if (!unitOk)
                    return NotFound(ApiResponse<object>.Fail(
                        "Unit not found or not accessible."));

                var topics = await _db.TopicMasters
                    .Where(t => t.UnitId == unitId && t.IsActive)
                    .OrderBy(t => t.SortOrder).ThenBy(t => t.Name)
                    .Select(t => new TopicDto
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Description = t.Description,
                        SortOrder = t.SortOrder,
                        HasVideo = _db.VideoMasters
                                      .Any(v => v.TopicId == t.Id && v.IsActive)
                    })
                    .ToListAsync();

                return Ok(ApiResponse<List<TopicDto>>.Success(topics));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTopics unitId={Id}", unitId);
                return StatusCode(500, ApiResponse<object>.Fail("Error fetching topics."));
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 4. GET /api/learning/topics/{topicId}/video
        //    Returns the active video for a topic. Full chain validation.
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("topics/{topicId}/video")]
        public async Task<IActionResult> GetVideoByTopic(int topicId)
        {
            try
            {
                int studentId = GetUserId();
                if (studentId == 0)
                    return Unauthorized(ApiResponse<object>.Fail("Not authenticated."));

                var semesterIds = await GetStudentSemesterIdsAsync(studentId);

                // Validate topic → unit → subject → semester
                var topicOk = await _db.TopicMasters
                    .Include(t => t.Unit).ThenInclude(u => u!.Subject)
                    .AnyAsync(t => t.Id == topicId &&
                                   t.IsActive &&
                                   t.Unit != null &&
                                   t.Unit.Subject != null &&
                                   semesterIds.Contains(t.Unit.Subject.SemesterId));
                if (!topicOk)
                    return NotFound(ApiResponse<object>.Fail(
                        "Topic not found or not accessible."));

                var video = await _db.VideoMasters
                    .Where(v => v.TopicId == topicId && v.IsActive)
                    .Select(v => new VideoDto
                    {
                        Id = v.Id,
                        Title = v.Title,
                        FilePath = v.FilePath,
                        VideoUrl = v.VideoUrl,
                        DurationSeconds = v.DurationSeconds,
                        Source = v.Source
                    })
                    .FirstOrDefaultAsync();

                if (video == null)
                    return NotFound(ApiResponse<object>.Fail(
                        "No video available for this topic."));

                return Ok(ApiResponse<VideoDto>.Success(video));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetVideoByTopic topicId={Id}", topicId);
                return StatusCode(500, ApiResponse<object>.Fail("Error fetching video."));
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 5. GET /api/learning/videos/{videoId}/progress
        //    Returns watch progress. Creates default record if none exists.
        //    Initial MaxAllowedSeconds = first quiz trigger time OR full duration.
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("videos/{videoId}/progress")]
        public async Task<IActionResult> GetVideoProgress(int videoId)
        {
            try
            {
                int studentId = GetUserId();
                if (studentId == 0)
                    return Unauthorized(ApiResponse<object>.Fail("Not authenticated."));

                var video = await GetAuthorizedVideoAsync(videoId, studentId);
                if (video == null)
                    return NotFound(ApiResponse<object>.Fail(
                        "Video not found or not accessible."));

                var progress = await _db.VideoProgresses
                    .FirstOrDefaultAsync(p =>
                        p.StudentId == studentId && p.VideoId == videoId);

                if (progress == null)
                {
                    // First time — create default progress record
                    int maxAllowed = await GetInitialMaxAllowedSecondsAsync(
                        videoId, video.DurationSeconds);

                    progress = new VideoProgress
                    {
                        StudentId   = studentId,
                        VideoId     = videoId,
                        LastWatchedSeconds = 0,
                        MaxAllowedSeconds  = maxAllowed,
                        IsCompleted = false,
                        LastUpdatedOn = DateTime.UtcNow,
                        CreatedBy   = studentId,
                        CreatedOn   = DateTime.UtcNow,
                        IsActive    = true
                    };
                    _db.VideoProgresses.Add(progress);
                    await _db.SaveChangesAsync();
                }

                return Ok(ApiResponse<VideoProgressDto>.Success(new VideoProgressDto
                {
                    VideoId            = videoId,
                    LastWatchedSeconds = progress.LastWatchedSeconds,
                    MaxAllowedSeconds  = progress.MaxAllowedSeconds,
                    IsCompleted        = progress.IsCompleted
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetVideoProgress videoId={Id}", videoId);
                return StatusCode(500, ApiResponse<object>.Fail("Error fetching progress."));
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 6. POST /api/learning/videos/{videoId}/progress
        //    Saves current watch position safely.
        //    Anti-skip: clamps if currentTimeSeconds > maxAllowed + 5.
        //    Completion: marks done if within last 5 seconds of video.
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost("videos/{videoId}/progress")]
        public async Task<IActionResult> SaveVideoProgress(
            int videoId, [FromBody] SaveProgressRequest request)
        {
            try
            {
                int studentId = GetUserId();
                if (studentId == 0)
                    return Unauthorized(ApiResponse<object>.Fail("Not authenticated."));

                if (request.CurrentTimeSeconds < 0)
                    return BadRequest(ApiResponse<object>.Fail(
                        "currentTimeSeconds cannot be negative."));

                var video = await GetAuthorizedVideoAsync(videoId, studentId);
                if (video == null)
                    return NotFound(ApiResponse<object>.Fail(
                        "Video not found or not accessible."));

                var progress = await _db.VideoProgresses
                    .FirstOrDefaultAsync(p =>
                        p.StudentId == studentId && p.VideoId == videoId);

                if (progress == null)
                {
                    // Auto-create if missing (e.g. player started before GET /progress was called)
                    int maxAllowed = await GetInitialMaxAllowedSecondsAsync(
                        videoId, video.DurationSeconds);
                    progress = new VideoProgress
                    {
                        StudentId  = studentId,
                        VideoId    = videoId,
                        LastWatchedSeconds = 0,
                        MaxAllowedSeconds  = maxAllowed,
                        IsCompleted = false,
                        LastUpdatedOn = DateTime.UtcNow,
                        CreatedBy  = studentId,
                        CreatedOn  = DateTime.UtcNow,
                        IsActive   = true
                    };
                    _db.VideoProgresses.Add(progress);
                }

                // ── Anti-skip enforcement ─────────────────────────────────────
                // Allow SKIP_BUFFER seconds grace for network/buffering jitter.
                // If the frontend sends a position further ahead → clamp silently.
                int safeTime = request.CurrentTimeSeconds;
                if (safeTime > progress.MaxAllowedSeconds + SKIP_BUFFER)
                    safeTime = progress.MaxAllowedSeconds;

                // Only advance — never allow rewinding the saved position
                if (safeTime > progress.LastWatchedSeconds)
                    progress.LastWatchedSeconds = safeTime;

                // ── Completion check ──────────────────────────────────────────
                // Mark complete when student reaches the final 5 seconds
                if (video.DurationSeconds > 0 &&
                    progress.LastWatchedSeconds >= video.DurationSeconds - 5)
                {
                    progress.IsCompleted       = true;
                    progress.LastWatchedSeconds = video.DurationSeconds;
                }

                progress.LastUpdatedOn = DateTime.UtcNow;
                progress.UpdatedBy     = studentId;
                progress.UpdatedOn     = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                string msg = progress.IsCompleted ? "Video completed!" : "Progress saved.";
                return Ok(ApiResponse<VideoProgressDto>.Success(new VideoProgressDto
                {
                    VideoId            = videoId,
                    LastWatchedSeconds = progress.LastWatchedSeconds,
                    MaxAllowedSeconds  = progress.MaxAllowedSeconds,
                    IsCompleted        = progress.IsCompleted
                }, msg));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveVideoProgress videoId={Id}", videoId);
                return StatusCode(500, ApiResponse<object>.Fail("Error saving progress."));
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 7. GET /api/learning/videos/{videoId}/quiz?triggerTimeSeconds=600
        //    Returns up to 2 questions at the given trigger time.
        //    CorrectOption is NEVER included in the response.
        //    AlreadyAttempted flag is set per question for the calling student.
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("videos/{videoId}/quiz")]
        public async Task<IActionResult> GetQuizQuestions(
            int videoId, [FromQuery] int triggerTimeSeconds)
        {
            try
            {
                int studentId = GetUserId();
                if (studentId == 0)
                    return Unauthorized(ApiResponse<object>.Fail("Not authenticated."));

                var video = await GetAuthorizedVideoAsync(videoId, studentId);
                if (video == null)
                    return NotFound(ApiResponse<object>.Fail(
                        "Video not found or not accessible."));

                // Fetch questions for this video + trigger time (max 2)
                var questions = await _db.VideoQuizQuestions
                    .Where(q => q.VideoId == videoId &&
                                q.TriggerTimeSeconds == triggerTimeSeconds &&
                                q.IsActive)
                    .OrderBy(q => q.SortOrder)
                    .Take(2)
                    .ToListAsync();

                if (!questions.Any())
                    return Ok(ApiResponse<List<VideoQuizQuestionDto>>.Success(
                        new List<VideoQuizQuestionDto>(),
                        "No quiz at this trigger time."));

                // Load this student's existing attempts for these question IDs
                var questionIds = questions.Select(q => q.Id).ToList();
                var attempts = await _db.VideoQuizAttempts
                    .Where(a => a.StudentId == studentId &&
                                questionIds.Contains(a.VideoQuizQuestionId))
                    .ToListAsync();

                // Build response — deliberately skipping CorrectOption
                var dtos = questions.Select(q =>
                {
                    var attempt = attempts.FirstOrDefault(a =>
                        a.VideoQuizQuestionId == q.Id);

                    // Build option list dynamically, skip null options
                    var options = new List<VideoQuizOptionDto>();
                    if (!string.IsNullOrEmpty(q.OptionA))
                        options.Add(new VideoQuizOptionDto { Key = "A", Text = q.OptionA });
                    if (!string.IsNullOrEmpty(q.OptionB))
                        options.Add(new VideoQuizOptionDto { Key = "B", Text = q.OptionB });
                    if (!string.IsNullOrEmpty(q.OptionC))
                        options.Add(new VideoQuizOptionDto { Key = "C", Text = q.OptionC });
                    if (!string.IsNullOrEmpty(q.OptionD))
                        options.Add(new VideoQuizOptionDto { Key = "D", Text = q.OptionD });

                    return new VideoQuizQuestionDto
                    {
                        Id                 = q.Id,
                        QuestionText       = q.QuestionText,
                        TriggerTimeSeconds = q.TriggerTimeSeconds,
                        Options            = options,
                        AlreadyAttempted   = attempt != null,
                        PreviousAnswer     = attempt?.SelectedOption
                        // CorrectOption intentionally omitted
                    };
                }).ToList();

                return Ok(ApiResponse<List<VideoQuizQuestionDto>>.Success(dtos));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetQuizQuestions videoId={Id}", videoId);
                return StatusCode(500, ApiResponse<object>.Fail("Error fetching quiz."));
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // 8. POST /api/learning/videos/{videoId}/quiz/submit
        //    Saves quiz answers. Evaluates correctness server-side.
        //    Skips questions already attempted (no duplicate attempts).
        //    After submission → unlocks next segment:
        //      nextTrigger = smallest TriggerTimeSeconds > current trigger
        //      If none → unlock full video duration (MaxAllowedSeconds = DurationSeconds)
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost("videos/{videoId}/quiz/submit")]
        public async Task<IActionResult> SubmitQuiz(
            int videoId, [FromBody] QuizSubmitRequest request)
        {
            try
            {
                int studentId = GetUserId();
                if (studentId == 0)
                    return Unauthorized(ApiResponse<object>.Fail("Not authenticated."));

                if (request?.Answers == null || !request.Answers.Any())
                    return BadRequest(ApiResponse<object>.Fail("No answers provided."));

                var video = await GetAuthorizedVideoAsync(videoId, studentId);
                if (video == null)
                    return NotFound(ApiResponse<object>.Fail(
                        "Video not found or not accessible."));

                // ── Validate all submitted question IDs ───────────────────────
                var submittedIds = request.Answers.Select(a => a.QuestionId).ToList();

                var dbQuestions = await _db.VideoQuizQuestions
                    .Where(q => submittedIds.Contains(q.Id) &&
                                q.VideoId == videoId &&
                                q.TriggerTimeSeconds == request.TriggerTimeSeconds &&
                                q.IsActive)
                    .ToListAsync();

                // Reject if any submitted question doesn't match videoId+trigger
                if (dbQuestions.Count != submittedIds.Distinct().Count())
                    return BadRequest(ApiResponse<object>.Fail(
                        "One or more questions do not belong to this video/trigger time."));

                // ── Check existing attempts to prevent duplicates ─────────────
                var existingAttempts = await _db.VideoQuizAttempts
                    .Where(a => a.StudentId == studentId &&
                                submittedIds.Contains(a.VideoQuizQuestionId))
                    .Select(a => a.VideoQuizQuestionId)
                    .ToListAsync();

                int correct = 0, wrong = 0, skipped = 0, newAnswers = 0;
                var newAttempts = new List<VideoQuizAttempt>();

                foreach (var answer in request.Answers)
                {
                    // Skip if already attempted (idempotency)
                    if (existingAttempts.Contains(answer.QuestionId))
                    {
                        skipped++;
                        continue;
                    }

                    var question = dbQuestions.First(q => q.Id == answer.QuestionId);

                    // Validate selected option is one of A/B/C/D
                    var validOptions = new[] { "A", "B", "C", "D" };
                    var selected = answer.SelectedOption?.Trim().ToUpper() ?? "";
                    if (!validOptions.Contains(selected))
                    {
                        return BadRequest(ApiResponse<object>.Fail(
                            $"Invalid option '{answer.SelectedOption}' for question {answer.QuestionId}. Use A, B, C, or D."));
                    }

                    // ── Server-side correctness evaluation ────────────────────
                    bool isCorrect = string.Equals(
                        selected,
                        question.CorrectOption.Trim().ToUpper(),
                        StringComparison.OrdinalIgnoreCase);

                    if (isCorrect) correct++; else wrong++;
                    newAnswers++;

                    newAttempts.Add(new VideoQuizAttempt
                    {
                        StudentId           = studentId,
                        VideoQuizQuestionId = answer.QuestionId,
                        SelectedOption      = selected,
                        IsCorrect           = isCorrect,
                        AttemptedOn         = DateTime.UtcNow,
                        CreatedBy           = studentId,
                        CreatedOn           = DateTime.UtcNow,
                        IsActive            = true
                    });
                }

                if (newAttempts.Any())
                {
                    _db.VideoQuizAttempts.AddRange(newAttempts);
                }

                // ── Unlock next video segment ─────────────────────────────────
                // Find the next quiz trigger time after the current one.
                // If none exists, unlock full video duration.
                int newMaxAllowed;
                var nextTrigger = await _db.VideoQuizQuestions
                    .Where(q => q.VideoId == videoId &&
                                q.TriggerTimeSeconds > request.TriggerTimeSeconds &&
                                q.IsActive)
                    .OrderBy(q => q.TriggerTimeSeconds)
                    .Select(q => (int?)q.TriggerTimeSeconds)
                    .FirstOrDefaultAsync();

                newMaxAllowed = nextTrigger ?? video.DurationSeconds;
                bool fullyUnlocked = !nextTrigger.HasValue;

                // Update VideoProgress.MaxAllowedSeconds (only increase, never decrease)
                var progress = await _db.VideoProgresses
                    .FirstOrDefaultAsync(p =>
                        p.StudentId == studentId && p.VideoId == videoId);

                if (progress != null)
                {
                    if (newMaxAllowed > progress.MaxAllowedSeconds)
                    {
                        progress.MaxAllowedSeconds = newMaxAllowed;
                        progress.UpdatedBy  = studentId;
                        progress.UpdatedOn  = DateTime.UtcNow;
                    }
                }
                else
                {
                    // Create progress record if somehow missing
                    progress = new VideoProgress
                    {
                        StudentId          = studentId,
                        VideoId            = videoId,
                        LastWatchedSeconds = 0,
                        MaxAllowedSeconds  = newMaxAllowed,
                        IsCompleted        = false,
                        LastUpdatedOn      = DateTime.UtcNow,
                        CreatedBy          = studentId,
                        CreatedOn          = DateTime.UtcNow,
                        IsActive           = true
                    };
                    _db.VideoProgresses.Add(progress);
                }

                await _db.SaveChangesAsync();

                string unlockMsg = fullyUnlocked
                    ? "All segments unlocked — you can now watch the full video!"
                    : $"Next {newMaxAllowed} seconds unlocked. Keep watching!";

                return Ok(ApiResponse<QuizSubmitResultDto>.Success(new QuizSubmitResultDto
                {
                    TotalSubmitted      = request.Answers.Count,
                    NewAnswers          = newAnswers,
                    SkippedDuplicates   = skipped,
                    CorrectCount        = correct,
                    WrongCount          = wrong,
                    NewMaxAllowedSeconds = newMaxAllowed,
                    VideoFullyUnlocked  = fullyUnlocked,
                    Message             = unlockMsg
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SubmitQuiz videoId={Id}", videoId);
                return StatusCode(500, ApiResponse<object>.Fail("Error submitting quiz."));
            }
        }
    }
}
