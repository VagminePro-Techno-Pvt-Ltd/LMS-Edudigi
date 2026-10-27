using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using TMS.Models.Masters;
using TMS.Repository;
using TMS.Repository.Managers;
using TMS.ViewModels;
using TMS.ViewModels.Academics;
using TMS.ViewModels.Masters;
using TMS.Web.Services;

namespace TMS.Web.Controllers
{
    [Authorize]
    public class StudentQuizController : BaseController
    {
        private readonly ApplicationDBContext _context;
        private readonly IMapper _mapper;
        private readonly IMasterBaseManager<StudentQuizAttemptViewModel> _attemptManager;
        private readonly IStudentLearningService _learningService;

        public StudentQuizController(
            ApplicationDBContext context,
            IMapper mapper,
            IMasterBaseManager<StudentQuizAttemptViewModel> attemptManager,
            IStudentLearningService learningService)
        {
            _context = context;
            _mapper = mapper;
            _attemptManager = attemptManager;
            _learningService = learningService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int studentId = GetUserId();
            var enrollments = await _context.CourseEnrollments.AsNoTracking()
                .Where(x => x.IsActive && x.StudentId == studentId && (x.Status == 0 || x.Status == 1))
                .Select(e => e.CourseId)
                .ToListAsync();

            var courseIds = new List<int>(enrollments);
            var allCourses = await _context.CourseMasters.AsNoTracking().Where(c => c.IsActive).ToListAsync();

            if (!courseIds.Any())
            {
                courseIds = allCourses.Select(c => c.Id).ToList();
            }

            var quizzesEntities = await _context.QuizMasters.AsNoTracking()
                .Include(q => q.Questions).ThenInclude(q => q.Options)
                .Include(q => q.Course)
                .Include(q => q.CourseQuadrant)
                .Where(q => q.IsActive && (courseIds.Contains(q.CourseId) || q.CourseId == 0))
                .ToListAsync();

            if (!quizzesEntities.Any())
            {
                quizzesEntities = await _context.QuizMasters.AsNoTracking()
                    .Include(q => q.Questions).ThenInclude(q => q.Options)
                    .Include(q => q.Course)
                    .Include(q => q.CourseQuadrant)
                    .Where(q => q.IsActive)
                    .ToListAsync();
            }

            var allQuizzes = _mapper.Map<List<QuizMasterViewModel>>(quizzesEntities);

            var attemptsEntities = await _context.StudentQuizAttempt.AsNoTracking()
                .Include(a => a.Quiz)
                .Where(a => a.IsActive && a.StudentId == studentId)
                .OrderByDescending(a => a.AttemptDate)
                .ToListAsync();

            var attempts = _mapper.Map<List<StudentQuizAttemptViewModel>>(attemptsEntities);

            ViewBag.Attempts = attempts ?? new List<StudentQuizAttemptViewModel>();
            return View(allQuizzes.OrderByDescending(q => q.Id).ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Attempt(int? courseId, int? quadrantId, int? quizId)
        {
            int studentId = GetUserId();
            bool isStaff = User.IsInRole("Admin") || User.IsInRole("Super Admin") || User.IsInRole("Faculty") || User.IsInRole("Exam Coordinator");

            QuizMaster? quizEntity = null;

            if (quizId.HasValue && quizId.Value > 0)
            {
                quizEntity = await _context.QuizMasters.AsNoTracking()
                    .Include(q => q.Questions).ThenInclude(q => q.Options)
                    .Include(q => q.Course)
                    .Include(q => q.CourseQuadrant)
                    .FirstOrDefaultAsync(q => q.Id == quizId.Value && q.IsActive);
            }
            else if (courseId.HasValue && quadrantId.HasValue)
            {
                quizEntity = await _context.QuizMasters.AsNoTracking()
                    .Include(q => q.Questions).ThenInclude(q => q.Options)
                    .Include(q => q.Course)
                    .Include(q => q.CourseQuadrant)
                    .FirstOrDefaultAsync(q => q.CourseId == courseId.Value && q.CourseQuadrantId == quadrantId.Value && q.IsActive);
            }

            if (quizEntity == null)
                return NotFound("No quiz found for the specified course and quadrant.");

            var quiz = _mapper.Map<QuizMasterViewModel>(quizEntity);

            if (!isStaff)
            {
                bool enrolled = await _learningService.IsStudentEnrolledAsync(studentId, quiz.CourseId);
                if (!enrolled)
                    return Forbid();
            }

            // 2. Check existing attempt
            var existingAttempt = await _context.StudentQuizAttempt.AsNoTracking()
                .Where(x => x.IsActive && x.StudentId == studentId && x.QuizId == quiz.Id)
                .OrderByDescending(a => a.AttemptDate)
                .FirstOrDefaultAsync();

            if (existingAttempt != null && existingAttempt.IsSubmitted)
            {
                TempData["Info"] = "You have already completed this quiz. Here are your results.";
                return RedirectToAction(nameof(Result), new { attemptId = existingAttempt.Id });
            }

            // 3. Map to ViewModel with Questions & Options
            var vm = new StudentQuizAttemptViewModel
            {
                StudentId = studentId,
                QuizId = quiz.Id,
                QuizTitle = quiz.Title,
                CourseId = quiz.CourseId,
                CourseQuadrantId = quiz.CourseQuadrantId,
                Score = 0,
                Answers = quiz.Questions.OrderBy(q => q.Id).Select(q => new StudentQuizAnswerViewModel
                {
                    QuizQuestionId = q.Id,
                    QuestionText = q.QuestionText,
                    QuestionType = q.QuestionType.ToString(),
                    Options = q.Options.Select(o => new QuizOptionViewModel
                    {
                        Id = o.Id,
                        OptionText = o.OptionText
                    }).ToList()
                }).ToList()
            };

            ViewBag.CourseName = quiz.Course?.Name ?? "Academic Course";
            ViewBag.QuadrantName = quiz.CourseQuadrant?.Name ?? "Quadrant";
            ViewBag.PassingMarks = quiz.PassingMarks ?? 50;

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(StudentQuizAttemptViewModel attemptVm)
        {
            int studentId = GetUserId();
            bool isStaff = User.IsInRole("Admin") || User.IsInRole("Super Admin") || User.IsInRole("Faculty") || User.IsInRole("Exam Coordinator");
            if (!isStaff)
            {
                bool enrolled = await _learningService.IsStudentEnrolledInQuizAsync(studentId, attemptVm.QuizId);
                if (!enrolled)
                    return Forbid();
            }

            attemptVm.StudentId = studentId;
            attemptVm.AttemptDate = DateTime.Now;
            attemptVm.IsSubmitted = true;

            bool success = await _attemptManager.AddUpdateStudentQuizAttemptAsync(attemptVm, studentId);
            if (success)
            {
                var latestAttempt = await _context.StudentQuizAttempt.AsNoTracking()
                    .Where(x => x.StudentId == studentId && x.QuizId == attemptVm.QuizId && x.IsActive)
                    .OrderByDescending(a => a.AttemptDate)
                    .FirstOrDefaultAsync();

                if (latestAttempt != null)
                {
                    TempData["Success"] = "Quiz submitted successfully!";
                    return RedirectToAction(nameof(Result), new { attemptId = latestAttempt.Id });
                }
            }

            SetApplicationResult(false, "Error submitting quiz. Please try again.");
            return RedirectToAction("StudentDashboard", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Result(int attemptId)
        {
            var attemptEntity = await _context.StudentQuizAttempt.AsNoTracking()
                .Include(a => a.Answers)
                .Include(a => a.Quiz)
                .FirstOrDefaultAsync(a => a.Id == attemptId && a.IsActive);

            if (attemptEntity == null)
                return NotFound("Attempt not found.");

            bool isStaff = User.IsInRole("Admin") || User.IsInRole("Super Admin") || User.IsInRole("Faculty") || User.IsInRole("Exam Coordinator");
            if (!isStaff && attemptEntity.StudentId != GetUserId())
            {
                return Forbid();
            }

            var attempt = _mapper.Map<StudentQuizAttemptViewModel>(attemptEntity);

            var quizEntity = await _context.QuizMasters.AsNoTracking()
                .Include(q => q.Questions).ThenInclude(q => q.Options)
                .Include(q => q.Course)
                .Include(q => q.CourseQuadrant)
                .FirstOrDefaultAsync(q => q.Id == attemptEntity.QuizId && q.IsActive);

            if (quizEntity != null)
            {
                var quiz = _mapper.Map<QuizMasterViewModel>(quizEntity);
                ViewBag.PassingMarks = quiz.PassingMarks ?? 50;
                ViewBag.Quiz = quiz;
                ViewBag.CourseName = quiz.Course?.Name;
                ViewBag.QuadrantName = quiz.CourseQuadrant?.Name;
                attempt.QuizTitle = quiz.Title;
            }
            else
            {
                ViewBag.PassingMarks = 50;
            }

            return View(attempt);
        }
    }
}
