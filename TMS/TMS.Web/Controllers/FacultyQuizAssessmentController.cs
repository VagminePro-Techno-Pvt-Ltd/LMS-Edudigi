using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.Models.Academics;
using TMS.Models.Masters;
using TMS.Repository.Managers;
using TMS.ViewModels;
using TMS.ViewModels.Academics;
using TMS.ViewModels.Masters;

namespace TMS.Web.Controllers
{
    [ValidateFormAccess(Common.FormDefination.QuizAssemement)]
    public class FacultyQuizAssessmentController : BaseController
    {
        private readonly IMasterBaseManager<StudentQuizAttemptViewModel> _attemptManager;
        private readonly IMasterBaseManager<StudentQuizAnswerViewModel> _answerManager;
        private readonly IMasterBaseManager<CourseFacultyMapViewModel> _facultyMapManager;
        private readonly IMasterBaseManager<QuizMasterViewModel> _quizManager;
        private readonly IMasterManager<CourseMasterViewModel> _courseManager;
        private readonly IMasterBaseManager<CourseQuadrantViewModel> _quadrantManager;
        private readonly IMasterManager<UserViewModel> _userManager;
        private readonly IMasterBaseManager<FacultyQuizAssessmentViewModel> _facultyQuizAssessmentManager;

        public FacultyQuizAssessmentController(
            IMasterBaseManager<StudentQuizAttemptViewModel> attemptManager,
            IMasterBaseManager<StudentQuizAnswerViewModel> answerManager,
            IMasterBaseManager<CourseFacultyMapViewModel> facultyMapManager,
            IMasterBaseManager<QuizMasterViewModel> quizManager,
            IMasterManager<CourseMasterViewModel> courseManager,
            IMasterBaseManager<CourseQuadrantViewModel> quadrantManager,
            IMasterManager<UserViewModel> userManager,
            IMasterBaseManager<FacultyQuizAssessmentViewModel> facultyQuizAssessmentManager)
        {
            _attemptManager = attemptManager;
            _answerManager = answerManager;
            _facultyMapManager = facultyMapManager;
            _quizManager = quizManager;
            _courseManager = courseManager;
            _quadrantManager = quadrantManager;
            _userManager = userManager;
            _facultyQuizAssessmentManager = facultyQuizAssessmentManager;
        }

