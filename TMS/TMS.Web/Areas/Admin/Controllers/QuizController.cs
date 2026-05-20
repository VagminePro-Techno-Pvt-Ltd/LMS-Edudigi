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
            string? userRole = User.FindFirstValue(ClaimTypes.Role);

            IEnumerable<QuizMasterViewModel> quizzes;

            if (userRole?.Equals("Faculty", StringComparison.OrdinalIgnoreCase) == true)
            {
                var mappings = await _courseFacultyMapManager.GetAsync(new[] { "Course" }, x => x.FacultyId == userId);
                var courseIds = mappings.Select(m => m.CourseId).Distinct().ToList();

                quizzes = await _quizManager.GetAsync(
                    new[] { "Course", "CourseQuadrant", "Questions" },
                    x => courseIds.Contains(x.CourseId)
                );
            }
            else
            {
                quizzes = await _quizManager.GetAsync(new[] { "Course", "CourseQuadrant", "Questions" });
            }

            return View(quizzes);
        }

        // GET: Admin/Quiz/Item/{id?}
        [HttpGet]
        public async Task<IActionResult> Item(string? iId)
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
                // Initialize with one question and one option
                model.Questions = new List<QuizQuestionViewModel>
                {
                    new QuizQuestionViewModel
                    {
                        Options = new List<QuizOptionViewModel>
                        {
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
            string? userRole = GetUserRole();

            //if (!ModelState.IsValid)
            //{
            //    await SetDropdowns(model);
            //    return View(model);
            //}

            // Validate duplicate quiz per course/quadrant
            if(model.Id==0)
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

        // Dropdown binding
        private async Task SetDropdowns(QuizMasterViewModel model)
        {
            int userId = GetUserId();
            string? userRole = GetUserRole();

            List<CourseMasterViewModel> courses;

            if (userRole?.Equals("Faculty", StringComparison.OrdinalIgnoreCase) == true)
            {
                var mappings = await _courseFacultyMapManager.GetAsync(new[] { "Course" }, x => x.FacultyId == userId);
                var courseIds = mappings.Select(m => m.CourseId).Distinct().ToList();

                var allCourses = await _courseManager.GetAsync(null, t => t.IsActive);
                courses = allCourses.Where(c => courseIds.Contains(c.Id)).ToList();
            }
            else
            {
                courses = await _courseManager.GetAsync(null, x => x.IsActive || x.Id == model.CourseId);
            }

            ViewBag.CourseId = courses ?? new List<CourseMasterViewModel>();

            // Load only quadrants that belong to selected course (if any)
            IEnumerable<CourseQuadrantViewModel> quadrants;

            if (model.CourseId > 0)
            {
                quadrants = await _quadrantManager.GetAsync(null, q => q.IsActive );
            }
            else
            {
                quadrants = await _quadrantManager.GetAsync(null, q => q.IsActive);
            }

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
            var quadrants = await _quadrantManager.GetAsync(null, q =>  q.IsActive);
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
                Options = new List<QuizOptionViewModel>
        {
            new QuizOptionViewModel()
        },
                CorrectOptionId = null
            };

            ViewData["qIndex"] = index;
            return PartialView("_QuestionTemplate", model);
        }

    }
}
