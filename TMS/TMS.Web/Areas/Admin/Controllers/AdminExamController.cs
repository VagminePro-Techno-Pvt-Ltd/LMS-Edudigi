using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.Models.Assessments;
using TMS.Repository;
using TMS.Repository.Managers;
using TMS.Web.Controllers;

namespace TMS.Web.Areas.Admin.Controllers
{
    [Authorize]
    [Area("Admin")]
    public class AdminExamController : BaseController
    {
        private readonly IAssessmentManager _assessmentManager;
        private readonly ApplicationDBContext _context;
        private readonly TMS.Web.Services.Email.IEmailOutboxService _outboxService;

        public AdminExamController(
            IAssessmentManager assessmentManager, 
            ApplicationDBContext context,
            TMS.Web.Services.Email.IEmailOutboxService outboxService)
        {
            _assessmentManager = assessmentManager;
            _context = context;
            _outboxService = outboxService;
        }

        // =========================================================================
        // --- ADMINISTRATIVE EXAM DASHBOARD ---
        // =========================================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Global Admin Statistics
            ViewBag.TotalExams = await _context.ExamMasters.CountAsync();
            ViewBag.ActiveExams = await _context.ExamMasters.CountAsync(e => e.Status == "Active");
            ViewBag.TotalAttempts = await _context.StudentExamAttempts.CountAsync();
            ViewBag.PendingEvaluations = await _context.StudentExamAttempts.CountAsync(a => a.ResultStatus == "Pending");
            ViewBag.TotalQuestions = await _context.QuestionBanks.CountAsync();

            var allCompletedAttempts = await _context.StudentExamAttempts
                .Where(a => a.ResultStatus == "Passed" || a.ResultStatus == "Failed")
                .ToListAsync();

            if (allCompletedAttempts.Any())
            {
                ViewBag.AverageScore = allCompletedAttempts.Average(a => a.Score ?? 0);
                ViewBag.FailedCount = allCompletedAttempts.Count(a => a.ResultStatus == "Failed");
                ViewBag.PassedCount = allCompletedAttempts.Count(a => a.ResultStatus == "Passed");
                ViewBag.PassPercentage = Math.Round((double)ViewBag.PassedCount / allCompletedAttempts.Count * 100, 2);
            }
            else
            {
                ViewBag.AverageScore = 0;
                ViewBag.FailedCount = 0;
                ViewBag.PassedCount = 0;
                ViewBag.PassPercentage = 0;
            }

            // Fetch Top Performers across the system
            var topPerformers = await _context.StudentExamAttempts
                .Include(a => a.Student)
                .Include(a => a.Exam)
                .Where(a => a.ResultStatus == "Passed" && (a.Score ?? 0) > 0)
                .OrderByDescending(a => a.Percentage)
                .Take(5)
                .Select(a => new StudentAttemptSummary
                {
                    StudentId = a.StudentId,
                    StudentName = a.Student != null ? a.Student.Name : "Anonymous",
                    Email = a.Student != null ? a.Student.Email : "",
                    Score = a.Score ?? 0,
                    Percentage = a.Percentage ?? 0,
                    ResultStatus = a.ResultStatus != null ? a.ResultStatus : "Passed"
                })
                .ToListAsync();

            ViewBag.TopPerformers = topPerformers;

            // Fetch all exams with their course details
            var exams = await _context.ExamMasters
                .Include(e => e.Course)
                .Include(e => e.Faculty)
                .OrderByDescending(e => e.CreatedOn)
                .ToListAsync();

            return View(exams);
        }

        // =========================================================================
        // --- REAL-TIME LIVE MONITORING ---
        // =========================================================================

        [HttpGet]
        public async Task<IActionResult> LiveMonitor(int examId)
        {
            var exam = await _context.ExamMasters.FindAsync(examId);
            if (exam == null) return NotFound();

            var liveAttempts = await _assessmentManager.GetLiveMonitoringAttemptsAsync(examId);
            
            ViewBag.Exam = exam;
            return View(liveAttempts);
        }

        // =========================================================================
        // --- EXAM ANALYTICS DEEP DIVE ---
        // =========================================================================

        [HttpGet]
        public async Task<IActionResult> Analytics(int examId)
        {
            var exam = await _context.ExamMasters.FindAsync(examId);
            if (exam == null) return NotFound();

            var analytics = await _assessmentManager.GetExamAnalyticsAsync(examId);
            return View(analytics);
        }

