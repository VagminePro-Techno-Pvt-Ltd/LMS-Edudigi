using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.Models.Academics;
using TMS.Models.Masters;
using TMS.Repository;
using TMS.ViewModels.Academics;

namespace TMS.Web.Controllers
{
    [ValidateFormAccess(Common.FormDefination.QuizAssemement)]
    public class FacultyQuizAssessmentController : BaseController
    {
        private readonly ApplicationDBContext _db;

        public FacultyQuizAssessmentController(ApplicationDBContext db)
        {
            _db = db;
        }

        // GET: List of student attempts for faculty or admin
        public async Task<IActionResult> Index()
        {
            int userId = GetUserId();
            bool isFaculty = IsFacultyRole();

            // 1. Determine course filter if faculty
            List<int> facultyCourseIds = new();
            if (isFaculty)
            {
                facultyCourseIds = await _db.CourseFacultyMaps.AsNoTracking()
                    .Where(f => f.IsActive && f.FacultyId == userId)
                    .Select(f => f.CourseId)
                    .Distinct()
                    .ToListAsync();
            }

            // 2. Query attempts
            var attemptsQuery = _db.StudentQuizAttempt.AsNoTracking()
                .Include(a => a.Quiz)
                .Where(a => a.IsActive);

            if (isFaculty && facultyCourseIds.Any())
            {
                attemptsQuery = attemptsQuery.Where(a => facultyCourseIds.Contains(a.CourseId ?? 0) || a.CourseId == null || a.CourseId == 0);
            }

            var allAttempts = await attemptsQuery.OrderByDescending(a => a.AttemptDate).ThenByDescending(a => a.Id).ToListAsync();

            if (!allAttempts.Any())
            {
                ViewBag.IsAdmin = !isFaculty;
                return View(new List<FacultyQuizAssessmentViewModel>());
            }

            // 3. Fetch lookups for fast in-memory display
            var courses = await _db.CourseMasters.AsNoTracking().Where(c => c.IsActive).ToDictionaryAsync(c => c.Id, c => c.Name);
            var quadrants = await _db.CourseQuadrantMasters.AsNoTracking().Where(q => q.IsActive).ToDictionaryAsync(q => q.Id, q => q.Name);
            var users = await _db.UserMasters.AsNoTracking().Where(u => u.IsActive).ToDictionaryAsync(u => u.Id, u => u.Name);
            var assessments = await _db.FacultyQuizAssessment.AsNoTracking()
                .Where(f => f.IsActive)
                .GroupBy(f => f.AttemptId)
                .Select(g => g.OrderByDescending(x => x.Id).First())
                .ToDictionaryAsync(f => f.AttemptId, f => f.TotalScore);

            // 4. Map to view models
            var model = allAttempts.Select(a =>
            {
                decimal? totalScore = assessments.ContainsKey(a.Id) ? assessments[a.Id] : a.Score;
                string courseName = (a.CourseId.HasValue && courses.ContainsKey(a.CourseId.Value)) ? courses[a.CourseId.Value] : (a.Quiz?.CourseId > 0 && courses.ContainsKey(a.Quiz.CourseId) ? courses[a.Quiz.CourseId] : "N/A");
                string quadName = (a.CourseQuadrantId.HasValue && quadrants.ContainsKey(a.CourseQuadrantId.Value)) ? quadrants[a.CourseQuadrantId.Value] : (a.Quiz?.CourseQuadrantId > 0 && quadrants.ContainsKey(a.Quiz.CourseQuadrantId) ? quadrants[a.Quiz.CourseQuadrantId] : "N/A");
                string studentName = users.ContainsKey(a.StudentId) ? users[a.StudentId] : $"Student #{a.StudentId}";
                string quizTitle = a.Quiz?.Title ?? "Quiz #" + a.QuizId;

                return new FacultyQuizAssessmentViewModel
                {
                    AttemptId = a.Id,
                    StudentId = a.StudentId,
                    StudentName = studentName,
                    CourseId = a.CourseId ?? 0,
                    CourseName = courseName,
                    QuadrantId = a.CourseQuadrantId ?? 0,
                    QuadrantName = quadName,
                    QuizId = a.QuizId,
                    QuizName = quizTitle,
                    AttemptedOn = a.AttemptDate != default ? a.AttemptDate : a.CreatedOn,
                    TotalScore = totalScore
                };
            }).ToList();

            ViewBag.IsAdmin = !isFaculty;
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int attemptId)
        {
            try
            {
                // 1️⃣ Fetch attempt and quiz
                var attempt = await _db.StudentQuizAttempt.AsNoTracking()
                    .Include(a => a.Quiz)
                    .FirstOrDefaultAsync(a => a.Id == attemptId);

                if (attempt == null)
                    return Content("<div class='p-4 text-center text-danger'>Attempt not found.</div>", "text/html");

                var quiz = await _db.QuizMasters.AsNoTracking()
                    .Include(q => q.Questions)
                        .ThenInclude(qq => qq.Options)
                    .Include(q => q.Course)
                    .Include(q => q.CourseQuadrant)
                    .FirstOrDefaultAsync(q => q.Id == attempt.QuizId);

                if (quiz == null)
                    return Content("<div class='p-4 text-center text-danger'>Quiz details not found.</div>", "text/html");

                // 2️⃣ Fetch student's submitted answers
                var answers = await _db.StudentQuizAnswer.AsNoTracking()
                    .Include(a => a.SelectedOption)
                    .Where(a => a.StudentQuizAttemptId == attemptId)
                    .ToListAsync();

                // 3️⃣ Check if faculty evaluation already exists
                var existingAssessment = await _db.FacultyQuizAssessment.AsNoTracking()
                    .Include(f => f.Questions)
                    .Where(f => f.AttemptId == attemptId && f.IsActive)
                    .OrderByDescending(f => f.Id)
                    .FirstOrDefaultAsync();

                bool isAssessed = existingAssessment != null && existingAssessment.Questions != null && existingAssessment.Questions.Any();

                var quizQuestions = quiz.Questions?.Where(q => q.IsActive).OrderBy(q => q.Id).ToList() ?? new List<QuizQuestion>();
                var questionAssessments = new List<FacultyQuizQuestionAssessmentViewModel>();

                if (isAssessed && existingAssessment?.Questions != null)
                {
                    foreach (var qEval in existingAssessment.Questions)
                    {
                        var originalQ = quizQuestions.FirstOrDefault(qq => qq.Id == qEval.QuestionId);
                        var answer = answers.FirstOrDefault(a => a.QuizQuestionId == qEval.QuestionId);

                        string? selectedOptionText = null;
                        string? correctOptionText = null;
                        bool? isOptionCorrect = null;
                        string? writtenAnswer = null;

                        if (originalQ != null && originalQ.QuestionType == QuestionType.Objective)
                        {
                            var selectedOpt = originalQ.Options?.FirstOrDefault(o => o.Id == answer?.SelectedOptionId) ?? answer?.SelectedOption;
                            selectedOptionText = selectedOpt?.OptionText;

                            var correctOpt = originalQ.Options?.FirstOrDefault(o => o.IsCorrectOption == true);
                            correctOptionText = correctOpt?.OptionText;
                            if (selectedOpt != null && correctOpt != null)
                            {
                                isOptionCorrect = selectedOpt.Id == correctOpt.Id;
                            }
                        }
                        else
                        {
                            writtenAnswer = answer?.WrittenAnswer;
                        }

                        questionAssessments.Add(new FacultyQuizQuestionAssessmentViewModel
                        {
                            Id = qEval.Id,
                            FacultyQuizAssessmentId = existingAssessment.Id,
                            QuestionId = qEval.QuestionId,
                            QuestionText = string.IsNullOrWhiteSpace(qEval.QuestionText) ? (originalQ?.QuestionText ?? "Question") : qEval.QuestionText,
                            MaxScore = qEval.MaxScore > 0 ? qEval.MaxScore : (originalQ?.MaxMarks ?? 1),
                            StudentScore = qEval.StudentScore,
                            QuestionType = originalQ?.QuestionType ?? QuestionType.Subjective,
                            SelectedOptionText = selectedOptionText,
                            CorrectOptionText = correctOptionText,
                            IsOptionCorrect = isOptionCorrect,
                            WrittenAnswer = writtenAnswer
                        });
                    }
                }
                else
                {
                    foreach (var q in quizQuestions)
                    {
                        var answer = answers.FirstOrDefault(a => a.QuizQuestionId == q.Id);
                        decimal studentScore = 0;
                        string? selectedOptionText = null;
                        string? correctOptionText = null;
                        bool? isOptionCorrect = null;
                        string? writtenAnswer = null;

                        if (answer != null)
                        {
                            if (q.QuestionType == QuestionType.Objective && q.Options != null)
                            {
                                var selectedOpt = q.Options.FirstOrDefault(o => o.Id == answer.SelectedOptionId) ?? answer.SelectedOption;
                                selectedOptionText = selectedOpt?.OptionText;

                                var correctOpt = q.Options.FirstOrDefault(o => o.IsCorrectOption == true);
                                correctOptionText = correctOpt?.OptionText;
                                if (correctOpt != null && selectedOpt != null)
                                {
                                    isOptionCorrect = (selectedOpt.Id == correctOpt.Id);
                                    if (isOptionCorrect == true)
                                    {
                                        studentScore = q.MaxMarks ?? 1;
                                    }
                                }
                            }
                            else if (q.QuestionType == QuestionType.Subjective)
                            {
                                writtenAnswer = answer.WrittenAnswer;
                            }
                        }

                        questionAssessments.Add(new FacultyQuizQuestionAssessmentViewModel
                        {
                            QuestionId = q.Id,
                            QuestionText = q.QuestionText,
                            MaxScore = q.MaxMarks ?? 1,
                            StudentScore = studentScore,
                            SelectedOptionText = selectedOptionText,
                            CorrectOptionText = correctOptionText,
                            IsOptionCorrect = isOptionCorrect,
                            WrittenAnswer = writtenAnswer,
                            QuestionType = q.QuestionType
                        });
                    }
                }

                // 4️⃣ Resolve user / course info
                var student = await _db.UserMasters.AsNoTracking().FirstOrDefaultAsync(u => u.Id == attempt.StudentId);
                var course = attempt.CourseId.HasValue ? await _db.CourseMasters.AsNoTracking().FirstOrDefaultAsync(c => c.Id == attempt.CourseId.Value) : quiz.Course;
                var quadrant = attempt.CourseQuadrantId.HasValue ? await _db.CourseQuadrantMasters.AsNoTracking().FirstOrDefaultAsync(qd => qd.Id == attempt.CourseQuadrantId.Value) : quiz.CourseQuadrant;

                decimal? totalScore = isAssessed ? (existingAssessment?.TotalScore ?? questionAssessments.Sum(q => q.StudentScore)) : questionAssessments.Sum(q => q.StudentScore);

                var model = new FacultyQuizAssessmentViewModel
                {
                    Id = existingAssessment?.Id ?? 0,
                    AttemptId = attempt.Id,
                    StudentId = attempt.StudentId,
                    StudentName = student?.Name ?? $"Student #{attempt.StudentId}",
                    CourseId = course?.Id ?? attempt.CourseId ?? 0,
                    CourseName = course?.Name ?? "General Course",
                    QuadrantId = quadrant?.Id ?? attempt.CourseQuadrantId ?? 0,
                    QuadrantName = quadrant?.Name ?? "Quadrant",
                    QuizId = quiz.Id,
                    QuizName = quiz.Title,
                    AttemptedOn = attempt.AttemptDate != default ? attempt.AttemptDate : attempt.CreatedOn,
                    TotalScore = totalScore,
                    Questions = questionAssessments,
                    IsAssessed = isAssessed
                };

                return PartialView("_FacultyQuizAssessmentModal", model);
            }
            catch (Exception ex)
            {
                return Content($"<div class='p-4 text-center text-danger'><strong>Failed to load assessment details:</strong><br/>{ex.Message}</div>", "text/html");
            }
        }

