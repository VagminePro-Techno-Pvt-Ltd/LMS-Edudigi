using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.Models.Academics;
using TMS.Repository;
using TMS.Web.Controllers;

namespace TMS.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class VideoQuizController : BaseController
    {
        private readonly ApplicationDBContext _db;
        private readonly ILogger<VideoQuizController> _logger;

        public VideoQuizController(ApplicationDBContext db, ILogger<VideoQuizController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // INDEX: List all video LectureMaterials with quiz question counts
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Video Quiz Manager";

            var videos = await _db.LectureMaterials
                .Where(m => m.IsActive && (m.MaterialType == "Video" || m.MaterialType == "VideoURL"))
                .OrderByDescending(m => m.Id)
                .Select(m => new VideoListItemVm
                {
                    Id          = m.Id,
                    Title       = m.Title ?? "Untitled",
                    MaterialType = m.MaterialType ?? "Video",
                    FilePath    = m.FilePath,
                    QuestionCount = _db.LectureQuizQuestions
                                      .Count(q => q.LectureMaterialId == m.Id && q.IsActive)
                })
                .ToListAsync();

            return View(videos);
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // MANAGE: Show + add + delete quiz questions for one video
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        [HttpGet]
        public async Task<IActionResult> Manage(int materialId)
        {
            var material = await _db.LectureMaterials.FirstOrDefaultAsync(m => m.Id == materialId && m.IsActive);
            if (material == null) return NotFound();

            var questions = await _db.LectureQuizQuestions
                .Where(q => q.LectureMaterialId == materialId && q.IsActive)
                .OrderBy(q => q.ShowAtSeconds).ThenBy(q => q.SortOrder)
                .ToListAsync();

            var vm = new ManageVideoQuizVm
            {
                MaterialId   = materialId,
                VideoTitle   = material.Title ?? "Untitled",
                MaterialType = material.MaterialType ?? "Video",
                FilePath     = material.FilePath,
                Questions    = questions
            };
            return View(vm);
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // ADD QUESTION (POST)
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestion(AddQuizQuestionVm model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fill in all required fields.";
                return RedirectToAction(nameof(Manage), new { materialId = model.LectureMaterialId });
            }

            var material = await _db.LectureMaterials.FirstOrDefaultAsync(m => m.Id == model.LectureMaterialId && m.IsActive);
            if (material == null) return NotFound();

            // Validate CorrectOption
            var validOptions = new[] { "A", "B", "C", "D" };
            var correct = (model.CorrectOption ?? "").Trim().ToUpper();
            if (!validOptions.Contains(correct))
            {
                TempData["Error"] = "CorrectOption must be A, B, C, or D.";
                return RedirectToAction(nameof(Manage), new { materialId = model.LectureMaterialId });
            }

            int facultyId = GetUserId();
            var question = new LectureQuizQuestion
            {
                LectureMaterialId = model.LectureMaterialId,
                QuestionText      = model.QuestionText.Trim(),
                OptionA           = model.OptionA.Trim(),
                OptionB           = model.OptionB?.Trim(),
                OptionC           = model.OptionC?.Trim(),
                OptionD           = model.OptionD?.Trim(),
                CorrectOption     = correct,
                ShowAtSeconds     = model.ShowAtSeconds,
                SortOrder         = 0,
                IsActive          = true,
                CreatedBy         = facultyId,
                CreatedOn         = DateTime.UtcNow
            };

            _db.LectureQuizQuestions.Add(question);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Quiz question added at {FormatTime(model.ShowAtSeconds)}.";
            return RedirectToAction(nameof(Manage), new { materialId = model.LectureMaterialId });
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // DELETE QUESTION (POST)
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuestion(int questionId, int materialId)
        {
            var question = await _db.LectureQuizQuestions.FirstOrDefaultAsync(q => q.Id == questionId);
            if (question != null)
            {
                question.IsActive  = false;
                question.UpdatedBy = GetUserId();
                question.UpdatedOn = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                TempData["Success"] = "Question removed.";
            }
            return RedirectToAction(nameof(Manage), new { materialId });
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // HELPER
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private static string FormatTime(int seconds)
        {
            var ts = TimeSpan.FromSeconds(seconds);
            return ts.TotalHours >= 1
                ? $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s"
                : $"{ts.Minutes}m {ts.Seconds}s";
        }
    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    // View-models (local to this controller file for simplicity)
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public class VideoListItemVm
    {
        public int    Id            { get; set; }
        public string Title         { get; set; } = "";
        public string MaterialType  { get; set; } = "";
        public string? FilePath     { get; set; }
        public int    QuestionCount { get; set; }
    }

    public class ManageVideoQuizVm
    {
        public int    MaterialId   { get; set; }
        public string VideoTitle   { get; set; } = "";
        public string MaterialType { get; set; } = "";
        public string? FilePath    { get; set; }
        public List<LectureQuizQuestion> Questions { get; set; } = new();
    }

    public class AddQuizQuestionVm
    {
        public int    LectureMaterialId { get; set; }
        public string QuestionText      { get; set; } = "";
        public string OptionA           { get; set; } = "";
        public string? OptionB          { get; set; }
        public string? OptionC          { get; set; }
        public string? OptionD          { get; set; }
        public string CorrectOption     { get; set; } = "";
        public int    ShowAtSeconds     { get; set; }
    }
}

