using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.Repository;
using TMS.ViewModels.Assessments;
using TMS.Web.Controllers;
using TMS.Web.Services.Proctoring;

namespace TMS.Web.Areas.Admin.Controllers
{
    [Authorize]
    [Area("Admin")]
    public class ProctoringExamController : BaseController
    {
        private readonly IProctoringApiClient _proctoringApi;
        private readonly ApplicationDBContext _db;
        private readonly TMS.Web.Services.Email.IEmailOutboxService _outboxService;

        public ProctoringExamController(
            IProctoringApiClient proctoringApi,
            ApplicationDBContext db,
            TMS.Web.Services.Email.IEmailOutboxService outboxService)
        {
            _proctoringApi = proctoringApi;
            _db = db;
            _outboxService = outboxService;
        }

        // =========================================================================
        // --- ALL PROCTORED EXAMS LIST (GET) ---
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? status)
        {
            var exams = await _proctoringApi.GetAllExamsAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.Trim();
                exams = exams.Where(e =>
                    e.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(e.TargetCourseName) && e.TargetCourseName.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(e.TargetBatch) && e.TargetBatch.Contains(q, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.Equals("live", StringComparison.OrdinalIgnoreCase) || status.Equals("published", StringComparison.OrdinalIgnoreCase))
                {
                    exams = exams.Where(e => e.IsActive).ToList();
                }
                else if (status.Equals("draft", StringComparison.OrdinalIgnoreCase))
                {
                    exams = exams.Where(e => !e.IsActive).ToList();
                }
            }

            ViewBag.Search = search;
            ViewBag.Status = status ?? "all";
            ViewBag.FrontendBaseUrl = _proctoringApi.GetFrontendUrl();

            return View(exams);
        }

        // =========================================================================
        // --- CREATE PROCTORED EXAM (GET) ---
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateMasterDataViewBags();

            var model = new CreateProctorExamRequest
            {
                DurationMinutes = 60,
                TotalMarks = 100,
                PassingPercentage = 40,
                MaxAttempts = 1,
                ExamType = "Standard",
                RequiresSEB = true,
                AllowRevisit = true,
                AiEvaluationEnabled = true,
                AiEvaluationDelay = 0,
                AiEvaluationDelayUnit = "Hours",
                IsActive = true,
                IsGlobal = false,
                StartTime = DateTime.UtcNow.AddHours(1)
            };