        // POST: Save assessment (faculty can update subjective scores)
        [HttpPost]
        public async Task<IActionResult> SaveAssessment(FacultyQuizAssessmentViewModel model)
        {
            if (model == null || model.AttemptId <= 0)
                return BadRequest(new { success = false, message = "Invalid assessment data." });

            int currentUserId = GetUserId();
            decimal totalScore = model.Questions?.Sum(q => q.StudentScore) ?? 0;

            try
            {
                var existingAssessment = await _db.FacultyQuizAssessment
                    .Include(f => f.Questions)
                    .FirstOrDefaultAsync(f => f.AttemptId == model.AttemptId && f.IsActive);

                if (existingAssessment == null)
                {
                    var newAssessment = new FacultyQuizAssessment
                    {
                        AttemptId = model.AttemptId,
                        QuizId = model.QuizId,
                        StudentId = model.StudentId,
                        StudentName = model.StudentName ?? "",
                        CourseId = model.CourseId,
                        CourseName = model.CourseName ?? "",
                        QuadrantId = model.QuadrantId,
                        QuadrantName = model.QuadrantName ?? "",
                        QuizName = model.QuizName ?? "",
                        AttemptedOn = model.AttemptedOn != default ? model.AttemptedOn : DateTime.Now,
                        TotalScore = totalScore,
                        CreatedBy = currentUserId,
                        CreatedOn = DateTime.Now,
                        IsActive = true,
                        Questions = new List<FacultyQuizQuestionAssessment>()
                    };

                    if (model.Questions != null)
                    {
                        foreach (var q in model.Questions)
                        {
                            newAssessment.Questions.Add(new FacultyQuizQuestionAssessment
                            {
                                QuestionId = q.QuestionId,
                                QuestionText = q.QuestionText ?? "",
                                MaxScore = q.MaxScore,
                                StudentScore = q.StudentScore,
                                CreatedBy = currentUserId,
                                CreatedOn = DateTime.Now,
                                IsActive = true
                            });
                        }
                    }

                    await _db.FacultyQuizAssessment.AddAsync(newAssessment);
                }
                else
                {
                    existingAssessment.TotalScore = totalScore;
                    existingAssessment.UpdatedBy = currentUserId;
                    existingAssessment.UpdatedOn = DateTime.Now;

                    if (existingAssessment.Questions == null)
                        existingAssessment.Questions = new List<FacultyQuizQuestionAssessment>();

                    if (model.Questions != null)
                    {
                        foreach (var qVm in model.Questions)
                        {
                            var existingQ = existingAssessment.Questions.FirstOrDefault(eq => eq.QuestionId == qVm.QuestionId);
                            if (existingQ != null)
                            {
                                existingQ.StudentScore = qVm.StudentScore;
                                existingQ.MaxScore = qVm.MaxScore;
                                existingQ.QuestionText = qVm.QuestionText ?? existingQ.QuestionText;
                                existingQ.UpdatedBy = currentUserId;
                                existingQ.UpdatedOn = DateTime.Now;
                            }
                            else
                            {
                                existingAssessment.Questions.Add(new FacultyQuizQuestionAssessment
                                {
                                    FacultyQuizAssessmentId = existingAssessment.Id,
                                    QuestionId = qVm.QuestionId,
                                    QuestionText = qVm.QuestionText ?? "",
                                    MaxScore = qVm.MaxScore,
                                    StudentScore = qVm.StudentScore,
                                    CreatedBy = currentUserId,
                                    CreatedOn = DateTime.Now,
                                    IsActive = true
                                });
                            }
                        }
                    }
                }

                // Also update StudentQuizAttempt score
                var studentAttempt = await _db.StudentQuizAttempt.FirstOrDefaultAsync(a => a.Id == model.AttemptId);
                if (studentAttempt != null)
                {
                    studentAttempt.Score = (int)Math.Round(totalScore);
                    studentAttempt.UpdatedBy = currentUserId;
                    studentAttempt.UpdatedOn = DateTime.Now;
                }

                await _db.SaveChangesAsync();

                return Json(new { success = true, totalScore });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