        // =========================================================================
        // --- TOGGLE EXAM STATUS OVERRIDE ---
        // =========================================================================

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int examId, string status)
        {
            var success = await _assessmentManager.UpdateExamStatusAsync(examId, status);
            if (success)
            {
                try
                {
                    if (status.Equals("Active", StringComparison.OrdinalIgnoreCase))
                    {
                        var exam = await _context.ExamMasters.Include(e => e.Course).FirstOrDefaultAsync(e => e.Id == examId);
                        if (exam != null)
                        {
                            var studentIds = await _context.CourseEnrollments
                                .Where(e => e.CourseId == exam.CourseId && e.IsActive && (e.Status == 0 || e.Status == 1))
                                .Select(e => e.StudentId)
                                .Distinct()
                                .ToListAsync();

                            var students = await _context.UserMasters
                                .Where(u => studentIds.Contains(u.Id) && u.IsActive && !string.IsNullOrEmpty(u.Email))
                                .ToListAsync();

                            string courseName = exam.Course?.Name ?? "Course";
                            var batch = new List<TMS.ViewModels.Email.EmailEnqueueRequest>();
                            var nowUtc = DateTime.UtcNow;

                            foreach (var s in students)
                            {
                                batch.Add(new TMS.ViewModels.Email.EmailEnqueueRequest
                                {
                                    RecipientEmail = s.Email.Trim(),
                                    RecipientName = s.Name,
                                    Subject = $"Examination Notice: {exam.Title} ({courseName})",
                                    TemplateName = "ExamPublication",
                                    TemplateData = new Dictionary<string, string>
                                    {
                                        ["RecipientName"] = s.Name ?? "Student",
                                        ["ExamTitle"] = exam.Title,
                                        ["CourseName"] = courseName,
                                        ["ExamDate"] = $"{exam.StartDate:dd MMM yyyy, hh:mm tt} – {exam.EndDate:dd MMM yyyy, hh:mm tt}",
                                        ["DurationMinutes"] = exam.DurationMinutes.ToString(),
                                        ["RequiresSeb"] = "false"
                                    },
                                    EventType = "ExamPublication",
                                    IdempotencyKey = $"exam-pub-{exam.Id}-{s.Id}"
                                });

                                var remind24 = exam.StartDate.AddHours(-24);
                                if (remind24 > nowUtc)
                                {
                                    batch.Add(new TMS.ViewModels.Email.EmailEnqueueRequest
                                    {
                                        RecipientEmail = s.Email.Trim(),
                                        RecipientName = s.Name,
                                        Subject = $"Exam Reminder: {exam.Title} starts in 24 hours",
                                        TemplateName = "ExamReminder",
                                        TemplateData = new Dictionary<string, string>
                                        {
                                            ["RecipientName"] = s.Name ?? "Student",
                                            ["ExamTitle"] = exam.Title,
                                            ["CourseName"] = courseName,
                                            ["StartTime"] = exam.StartDate.ToString("dd MMM yyyy, hh:mm tt"),
                                            ["HoursLeft"] = "24",
                                            ["RequiresSeb"] = "false"
                                        },
                                        ScheduledForUtc = remind24,
                                        EventType = "ExamReminder24h",
                                        IdempotencyKey = $"remind-exam-24h-{exam.Id}-{s.Id}"
                                    });
                                }
                            }

                            if (batch.Any())
                            {
                                await _outboxService.EnqueueBatchAsync(batch);
                            }
                        }
                    }
                    else
                    {
                        await _outboxService.CancelScheduledJobsAsync($"remind-exam-*-{examId}-*");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AdminExamController] Outbox error in ToggleStatus: {ex.Message}");
                }
            }
            return Json(new { success });
        }

        // =========================================================================
        // --- SYSTEM ATTEMPT HISTORY & VIEW ---
        // =========================================================================

        [HttpGet]
        public async Task<IActionResult> Attempts()
        {
            var attempts = await _context.StudentExamAttempts
                .Include(a => a.Exam)
                .Include(a => a.Student)
                .OrderByDescending(a => a.StartedAt)
                .ToListAsync();

            return View(attempts);
        }
    }
}
