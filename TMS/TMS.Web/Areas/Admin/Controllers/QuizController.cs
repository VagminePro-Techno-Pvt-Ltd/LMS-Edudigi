using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TMS.Repository.Managers;
using TMS.ViewModels.Masters;
using TMS.Web.Controllers;

namespace TMS.Web.Areas.Admin.Controllers
{
    [ValidateFormAccess(Common.FormDefination.Quiz)]
    [Area("Admin")]
    public class QuizController : BaseController
    {
        private readonly IMasterBaseManager<QuizMasterViewModel> _quizManager;
        private readonly IMasterManager<CourseMasterViewModel> _courseManager;
        private readonly IMasterBaseManager<CourseQuadrantViewModel> _quadrantManager;
        private readonly IMasterBaseManager<CourseFacultyMapViewModel> _courseFacultyMapManager;

        public QuizController(
            IMasterBaseManager<QuizMasterViewModel> quizManager,
            IMasterManager<CourseMasterViewModel> courseManager,
            IMasterBaseManager<CourseQuadrantViewModel> quadrantManager,
            IMasterBaseManager<CourseFacultyMapViewModel> courseFacultyMapManager)
        {
            _quizManager = quizManager;
            _courseManager = courseManager;
            _quadrantManager = quadrantManager;
            _courseFacultyMapManager = courseFacultyMapManager;
        }

        // GET: Admin/Quiz
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Quiz Management";

            int userId = GetUserId();
            bool isFaculty = IsFacultyRole();

            IEnumerable<QuizMasterViewModel> quizzes;

            if (isFaculty)
            {
                var mappings = await _courseFacultyMapManager.GetAsync(null, x => x.FacultyId == userId && x.IsActive);
                var courseIds = mappings.Select(m => m.CourseId).Distinct().ToHashSet();

                if (courseIds.Any())
                {
                    var allQuizzes = await _quizManager.GetAsync(new[] { "Course", "CourseQuadrant", "Questions" });
                    quizzes = allQuizzes.Where(x => courseIds.Contains(x.CourseId)).ToList();
                }
                else
                {
                    quizzes = new List<QuizMasterViewModel>();
                }
            }
            else
            {
                quizzes = await _quizManager.GetAsync(new[] { "Course", "CourseQuadrant", "Questions" });
            }

            ViewBag.IsFaculty = isFaculty;
            return View(quizzes);
        }

        // GET: Admin/Quiz/Item/{id?}
        [HttpGet]
        public async Task<IActionResult> Item(string? iId, int? courseId, int? quadrantId)
        {
            QuizMasterViewModel model = new();

            if (!string.IsNullOrEmpty(iId))
            {
                iId = iId.Decrypt(true);
                if (string.IsNullOrWhiteSpace(iId))
                    return NotFound();

                int quizId = Convert.ToInt32(iId);
                var existing = await _quizManager.GetAsync(
                    quizId,
                    new[] { "Course", "CourseQuadrant", "Questions", "Questions.Options" }
                );

                if (existing != null)
                    model = existing;
            }
            else
            {
                if (courseId.HasValue && courseId.Value > 0)
                    model.CourseId = courseId.Value;
                if (quadrantId.HasValue && quadrantId.Value > 0)
                    model.CourseQuadrantId = quadrantId.Value;

                // Initialize with one default question and two options
                model.Questions = new List<QuizQuestionViewModel>
                {
                    new QuizQuestionViewModel
                    {
                        QuestionType = TMS.Models.Masters.QuestionType.Objective,
                        MaxMarks = 1,
                        Options = new List<QuizOptionViewModel>
                        {
                            new QuizOptionViewModel(),
                            new QuizOptionViewModel()
                        }
                    }
                };
            }

            await SetDropdowns(model);
            return View(model);
        }

        // POST: Admin/Quiz/Item
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Item(QuizMasterViewModel model)
        {
            int userId = GetUserId();

            // Validate duplicate quiz per course/quadrant
            if (model.Id == 0)
            {
                bool duplicate = await ValidateModel(model);
                if (duplicate)
                {
                    await SetDropdowns(model);
                    return View(model);
                }
            }

            bool result = await _quizManager.AddUpdateQuizAsync(model, userId);

            if (result)
            {
                TempData["Success"] = "Quiz saved successfully.";
                SetApplicationResult(true, "Quiz saved successfully.");
                return RedirectToAction(nameof(Index));
            }

            SetApplicationResult(false, "Error while saving quiz.");
            await SetDropdowns(model);
            return View(model);
        }

        // POST: Admin/Quiz/Delete
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var quiz = await _quizManager.GetAsync(id);
            if (quiz == null)
                return Json(new { success = false, message = "Quiz not found." });

            if (IsFacultyRole())
            {
                int userId = GetUserId();
                var mappings = await _courseFacultyMapManager.GetAsync(null, x => x.FacultyId == userId && x.CourseId == quiz.CourseId && x.IsActive);
                if (!mappings.Any())
                    return Json(new { success = false, message = "You do not have permission to delete this quiz." });
            }

            bool result = await _quizManager.DeleteAsync(id);
            return Json(new { success = result, message = result ? "Quiz deleted successfully." : "Failed to delete quiz." });
        }

        // Dropdown binding
        private async Task SetDropdowns(QuizMasterViewModel model)
        {
            int userId = GetUserId();
            bool isFaculty = IsFacultyRole();

            List<CourseMasterViewModel> courses;

            if (isFaculty)
            {
                var mappings = await _courseFacultyMapManager.GetAsync(new[] { "Course" }, x => x.FacultyId == userId && x.IsActive);
                var courseIds = mappings.Select(m => m.CourseId).Distinct().ToList();

                var allCourses = await _courseManager.GetAsync(null, t => t.IsActive);
                courses = allCourses.Where(c => courseIds.Contains(c.Id) || c.Id == model.CourseId).ToList();
            }
            else
            {
                courses = await _courseManager.GetAsync(null, x => x.IsActive || x.Id == model.CourseId);
            }

            ViewBag.CourseId = courses ?? new List<CourseMasterViewModel>();

            // Load quadrants
            var quadrants = await _quadrantManager.GetAsync(null, q => q.IsActive);
            ViewBag.QuadrantId = quadrants ?? new List<CourseQuadrantViewModel>();
        }

        private async Task<bool> ValidateModel(QuizMasterViewModel model)
        {
            var exists = await _quizManager.CheckExpression(t =>
                (model.Id == 0 || t.Id != model.Id) &&
                t.CourseId == model.CourseId &&
                t.CourseQuadrantId == model.CourseQuadrantId &&
                t.Title == model.Title
            );

            if (exists)
                ModelState.AddModelError("Title", "A quiz with this title already exists for the selected course/quadrant.");

            return exists;
        }

        // AJAX: Get Quadrants by Course
        [HttpGet]
        public async Task<JsonResult> GetQuadrantsByCourse(int courseId)
        {
            var quadrants = await _quadrantManager.GetAsync(null, q => q.IsActive);
            var list = quadrants.Select(q => new { q.Id, q.Name }).ToList();
            return Json(list);
        }

        [HttpGet]
        public IActionResult RenderQuestionPartial(int index)
        {
            var model = new QuizQuestionViewModel
            {
                QuestionText = string.Empty,
                QuestionType = TMS.Models.Masters.QuestionType.Objective,
                MaxMarks = 1,
                Options = new List<QuizOptionViewModel>
                {
                    new QuizOptionViewModel(),
                    new QuizOptionViewModel()
                },
                CorrectOptionId = null
            };

            ViewData["qIndex"] = index;
            return PartialView("_QuestionTemplate", model);
        }
    }
}