        // GET: List of student attempts for faculty courses
        public async Task<IActionResult> Index()
        {
            int facultyId = GetUserId();

            var facultyCourses = await _facultyMapManager.GetAsync(null, f => f.IsActive && f.FacultyId == facultyId);
            var courseIds = facultyCourses.Select(f => f.CourseId).ToList();

            var allAttempts = await _attemptManager.GetAsync(null, a => a.IsActive);
            var attempts = allAttempts.Where(a => courseIds.Contains(a.CourseId)).ToList();

            var allCourses = await _courseManager.GetAsync(null, c => c.IsActive);
            var courses = allCourses.Where(c => courseIds.Contains(c.Id)).ToList();

            //var allCourses = await _courseManager.GetAsync(null, c => courseIds.Contains(c.Id));
            var quadrants = await _quadrantManager.GetAsync(null, q => q.IsActive);
            var users = await _userManager.GetAsync(null, u => u.IsActive);

            var model = attempts.Select(a =>
            {
                var course = courses.FirstOrDefault(c => c.Id == a.CourseId);
                var quadrant = quadrants.FirstOrDefault(q => q.Id == a.CourseQuadrantId);
                var student = users.FirstOrDefault(u => u.Id == a.StudentId);
                var quiz = _quizManager.GetAsync(null, q => q.Id == a.QuizId).Result.FirstOrDefault();

                return new FacultyQuizAssessmentViewModel
                {
                    AttemptId = a.Id,
                    StudentId = a.StudentId,
                    StudentName = student?.Name ?? "N/A",
                    CourseId = a.CourseId,
                    CourseName = course?.Name ?? "N/A",
                    QuadrantId = a.CourseQuadrantId,
                    QuadrantName = quadrant?.Name ?? "N/A",
                    QuizId = a.QuizId,
                    QuizName = quiz?.Title ?? "N/A",
                    AttemptedOn = a.CreatedOn,
                    TotalScore = a.Score
                };
            }).ToList();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int attemptId)
        {
            // 1️⃣ Fetch attempt and related quiz
            var attempt = (await _attemptManager.GetAsync(null, a => a.Id == attemptId)).FirstOrDefault();
            if (attempt == null) return NotFound();

            var quiz = (await _quizManager.GetAsync(new[] { "Questions", "Questions.Options" }, q => q.Id == attempt.QuizId))
                .FirstOrDefault();
            if (quiz == null) return NotFound("Quiz not found");

            var answers = await _answerManager.GetAsync(null, a => a.StudentQuizAttemptId == attemptId);

            // 2️⃣ Check if faculty assessment already exists
            var existingAssessment = (await _facultyQuizAssessmentManager
                .GetAsync(new[] { "Questions" }, f => f.AttemptId == attemptId))
                .FirstOrDefault();

            bool isAssessed = existingAssessment != null;

            // 3️⃣ Prepare Question list (either from existing assessment or attempt)
            List<FacultyQuizQuestionAssessmentViewModel> questionAssessments;

            if (isAssessed)
            {
                // ✅ Use saved assessment data but include student answers
                questionAssessments = existingAssessment.Questions.Select(q =>
                {
                    var quizQuestion = quiz.Questions.FirstOrDefault(qq => qq.Id == q.QuestionId);
                    var answer = answers.FirstOrDefault(a => a.QuizQuestionId == q.QuestionId);

                    string? selectedOptionText = null;
                    string? writtenAnswer = null;

                    if (quizQuestion != null && quizQuestion.QuestionType == QuestionType.Objective)
                    {
                        var selectedOption = quizQuestion.Options.FirstOrDefault(o => o.Id == answer?.SelectedOptionId);
                        selectedOptionText = selectedOption?.OptionText;
                    }
                    else if (quizQuestion != null && quizQuestion.QuestionType == QuestionType.Subjective)
                    {
                        writtenAnswer = answer?.WrittenAnswer;
                    }

                    return new FacultyQuizQuestionAssessmentViewModel
                    {
                        Id = q.Id,
                        FacultyQuizAssessmentId = existingAssessment.Id,
                        QuestionId = q.QuestionId,
                        QuestionText = q.QuestionText,
                        MaxScore = quizQuestion?.MaxMarks ?? q.MaxScore,
                        StudentScore = q.StudentScore,
                        QuestionType = quizQuestion?.QuestionType ?? QuestionType.Subjective,
                        SelectedOptionText = selectedOptionText,
                        WrittenAnswer = writtenAnswer
                    };
                }).ToList();
            }
            else
            {
                // 🟡 Build new dynamic evaluation data from quiz + answers
                questionAssessments = quiz.Questions?.Select(q =>
                {
                    var answer = answers.FirstOrDefault(a => a.QuizQuestionId == q.Id);
                    decimal studentScore = 0;
                    string? selectedOptionText = null;
                    string? writtenAnswer = null;

                    if (answer != null)
                    {
                        if (q.QuestionType == QuestionType.Objective && q.Options != null)
                        {
                            var selectedOption = q.Options.FirstOrDefault(o => o.Id == answer.SelectedOptionId);
                            selectedOptionText = selectedOption?.OptionText;

                            // ✅ Fix: Use IsCorrectOption instead of CorrectOptionId
                            var correctOption = q.Options.FirstOrDefault(o => o.IsCorrectOption == true);
                            if (correctOption != null && selectedOption?.Id == correctOption.Id)
                            {
                                studentScore = q.MaxMarks ?? 0;
                            }
                        }
                        else if (q.QuestionType == QuestionType.Subjective)
                        {
                            writtenAnswer = answer.WrittenAnswer;
                        }
                    }

                    return new FacultyQuizQuestionAssessmentViewModel
                    {
                        QuestionId = q.Id,
                        QuestionText = q.QuestionText,
                        MaxScore = q.MaxMarks ?? 0,
                        StudentScore = studentScore,
                        SelectedOptionText = selectedOptionText,
                        WrittenAnswer = writtenAnswer,
                        QuestionType = q.QuestionType
                    };
                }).ToList() ?? new List<FacultyQuizQuestionAssessmentViewModel>();
            }

            // 4️⃣ Resolve student/course info
            var student = (await _userManager.GetAsync(null, u => u.Id == attempt.StudentId)).FirstOrDefault();
            var course = (await _courseManager.GetAsync(null, c => c.Id == attempt.CourseId)).FirstOrDefault();
            var quadrant = (await _quadrantManager.GetAsync(null, q => q.Id == attempt.CourseQuadrantId)).FirstOrDefault();

            // 5️⃣ Build final ViewModel
            var model = new FacultyQuizAssessmentViewModel
            {
                Id = existingAssessment?.Id ?? 0,
                AttemptId = attempt.Id,
                StudentId = attempt.StudentId,
                StudentName = student?.Name ?? "N/A",
                CourseId = attempt.CourseId,
                CourseName = course?.Name ?? "N/A",
                QuadrantId = attempt.CourseQuadrantId,
                QuadrantName = quadrant?.Name ?? "N/A",
                QuizId = attempt.QuizId,
                QuizName = quiz.Title,
                AttemptedOn = attempt.CreatedOn,
                TotalScore = existingAssessment?.TotalScore,
                Questions = questionAssessments,
                IsAssessed = isAssessed
            };

            model.IsAssessed = isAssessed;
            return PartialView("_FacultyQuizAssessmentModal", model);
        }




        // POST: Save assessment (faculty can update subjective scores)
        [HttpPost]
        public async Task<IActionResult> SaveAssessment(FacultyQuizAssessmentViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest();

            decimal totalScore = model.Questions.Sum(q => q.StudentScore);

            // Create new assessment record
            var assessment = new FacultyQuizAssessmentViewModel
            {
                AttemptId = model.AttemptId,
                StudentId = model.StudentId,
                StudentName = model.StudentName,
                CourseId = model.CourseId,
                CourseName = model.CourseName,
                QuadrantId = model.QuadrantId,
                QuadrantName = model.QuadrantName,
                QuizId = model.QuizId,
                QuizName = model.QuizName,
                AttemptedOn = model.AttemptedOn,
                TotalScore = totalScore,
                Questions = model.Questions.Select(q => new FacultyQuizQuestionAssessmentViewModel
                {
                    QuestionId = q.QuestionId,
                    QuestionText = q.QuestionText,
                    MaxScore = q.MaxScore,
                    StudentScore = q.StudentScore,
                    SelectedOptionText = q.SelectedOptionText,
                    WrittenAnswer = q.WrittenAnswer,
                    QuestionType = q.QuestionType
                }).ToList()
            };

            await _facultyQuizAssessmentManager.AddUpdateQuizAssessmentAsync(assessment, GetUserId());
            return Json(new { success = true, totalScore });
        }

    }
}
