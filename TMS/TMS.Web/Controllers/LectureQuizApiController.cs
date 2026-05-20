using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.Models.Academics;
using TMS.Repository;

namespace TMS.Web.Controllers
{
    /// <summary>
    /// Non-Area API controller for in-video quiz interactions by students.
    /// Accessible at /api/lecture-quiz/... by any authenticated user (student or faculty).
    /// </summary>
    [Route("api/lecture-quiz")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class LectureQuizApiController : BaseController
    {
        private readonly ApplicationDBContext _db;
        private readonly ILogger<LectureQuizApiController> _logger;

        public LectureQuizApiController(ApplicationDBContext db, ILogger<LectureQuizApiController> logger)
        {
            _db     = db;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/lecture-quiz/questions/{materialId}
        // Returns all active questions for the material, marking already-answered ones.
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet]
        [Route("questions/{materialId:int}")]
        public async Task<IActionResult> GetQuestions(int materialId)
        {
            int studentId = GetUserId();
            if (studentId == 0)
                return Json(new { success = false, message = "Not authenticated." });

            try
            {
                var questions = await _db.LectureQuizQuestions
                    .Where(q => q.LectureMaterialId == materialId && q.IsActive)
                    .OrderBy(q => q.ShowAtSeconds)
                    .ThenBy(q => q.SortOrder)
                    .ToListAsync();

                var attemptedIds = await _db.LectureQuizAttempts
                    .Where(a => a.StudentId == studentId
                             && a.Question!.LectureMaterialId == materialId)
                    .Select(a => a.LectureQuizQuestionId)
                    .ToListAsync();

                var result = questions.Select(q => new
                {
                    id              = q.Id,
                    questionText    = q.QuestionText,
                    showAtSeconds   = q.ShowAtSeconds,
                    optionA         = q.OptionA,
                    optionB         = q.OptionB,
                    optionC         = q.OptionC,
                    optionD         = q.OptionD,
                    alreadyAnswered = attemptedIds.Contains(q.Id)
                });

                return Json(new { success = true, questions = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching quiz questions for materialId={MaterialId}", materialId);
                return Json(new { success = false, message = "Server error loading questions." });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/lecture-quiz/submit
        // Submits a student's answer. Idempotent — duplicate submissions return
        // the previously recorded result.
        // Body: { questionId: int, selectedOption: "A"|"B"|"C"|"D" }
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost]
        [Route("submit")]
        public async Task<IActionResult> SubmitAnswer([FromBody] LectureQuizSubmitRequest req)
        {
            // Always return JSON — never redirect
            Response.ContentType = "application/json";

            int studentId = GetUserId();
            if (studentId == 0)
                return Json(new { success = false, message = "Not authenticated. Please log in again." });

            if (req == null || req.QuestionId <= 0 || string.IsNullOrWhiteSpace(req.SelectedOption))
                return Json(new { success = false, message = "Invalid request. QuestionId and SelectedOption are required." });

            var validOptions = new[] { "A", "B", "C", "D" };
            var selected = req.SelectedOption.Trim().ToUpper();
            if (!validOptions.Contains(selected))
                return Json(new { success = false, message = "Invalid option. Must be A, B, C, or D." });

            try
            {
                var question = await _db.LectureQuizQuestions
                    .FirstOrDefaultAsync(q => q.Id == req.QuestionId && q.IsActive);

                if (question == null)
                    return Json(new { success = false, message = "Question not found or has been removed." });

                // Idempotent: return existing answer if already submitted
                var existing = await _db.LectureQuizAttempts
                    .FirstOrDefaultAsync(a => a.StudentId == studentId
                                           && a.LectureQuizQuestionId == req.QuestionId);
                if (existing != null)
                {
                    _logger.LogInformation("Duplicate quiz attempt: student={StudentId} question={QuestionId}", studentId, req.QuestionId);
                    return Json(new
                    {
                        success       = true,
                        isCorrect     = existing.IsCorrect,
                        correctOption = question.CorrectOption,
                        alreadyDone   = true,
                        message       = "Quiz already completed."
                    });
                }

                bool isCorrect = string.Equals(selected, question.CorrectOption?.Trim(),
                    StringComparison.OrdinalIgnoreCase);

                var attempt = new LectureQuizAttempt
                {
                    StudentId             = studentId,
                    LectureQuizQuestionId = req.QuestionId,
                    SelectedOption        = selected,
                    IsCorrect             = isCorrect,
                    IsActive              = true,
                    CreatedBy             = studentId,
                    CreatedOn             = DateTime.UtcNow
                };

                _db.LectureQuizAttempts.Add(attempt);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Quiz answer saved: student={StudentId} question={QuestionId} correct={IsCorrect}",
                    studentId, req.QuestionId, isCorrect);

                return Json(new
                {
                    success       = true,
                    isCorrect     = isCorrect,
                    correctOption = question.CorrectOption,
                    alreadyDone   = false,
                    message       = isCorrect ? "Correct! Well done." : "Answer submitted."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving quiz attempt: student={StudentId} question={QuestionId}", studentId, req.QuestionId);
                return Json(new { success = false, message = "Server error saving your answer. Please try again." });
            }
        }
    }

    /// <summary>Request model for quiz answer submission.</summary>
    public class LectureQuizSubmitRequest
    {
        public int    QuestionId     { get; set; }
        public string SelectedOption { get; set; } = "";
    }
}
