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

        public AdminExamController(IAssessmentManager assessmentManager, ApplicationDBContext context)
        {
            _assessmentManager = assessmentManager;
            _context = context;
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
