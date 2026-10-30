using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TMS.Models.Assessments;
using TMS.Models.Academics;
using TMS.Repository.Managers;
using TMS.Repository;
using TMS.Web.Services;

namespace TMS.Web.Controllers
{
    public class FacultyExamController : BaseController
    {
        private readonly IAssessmentManager _assessmentManager;
        private readonly ApplicationDBContext _context; // Direct DB query for lightweight lists
        private readonly IAiEvaluationService _aiService;
        private readonly IAutomatedResultService _resultService;
        private readonly TMS.Web.Services.Email.IEmailOutboxService _outboxService;

        public FacultyExamController(
            IAssessmentManager assessmentManager, 
            ApplicationDBContext context,
            IAiEvaluationService aiService,
            IAutomatedResultService resultService,
            TMS.Web.Services.Email.IEmailOutboxService outboxService)
        {
            _assessmentManager = assessmentManager;
            _context = context;
            _aiService = aiService;
            _resultService = resultService;
            _outboxService = outboxService;
        }

        // =========================================================================
        // --- EXAM MANAGEMENT ---
        // =========================================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int facultyId = GetUserId();
            var exams = await _assessmentManager.GetExamsAsync(facultyId);
            return View(exams);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Courses = new SelectList(await _context.CourseMasters.Where(c => c.IsActive).ToListAsync(), "Id", "Name");
            return View(new ExamMaster());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ExamMaster exam)
        {
            if (ModelState.IsValid)
            {
                exam.FacultyId = GetUserId();
                int examId = await _assessmentManager.CreateOrUpdateExamAsync(exam, GetUserId());
                if (examId > 0)
                {
                    if (exam.Status == "Active")
                    {
                        await QueueExamPublicationAndRemindersAsync(examId);
                    }
                    SetApplicationResult(true, "Exam created successfully. Now define sections and map questions.");
                    return RedirectToAction(nameof(Edit), new { id = examId });
                }
            }

            ViewBag.Courses = new SelectList(await _context.CourseMasters.Where(c => c.IsActive).ToListAsync(), "Id", "Name");
            return View(exam);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var exam = await _assessmentManager.GetExamDetailsAsync(id);
            if (exam == null || exam.FacultyId != GetUserId())
            {
                return Unauthorized();
            }

            ViewBag.Courses = new SelectList(await _context.CourseMasters.Where(c => c.IsActive).ToListAsync(), "Id", "Name", exam.CourseId);
            return View(exam);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ExamMaster exam)
        {
            if (ModelState.IsValid)
            {
                exam.FacultyId = GetUserId();
                int examId = await _assessmentManager.CreateOrUpdateExamAsync(exam, GetUserId());
                if (examId > 0)
                {
                    await HandleExamUpdateNotificationsAsync(exam);
                    SetApplicationResult(true, "Exam updated successfully.");
                    return RedirectToAction(nameof(Index));
                }
            }

            ViewBag.Courses = new SelectList(await _context.CourseMasters.Where(c => c.IsActive).ToListAsync(), "Id", "Name", exam.CourseId);
            return View(exam);
        }

