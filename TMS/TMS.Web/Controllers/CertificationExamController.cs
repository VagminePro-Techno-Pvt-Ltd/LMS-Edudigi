using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TMS.Models.Academics;
using TMS.ViewModels.Academics;
using TMS.Web.Services;

namespace TMS.Web.Controllers
{
    public class CertificationExamController : BaseController
    {
        private readonly ICertificationExamService _examService;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<CertificationExamController> _logger;

        public CertificationExamController(
            ICertificationExamService examService,
            IWebHostEnvironment env,
            ILogger<CertificationExamController> logger)
        {
            _examService = examService;
            _env = env;
            _logger = logger;
        }

        #region Faculty Routes

        [HttpGet]
        public async Task<IActionResult> FacultyDashboard()
        {
            int facultyId = GetUserId();
            var model = await _examService.GetFacultyDashboardAsync(facultyId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CertificationExamFormModel model)
        {
            int facultyId = GetUserId();
            if (!ModelState.IsValid)
            {
                SetApplicationResult(false, "Please provide all required fields correctly.");
                return RedirectToAction(nameof(FacultyDashboard));
            }

            if (model.EndDate <= model.StartDate)
            {
                SetApplicationResult(false, "End Date must be after Start Date.");
                return RedirectToAction(nameof(FacultyDashboard));
            }

            int examId = await _examService.CreateExamAsync(model, facultyId);
            SetApplicationResult(true, "Certification Exam draft created successfully. You can now add questions.");
            return RedirectToAction(nameof(Questions), new { id = examId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var exam = await _examService.GetExamByIdAsync(id);
            if (exam == null)
            {
                SetApplicationResult(false, "Certification exam not found.");
                return RedirectToAction(nameof(FacultyDashboard));
            }

            var model = new CertificationExamFormModel
            {
                Id = exam.Id,
                Title = exam.Title,
                CourseId = exam.CourseId,
                SubjectId = exam.SubjectId,
                CertificationTopic = exam.CertificationTopic,
                Description = exam.Description,
                DurationMinutes = exam.DurationMinutes,
                TotalMarks = exam.TotalMarks,
                PassingPercentage = exam.PassingPercentage,
                StartDate = exam.StartDate,
                EndDate = exam.EndDate,
                MaxAttempts = exam.MaxAttempts
            };

            int facultyId = GetUserId();
            var dashboard = await _examService.GetFacultyDashboardAsync(facultyId);
            ViewBag.Courses = dashboard.Courses;
            ViewBag.Subjects = dashboard.Subjects;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CertificationExamFormModel model)
        {
            int facultyId = GetUserId();
            if (!ModelState.IsValid)
            {
                SetApplicationResult(false, "Please correct validation errors.");
                return View(model);
            }

            if (model.EndDate <= model.StartDate)
            {
                SetApplicationResult(false, "End Date must be after Start Date.");
                return View(model);
            }

            bool updated = await _examService.UpdateExamAsync(model, facultyId);
            if (!updated)
            {
                SetApplicationResult(false, "Failed to update exam.");
                return RedirectToAction(nameof(FacultyDashboard));
            }

            SetApplicationResult(true, "Certification exam details updated successfully.");
            return RedirectToAction(nameof(FacultyDashboard));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            int facultyId = GetUserId();
            bool deleted = await _examService.DeleteExamAsync(id, facultyId);
            if (!deleted)
            {
                SetApplicationResult(false, "Exam not found or cannot be deleted.");
            }
            else
            {
                SetApplicationResult(true, "Exam removed successfully.");
            }
            return RedirectToAction(nameof(FacultyDashboard));
        }

        [HttpGet]
        public async Task<IActionResult> Questions(int id)
        {
            var exam = await _examService.GetExamByIdAsync(id);
            if (exam == null)
            {
                SetApplicationResult(false, "Certification exam not found.");
                return RedirectToAction(nameof(FacultyDashboard));
            }

            var questions = await _examService.GetQuestionsAsync(id);
            ViewBag.Exam = exam;
            return View(questions);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveQuestion([FromBody] CertificationQuestionFormModel model)
        {
            int facultyId = GetUserId();
            if (model == null || string.IsNullOrWhiteSpace(model.QuestionText))
            {
                return Json(new { success = false, message = "Question text is required." });
            }

            if (model.Options == null || !model.Options.Any())
            {
                return Json(new { success = false, message = "Options are required." });
            }

            if (!model.Options.Any(o => o.IsCorrect))
            {
                return Json(new { success = false, message = "At least one correct answer must be marked." });
            }

            bool saved = await _examService.SaveQuestionAsync(model, facultyId);
            return Json(new { success = saved, message = saved ? "Question saved successfully!" : "Failed to save question." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuestion(int id, int examId)
        {
            int facultyId = GetUserId();
            bool deleted = await _examService.DeleteQuestionAsync(id, facultyId);
            if (deleted)
            {
                SetApplicationResult(true, "Question deleted successfully.");
            }
            else
            {
                SetApplicationResult(false, "Failed to delete question.");
            }
            return RedirectToAction(nameof(Questions), new { id = examId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateAiQuestions([FromBody] AiQuestionGenerateRequestModel request)
        {
            int facultyId = GetUserId();
            if (request == null || request.ExamId <= 0)
            {
                return Json(new { success = false, message = "Invalid generation request parameters." });
            }

            var response = await _examService.GenerateAiQuestionsAsync(request, facultyId);
            if (!response.Success)
            {
                return Json(new { success = false, message = response.ErrorMessage ?? "Question generation failed." });
            }

            return Json(new
            {
                success = true,
                questions = response.Questions,
                count = response.Questions.Count,
                message = $"Successfully generated {response.Questions.Count} question(s)."
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveBatchQuestions([FromBody] AiQuestionBatchSaveRequestModel model)
        {
            int facultyId = GetUserId();
            if (model == null || model.ExamId <= 0 || model.Questions == null || !model.Questions.Any())
            {
                return Json(new { success = false, message = "No questions selected to save." });
            }

            var (success, message, savedCount, totalMarks) = await _examService.SaveBatchQuestionsAsync(model.ExamId, model.Questions, facultyId);
            return Json(new
            {
                success = success,
                message = message,
                savedCount = savedCount,
                totalMarks = totalMarks
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(int id)
        {
            int facultyId = GetUserId();
            var (success, message) = await _examService.PublishExamAsync(id, facultyId);
            SetApplicationResult(success, message);
            return RedirectToAction(nameof(FacultyDashboard));
        }

        [HttpGet]
        public async Task<IActionResult> Preview(int id)
        {
            var exam = await _examService.GetExamByIdAsync(id);
            if (exam == null)
            {
                SetApplicationResult(false, "Certification exam not found.");
                return RedirectToAction(nameof(FacultyDashboard));
            }

            var questions = await _examService.GetQuestionsAsync(id);
            ViewBag.Exam = exam;
            return View(questions);
        }

        [HttpGet]
        public async Task<IActionResult> FacultyAttempts(int id)
        {
            int facultyId = GetUserId();
            var model = await _examService.GetFacultyAttemptsAsync(id, facultyId);
            return View(model);
        }

        #endregion

        #region Student Routes

        [HttpGet]
        public async Task<IActionResult> StudentDashboard()
        {
            int studentId = GetUserId();
            var exams = await _examService.GetStudentExamsAsync(studentId);
            return View(exams);
        }

        [HttpGet]
        public async Task<IActionResult> Attempt(int id)
        {
            int studentId = GetUserId();
            var takingModel = await _examService.StartOrResumeAttemptAsync(id, studentId);
            if (takingModel == null)
            {
                SetApplicationResult(false, "This exam is currently unavailable, outside its schedule, or you have already reached the maximum attempts.");
                return RedirectToAction(nameof(StudentDashboard));
            }

            return View(takingModel);
        }

        [HttpPost]
        public async Task<IActionResult> SaveAnswer(int attemptId, int questionId, string selectedOptionIds, int remainingSeconds)
        {
            int studentId = GetUserId();
            bool saved = await _examService.SaveAnswerAsync(attemptId, questionId, selectedOptionIds, remainingSeconds, studentId);
            return Json(new { success = saved });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int attemptId)
        {
            int studentId = GetUserId();
            var (success, message, attId) = await _examService.SubmitAttemptAsync(attemptId, studentId, _env.WebRootPath);
            if (!success)
            {
                SetApplicationResult(false, message);
                return RedirectToAction(nameof(StudentDashboard));
            }

            SetApplicationResult(true, message);
            return RedirectToAction(nameof(Result), new { attemptId = attId });
        }

        [HttpGet]
        public async Task<IActionResult> Result(int attemptId)
        {
            int studentId = GetUserId();
            var result = await _examService.GetAttemptResultAsync(attemptId, studentId);
            if (result == null)
            {
                SetApplicationResult(false, "Result record not found.");
                return RedirectToAction(nameof(StudentDashboard));
            }

            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadCertificate(int certificateId)
        {
            var cert = await _examService.GetCertificateByIdAsync(certificateId);
            if (cert == null)
            {
                return NotFound("Certificate record not found.");
            }

            // Security check: student can only download their own; faculty or admin can download any
            int userId = GetUserId();
            string userRole = GetUserRole().ToLower();
            bool isStaff = userRole.Contains("faculty") || userRole.Contains("admin") || userRole.Contains("teacher");

            if (!isStaff && cert.StudentId != userId)
            {
                return Forbid();
            }

            string fullPath = Path.Combine(_env.WebRootPath, cert.PdfFilePath.TrimStart('/', '\\'));
            if (!System.IO.File.Exists(fullPath))
            {
                // Try regenerating if file missing
                var regenerated = await _examService.GenerateCertificateForAttemptAsync(cert.CertificationExamAttemptId, _env.WebRootPath);
                if (regenerated != null)
                {
                    fullPath = Path.Combine(_env.WebRootPath, regenerated.PdfFilePath.TrimStart('/', '\\'));
                }
            }

            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound("Certificate PDF file could not be generated or located.");
            }

            byte[] pdfBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            return File(pdfBytes, "application/pdf", $"{cert.CertificateNumber}.pdf");
        }

        #endregion

        #region QR Public Verification

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Verify(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return View(new PublicCertificateVerificationViewModel { IsFound = false });
            }

            var cert = await _examService.GetCertificateByTokenAsync(token.Trim());
            if (cert == null)
            {
                return View(new PublicCertificateVerificationViewModel { IsFound = false });
            }

            var vm = new PublicCertificateVerificationViewModel
            {
                CertificateNumber = cert.CertificateNumber,
                StudentFullName = cert.StudentFullName,
                CertificationTopic = cert.CertificationTopic,
                FinalGrade = cert.FinalGrade,
                IsPassed = cert.IsPassed,
                IssueDate = cert.IssueDate,
                Status = cert.Status,
                RevocationReason = cert.RevocationReason,
                RevokedOn = cert.RevokedOn,
                InstitutionName = "EduDigiClass University",
                IsFound = true
            };

            return View(vm);
        }

        #endregion

        #region Admin Certificate Management

        [HttpGet]
        public async Task<IActionResult> AdminCertificates(string? searchTerm, string? statusFilter, int? examId)
        {
            var certs = await _examService.GetAllCertificatesAsync(searchTerm, statusFilter, examId);
            ViewBag.SearchTerm = searchTerm;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.ExamId = examId;
            return View(certs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeCertificate(int certificateId, string reason)
        {
            int adminId = GetUserId();
            var (success, msg) = await _examService.RevokeCertificateAsync(certificateId, reason, adminId);
            SetApplicationResult(success, msg);
            return RedirectToAction(nameof(AdminCertificates));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RetryCertificate(int certificateId)
        {
            var (success, msg) = await _examService.RetryCertificateGenerationAsync(certificateId, _env.WebRootPath);
            SetApplicationResult(success, msg);
            return RedirectToAction(nameof(AdminCertificates));
        }

        #endregion
    }
}