            return View(model);
        }

        // =========================================================================
        // --- CREATE PROCTORED EXAM (POST) ---
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProctorExamRequest model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateMasterDataViewBags();
                return View(model);
            }

            if (!model.IsGlobal && !model.TargetCourseId.HasValue && string.IsNullOrWhiteSpace(model.TargetCourseName))
            {
                ModelState.AddModelError(nameof(model.TargetCourseId), "Please select a target course or mark the exam as Global / All Courses.");
                await PopulateMasterDataViewBags();
                return View(model);
            }

            try
            {
                // Ensure StartTime is a valid SQL datetime
                if (!model.StartTime.HasValue || model.StartTime.Value == default || model.StartTime.Value.Year < 2000)
                {
                    model.StartTime = DateTime.UtcNow.AddMinutes(15);
                }

                // Resolve course name from LMS CourseMaster if ID provided
                if (model.TargetCourseId.HasValue && string.IsNullOrWhiteSpace(model.TargetCourseName))
                {
                    var c = await _db.CourseMasters.FindAsync(model.TargetCourseId.Value);
                    if (c != null) model.TargetCourseName = c.Name;
                }

                // Call existing AI Proctoring Platform API
                var createdExam = await _proctoringApi.CreateExamAsync(model);
                if (createdExam == null)
                {
                    ModelState.AddModelError("", "Failed to create exam in the AI Proctoring Platform. Please verify backend service availability.");
                    await PopulateMasterDataViewBags();
                    return View(model);
                }

                TempData["SuccessMessage"] = $"Exam \"{createdExam.Title}\" was successfully created. You can now add questions to this exam.";
                return RedirectToAction("ManageQuestions", "ProctoringExam", new { area = "Admin", examId = createdExam.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error connecting to AI Proctoring API: " + ex.Message);
                await PopulateMasterDataViewBags();
                return View(model);
            }
        }

        // =========================================================================
        // --- MANAGE QUESTIONS & QUESTION BUILDER (GET) ---
        // =========================================================================
        [HttpGet]
        public async Task<IActionResult> ManageQuestions(
            Guid? id,
            [FromQuery] Guid? examId,
            [FromQuery] string? tab,
            [FromQuery] string? search,
            [FromQuery] string? type)
        {
            var targetId = id ?? examId;
            if (!targetId.HasValue || targetId.Value == Guid.Empty)
            {
                TempData["ErrorMessage"] = "Invalid exam ID specified.";
                return RedirectToAction(nameof(Create));
            }

            var exam = await _proctoringApi.GetExamByIdAsync(targetId.Value);
            if (exam == null)
            {
                TempData["ErrorMessage"] = "Exam record not found or could not be loaded.";
                return RedirectToAction(nameof(Create));
            }

            var existingQuestions = await _proctoringApi.GetExamQuestionsAsync(targetId.Value);
            var totalMarks = existingQuestions.Sum(q => q.Marks);

            // Load central Question Bank items with optional filter
            var questionBank = await _proctoringApi.GetQuestionBankAsync(search, type, null, 1, 150);

            var vm = new ManageQuestionsViewModel
            {
                Exam = exam,
                ExistingQuestions = existingQuestions,
                TotalCalculatedMarks = totalMarks,
                QuestionBank = questionBank,
                ActiveTab = string.IsNullOrWhiteSpace(tab) ? "review" : tab.Trim().ToLowerInvariant(),
                BankSearch = search,
                BankTypeFilter = type,
                NewQuestion = new ExamQuestionItemDto
                {
                    ExamId = exam.Id,
                    Marks = 1,
                    QuestionType = "MCQ",
                    Difficulty = "Medium"
                }
            };

            ViewBag.FrontendBaseUrl = _proctoringApi.GetFrontendUrl();
            return View(vm);
        }

        // =========================================================================
        // --- ADD MANUAL QUESTION (POST) ---
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddManualQuestion(Guid examId, ExamQuestionItemDto newQuestion)
        {
            if (examId == Guid.Empty)
            {
                TempData["ErrorMessage"] = "Invalid exam ID.";
                return RedirectToAction(nameof(Create));
            }

            newQuestion.ExamId = examId;

            if (string.IsNullOrWhiteSpace(newQuestion.QuestionText))
            {
                TempData["ErrorMessage"] = "Question text is required.";
                return RedirectToAction(nameof(ManageQuestions), new { area = "Admin", examId, tab = "manual" });
            }

            var qType = string.IsNullOrWhiteSpace(newQuestion.QuestionType) ? "MCQ" : newQuestion.QuestionType.ToUpperInvariant();
            newQuestion.QuestionType = qType;

            // Type-specific adjustments
            if (qType == "MCQ")
            {
                if (string.IsNullOrWhiteSpace(newQuestion.OptionA) || string.IsNullOrWhiteSpace(newQuestion.OptionB))
                {
                    TempData["ErrorMessage"] = "Please provide at least Option A and Option B for the multiple-choice question.";
                    return RedirectToAction(nameof(ManageQuestions), new { area = "Admin", examId, tab = "manual" });
                }
                if (string.IsNullOrWhiteSpace(newQuestion.CorrectOption))
                {
                    newQuestion.CorrectOption = "A";
                }
            }
            else if (qType == "CODING")
            {
                if (string.IsNullOrWhiteSpace(newQuestion.CodeLanguage))
                {
                    newQuestion.CodeLanguage = "python";
                }
                if (newQuestion.TestCases == null || newQuestion.TestCases.Count == 0)
                {
                    newQuestion.TestCases = new List<CreateCodingTestCaseDto>
                    {
                        new CreateCodingTestCaseDto
                        {
                            InputData = "",
                            ExpectedOutput = !string.IsNullOrWhiteSpace(newQuestion.AnswerText) ? newQuestion.AnswerText.Trim() : "1",
                            IsHidden = false,
                            Weightage = newQuestion.Marks > 0 ? newQuestion.Marks : 1.0m,
                            OrderIndex = 1,
                            Explanation = "Sample test case"
                        }
                    };
                }
            }

            var ok = await _proctoringApi.AddQuestionAsync(newQuestion);
            if (ok)
            {
                TempData["SuccessMessage"] = $"Question successfully added to exam.";
                return RedirectToAction(nameof(ManageQuestions), new { area = "Admin", examId, tab = "review" });
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to add question to exam in AI Proctoring Platform.";
                return RedirectToAction(nameof(ManageQuestions), new { area = "Admin", examId, tab = "manual" });
            }
        }

        // =========================================================================
        // --- SAVE MANUAL QUESTION (AJAX) ---
        // =========================================================================
        [HttpPost]
        public async Task<IActionResult> SaveManualQuestionAjax([FromBody] ExamQuestionItemDto newQuestion)
        {
            if (newQuestion == null || newQuestion.ExamId == Guid.Empty)
            {
                return Json(new { success = false, message = "Invalid exam specification." });
            }

            if (string.IsNullOrWhiteSpace(newQuestion.QuestionText))
            {
                return Json(new { success = false, message = "Question statement is required." });
            }

            var qType = string.IsNullOrWhiteSpace(newQuestion.QuestionType) ? "MCQ" : newQuestion.QuestionType.ToUpperInvariant();
            newQuestion.QuestionType = qType;

            if (qType == "MCQ")
            {
                if (string.IsNullOrWhiteSpace(newQuestion.OptionA) || string.IsNullOrWhiteSpace(newQuestion.OptionB))
                {
                    return Json(new { success = false, message = "At least Option A and Option B are required for MCQ." });
                }
                if (string.IsNullOrWhiteSpace(newQuestion.CorrectOption))
                {
                    newQuestion.CorrectOption = "A";
                }
            }
            else if (qType == "CODING")
            {
                if (string.IsNullOrWhiteSpace(newQuestion.CodeLanguage))
                {
                    newQuestion.CodeLanguage = "python";
                }
                if (newQuestion.TestCases == null || !newQuestion.TestCases.Any())
                {
                    return Json(new { success = false, message = "At least one test case is required for a coding question." });
                }
                if (newQuestion.TestCases.Any(tc => string.IsNullOrWhiteSpace(tc.ExpectedOutput)))
                {
                    return Json(new { success = false, message = "All test cases must specify non-empty expected output." });
                }
            }
            else if (qType == "NUMERICAL" || qType == "EQUATION")
            {
                if (string.IsNullOrWhiteSpace(newQuestion.AnswerText) && string.IsNullOrWhiteSpace(newQuestion.Formula))
                {
                    return Json(new { success = false, message = $"Expected {(qType == "NUMERICAL" ? "numerical value" : "equation formula")} is required." });
                }
            }

            if (newQuestion.Marks <= 0)
            {
                newQuestion.Marks = 1;
            }

            var ok = await _proctoringApi.AddQuestionAsync(newQuestion);
            if (ok)
            {
                return Json(new { success = true, message = "Question saved successfully!" });
            }

            return Json(new { success = false, message = "Failed to save question in AI Proctoring backend." });
        }

        // =========================================================================
        // --- UPLOAD QUESTION IMAGE (AJAX) ---
        // =========================================================================
        [HttpPost]
        public async Task<IActionResult> UploadQuestionImage(Microsoft.AspNetCore.Http.IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "No image file provided." });
            }

            var imageUrl = await _proctoringApi.UploadQuestionImageAsync(file);
            if (!string.IsNullOrEmpty(imageUrl))
            {
                var fullUrl = imageUrl.StartsWith("http") ? imageUrl : $"{_proctoringApi.GetFrontendUrl()}{imageUrl}";
                return Json(new { success = true, imageUrl, fullUrl });
            }

            return Json(new { success = false, message = "Failed to upload image to AI Proctoring service." });
        }

        // =========================================================================
        // --- IMPORT FROM QUESTION BANK (POST) ---
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportFromBank(Guid examId, List<Guid> selectedQuestionIds)
        {
            if (examId == Guid.Empty)
            {
                TempData["ErrorMessage"] = "Invalid exam ID.";
                return RedirectToAction(nameof(Create));
            }

            if (selectedQuestionIds == null || !selectedQuestionIds.Any())
            {
                TempData["ErrorMessage"] = "Please select at least one question from the Question Bank to import.";
                return RedirectToAction(nameof(ManageQuestions), new { area = "Admin", examId, tab = "bank" });
            }

            var existing = await _proctoringApi.GetExamQuestionsAsync(examId);
            int startOrder = existing.Count;

            var dtos = selectedQuestionIds.Distinct().Select((qId, idx) => new AddQuestionToExamDto
            {
                QuestionId = qId,
                SortOrder = startOrder + idx + 1,
                IsRequired = true
            }).ToList();

            var result = await _proctoringApi.ImportQuestionsBulkAsync(examId, dtos);
            if (result.success)
            {
                TempData["SuccessMessage"] = result.message;
                return RedirectToAction(nameof(ManageQuestions), new { area = "Admin", examId, tab = "review" });
            }
            else
            {
                TempData["ErrorMessage"] = result.message;
                return RedirectToAction(nameof(ManageQuestions), new { area = "Admin", examId, tab = "bank" });
            }
        }

        // =========================================================================
        // --- UPLOAD PDF & EXTRACT QUESTIONS (POST JSON) ---
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPdfExtract(Guid examId, Microsoft.AspNetCore.Http.IFormFile? pdfFile)
        {
            if (pdfFile == null || pdfFile.Length == 0)
            {
                return Json(new { success = false, message = "No PDF file uploaded. Please select a valid .pdf file." });
            }

            if (!pdfFile.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { success = false, message = "Only PDF (.pdf) files are supported for question extraction." });
            }

            var result = await _proctoringApi.ExtractPdfQuestionsAsync(pdfFile);
            if (result == null || (result.Questions.Count == 0 && !string.IsNullOrEmpty(result.Error)))
            {
                return Json(new { success = false, message = result?.Error ?? "Failed to extract questions from the PDF document." });
            }

            return Json(new
            {
                success = true,
                source = result.Source,
                message = result.Message ?? $"Extracted {result.Questions.Count} question(s) successfully.",
                questions = result.Questions
            });
        }

        // =========================================================================
        // --- SAVE EXTRACTED QUESTIONS (POST JSON) ---
        // =========================================================================
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SaveExtractedQuestions([FromBody] SaveExtractedQuestionsDto dto)
        {
            if (dto == null || dto.Questions == null || dto.Questions.Count == 0)
            {
                return Json(new { success = false, message = "No questions selected to save." });
            }

            if (!dto.ExamId.HasValue || dto.ExamId.Value == Guid.Empty)
            {
                return Json(new { success = false, message = "Exam ID is missing." });
            }

            var result = await _proctoringApi.SaveExtractedQuestionsAsync(dto);
            return Json(new
            {
                success = result.success,
                saved = result.saved,
                message = result.message
            });
        }

        // =========================================================================
        // --- DELETE QUESTION FROM EXAM (POST) ---
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuestion(Guid examId, Guid questionId)
        {
            if (examId == Guid.Empty || questionId == Guid.Empty)
            {
                TempData["ErrorMessage"] = "Invalid exam or question ID.";
                return RedirectToAction(nameof(Create));
            }

            var ok = await _proctoringApi.RemoveExamQuestionAsync(examId, questionId);
            if (ok)
            {
                TempData["SuccessMessage"] = "Question removed from exam.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to remove question from exam.";
            }

            return RedirectToAction(nameof(ManageQuestions), new { area = "Admin", examId, tab = "review" });
        }

        // =========================================================================
        // --- TOGGLE PUBLISH STATUS (POST) ---
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePublish(Guid examId, string? returnUrl = null)
        {
            if (examId == Guid.Empty)
            {
                TempData["ErrorMessage"] = "Invalid exam ID.";
                return RedirectToAction(nameof(Index));
            }

            var exam = await _proctoringApi.GetExamByIdAsync(examId);
            if (exam == null)
            {
                TempData["ErrorMessage"] = "Exam not found.";
                return RedirectToAction(nameof(Index));
            }

            // If exam is currently Draft (not active) and we want to publish it, verify questions exist
            if (!exam.IsActive)
            {
                var questions = await _proctoringApi.GetExamQuestionsAsync(examId);
                if (questions.Count == 0)
                {
                    TempData["ErrorMessage"] = "Cannot publish an exam with 0 questions. Please add at least one question before publishing.";
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                    return RedirectToAction(nameof(ManageQuestions), new { area = "Admin", examId, tab = "review" });
                }
            }

            bool wasDraft = !exam.IsActive;
            var result = await _proctoringApi.ToggleExamStatusAsync(examId);
            if (result.success)
            {
                TempData["SuccessMessage"] = result.message;

                try
                {
                    if (wasDraft)
                    {
                        // Now published - notify eligible students
                        List<int> studentIds = new();
                        if (exam.TargetCourseId.HasValue && exam.TargetCourseId.Value > 0)
                        {
                            studentIds = await _db.CourseEnrollments
                                .Where(e => e.CourseId == exam.TargetCourseId.Value && e.IsActive && (e.Status == 0 || e.Status == 1))
                                .Select(e => e.StudentId)
                                .Distinct()
                                .ToListAsync();
                        }
                        else
                        {
                            studentIds = await _db.UserMasters
                                .Where(u => u.IsActive && u.Role != null && u.Role.Name.ToLower() == "student" && !string.IsNullOrEmpty(u.Email))
                                .Select(u => u.Id)
                                .ToListAsync();
                        }

                        var students = await _db.UserMasters
                            .Where(u => studentIds.Contains(u.Id) && u.IsActive && !string.IsNullOrEmpty(u.Email))
                            .ToListAsync();

                        string courseName = !string.IsNullOrWhiteSpace(exam.TargetCourseName) ? exam.TargetCourseName : "Academic Course";
                        string scheduleDisplay = exam.StartTime.HasValue
                            ? exam.StartTime.Value.ToString("dd MMM yyyy, hh:mm tt") + " UTC"
                            : "Scheduled as announced";

                        var batch = new List<TMS.ViewModels.Email.EmailEnqueueRequest>();
                        var nowUtc = DateTime.UtcNow;

                        foreach (var s in students)
                        {
                            batch.Add(new TMS.ViewModels.Email.EmailEnqueueRequest
                            {
                                RecipientEmail = s.Email.Trim(),
                                RecipientName = s.Name,
                                Subject = $"AI-Proctored Examination Published: {exam.Title} ({courseName})",
                                TemplateName = "ExamPublication",
                                TemplateData = new Dictionary<string, string>
                                {
                                    ["RecipientName"] = s.Name ?? "Student",
                                    ["ExamTitle"] = exam.Title,
                                    ["CourseName"] = courseName,
                                    ["ExamDate"] = scheduleDisplay,
                                    ["DurationMinutes"] = exam.DurationMinutes.ToString(),
                                    ["RequiresSeb"] = exam.RequiresSEB ? "true" : "false"
                                },
                                EventType = "ProctorExamPublication",
                                IdempotencyKey = $"proctor-pub-{exam.Id}-{s.Id}"
                            });

                            if (exam.StartTime.HasValue)
                            {
                                var remind24 = exam.StartTime.Value.AddHours(-24);
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
                                            ["StartTime"] = scheduleDisplay,
                                            ["HoursLeft"] = "24",
                                            ["RequiresSeb"] = exam.RequiresSEB ? "true" : "false"
                                        },
                                        ScheduledForUtc = remind24,
                                        EventType = "ProctorExamReminder24h",
                                        IdempotencyKey = $"remind-proctor-24h-{exam.Id}-{s.Id}"
                                    });
                                }

                                var remind2 = exam.StartTime.Value.AddHours(-2);
                                if (remind2 > nowUtc)
                                {
                                    batch.Add(new TMS.ViewModels.Email.EmailEnqueueRequest
                                    {
                                        RecipientEmail = s.Email.Trim(),
                                        RecipientName = s.Name,
                                        Subject = $"Exam Reminder: {exam.Title} starts in 2 hours",
                                        TemplateName = "ExamReminder",
                                        TemplateData = new Dictionary<string, string>
                                        {
                                            ["RecipientName"] = s.Name ?? "Student",
                                            ["ExamTitle"] = exam.Title,
                                            ["CourseName"] = courseName,
                                            ["StartTime"] = scheduleDisplay,
                                            ["HoursLeft"] = "2",
                                            ["RequiresSeb"] = exam.RequiresSEB ? "true" : "false"
                                        },
                                        ScheduledForUtc = remind2,
                                        EventType = "ProctorExamReminder2h",
                                        IdempotencyKey = $"remind-proctor-2h-{exam.Id}-{s.Id}"
                                    });
                                }
                            }
                        }

                        if (batch.Any())
                        {
                            await _outboxService.EnqueueBatchAsync(batch);
                        }
                    }
                    else
                    {
                        // Was active, now toggled to Draft/Inactive - cancel pending reminders
                        await _outboxService.CancelScheduledJobsAsync($"remind-proctor-*-{examId}-*");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ProctoringExamController] Outbox error in TogglePublish: {ex.Message}");
                }
            }
            else
            {
                TempData["ErrorMessage"] = result.message;
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(ManageQuestions), new { area = "Admin", examId, tab = "review" });
        }

        // =========================================================================
        // --- DELETE EXAM (POST) ---
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExam(Guid id)
        {
            if (id == Guid.Empty)
            {
                TempData["ErrorMessage"] = "Invalid exam ID.";
                return RedirectToAction(nameof(Index));
            }

            var ok = await _proctoringApi.DeleteExamAsync(id);
            if (ok)
            {
                TempData["SuccessMessage"] = "Exam successfully deleted.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete exam from AI Proctoring platform.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================================
        // --- EXAM CREATED CONFIRMATION (GET - LEGACY REDIRECT TO BUILDER) ---
        // =========================================================================
        [HttpGet]
        public IActionResult Created(Guid? id, [FromQuery] Guid? examId)
        {
            var targetId = id ?? examId;
            if (!targetId.HasValue || targetId.Value == Guid.Empty)
            {
                return RedirectToAction(nameof(Create));
            }

            return RedirectToAction(nameof(ManageQuestions), new { area = "Admin", examId = targetId.Value });
        }

        // =========================================================================
        // --- HELPER: POPULATE MASTER DATA ---
        // =========================================================================
        private async Task PopulateMasterDataViewBags()
        {
            var lmsCourses = await _db.CourseMasters
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new CourseItemDto { Id = c.Id, Name = c.Name, CourseCode = c.CourseCode })
                .ToListAsync();

            ViewBag.Courses = lmsCourses;

            try
            {
                var master = await _proctoringApi.GetMasterDataAsync();
                ViewBag.Departments = master.Departments ?? new List<DepartmentItemDto>();
                ViewBag.Semesters = master.Semesters ?? new List<SemesterItemDto>();
                ViewBag.Batches = master.Batches ?? new List<BatchItemDto>();
            }
            catch
            {
                ViewBag.Departments = new List<DepartmentItemDto>();
                ViewBag.Semesters = new List<SemesterItemDto>();
                ViewBag.Batches = new List<BatchItemDto>();
            }

            ViewBag.FrontendBaseUrl = _proctoringApi.GetFrontendUrl();
        }
    }
}
