using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TMS.Models.Assessments;
using TMS.Repository.Managers;
using TMS.Repository;

namespace TMS.Web.Controllers
{
    public class FacultyExamController : BaseController
    {
        private readonly IAssessmentManager _assessmentManager;
        private readonly ApplicationDBContext _context; // Direct DB query for lightweight lists

        public FacultyExamController(IAssessmentManager assessmentManager, ApplicationDBContext context)
        {
            _assessmentManager = assessmentManager;
            _context = context;
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
            if (attempt == null || attempt.Exam!.FacultyId != GetUserId())
            {
                return Unauthorized();
            }

            return View(attempt);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveEvaluation(int attemptId, decimal totalMarksAwarded, string? feedback, Dictionary<int, decimal> questionMarks)
        {
            var success = await _assessmentManager.SaveFacultyEvaluationAsync(attemptId, GetUserId(), feedback, totalMarksAwarded, questionMarks);
            if (success)
            {
                SetApplicationResult(true, "Evaluation saved and results published successfully.");
                return RedirectToAction(nameof(EvaluateList));
            }

            SetApplicationResult(false, "Failed to save evaluation.");
            return RedirectToAction(nameof(EvaluateAttempt), new { attemptId });
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
    }
}
