using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TMS.Models.Assessments;
using TMS.Repository.Managers;

namespace TMS.Web.Controllers
{
    public class StudentExamController : BaseController
    {
        private readonly IAssessmentManager _assessmentManager;

        public StudentExamController(IAssessmentManager assessmentManager)
        {
            _assessmentManager = assessmentManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int studentId = GetUserId();
            var exams = await _assessmentManager.GetAssignedExamsForStudentAsync(studentId);
            return View(exams);
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

            // Load attempt with full details (questions and options)
            var attemptDetails = await _assessmentManager.GetAttemptDetailsAsync(attempt.Id);
            return View(attemptDetails);
        }

        [HttpPost]
        public async Task<IActionResult> SaveAnswer(int attemptId, int questionId, int? selectedOptionId, string? subjectiveAnswer, int remainingSeconds)
        {
            var success = await _assessmentManager.SaveStudentAnswerAsync(attemptId, questionId, selectedOptionId, subjectiveAnswer, remainingSeconds);
            return Json(new { success });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int attemptId)
        {
            var result = await _assessmentManager.SubmitExamAttemptAsync(attemptId);
            if (result == null)
            {
                SetApplicationResult(false, "Failed to submit exam attempt.");
                return RedirectToAction(nameof(Index));
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
