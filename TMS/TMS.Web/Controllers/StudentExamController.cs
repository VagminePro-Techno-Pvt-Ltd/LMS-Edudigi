using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.Models.Assessments;
using TMS.Repository;
using TMS.Repository.Managers;
using TMS.ViewModels;
using TMS.ViewModels.Academics;
using TMS.ViewModels.Masters;
using TMS.Web.Models;
using TMS.Web.Services;
using TMS.Web.Services.Email;

namespace TMS.Web.Controllers
{
    public class StudentExamController : BaseController
    {
        private readonly IAssessmentManager _assessmentManager;
        private readonly ApplicationDBContext _context;
        private readonly IMapper _mapper;
        private readonly IEmailOutboxService? _outboxService;
        private readonly IAuditLogService? _auditLogService;

        public StudentExamController(
            IAssessmentManager assessmentManager,
            ApplicationDBContext context,
            IMapper mapper,
            IEmailOutboxService? outboxService = null,
            IAuditLogService? auditLogService = null)
        {
            _assessmentManager = assessmentManager;
            _context = context;
            _mapper = mapper;
            _outboxService = outboxService;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int studentId = GetUserId();
            var exams = await _assessmentManager.GetAssignedExamsForStudentAsync(studentId);

            // Fetch course quizzes
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

            ViewBag.Quizzes = allQuizzes.OrderByDescending(q => q.Id).ToList();
            ViewBag.QuizAttempts = attempts ?? new List<StudentQuizAttemptViewModel>();

            return View(exams ?? new List<ExamMaster>());
        }

        [HttpGet]
        public async Task<IActionResult> Instructions(int examId)
        {
            var exam = await _assessmentManager.GetExamDetailsAsync(examId);
            if (exam == null || exam.Status != "Active")
            {
                SetApplicationResult(false, "Exam is not active or not found.");
                return RedirectToAction(nameof(Index));
            }
            return View(exam);
        }

        [HttpGet]
        public async Task<IActionResult> Attempt(int examId)
        {
            int studentId = GetUserId();
            var attempt = await _assessmentManager.StartExamAttemptAsync(examId, studentId);
            
            if (attempt == null)
            {
                SetApplicationResult(false, "You have exceeded the maximum allowed attempts or this exam is unavailable.");
                return RedirectToAction(nameof(Index));
            }

            if (_auditLogService != null)
            {
                await _auditLogService.LogActionAsync(
                    "START_EXAM",
                    "Examinations",
                    "ExamMaster",
                    examId.ToString(),
                    null,
                    new { AttemptId = attempt.Id, ExamId = examId },
                    null,
                    "SUCCESS",
                    $"Student started attempt #{attempt.Id} for exam #{examId}.",
                    studentId);
            }

            // Load attempt with full details (questions and options)
            var attemptDetails = await _assessmentManager.GetAttemptDetailsAsync(attempt.Id);
            return View(attemptDetails);
        }

        [HttpPost]
        public async Task<IActionResult> SaveAnswer(int attemptId, int questionId, int? selectedOptionId, string? subjectiveAnswer, int remainingSeconds)
        {
            var attempt = await _assessmentManager.GetAttemptDetailsAsync(attemptId);
            if (attempt == null || attempt.StudentId != GetUserId())
            {
                return Unauthorized();
            }

            var success = await _assessmentManager.SaveStudentAnswerAsync(attemptId, questionId, selectedOptionId, subjectiveAnswer, remainingSeconds);
            return Json(new { success });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int attemptId)
        {
            var attempt = await _assessmentManager.GetAttemptDetailsAsync(attemptId);
            if (attempt == null || attempt.StudentId != GetUserId())
            {
                return Unauthorized();
            }

            var result = await _assessmentManager.SubmitExamAttemptAsync(attemptId);
            if (result == null)
            {
                SetApplicationResult(false, "Failed to submit exam attempt.");
                return RedirectToAction(nameof(Index));
            }

            if (_auditLogService != null)
            {
                await _auditLogService.LogActionAsync(
                    "SUBMIT_EXAM",
                    "Examinations",
                    "ExamMaster",
                    attempt.ExamId.ToString(),
                    null,
                    new { AttemptId = attemptId, ExamId = attempt.ExamId },
                    null,
                    "SUCCESS",
                    $"Student submitted exam attempt #{attemptId}.",
                    GetUserId());
            }

            if (_outboxService != null)
            {
                try
                {
                    // Invalidate pending reminders for this student on this exam
                    await _outboxService.CancelScheduledJobsAsync($"remind-exam-*-{attempt.ExamId}-{attempt.StudentId}");

                    var student = await _context.UserMasters.FindAsync(attempt.StudentId);
                    var exam = attempt.Exam ?? await _context.ExamMasters.Include(e => e.Course).FirstOrDefaultAsync(e => e.Id == attempt.ExamId);
                    string courseName = exam?.Course?.Name ?? "Course";

                    if (student != null && !string.IsNullOrWhiteSpace(student.Email))
                    {
                        await _outboxService.EnqueueAsync(new TMS.ViewModels.Email.EmailEnqueueRequest
                        {
                            RecipientEmail = student.Email.Trim(),
                            RecipientName = student.Name,
                            Subject = $"Exam Submission Receipt: {exam?.Title ?? "Examination"} ({courseName})",
                            TemplateName = "ExamSubmissionReceipt",
                            TemplateData = new Dictionary<string, string>
                            {
                                ["RecipientName"] = student.Name ?? "Student",
                                ["ExamTitle"] = exam?.Title ?? "Examination",
                                ["CourseName"] = courseName,
                                ["SubmittedAt"] = DateTime.UtcNow.ToString("dd MMM yyyy, hh:mm tt") + " UTC",
                                ["AttemptRef"] = $"EXAM-ATTEMPT-{attemptId}"
                            },
                            EventType = "ExamSubmissionReceipt",
                            IdempotencyKey = $"exam-sub-receipt-{attemptId}"
                        });
                    }
                }
                catch
                {
                    // Non-fatal for submission response
                }
            }

            SetApplicationResult(true, "Exam submitted successfully.");
            return RedirectToAction(nameof(Result), new { attemptId });
        }

        [HttpGet]
        public async Task<IActionResult> Result(int attemptId)
        {
            var result = await _assessmentManager.GetExamResultAsync(attemptId);
            if (result == null)
            {
                SetApplicationResult(false, "Result not found.");
                return RedirectToAction(nameof(Index));
            }

            // Verify if student is authorized to view this result
            if (result.Attempt?.StudentId != GetUserId())
            {
                return Unauthorized();
            }

            // Check if results are configured to be visible immediately
            if (!result.Attempt.Exam!.ShowResultImmediately)
            {
                SetApplicationResult(true, "Exam submitted successfully! Results will be published later by your instructor.");
                return RedirectToAction(nameof(Index));
            }

            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> Review(int attemptId)
        {
            var attempt = await _assessmentManager.GetAttemptDetailsAsync(attemptId);
            if (attempt == null || attempt.StudentId != GetUserId() || !attempt.IsSubmitted)
            {
                return Unauthorized();
            }

            if (!attempt.Exam!.ShowResultImmediately)
            {
                SetApplicationResult(false, "Exam reviews are not available for this assessment.");
                return RedirectToAction(nameof(Index));
            }

            return View(attempt);
        }
    }
}