        [HttpPost]
        public async Task<IActionResult> AddSection(int examId, string sectionName, int totalQuestions, decimal totalMarks, int displayOrder)
        {
            var section = new ExamSection
            {
                ExamId = examId,
                SectionName = sectionName,
                TotalQuestions = totalQuestions,
                TotalMarks = totalMarks,
                DisplayOrder = displayOrder
            };

            var success = await _assessmentManager.AddExamSectionAsync(section);
            return Json(new { success });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSection(int sectionId)
        {
            var success = await _assessmentManager.DeleteExamSectionAsync(sectionId);
            return Json(new { success });
        }

        // =========================================================================
        // --- MAP QUESTIONS SYSTEM ---
        // =========================================================================

        [HttpGet]
        public async Task<IActionResult> MapQuestions(int examId, int sectionId)
        {
            var exam = await _assessmentManager.GetExamDetailsAsync(examId);
            if (exam == null || exam.FacultyId != GetUserId()) return Unauthorized();

            var section = exam.Sections.FirstOrDefault(s => s.Id == sectionId);
            if (section == null) return NotFound("Section not found.");

            // Get reusable questions from question bank for this Course
            var questionPool = await _assessmentManager.GetQuestionBankAsync(courseId: exam.CourseId);
            
            // Get currently mapped question IDs
            var mappedIds = section.QuestionMaps.Select(qm => qm.QuestionId).ToList();

            ViewBag.Exam = exam;
            ViewBag.Section = section;
            ViewBag.MappedIds = mappedIds;

            return View(questionPool);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MapQuestions(int examId, int sectionId, List<int> questionIds)
        {
            var success = await _assessmentManager.MapQuestionsToExamAsync(examId, sectionId, questionIds);
            if (success)
            {
                SetApplicationResult(true, "Questions mapped successfully to the section.");
                return RedirectToAction(nameof(Edit), new { id = examId });
            }

            SetApplicationResult(false, "Failed to map questions.");
            return RedirectToAction(nameof(MapQuestions), new { examId, sectionId });
        }

        // =========================================================================
        // --- QUESTION BANK SYSTEM ---
        // =========================================================================

        [HttpGet]
        public async Task<IActionResult> QuestionBank()
        {
            int facultyId = GetUserId();
            var questions = await _assessmentManager.GetQuestionBankAsync(facultyId: facultyId);
            return View(questions);
        }

        [HttpGet]
        public async Task<IActionResult> CreateQuestion(int? courseId, string? type, string? difficulty)
        {
            ViewBag.Courses = new SelectList(await _context.CourseMasters.Where(c => c.IsActive).ToListAsync(), "Id", "Name", courseId);
            
            var model = new QuestionBank
            {
                CourseId = courseId ?? 0,
                QuestionType = type ?? "MCQ",
                DifficultyLevel = difficulty ?? "Medium",
                Marks = 1,
                NegativeMarks = 0
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuestion(QuestionBank question, List<string> optionText, List<int> correctIndices, string submitAction)
        {
            if (ModelState.IsValid)
            {
                question.FacultyId = GetUserId();
                var options = new List<QuestionOption>();

                if (question.QuestionType != "Subjective" && optionText != null)
                {
                    for (int i = 0; i < optionText.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(optionText[i]))
                        {
                            options.Add(new QuestionOption
                            {
                                OptionText = optionText[i],
                                IsCorrect = correctIndices != null && correctIndices.Contains(i)
                            });
                        }
                    }
                }

                int qid = await _assessmentManager.CreateOrUpdateQuestionAsync(question, options, GetUserId());
                if (qid > 0)
                {
                    SetApplicationResult(true, "Question saved successfully.");
                    if (submitAction == "saveAndAddAnother")
                    {
                        return RedirectToAction(nameof(CreateQuestion), new { courseId = question.CourseId, type = question.QuestionType, difficulty = question.DifficultyLevel });
                    }
                    return RedirectToAction(nameof(QuestionBank));
                }
            }

            ViewBag.Courses = new SelectList(await _context.CourseMasters.Where(c => c.IsActive).ToListAsync(), "Id", "Name", question.CourseId);
            return View(question);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var success = await _assessmentManager.DeleteQuestionAsync(id);
            return Json(new { success });
        }

        // =========================================================================
        // --- EXAM ASSIGNMENT SYSTEM ---
        // =========================================================================

        [HttpGet]
        public async Task<IActionResult> Assign(int examId)
        {
            var exam = await _assessmentManager.GetExamDetailsAsync(examId);
            if (exam == null || exam.FacultyId != GetUserId()) return Unauthorized();

            // Fetch all active students
            var students = await _context.UserMasters
                .Where(u => u.RoleId == 3 && u.IsActive) // Role 3 is Student
                .ToListAsync();

            ViewBag.Exam = exam;
            return View(students);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(int examId, List<int> studentIds)
        {
            if (studentIds == null || !studentIds.Any())
            {
                SetApplicationResult(false, "Please select at least one student.");
                return RedirectToAction(nameof(Assign), new { examId });
            }

            var success = await _assessmentManager.AssignExamToStudentsAsync(examId, studentIds, GetUserId());
            if (success)
            {
                await QueueAssignedExamPublicationAndRemindersAsync(examId, studentIds);
                SetApplicationResult(true, "Exam assigned to selected students successfully.");
                return RedirectToAction(nameof(Index));
            }

            SetApplicationResult(false, "Failed to assign exam.");
            return RedirectToAction(nameof(Assign), new { examId });
        }

        // =========================================================================
        // --- EVALUATION SYSTEM ---
        // =========================================================================

        [HttpGet]
        public async Task<IActionResult> EvaluateList()
        {
            int facultyId = GetUserId();
            var attempts = await _assessmentManager.GetAttemptsForEvaluationAsync(facultyId);
            return View(attempts);
        }

        [HttpGet]
        public async Task<IActionResult> EvaluateAttempt(int attemptId)
        {
            var attempt = await _assessmentManager.GetAttemptDetailsAsync(attemptId);
            if (attempt == null || attempt.Exam == null || attempt.Exam.FacultyId != GetUserId())
            {
                return Unauthorized();
            }

            return View(attempt);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EvaluateSubjectiveAnswerAi(int attemptId, int questionId, System.Threading.CancellationToken cancellationToken)
        {
            int facultyId = GetUserId();
            var attempt = await _context.StudentExamAttempts
                .Include(a => a.Exam)
                .Include(a => a.Answers)
                .FirstOrDefaultAsync(a => a.Id == attemptId, cancellationToken);

            if (attempt == null || attempt.Exam == null || attempt.Exam.FacultyId != facultyId)
            {
                return Forbid();
            }

            var studentAnswer = attempt.Answers.FirstOrDefault(a => a.QuestionId == questionId);
            var question = await _context.QuestionBanks.FindAsync(new object[] { questionId }, cancellationToken);

            if (question == null)
            {
                return NotFound(new { success = false, message = "Question not found." });
            }

            var request = new AiEvaluationRequest
            {
                QuestionId = question.Id,
                QuestionType = question.QuestionType ?? "Written",
                QuestionText = question.QuestionText,
                MaxMarks = question.Marks,
                ExpectedAnswer = !string.IsNullOrWhiteSpace(question.Explanation) ? question.Explanation : question.CorrectAnswer,
                Rubric = question.Explanation,
                RubricVersion = 1,
                StudentAnswer = studentAnswer?.SubjectiveAnswer ?? ""
            };

            var eval = await _aiService.EvaluateQuestionAsync(request, cancellationToken);

            return Json(new
            {
                success = eval.Success,
                errorMessage = eval.ErrorMessage,
                questionId = question.Id,
                evaluation = eval
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveEvaluation(int attemptId, decimal totalMarksAwarded, string? feedback, Dictionary<int, decimal> questionMarks)
        {
            var attempt = await _context.StudentExamAttempts.FindAsync(attemptId);
            var success = await _assessmentManager.SaveFacultyEvaluationAsync(attemptId, GetUserId(), feedback, totalMarksAwarded, questionMarks);
            if (success)
            {
                if (attempt != null)
                {
                    try
                    {
                        await _resultService.CalculateExamMasterResultsAsync(attempt.ExamId, GetUserId());
                    }
                    catch (Exception)
                    {
                        // Preserve faculty evaluation if secondary institutional sync encounters policy warning
                    }

                    await QueueExamResultPublishedAsync(attemptId, totalMarksAwarded);
                }
                SetApplicationResult(true, "Evaluation saved and results published successfully.");
                return RedirectToAction(nameof(EvaluateList));
            }

            SetApplicationResult(false, "Failed to save evaluation.");
            return RedirectToAction(nameof(EvaluateAttempt), new { attemptId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CalculateExamResults(int examId, System.Threading.CancellationToken cancellationToken)
        {
            var exam = await _context.ExamMasters.FindAsync(new object[] { examId }, cancellationToken);
            if (exam == null || exam.FacultyId != GetUserId()) return Forbid();

            var summary = await _resultService.CalculateExamMasterResultsAsync(examId, GetUserId(), cancellationToken: cancellationToken);
            return Json(new { success = true, summary });
        }

        // =========================================================================
        // --- ANALYTICS DASHBOARD ---
        // =========================================================================

        [HttpGet]
        public async Task<IActionResult> Analytics(int examId)
        {
            var exam = await _context.ExamMasters.FindAsync(examId);
            if (exam == null || exam.FacultyId != GetUserId()) return Unauthorized();

            var analytics = await _assessmentManager.GetExamAnalyticsAsync(examId);
            return View(analytics);
        }

        #region Private Email Queue Helpers

        private static string FormatAcademicDate(DateTime utcDateTime)
        {
            try
            {
                var istZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
                var istTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc), istZone);
                return istTime.ToString("dd MMM yyyy, hh:mm tt") + " IST";
            }
            catch
            {
                return utcDateTime.ToString("dd MMM yyyy, hh:mm tt") + " UTC";
            }
        }

        private async Task QueueExamPublicationAndRemindersAsync(int examId)
        {
            try
            {
                var exam = await _context.ExamMasters.Include(e => e.Course).FirstOrDefaultAsync(e => e.Id == examId);
                if (exam == null || exam.Status != "Active") return;

                var studentIds = await _context.CourseEnrollments
                    .Where(e => e.CourseId == exam.CourseId && e.IsActive && (e.Status == 0 || e.Status == 1))
                    .Select(e => e.StudentId)
                    .Distinct()
                    .ToListAsync();

                await QueueAssignedExamPublicationAndRemindersAsync(examId, studentIds);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FacultyExamController] Failed to queue exam publication: {ex.Message}");
            }
        }

        private async Task QueueAssignedExamPublicationAndRemindersAsync(int examId, List<int> studentIds)
        {
            try
            {
                if (!studentIds.Any()) return;

                var exam = await _context.ExamMasters.Include(e => e.Course).FirstOrDefaultAsync(e => e.Id == examId);
                if (exam == null) return;

                var students = await _context.UserMasters
                    .Where(u => studentIds.Contains(u.Id) && u.IsActive && !string.IsNullOrEmpty(u.Email))
                    .ToListAsync();

                string courseName = exam.Course?.Name ?? "Course";
                string scheduleDisplay = $"{FormatAcademicDate(exam.StartDate)} – {FormatAcademicDate(exam.EndDate)}";
                var batch = new List<TMS.ViewModels.Email.EmailEnqueueRequest>();
                var nowUtc = DateTime.UtcNow;

                foreach (var student in students)
                {
                    // 1. Publication Notice
                    batch.Add(new TMS.ViewModels.Email.EmailEnqueueRequest
                    {
                        RecipientEmail = student.Email.Trim(),
                        RecipientName = student.Name,
                        Subject = $"Examination Notice: {exam.Title} ({courseName})",
                        TemplateName = "ExamPublication",
                        TemplateData = new Dictionary<string, string>
                        {
                            ["RecipientName"] = student.Name ?? "Student",
                            ["ExamTitle"] = exam.Title,
                            ["CourseName"] = courseName,
                            ["ExamDate"] = scheduleDisplay,
                            ["DurationMinutes"] = exam.DurationMinutes.ToString(),
                            ["RequiresSeb"] = "false"
                        },
                        EventType = "ExamPublication",
                        IdempotencyKey = $"exam-pub-{exam.Id}-{student.Id}"
                    });

                    // 2. 24-Hour Reminder
                    var remind24 = exam.StartDate.AddHours(-24);
                    if (remind24 > nowUtc)
                    {
                        batch.Add(new TMS.ViewModels.Email.EmailEnqueueRequest
                        {
                            RecipientEmail = student.Email.Trim(),
                            RecipientName = student.Name,
                            Subject = $"Exam Reminder: {exam.Title} starts in 24 hours",
                            TemplateName = "ExamReminder",
                            TemplateData = new Dictionary<string, string>
                            {
                                ["RecipientName"] = student.Name ?? "Student",
                                ["ExamTitle"] = exam.Title,
                                ["CourseName"] = courseName,
                                ["StartTime"] = FormatAcademicDate(exam.StartDate),
                                ["HoursLeft"] = "24",
                                ["RequiresSeb"] = "false"
                            },
                            ScheduledForUtc = remind24,
                            EventType = "ExamReminder24h",
                            IdempotencyKey = $"remind-exam-24h-{exam.Id}-{student.Id}"
                        });
                    }

                    // 3. 2-Hour Reminder
                    var remind2 = exam.StartDate.AddHours(-2);
                    if (remind2 > nowUtc)
                    {
                        batch.Add(new TMS.ViewModels.Email.EmailEnqueueRequest
                        {
                            RecipientEmail = student.Email.Trim(),
                            RecipientName = student.Name,
                            Subject = $"Exam Reminder: {exam.Title} starts in 2 hours",
                            TemplateName = "ExamReminder",
                            TemplateData = new Dictionary<string, string>
                            {
                                ["RecipientName"] = student.Name ?? "Student",
                                ["ExamTitle"] = exam.Title,
                                ["CourseName"] = courseName,
                                ["StartTime"] = FormatAcademicDate(exam.StartDate),
                                ["HoursLeft"] = "2",
                                ["RequiresSeb"] = "false"
                            },
                            ScheduledForUtc = remind2,
                            EventType = "ExamReminder2h",
                            IdempotencyKey = $"remind-exam-2h-{exam.Id}-{student.Id}"
                        });
                    }
                }

                if (batch.Any())
                {
                    await _outboxService.EnqueueBatchAsync(batch);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FacultyExamController] Failed to queue assigned exam emails: {ex.Message}");
            }
        }

        private async Task HandleExamUpdateNotificationsAsync(ExamMaster exam)
        {
            try
            {
                // Invalidate obsolete reminders
                await _outboxService.CancelScheduledJobsAsync($"remind-exam-*-{exam.Id}-*");

                if (exam.Status == "Active")
                {
                    await QueueExamPublicationAndRemindersAsync(exam.Id);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FacultyExamController] Failed to handle exam update notifications: {ex.Message}");
            }
        }

        private async Task QueueExamResultPublishedAsync(int attemptId, decimal totalMarks)
        {
            try
            {
                var attempt = await _context.StudentExamAttempts
                    .Include(a => a.Exam)
                        .ThenInclude(e => e.Course)
                    .Include(a => a.Student)
                    .FirstOrDefaultAsync(a => a.Id == attemptId);

                if (attempt?.Student == null || string.IsNullOrWhiteSpace(attempt.Student.Email) || attempt.Exam == null) return;

                string courseName = attempt.Exam.Course?.Name ?? "Course";

                await _outboxService.EnqueueAsync(new TMS.ViewModels.Email.EmailEnqueueRequest
                {
                    RecipientEmail = attempt.Student.Email.Trim(),
                    RecipientName = attempt.Student.Name,
                    Subject = $"Official Result Published: {attempt.Exam.Title} ({courseName})",
                    TemplateName = "ResultsPublished",
                    TemplateData = new Dictionary<string, string>
                    {
                        ["RecipientName"] = attempt.Student.Name ?? "Student",
                        ["AssessmentTitle"] = attempt.Exam.Title,
                        ["CourseName"] = courseName,
                        ["MarksObtained"] = totalMarks.ToString("0.##"),
                        ["MaxMarks"] = attempt.Exam.TotalMarks.ToString(),
                        ["Grade"] = attempt.ResultStatus ?? "Pass"
                    },
                    EventType = "ResultsPublished",
                    IdempotencyKey = $"result-exam-{attemptId}"
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FacultyExamController] Failed to queue exam result email: {ex.Message}");
            }
        }

        #endregion
    }
}

