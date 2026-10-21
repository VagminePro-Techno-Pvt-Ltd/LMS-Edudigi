using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TMS.Models.Academics;
using TMS.Models.Assessments;
using TMS.Repository;
using TMS.Repository.Managers;
using TMS.ViewModels.Academics;
using TMS.ViewModels.Masters;
using TMS.Web.Controllers;
using TMS.Web.Services;

namespace TMS.Web.Areas.Admin.Controllers
{
    [ValidateFormAccess(Common.FormDefination.Quiz)]
    [Area("Admin")]
    public class QuizController : BaseController
    {
        private readonly IMasterBaseManager<QuizMasterViewModel> _quizManager;
        private readonly IMasterManager<CourseMasterViewModel> _courseManager;
        private readonly IMasterBaseManager<CourseQuadrantViewModel> _quadrantManager;
        private readonly IMasterBaseManager<CourseFacultyMapViewModel> _courseFacultyMapManager;
        private readonly ApplicationDBContext _db;
        private readonly ISuggestedQuestionService _suggestedQuestionService;
        private readonly ILogger<QuizController> _logger;

        public QuizController(
            IMasterBaseManager<QuizMasterViewModel> quizManager,
            IMasterManager<CourseMasterViewModel> courseManager,
            IMasterBaseManager<CourseQuadrantViewModel> quadrantManager,
            IMasterBaseManager<CourseFacultyMapViewModel> courseFacultyMapManager,
            ApplicationDBContext db,
            ISuggestedQuestionService suggestedQuestionService,
            ILogger<QuizController> logger)
        {
            _quizManager = quizManager;
            _courseManager = courseManager;
            _quadrantManager = quadrantManager;
            _courseFacultyMapManager = courseFacultyMapManager;
            _db = db;
            _suggestedQuestionService = suggestedQuestionService;
            _logger = logger;
        }

        // =========================================================================
        // --- AUTHORIZATION HELPERS ---
        // =========================================================================

        private async Task<bool> CanAccessCourseAsync(int courseId)
        {
            if (courseId <= 0) return false;
            if (!IsFacultyRole()) return true; // Admin / Exam Coordinator / Management can access all

            int userId = GetUserId();
            return await _db.CourseFacultyMaps.AsNoTracking()
                .AnyAsync(m => m.CourseId == courseId && m.FacultyId == userId && m.IsActive);
        }

        private async Task<List<int>> GetAuthorizedCourseIdsAsync()
        {
            int userId = GetUserId();
            if (!IsFacultyRole())
            {
                return await _db.CourseMasters.AsNoTracking()
                    .Where(c => c.IsActive)
                    .Select(c => c.Id)
                    .ToListAsync();
            }

            return await _db.CourseFacultyMaps.AsNoTracking()
                .Where(m => m.FacultyId == userId && m.IsActive)
                .Select(m => m.CourseId)
                .Distinct()
                .ToListAsync();
        }

        // =========================================================================
        // --- MAIN VIEW ---
        // =========================================================================

        // GET: Admin/Quiz
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Quiz Management";

            int userId = GetUserId();
            bool isFaculty = IsFacultyRole();
            var authCourseIds = await GetAuthorizedCourseIdsAsync();

            IEnumerable<QuizMasterViewModel> quizzes;

            if (isFaculty)
            {
                if (authCourseIds.Any())
                {
                    var allQuizzes = await _quizManager.GetAsync(new[] { "Course", "CourseQuadrant", "Questions" });
                    quizzes = allQuizzes.Where(x => authCourseIds.Contains(x.CourseId)).ToList();
                }
                else
                {
                    quizzes = new List<QuizMasterViewModel>();
                }
            }
            else
            {
                quizzes = await _quizManager.GetAsync(new[] { "Course", "CourseQuadrant", "Questions" });
            }

            // Stats for Question Bank and Papers
            int totalSavedQuestions = await _db.QuestionBanks.AsNoTracking()
                .CountAsync(q => q.IsActive && authCourseIds.Contains(q.CourseId));

            int totalQuestionPapers = await _db.QuestionPapers.AsNoTracking()
                .CountAsync(p => p.IsActive && authCourseIds.Contains(p.CourseId));

            ViewBag.IsFaculty = isFaculty;
            ViewBag.TotalSavedQuestions = totalSavedQuestions;
            ViewBag.TotalQuestionPapers = totalQuestionPapers;
            ViewBag.AuthorizedCourseIds = authCourseIds;

            return View(quizzes);
        }

        // =========================================================================
        // --- DROPDOWN CASCADE ENDPOINTS ---
        // =========================================================================

        [HttpGet]
        public async Task<IActionResult> GetFacultyCourses()
        {
            var authCourseIds = await GetAuthorizedCourseIdsAsync();
            var courses = await _db.CourseMasters.AsNoTracking()
                .Where(c => authCourseIds.Contains(c.Id) && c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    code = c.CourseCode ?? "",
                    semesterId = c.SemesterId
                })
                .ToListAsync();

            return Json(courses);
        }

        [HttpGet]
        public async Task<IActionResult> GetCourseSemesters(int courseId)
        {
            if (courseId > 0 && !await CanAccessCourseAsync(courseId))
                return Json(new List<object>());

            var course = courseId > 0
                ? await _db.CourseMasters.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId)
                : null;

            var semesters = await _db.SemesterMasters.AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.Id)
                .Select(s => new
                {
                    id = s.Id,
                    name = s.Name,
                    isCourseDefault = (course != null && course.SemesterId == s.Id)
                })
                .ToListAsync();

            return Json(semesters);
        }

        [HttpGet]
        public async Task<IActionResult> GetSubjects(int? semesterId, int? courseId)
        {
            if (courseId.HasValue && courseId.Value > 0 && !await CanAccessCourseAsync(courseId.Value))
                return Json(new List<object>());

            var query = _db.SubjectMasters.AsNoTracking().Where(s => s.IsActive);

            if (semesterId.HasValue && semesterId.Value > 0)
            {
                query = query.Where(s => s.SemesterId == semesterId.Value);
            }
            else if (courseId.HasValue && courseId.Value > 0)
            {
                var course = await _db.CourseMasters.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId.Value);
                if (course?.SemesterId.HasValue == true)
                {
                    query = query.Where(s => s.SemesterId == course.SemesterId.Value);
                }
            }

            var subjects = await query
                .OrderBy(s => s.SortOrder).ThenBy(s => s.Name)
                .Select(s => new
                {
                    id = s.Id,
                    name = s.Name,
                    code = s.SubjectCode ?? "",
                    semesterId = s.SemesterId
                })
                .ToListAsync();

            return Json(subjects);
        }

        [HttpGet]
        public async Task<IActionResult> GetSubjectUnits(int subjectId)
        {
            if (subjectId <= 0) return Json(new List<object>());
            var units = await _db.UnitMasters.AsNoTracking()
                .Where(u => u.SubjectId == subjectId && u.IsActive)
                .OrderBy(u => u.SortOrder)
                .Select(u => new
                {
                    id = u.Id,
                    name = u.Name,
                    description = u.Description ?? ""
                })
                .ToListAsync();
            return Json(units);
        }

        [HttpGet]
        public async Task<IActionResult> GetUnitTopics(int unitId)
        {
            if (unitId <= 0) return Json(new List<object>());
            var topics = await _db.TopicMasters.AsNoTracking()
                .Where(t => t.UnitId == unitId && t.IsActive)
                .OrderBy(t => t.SortOrder)
                .Select(t => new
                {
                    id = t.Id,
                    name = t.Name
                })
                .ToListAsync();
            return Json(topics);
        }

        // =========================================================================
        // --- TAB 1: QUESTION BANK ENDPOINTS ---
        // =========================================================================

        [HttpGet]
        public async Task<IActionResult> GetQuestions(
            int? courseId,
            int? semesterId,
            int? subjectId,
            int? unitId,
            int? topicId,
            string? questionType,
            string? difficulty,
            string? learningOutcome,
            decimal? minMarks,
            decimal? maxMarks,
            string? search,
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                var authCourseIds = await GetAuthorizedCourseIdsAsync();
                if (courseId.HasValue && courseId.Value > 0)
                {
                    if (!authCourseIds.Contains(courseId.Value))
                        return Json(new { success = false, message = "Access denied for this course." });
                    authCourseIds = new List<int> { courseId.Value };
                }

                var query = _db.QuestionBanks.AsNoTracking()
                    .Include(q => q.Course)
                    .Include(q => q.Semester)
                    .Include(q => q.Subject)
                    .Include(q => q.Unit)
                    .Include(q => q.Topic)
                    .Include(q => q.Options)
                    .Where(q => q.IsActive && authCourseIds.Contains(q.CourseId));

                if (semesterId.HasValue && semesterId.Value > 0)
                    query = query.Where(q => q.SemesterId == semesterId.Value);

                if (subjectId.HasValue && subjectId.Value > 0)
                    query = query.Where(q => q.SubjectId == subjectId.Value);

                if (unitId.HasValue && unitId.Value > 0)
                    query = query.Where(q => q.UnitId == unitId.Value);

                if (topicId.HasValue && topicId.Value > 0)
                    query = query.Where(q => q.TopicId == topicId.Value);

                if (!string.IsNullOrWhiteSpace(questionType) && questionType != "All")
                    query = query.Where(q => q.QuestionType == questionType);

                if (!string.IsNullOrWhiteSpace(difficulty) && difficulty != "All")
                    query = query.Where(q => q.DifficultyLevel == difficulty);

                if (!string.IsNullOrWhiteSpace(learningOutcome) && learningOutcome != "All")
                {
                    var loTerm = learningOutcome.Trim().ToLower();
                    query = query.Where(q => q.LearningOutcome != null && q.LearningOutcome.ToLower().Contains(loTerm));
                }

                if (minMarks.HasValue && minMarks.Value > 0)
                    query = query.Where(q => q.Marks >= minMarks.Value);

                if (maxMarks.HasValue && maxMarks.Value > 0)
                    query = query.Where(q => q.Marks <= maxMarks.Value);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var sTerm = search.Trim().ToLower();
                    query = query.Where(q => q.QuestionText.ToLower().Contains(sTerm) || 
                                             (q.Explanation != null && q.Explanation.ToLower().Contains(sTerm)) ||
                                             (q.LearningOutcome != null && q.LearningOutcome.ToLower().Contains(sTerm)));
                }

                int totalCount = await query.CountAsync();
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100;
                int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var list = await query
                    .OrderByDescending(q => q.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var result = list.Select(q => new QuestionBankItemViewModel
                {
                    Id = q.Id,
                    CourseId = q.CourseId,
                    CourseName = q.Course?.Name ?? "",
                    CourseCode = q.Course?.CourseCode ?? "",
                    SemesterId = q.SemesterId,
                    SemesterName = q.Semester?.Name ?? "",
                    SubjectId = q.SubjectId,
                    SubjectName = q.Subject?.Name ?? "",
                    SubjectCode = q.Subject?.SubjectCode ?? "",
                    UnitId = q.UnitId,
                    UnitName = q.Unit?.Name,
                    TopicId = q.TopicId,
                    TopicName = q.Topic?.Name,
                    LearningOutcome = q.LearningOutcome,
                    QuestionType = q.QuestionType,
                    DifficultyLevel = q.DifficultyLevel,
                    QuestionText = q.QuestionText,
                    Marks = q.Marks,
                    NegativeMarks = q.NegativeMarks,
                    CorrectAnswer = q.CorrectAnswer,
                    Explanation = q.Explanation,
                    Status = q.Status ?? "Active",
                    CreatedOn = q.CreatedOn,
                    UpdatedOn = q.UpdatedOn,
                    FacultyId = q.FacultyId,
                    Options = q.Options.Select(o => new QuestionOptionDto
                    {
                        Id = o.Id,
                        OptionText = o.OptionText,
                        IsCorrect = o.IsCorrect
                    }).ToList()
                }).ToList();

                return Json(new
                {
                    success = true,
                    totalCount,
                    page,
                    pageSize,
                    totalPages,
                    questions = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in QuizController.GetQuestions");
                return Json(new { success = false, message = "Unable to load question bank data: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckDuplicateQuestion([FromBody] DuplicateCheckRequestDto req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.QuestionText) || req.CourseId <= 0)
                return Json(new DuplicateCheckResultDto { IsDuplicate = false, Message = "Incomplete parameters." });

            var normText = GeminiSuggestedQuestionService.ComputeNormalizedText(req.QuestionText);
            var query = _db.QuestionBanks.AsNoTracking()
                .Where(q => q.CourseId == req.CourseId && q.IsActive);

            if (req.SubjectId > 0)
                query = query.Where(q => q.SubjectId == req.SubjectId);

            if (req.ExcludeQuestionId.HasValue && req.ExcludeQuestionId.Value > 0)
                query = query.Where(q => q.Id != req.ExcludeQuestionId.Value);

            var existingQuestions = await query
                .Select(q => new { q.Id, q.QuestionText, q.NormalizedText })
                .ToListAsync();

            // 1. Exact match
            var exactMatch = existingQuestions.FirstOrDefault(q => q.QuestionText.Trim().Equals(req.QuestionText.Trim(), StringComparison.OrdinalIgnoreCase));
            if (exactMatch != null)
            {
                return Json(new DuplicateCheckResultDto
                {
                    IsDuplicate = true,
                    DuplicateType = "Exact",
                    ExistingQuestionId = exactMatch.Id,
                    ExistingQuestionText = exactMatch.QuestionText,
                    SimilarityScore = 1.0,
                    Message = "An identical question already exists in this question bank."
                });
            }

            // 2. Normalized text match
            var normMatch = existingQuestions.FirstOrDefault(q => 
                (!string.IsNullOrEmpty(q.NormalizedText) && q.NormalizedText == normText) ||
                GeminiSuggestedQuestionService.ComputeNormalizedText(q.QuestionText) == normText);

            if (normMatch != null)
            {
                return Json(new DuplicateCheckResultDto
                {
                    IsDuplicate = true,
                    DuplicateType = "Normalized",
                    ExistingQuestionId = normMatch.Id,
                    ExistingQuestionText = normMatch.QuestionText,
                    SimilarityScore = 0.95,
                    Message = "A question with matching normalized phrasing already exists."
                });
            }

            // 3. Similar / Token overlap check
            double highestScore = 0.0;
            int? similarId = null;
            string? similarText = null;

            foreach (var eq in existingQuestions)
            {
                double score = GeminiSuggestedQuestionService.CalculateSimilarity(req.QuestionText, eq.QuestionText);
                if (score > highestScore)
                {
                    highestScore = score;
                    similarId = eq.Id;
                    similarText = eq.QuestionText;
                }
            }

            if (highestScore >= 0.70 && similarId.HasValue)
            {
                return Json(new DuplicateCheckResultDto
                {
                    IsDuplicate = true,
                    DuplicateType = "Similar",
                    ExistingQuestionId = similarId,
                    ExistingQuestionText = similarText,
                    SimilarityScore = Math.Round(highestScore, 2),
                    Message = $"Potentially similar question detected ({Math.Round(highestScore * 100)}% match)."
                });
            }

            return Json(new DuplicateCheckResultDto
            {
                IsDuplicate = false,
                DuplicateType = "None",
                SimilarityScore = Math.Round(highestScore, 2),
                Message = "No duplicate detected. Question is unique."
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetQuestionBankAnalytics(int? courseId, int? subjectId)
        {
            var authCourseIds = await GetAuthorizedCourseIdsAsync();
            if (courseId.HasValue && courseId.Value > 0)
            {
                if (!authCourseIds.Contains(courseId.Value))
                    return Json(new { success = false, message = "Access denied." });
                authCourseIds = new List<int> { courseId.Value };
            }

            var query = _db.QuestionBanks.AsNoTracking()
                .Include(q => q.Subject)
                .Include(q => q.Unit)
                .Where(q => q.IsActive && authCourseIds.Contains(q.CourseId));

            if (subjectId.HasValue && subjectId.Value > 0)
                query = query.Where(q => q.SubjectId == subjectId.Value);

            var items = await query.Select(q => new
            {
                q.Id,
                SubjectName = q.Subject != null ? q.Subject.Name : "General",
                Difficulty = q.DifficultyLevel ?? "Medium",
                UnitName = q.Unit != null ? q.Unit.Name : "Unassigned Unit",
                Type = q.QuestionType ?? "MCQ",
                LearningOutcome = string.IsNullOrWhiteSpace(q.LearningOutcome) ? "General / Unassigned" : q.LearningOutcome
            }).ToListAsync();

            var vm = new QuestionBankAnalyticsViewModel
            {
                TotalQuestions = items.Count,
                QuestionsBySubject = items.GroupBy(x => x.SubjectName).ToDictionary(g => g.Key, g => g.Count()),
                QuestionsByDifficulty = items.GroupBy(x => x.Difficulty).ToDictionary(g => g.Key, g => g.Count()),
                QuestionsByUnit = items.GroupBy(x => x.UnitName).ToDictionary(g => g.Key, g => g.Count()),
                QuestionsByType = items.GroupBy(x => x.Type).ToDictionary(g => g.Key, g => g.Count()),
                QuestionsByLearningOutcome = items.GroupBy(x => x.LearningOutcome).ToDictionary(g => g.Key, g => g.Count())
            };

            return Json(new { success = true, analytics = vm });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuestion([FromBody] QuestionBankItemViewModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.QuestionText) || model.CourseId <= 0)
                return Json(new { success = false, message = "Question text and Course are required." });

            if (!await CanAccessCourseAsync(model.CourseId))
                return Json(new { success = false, message = "Unauthorized course access." });

            var normText = GeminiSuggestedQuestionService.ComputeNormalizedText(model.QuestionText);
            bool isDuplicate = await _db.QuestionBanks.AnyAsync(x =>
                x.CourseId == model.CourseId &&
                x.SubjectId == model.SubjectId &&
                x.IsActive &&
                x.NormalizedText == normText);

            if (isDuplicate)
                return Json(new { success = false, message = "A question with identical wording already exists for this course and subject." });

            int facultyId = GetUserId();
            var entity = new QuestionBank
            {
                CourseId = model.CourseId,
                SemesterId = model.SemesterId,
                SubjectId = model.SubjectId > 0 ? model.SubjectId : null,
                UnitId = model.UnitId > 0 ? model.UnitId : null,
                TopicId = model.TopicId > 0 ? model.TopicId : null,
                LearningOutcome = string.IsNullOrWhiteSpace(model.LearningOutcome) ? null : model.LearningOutcome.Trim(),
                NormalizedText = normText,
                FacultyId = facultyId,
                QuestionType = model.QuestionType ?? "MCQ",
                DifficultyLevel = model.DifficultyLevel ?? "Medium",
                QuestionText = model.QuestionText.Trim(),
                Marks = model.Marks > 0 ? model.Marks : 1,
                NegativeMarks = model.NegativeMarks,
                CorrectAnswer = model.CorrectAnswer,
                Explanation = model.Explanation,
                Status = "Active",
                IsActive = true,
                CreatedBy = facultyId,
                CreatedOn = DateTime.UtcNow
            };

            await _db.QuestionBanks.AddAsync(entity);
            await _db.SaveChangesAsync();

            if (model.QuestionType == "MCQ" && model.Options != null && model.Options.Any())
            {
                var optEntities = model.Options
                    .Where(o => !string.IsNullOrWhiteSpace(o.OptionText))
                    .Select(o => new QuestionOption
                    {
                        QuestionId = entity.Id,
                        OptionText = o.OptionText.Trim(),
                        IsCorrect = o.IsCorrect
                    }).ToList();

                if (optEntities.Any())
                {
                    await _db.QuestionOptions.AddRangeAsync(optEntities);
                    await _db.SaveChangesAsync();
                }
            }

            return Json(new { success = true, id = entity.Id, message = "Question saved to Question Bank successfully." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuestion([FromBody] QuestionBankItemViewModel model)
        {
            if (model == null || model.Id <= 0 || string.IsNullOrWhiteSpace(model.QuestionText))
                return Json(new { success = false, message = "Invalid question update request." });

            var entity = await _db.QuestionBanks
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == model.Id && q.IsActive);

            if (entity == null)
                return Json(new { success = false, message = "Question not found." });

            if (!await CanAccessCourseAsync(entity.CourseId))
                return Json(new { success = false, message = "Unauthorized access to this question." });

            var normText = GeminiSuggestedQuestionService.ComputeNormalizedText(model.QuestionText);
            entity.QuestionText = model.QuestionText.Trim();
            entity.NormalizedText = normText;
            entity.DifficultyLevel = model.DifficultyLevel ?? entity.DifficultyLevel;
            entity.QuestionType = model.QuestionType ?? entity.QuestionType;
            entity.Marks = model.Marks > 0 ? model.Marks : entity.Marks;
            entity.NegativeMarks = model.NegativeMarks;
            entity.CorrectAnswer = model.CorrectAnswer;
            entity.Explanation = model.Explanation;
            if (model.SemesterId.HasValue && model.SemesterId.Value > 0) entity.SemesterId = model.SemesterId;
            if (model.SubjectId.HasValue && model.SubjectId.Value > 0) entity.SubjectId = model.SubjectId;
            if (model.UnitId.HasValue && model.UnitId.Value > 0) entity.UnitId = model.UnitId;
            if (model.TopicId.HasValue && model.TopicId.Value > 0) entity.TopicId = model.TopicId;
            if (!string.IsNullOrWhiteSpace(model.LearningOutcome)) entity.LearningOutcome = model.LearningOutcome.Trim();
            entity.UpdatedBy = GetUserId();
            entity.UpdatedOn = DateTime.UtcNow;

            if (entity.QuestionType == "MCQ" && model.Options != null && model.Options.Any())
            {
                _db.QuestionOptions.RemoveRange(entity.Options);
                var newOptions = model.Options
                    .Where(o => !string.IsNullOrWhiteSpace(o.OptionText))
                    .Select(o => new QuestionOption
                    {
                        QuestionId = entity.Id,
                        OptionText = o.OptionText.Trim(),
                        IsCorrect = o.IsCorrect
                    }).ToList();

                await _db.QuestionOptions.AddRangeAsync(newOptions);
            }

            await _db.SaveChangesAsync();
            return Json(new { success = true, message = "Question updated successfully." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuestion([FromBody] QuestionIdDto? dto, [FromForm] int? formId)
        {
            int qId = dto?.Id ?? formId ?? 0;
            if (qId <= 0)
                return Json(new { success = false, message = "Question ID is required." });

            var entity = await _db.QuestionBanks.FirstOrDefaultAsync(q => q.Id == qId);
            if (entity == null)
                return Json(new { success = false, message = "Question not found." });

            if (!await CanAccessCourseAsync(entity.CourseId))
                return Json(new { success = false, message = "Unauthorized access." });

            entity.IsActive = false;
            entity.Status = "Archived";
            entity.UpdatedBy = GetUserId();
            entity.UpdatedOn = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Json(new { success = true, message = "Question removed from Question Bank successfully." });
        }

        // =========================================================================
        // --- TAB 2: SUGGESTED QUESTIONS ENDPOINTS ---
        // =========================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateSuggestedQuestions([FromBody] SuggestedQuestionRequestViewModel model)
        {
            if (model == null || model.CourseId <= 0 || model.SubjectId <= 0)
                return Json(new { success = false, message = "Please select both a Course and a Subject." });

            if (!await CanAccessCourseAsync(model.CourseId))
                return Json(new { success = false, message = "You are not authorized to access the selected course." });

            int facultyId = GetUserId();
            var res = await _suggestedQuestionService.GenerateSuggestedQuestionsAsync(model, facultyId);

            if (!res.Success)
            {
                return Json(new
                {
                    success = false,
                    requiresTopic = res.RequiresTopicContext,
                    message = res.ErrorMessage ?? "Failed to generate suggested questions."
                });
            }

            return Json(new { success = true, questions = res.Questions });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSuggestedQuestions([FromBody] SaveSuggestedQuestionsRequestViewModel model)
        {
            if (model == null || model.CourseId <= 0 || model.Questions == null || !model.Questions.Any())
                return Json(new { success = false, message = "No questions provided to save." });

            if (!await CanAccessCourseAsync(model.CourseId))
                return Json(new { success = false, message = "You are not authorized for this course." });

            int facultyId = GetUserId();
            int savedCount = 0;
            int duplicateCount = 0;
            var savedQuestionIds = new List<int>();

            foreach (var q in model.Questions)
            {
                if (string.IsNullOrWhiteSpace(q.QuestionText)) continue;

                var normText = GeminiSuggestedQuestionService.ComputeNormalizedText(q.QuestionText);
                bool isDuplicate = await _db.QuestionBanks.AnyAsync(x =>
                    x.CourseId == model.CourseId &&
                    x.SubjectId == model.SubjectId &&
                    x.IsActive &&
                    x.NormalizedText == normText);

                if (isDuplicate)
                {
                    duplicateCount++;
                    continue;
                }

                int? uId = q.UnitId.HasValue && q.UnitId.Value > 0 ? q.UnitId : (model.UnitId.HasValue && model.UnitId.Value > 0 ? model.UnitId : null);
                int? tId = q.TopicId.HasValue && q.TopicId.Value > 0 ? q.TopicId : (model.TopicId.HasValue && model.TopicId.Value > 0 ? model.TopicId : null);
                string? lo = !string.IsNullOrWhiteSpace(q.LearningOutcome) ? q.LearningOutcome.Trim() : (!string.IsNullOrWhiteSpace(model.LearningOutcome) ? model.LearningOutcome.Trim() : null);

                var entity = new QuestionBank
                {
                    CourseId = model.CourseId,
                    SemesterId = model.SemesterId,
                    SubjectId = model.SubjectId > 0 ? model.SubjectId : null,
                    UnitId = uId,
                    TopicId = tId,
                    LearningOutcome = lo,
                    NormalizedText = normText,
                    FacultyId = facultyId,
                    QuestionType = q.QuestionType ?? "MCQ",
                    DifficultyLevel = q.DifficultyLevel ?? "Medium",
                    QuestionText = q.QuestionText.Trim(),
                    Marks = q.Marks > 0 ? q.Marks : 1,
                    NegativeMarks = 0,
                    CorrectAnswer = q.CorrectAnswer,
                    Explanation = q.Explanation,
                    Status = "Active",
                    IsActive = true,
                    CreatedBy = facultyId,
                    CreatedOn = DateTime.UtcNow
                };

                await _db.QuestionBanks.AddAsync(entity);
                await _db.SaveChangesAsync();

                if (q.QuestionType == "MCQ" && q.Options != null && q.Options.Any())
                {
                    var optEntities = q.Options.Select(optText => new QuestionOption
                    {
                        QuestionId = entity.Id,
                        OptionText = optText.Trim(),
                        IsCorrect = !string.IsNullOrWhiteSpace(q.CorrectAnswer) &&
                                    (optText.Trim().Equals(q.CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase) ||
                                     q.CorrectAnswer.Trim().StartsWith(optText.Trim(), StringComparison.OrdinalIgnoreCase))
                    }).ToList();

                    if (!optEntities.Any(o => o.IsCorrect) && optEntities.Any())
                    {
                        optEntities[0].IsCorrect = true;
                    }

                    await _db.QuestionOptions.AddRangeAsync(optEntities);
                    await _db.SaveChangesAsync();
                }

                savedQuestionIds.Add(entity.Id);
                savedCount++;
            }

            var msg = $"{savedCount} question(s) saved to Question Bank successfully.";
            if (duplicateCount > 0)
                msg += $" ({duplicateCount} duplicate(s) were detected and skipped).";

            return Json(new { success = true, savedCount, duplicateCount, savedIds = savedQuestionIds, message = msg });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegenerateQuestion([FromBody] RegenerateQuestionRequestDto model)
        {
            if (model == null || model.Question == null || model.Context == null)
                return Json(new { success = false, message = "Invalid regeneration request." });

            if (!await CanAccessCourseAsync(model.Context.CourseId))
                return Json(new { success = false, message = "Unauthorized course access." });

            int facultyId = GetUserId();
            var res = await _suggestedQuestionService.RegenerateSingleQuestionAsync(model.Question, model.Context, facultyId);

            if (!res.Success || !res.Questions.Any())
                return Json(new { success = false, message = res.ErrorMessage ?? "Regeneration failed." });

            return Json(new { success = true, question = res.Questions.First() });
        }

        // =========================================================================
        // --- TAB 3: CREATE QUESTION PAPER ENDPOINTS ---
        // =========================================================================

        [HttpGet]
        public async Task<IActionResult> GetQuestionPapers(int? courseId)
        {
            var authCourseIds = await GetAuthorizedCourseIdsAsync();
            if (courseId.HasValue && courseId.Value > 0)
            {
                if (!authCourseIds.Contains(courseId.Value))
                    return Json(new { success = false, message = "Unauthorized course access." });
                authCourseIds = new List<int> { courseId.Value };
            }

            var papers = await _db.QuestionPapers.AsNoTracking()
                .Include(p => p.Course)
                .Include(p => p.Semester)
                .Include(p => p.Subject)
                .Include(p => p.Questions)
                .Where(p => p.IsActive && authCourseIds.Contains(p.CourseId))
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            var list = papers.Select(p => new QuestionPaperListViewModel
            {
                Id = p.Id,
                Title = p.Title,
                CourseId = p.CourseId,
                CourseName = p.Course?.Name ?? "",
                CourseCode = p.Course?.CourseCode ?? "",
                SemesterId = p.SemesterId,
                SemesterName = p.Semester?.Name ?? "",
                SubjectId = p.SubjectId,
                SubjectName = p.Subject?.Name ?? "",
                AssessmentType = p.AssessmentType,
                TotalMarks = p.TotalMarks,
                QuestionCount = p.Questions.Count(q => q.IsActive),
                TotalAssignedMarks = p.Questions.Where(q => q.IsActive).Sum(q => q.Marks),
                Status = p.Status,
                CreatedOn = p.CreatedOn,
                PublishedOn = p.PublishedOn
            }).ToList();

            return Json(new { success = true, papers = list });
        }

        [HttpGet]
        public async Task<IActionResult> GetQuestionPaperDetails(int id)
        {
            if (id <= 0) return Json(new { success = false, message = "Invalid paper ID." });

            var paper = await _db.QuestionPapers.AsNoTracking()
                .Include(p => p.Course)
                .Include(p => p.Semester)
                .Include(p => p.Subject)
                .Include(p => p.Faculty)
                .Include(p => p.Questions)
                    .ThenInclude(q => q.QuestionBank)
                        .ThenInclude(qb => qb!.Options)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (paper == null)
                return Json(new { success = false, message = "Question paper not found." });

            if (!await CanAccessCourseAsync(paper.CourseId))
                return Json(new { success = false, message = "Unauthorized course access." });

            var detail = new QuestionPaperDetailViewModel
            {
                Id = paper.Id,
                Title = paper.Title,
                CourseId = paper.CourseId,
                CourseName = paper.Course?.Name ?? "",
                CourseCode = paper.Course?.CourseCode ?? "",
                SemesterId = paper.SemesterId,
                SemesterName = paper.Semester?.Name ?? "",
                SubjectId = paper.SubjectId,
                SubjectName = paper.Subject?.Name ?? "",
                AssessmentType = paper.AssessmentType,
                TotalMarks = paper.TotalMarks,
                DurationMinutes = paper.DurationMinutes ?? 180,
                Instructions = paper.Instructions,
                Status = paper.Status,
                CreatedOn = paper.CreatedOn,
                PublishedOn = paper.PublishedOn,
                FacultyName = paper.Faculty?.Name ?? "Faculty",
                Questions = paper.Questions
                    .Where(q => q.IsActive && q.QuestionBank != null)
                    .OrderBy(q => q.DisplayOrder)
                    .Select(q => new QuestionPaperQuestionItemViewModel
                    {
                        QuestionBankId = q.QuestionBankId,
                        SectionName = string.IsNullOrWhiteSpace(q.SectionName) ? "Section A" : q.SectionName,
                        SectionInstructions = q.SectionInstructions,
                        DisplayOrder = q.DisplayOrder,
                        Marks = q.Marks,
                        QuestionText = q.QuestionBank!.QuestionText,
                        QuestionType = q.QuestionBank.QuestionType,
                        DifficultyLevel = q.QuestionBank.DifficultyLevel,
                        CorrectAnswer = q.QuestionBank.CorrectAnswer,
                        Explanation = q.QuestionBank.Explanation,
                        Options = q.QuestionBank.Options.Select(o => new QuestionOptionDto
                        {
                            Id = o.Id,
                            OptionText = o.OptionText,
                            IsCorrect = o.IsCorrect
                        }).ToList()
                    }).ToList()
            };

            return Json(new { success = true, paper = detail });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuestionPaper([FromBody] QuestionPaperCreateViewModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Title) || model.CourseId <= 0 || model.TotalMarks <= 0)
                return Json(new { success = false, message = "Title, Course, and Total Marks are required." });

            if (!await CanAccessCourseAsync(model.CourseId))
                return Json(new { success = false, message = "Unauthorized course access." });

            if (model.Questions == null || !model.Questions.Any())
                return Json(new { success = false, message = "Please select or add at least one question to the paper." });

            decimal assignedMarks = model.Questions.Sum(q => q.Marks);
            bool isPublishing = model.Status.Equals("Published", StringComparison.OrdinalIgnoreCase);

            if (isPublishing && Math.Abs(assignedMarks - model.TotalMarks) > 0.01m)
            {
                return Json(new
                {
                    success = false,
                    message = $"Cannot publish question paper: Total assigned question marks ({assignedMarks}) does not match configured total marks ({model.TotalMarks}). Please adjust individual question marks before publishing."
                });
            }

            int facultyId = GetUserId();
            var paper = new QuestionPaper
            {
                CourseId = model.CourseId,
                SemesterId = model.SemesterId,
                SubjectId = model.SubjectId > 0 ? model.SubjectId : null,
                Title = model.Title.Trim(),
                AssessmentType = model.AssessmentType ?? "Internal Assessment",
                TotalMarks = model.TotalMarks,
                DurationMinutes = model.DurationMinutes ?? 180,
                Instructions = model.Instructions,
                Status = isPublishing ? "Published" : "Draft",
                FacultyId = facultyId,
                PublishedOn = isPublishing ? DateTime.UtcNow : null,
                IsActive = true,
                CreatedBy = facultyId,
                CreatedOn = DateTime.UtcNow
            };

            await _db.QuestionPapers.AddAsync(paper);
            await _db.SaveChangesAsync();

            int order = 1;
            foreach (var q in model.Questions)
            {
                var qpItem = new QuestionPaperQuestion
                {
                    QuestionPaperId = paper.Id,
                    QuestionBankId = q.QuestionBankId,
                    SectionName = string.IsNullOrWhiteSpace(q.SectionName) ? "Section A" : q.SectionName.Trim(),
                    SectionInstructions = q.SectionInstructions,
                    DisplayOrder = q.DisplayOrder > 0 ? q.DisplayOrder : order++,
                    Marks = q.Marks > 0 ? q.Marks : 1,
                    IsActive = true,
                    CreatedOn = DateTime.UtcNow
                };
                await _db.QuestionPaperQuestions.AddAsync(qpItem);
            }

            await _db.SaveChangesAsync();

            return Json(new
            {
                success = true,
                id = paper.Id,
                status = paper.Status,
                message = isPublishing
                    ? "Question paper published successfully."
                    : "Question paper saved as Draft successfully."
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuestionPaper([FromBody] QuestionPaperCreateViewModel model)
        {
            if (model == null || model.Id <= 0 || string.IsNullOrWhiteSpace(model.Title))
                return Json(new { success = false, message = "Invalid question paper update request." });

            var paper = await _db.QuestionPapers
                .Include(p => p.Questions)
                .FirstOrDefaultAsync(p => p.Id == model.Id && p.IsActive);

            if (paper == null)
                return Json(new { success = false, message = "Question paper not found." });

            if (!await CanAccessCourseAsync(paper.CourseId))
                return Json(new { success = false, message = "Unauthorized access." });

            decimal assignedMarks = model.Questions?.Sum(q => q.Marks) ?? 0;
            bool isPublishing = model.Status.Equals("Published", StringComparison.OrdinalIgnoreCase);

            if (isPublishing && Math.Abs(assignedMarks - model.TotalMarks) > 0.01m)
            {
                return Json(new
                {
                    success = false,
                    message = $"Cannot publish: Question marks total ({assignedMarks}) must match configured total marks ({model.TotalMarks})."
                });
            }

            paper.Title = model.Title.Trim();
            paper.AssessmentType = model.AssessmentType ?? paper.AssessmentType;
            paper.TotalMarks = model.TotalMarks;
            paper.DurationMinutes = model.DurationMinutes ?? paper.DurationMinutes;
            paper.Instructions = model.Instructions ?? paper.Instructions;
            paper.Status = isPublishing ? "Published" : (model.Status ?? paper.Status);
            if (isPublishing && !paper.PublishedOn.HasValue) paper.PublishedOn = DateTime.UtcNow;
            if (model.SemesterId.HasValue && model.SemesterId.Value > 0) paper.SemesterId = model.SemesterId;
            if (model.SubjectId > 0) paper.SubjectId = model.SubjectId;
            paper.UpdatedBy = GetUserId();
            paper.UpdatedOn = DateTime.UtcNow;

            _db.QuestionPaperQuestions.RemoveRange(paper.Questions);

            if (model.Questions != null)
            {
                int order = 1;
                foreach (var q in model.Questions)
                {
                    var qpItem = new QuestionPaperQuestion
                    {
                        QuestionPaperId = paper.Id,
                        QuestionBankId = q.QuestionBankId,
                        SectionName = string.IsNullOrWhiteSpace(q.SectionName) ? "Section A" : q.SectionName.Trim(),
                        SectionInstructions = q.SectionInstructions,
                        DisplayOrder = q.DisplayOrder > 0 ? q.DisplayOrder : order++,
                        Marks = q.Marks > 0 ? q.Marks : 1,
                        IsActive = true,
                        CreatedOn = DateTime.UtcNow
                    };
                    await _db.QuestionPaperQuestions.AddAsync(qpItem);
                }
            }

            await _db.SaveChangesAsync();

            return Json(new
            {
                success = true,
                id = paper.Id,
                status = paper.Status,
                message = isPublishing ? "Question paper published successfully." : "Question paper updated successfully."
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishQuestionPaper([FromBody] QuestionPaperPublishDto? dto, [FromForm] int? formId)
        {
            int paperId = dto?.Id ?? formId ?? 0;
            if (paperId <= 0) return Json(new { success = false, message = "Paper ID is required." });

            var paper = await _db.QuestionPapers
                .Include(p => p.Questions)
                .FirstOrDefaultAsync(p => p.Id == paperId && p.IsActive);

            if (paper == null) return Json(new { success = false, message = "Question paper not found." });

            if (!await CanAccessCourseAsync(paper.CourseId))
                return Json(new { success = false, message = "Unauthorized course access." });

            var activeQuestions = paper.Questions.Where(q => q.IsActive).ToList();
            if (!activeQuestions.Any())
                return Json(new { success = false, message = "Cannot publish an empty question paper. Please add questions first." });

            decimal assignedMarks = activeQuestions.Sum(q => q.Marks);
            if (Math.Abs(assignedMarks - paper.TotalMarks) > 0.01m)
            {
                return Json(new
                {
                    success = false,
                    message = $"Cannot publish: Total assigned question marks ({assignedMarks}) does not match configured total marks ({paper.TotalMarks}). Please adjust marks."
                });
            }

            paper.Status = "Published";
            paper.PublishedOn = DateTime.UtcNow;
            paper.UpdatedBy = GetUserId();
            paper.UpdatedOn = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Json(new { success = true, message = "Question paper published successfully." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuestionPaper([FromBody] QuestionPaperIdDto? dto, [FromForm] int? formId)
        {
            int paperId = dto?.Id ?? formId ?? 0;
            if (paperId <= 0) return Json(new { success = false, message = "Paper ID is required." });

            var paper = await _db.QuestionPapers.FirstOrDefaultAsync(p => p.Id == paperId);
            if (paper == null) return Json(new { success = false, message = "Question paper not found." });

            if (!await CanAccessCourseAsync(paper.CourseId))
                return Json(new { success = false, message = "Unauthorized access." });

            paper.IsActive = false;
            paper.UpdatedBy = GetUserId();
            paper.UpdatedOn = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Json(new { success = true, message = "Question paper deleted successfully." });
        }

        // =========================================================================
        // --- FEATURE 25: AI QUESTION PAPER BLUEPRINT & AUTO-ASSEMBLY ---
        // =========================================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AutoAssemblePaperFromBlueprint([FromBody] AutoAssemblePaperRequestDto request)
        {
            if (request == null || request.CourseId <= 0 || string.IsNullOrWhiteSpace(request.Title))
                return Json(new AutoAssemblePaperResultDto { Success = false, Message = "Course and Paper Title are required." });

            if (!await CanAccessCourseAsync(request.CourseId))
                return Json(new AutoAssemblePaperResultDto { Success = false, Message = "Unauthorized course access." });

            if (request.Sections == null || !request.Sections.Any())
                return Json(new AutoAssemblePaperResultDto { Success = false, Message = "At least one blueprint section is required (e.g. Section A)." });

            decimal totalCalculatedMarks = request.Sections.Sum(s => s.NumberOfQuestions * s.MarksPerQuestion);
            if (totalCalculatedMarks <= 0)
                return Json(new AutoAssemblePaperResultDto { Success = false, Message = "Configured blueprint must have total marks greater than 0." });

            var warnings = new List<string>();
            var usedQuestionIds = new HashSet<int>();
            var assembledQuestions = new List<(QuestionBank Question, string SectionName, string? SectionInstructions, decimal Marks)>();
            var aiGeneratedNeedingReview = new List<SuggestedQuestionItemViewModel>();

            int facultyId = GetUserId();

            foreach (var section in request.Sections)
            {
                if (section.NumberOfQuestions <= 0) continue;

                var query = _db.QuestionBanks
                    .Include(qb => qb.Options)
                    .Where(qb => qb.CourseId == request.CourseId &&
                                 qb.IsActive &&
                                 (qb.Status == null || qb.Status == "Active") &&
                                 !usedQuestionIds.Contains(qb.Id));

                if (request.SubjectId > 0)
                {
                    query = query.Where(qb => qb.SubjectId == request.SubjectId || qb.SubjectId == null);
                }

                if (!string.IsNullOrWhiteSpace(section.QuestionType) && !section.QuestionType.Equals("Any", StringComparison.OrdinalIgnoreCase))
                {
                    string secType = section.QuestionType.Trim().ToLower();
                    query = query.Where(qb => qb.QuestionType != null && qb.QuestionType.ToLower() == secType);
                }

                if (!string.IsNullOrWhiteSpace(section.DifficultyLevel) && !section.DifficultyLevel.Equals("Any", StringComparison.OrdinalIgnoreCase))
                {
                    string secDiff = section.DifficultyLevel.Trim().ToLower();
                    query = query.Where(qb => qb.DifficultyLevel == null || qb.DifficultyLevel == "" || qb.DifficultyLevel.ToLower() == secDiff);
                }

                if (section.UnitId.HasValue && section.UnitId.Value > 0)
                {
                    query = query.Where(qb => qb.UnitId == section.UnitId.Value);
                }

                if (section.TopicId.HasValue && section.TopicId.Value > 0)
                {
                    query = query.Where(qb => qb.TopicId == section.TopicId.Value);
                }

                if (!string.IsNullOrWhiteSpace(section.LearningOutcome) && !section.LearningOutcome.Equals("Any", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(qb => qb.LearningOutcome == null || qb.LearningOutcome == "" || qb.LearningOutcome == section.LearningOutcome);
                }

                var matchedQuestions = await query
                    .OrderBy(x => Guid.NewGuid())
                    .Take(section.NumberOfQuestions)
                    .ToListAsync();

                foreach (var q in matchedQuestions)
                {
                    usedQuestionIds.Add(q.Id);
                    assembledQuestions.Add((q, section.SectionName, section.SectionInstructions, section.MarksPerQuestion));
                }

                int shortfall = section.NumberOfQuestions - matchedQuestions.Count;
                if (shortfall > 0)
                {
                    if (request.AutoGenerateMissingWithAi)
                    {
                        var aiReq = new SuggestedQuestionRequestViewModel
                        {
                            CourseId = request.CourseId,
                            SemesterId = request.SemesterId,
                            SubjectId = request.SubjectId ?? 0,
                            UnitId = section.UnitId,
                            TopicId = section.TopicId,
                            LearningOutcome = section.LearningOutcome,
                            QuestionType = string.IsNullOrWhiteSpace(section.QuestionType) || section.QuestionType.Equals("Any", StringComparison.OrdinalIgnoreCase) ? "MCQ" : section.QuestionType,
                            DifficultyLevel = string.IsNullOrWhiteSpace(section.DifficultyLevel) || section.DifficultyLevel.Equals("Any", StringComparison.OrdinalIgnoreCase) ? "Medium" : section.DifficultyLevel,
                            QuestionCount = shortfall,
                            MarksPerQuestion = section.MarksPerQuestion
                        };

                        var aiResult = await _suggestedQuestionService.GenerateSuggestedQuestionsAsync(aiReq, facultyId);
                        if (aiResult.Success && aiResult.Questions.Any())
                        {
                            foreach (var gq in aiResult.Questions)
                            {
                                aiGeneratedNeedingReview.Add(gq);
                            }
                            warnings.Add($"Section '{section.SectionName}' had {matchedQuestions.Count} matching questions in the bank. {aiResult.Questions.Count} supplementary questions were generated using AI and require faculty review.");
                        }
                        else
                        {
                            warnings.Add($"Section '{section.SectionName}' has a shortfall of {shortfall} question(s). AI generation could not fulfill: {aiResult.ErrorMessage ?? "AI service unavailable"}");
                        }
                    }
                    else
                    {
                        warnings.Add($"Section '{section.SectionName}' requested {section.NumberOfQuestions} questions ({section.QuestionType}, {section.DifficultyLevel}), but only {matchedQuestions.Count} matching questions were found in the Question Bank.");
                    }
                }
            }

            if (!assembledQuestions.Any() && !aiGeneratedNeedingReview.Any())
            {
                return Json(new AutoAssemblePaperResultDto
                {
                    Success = false,
                    Message = "Unable to auto-assemble paper: No questions matching the specified blueprint criteria exist in the Question Bank.",
                    Warnings = warnings
                });
            }

            // Persist Question Paper
            var paper = new QuestionPaper
            {
                CourseId = request.CourseId,
                SemesterId = request.SemesterId,
                SubjectId = request.SubjectId,
                Title = request.Title.Trim(),
                AssessmentType = request.AssessmentType ?? "Internal Assessment",
                TotalMarks = totalCalculatedMarks,
                DurationMinutes = request.DurationMinutes ?? 180,
                Instructions = request.Instructions ?? "1. Read all questions carefully.\n2. Attempt questions according to section instructions.\n3. Verify your answers before submission.",
                Status = "Draft",
                FacultyId = facultyId,
                IsActive = true,
                CreatedBy = facultyId,
                CreatedOn = DateTime.UtcNow
            };

            await _db.QuestionPapers.AddAsync(paper);
            await _db.SaveChangesAsync();

            int displayOrder = 1;
            foreach (var item in assembledQuestions)
            {
                var qpQuestion = new QuestionPaperQuestion
                {
                    QuestionPaperId = paper.Id,
                    QuestionBankId = item.Question.Id,
                    SectionName = string.IsNullOrWhiteSpace(item.SectionName) ? "Section A" : item.SectionName.Trim(),
                    SectionInstructions = item.SectionInstructions,
                    DisplayOrder = displayOrder++,
                    Marks = item.Marks > 0 ? item.Marks : 1,
                    IsActive = true,
                    CreatedOn = DateTime.UtcNow
                };
                await _db.QuestionPaperQuestions.AddAsync(qpQuestion);
            }

            await _db.SaveChangesAsync();

            return Json(new AutoAssemblePaperResultDto
            {
                Success = true,
                Message = $"Question paper '{paper.Title}' auto-assembled with {assembledQuestions.Count} questions across {request.Sections.Count} section(s).",
                QuestionPaperId = paper.Id,
                TotalMarks = paper.TotalMarks,
                TotalQuestions = assembledQuestions.Count,
                ExistingQuestionsUsed = assembledQuestions.Count,
                AiGeneratedQuestionsCount = aiGeneratedNeedingReview.Count,
                Warnings = warnings,
                GeneratedQuestionsNeedingApproval = aiGeneratedNeedingReview
            });
        }

        [HttpGet]
        public async Task<IActionResult> ExportQuestionPaperPdf(int id)
        {
            if (id <= 0) return BadRequest("Invalid question paper ID.");

            var paper = await _db.QuestionPapers
                .Include(p => p.Course)
                .Include(p => p.Semester)
                .Include(p => p.Subject)
                .Include(p => p.Faculty)
                .Include(p => p.Questions)
                    .ThenInclude(q => q.QuestionBank)
                        .ThenInclude(qb => qb!.Options)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (paper == null) return NotFound("Question paper not found.");

            if (!await CanAccessCourseAsync(paper.CourseId))
                return Forbid();

            ViewBag.Sections = paper.Questions
                .Where(q => q.IsActive && q.QuestionBank != null)
                .GroupBy(q => string.IsNullOrWhiteSpace(q.SectionName) ? "Section A" : q.SectionName)
                .OrderBy(g => g.Key)
                .ToList();

            return View("QuestionPaperPrintPdf", paper);
        }

        // =========================================================================
        // --- FEATURE 26: QUESTION BANK INTELLIGENCE & CLASSIFICATION ---
        // =========================================================================

        [HttpGet]
        public async Task<IActionResult> GetLegacyUnclassifiedQuestions(int courseId, int? subjectId)
        {
            if (courseId <= 0) return Json(new { success = false, message = "Course ID is required." });
            if (!await CanAccessCourseAsync(courseId)) return Json(new { success = false, message = "Unauthorized access." });

            var query = _db.QuestionBanks.AsNoTracking()
                .Where(q => q.CourseId == courseId && q.IsActive &&
                           (q.UnitId == null || q.TopicId == null || string.IsNullOrEmpty(q.LearningOutcome) || string.IsNullOrEmpty(q.DifficultyLevel)));

            if (subjectId.HasValue && subjectId.Value > 0)
                query = query.Where(q => q.SubjectId == subjectId.Value);

            var unclassified = await query
                .OrderByDescending(q => q.Id)
                .Take(50)
                .Select(q => new LegacyQuestionToClassifyDto
                {
                    Id = q.Id,
                    QuestionText = q.QuestionText,
                    CurrentType = q.QuestionType,
                    Marks = q.Marks
                })
                .ToListAsync();

            return Json(new { success = true, questions = unclassified });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BatchClassifyQuestions([FromBody] BatchClassifyQuestionsRequestDto request)
        {
            if (request == null || request.CourseId <= 0 || request.SubjectId <= 0 || request.QuestionIds == null || !request.QuestionIds.Any())
                return Json(new { success = false, message = "Invalid classification request. Course, Subject, and Question IDs are required." });

            if (!await CanAccessCourseAsync(request.CourseId))
                return Json(new { success = false, message = "Unauthorized access." });

            var questions = await _db.QuestionBanks.AsNoTracking()
                .Where(q => request.QuestionIds.Contains(q.Id) && q.IsActive)
                .Select(q => new LegacyQuestionToClassifyDto
                {
                    Id = q.Id,
                    QuestionText = q.QuestionText,
                    CurrentType = q.QuestionType,
                    Marks = q.Marks
                })
                .ToListAsync();

            if (!questions.Any())
                return Json(new { success = false, message = "No valid questions found to classify." });

            var suggestions = await _suggestedQuestionService.ClassifyLegacyQuestionsAsync(request.CourseId, request.SubjectId, questions);

            return Json(new
            {
                success = true,
                message = $"Generated pedagogical classification suggestions for {suggestions.Count} questions.",
                suggestions = suggestions
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyQuestionClassifications([FromBody] List<QuestionClassificationApplyDto> items)
        {
            if (items == null || !items.Any())
                return Json(new { success = false, message = "No classification items provided." });

            var ids = items.Select(x => x.QuestionId).Distinct().ToList();
            var questions = await _db.QuestionBanks
                .Where(q => ids.Contains(q.Id) && q.IsActive)
                .ToListAsync();

            int updatedCount = 0;
            int userId = GetUserId();

            foreach (var item in items)
            {
                var q = questions.FirstOrDefault(x => x.Id == item.QuestionId);
                if (q != null)
                {
                    if (item.UnitId.HasValue && item.UnitId.Value > 0) q.UnitId = item.UnitId.Value;
                    if (item.TopicId.HasValue && item.TopicId.Value > 0) q.TopicId = item.TopicId.Value;
                    if (!string.IsNullOrWhiteSpace(item.DifficultyLevel)) q.DifficultyLevel = item.DifficultyLevel.Trim();
                    if (!string.IsNullOrWhiteSpace(item.LearningOutcome)) q.LearningOutcome = item.LearningOutcome.Trim();

                    q.UpdatedBy = userId;
                    q.UpdatedOn = DateTime.UtcNow;
                    updatedCount++;
                }
            }

            await _db.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"Successfully updated classifications for {updatedCount} question(s) after faculty review."
            });
        }

        [HttpGet]
        public IActionResult DownloadQuestionImportTemplate(string format = "csv")
        {
            var csv = new StringBuilder();
            csv.AppendLine("QuestionText,QuestionType,DifficultyLevel,Marks,NegativeMarks,LearningOutcome,UnitName,TopicName,OptionA,OptionB,OptionC,OptionD,CorrectOption,Explanation");
            csv.AppendLine("\"What is the time complexity of binary search?\",\"MCQ\",\"Medium\",2,0.5,\"Apply\",\"Unit 1: Data Structures\",\"Topic: Searching Algorithms\",\"O(n)\",\"O(log n)\",\"O(n^2)\",\"O(1)\",\"B\",\"Binary search halves the search space each step.\"");
            csv.AppendLine("\"Explain the principle of object-oriented encapsulation.\",\"Short Answer\",\"Medium\",5,0,\"Understand\",\"Unit 2: OOP Concepts\",\"Topic: Encapsulation\",\"\",\"\",\"\",\"\",\"Encapsulation wraps data and methods into a single unit.\",\"Full model answer should mention access specifiers.\"");

            byte[] bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", "QuestionBank_Import_Template.csv");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkImportQuestions(int courseId, int? semesterId, int? subjectId, IFormFile file)
        {
            if (courseId <= 0) return Json(new BulkQuestionImportResultDto { Success = false, Message = "Please select a valid Course." });
            if (!await CanAccessCourseAsync(courseId)) return Json(new BulkQuestionImportResultDto { Success = false, Message = "Unauthorized access." });
            if (file == null || file.Length == 0) return Json(new BulkQuestionImportResultDto { Success = false, Message = "Please select a CSV file to upload." });

            var result = new BulkQuestionImportResultDto();
            var rows = new List<BulkQuestionImportRowDto>();

            try
            {
                using var stream = file.OpenReadStream();
                using var reader = new System.IO.StreamReader(stream, Encoding.UTF8);
                string? headerLine = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(headerLine))
                {
                    return Json(new BulkQuestionImportResultDto { Success = false, Message = "The uploaded file is empty." });
                }

                int rowNum = 1;
                while (!reader.EndOfStream)
                {
                    rowNum++;
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = ParseCsvLine(line);
                    if (parts.Count < 4)
                    {
                        result.RowErrors.Add($"Row {rowNum}: Insufficient columns. Minimum required: QuestionText, QuestionType, DifficultyLevel, Marks.");
                        result.ErrorCount++;
                        continue;
                    }

                    var row = new BulkQuestionImportRowDto
                    {
                        RowNumber = rowNum,
                        QuestionText = parts.ElementAtOrDefault(0)?.Trim() ?? "",
                        QuestionType = parts.ElementAtOrDefault(1)?.Trim() ?? "MCQ",
                        DifficultyLevel = parts.ElementAtOrDefault(2)?.Trim() ?? "Medium",
                        Marks = decimal.TryParse(parts.ElementAtOrDefault(3), out var m) ? m : 1m,
                        NegativeMarks = decimal.TryParse(parts.ElementAtOrDefault(4), out var nm) ? nm : 0m,
                        LearningOutcome = parts.ElementAtOrDefault(5)?.Trim(),
                        UnitName = parts.ElementAtOrDefault(6)?.Trim(),
                        TopicName = parts.ElementAtOrDefault(7)?.Trim(),
                        OptionA = parts.ElementAtOrDefault(8)?.Trim(),
                        OptionB = parts.ElementAtOrDefault(9)?.Trim(),
                        OptionC = parts.ElementAtOrDefault(10)?.Trim(),
                        OptionD = parts.ElementAtOrDefault(11)?.Trim(),
                        CorrectOption = parts.ElementAtOrDefault(12)?.Trim(),
                        Explanation = parts.ElementAtOrDefault(13)?.Trim()
                    };

                    if (string.IsNullOrWhiteSpace(row.QuestionText))
                    {
                        row.IsValid = false;
                        row.ErrorMessage = "QuestionText cannot be empty.";
                    }
                    else if (row.Marks <= 0)
                    {
                        row.IsValid = false;
                        row.ErrorMessage = "Marks must be greater than 0.";
                    }
                    else if (row.QuestionType.Equals("MCQ", StringComparison.OrdinalIgnoreCase) &&
                            (string.IsNullOrWhiteSpace(row.OptionA) || string.IsNullOrWhiteSpace(row.OptionB) || string.IsNullOrWhiteSpace(row.CorrectOption)))
                    {
                        row.IsValid = false;
                        row.ErrorMessage = "MCQ requires at least OptionA, OptionB, and CorrectOption.";
                    }

                    if (!row.IsValid)
                    {
                        result.RowErrors.Add($"Row {rowNum}: {row.ErrorMessage}");
                        result.ErrorCount++;
                    }
                    else
                    {
                        rows.Add(row);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse imported questions file.");
                return Json(new BulkQuestionImportResultDto { Success = false, Message = $"File parsing error: {ex.Message}" });
            }

            result.TotalRows = rows.Count + result.ErrorCount;

            var units = subjectId.HasValue
                ? await _db.UnitMasters.Where(u => u.SubjectId == subjectId.Value && u.IsActive).ToListAsync()
                : new List<UnitMaster>();

            var unitIds = units.Select(u => u.Id).ToList();
            var topics = unitIds.Any()
                ? await _db.TopicMasters.Where(t => unitIds.Contains(t.UnitId) && t.IsActive).ToListAsync()
                : new List<TopicMaster>();

            int facultyId = GetUserId();
            var existingNorms = await _db.QuestionBanks
                .Where(q => q.CourseId == courseId && (subjectId == null || q.SubjectId == subjectId) && q.IsActive)
                .Select(q => q.NormalizedText)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToHashSetAsync();

            int imported = 0;
            int duplicate = 0;

            foreach (var r in rows)
            {
                var normText = GeminiSuggestedQuestionService.ComputeNormalizedText(r.QuestionText);
                if (existingNorms.Contains(normText))
                {
                    duplicate++;
                    result.RowErrors.Add($"Row {r.RowNumber}: Duplicate question detected (already exists in Question Bank).");
                    continue;
                }

                int? matchedUnitId = null;
                int? matchedTopicId = null;

                if (!string.IsNullOrWhiteSpace(r.UnitName))
                {
                    var u = units.FirstOrDefault(x => x.Name.Contains(r.UnitName, StringComparison.OrdinalIgnoreCase));
                    matchedUnitId = u?.Id;
                }

                if (!string.IsNullOrWhiteSpace(r.TopicName))
                {
                    var t = topics.FirstOrDefault(x => x.Name.Contains(r.TopicName, StringComparison.OrdinalIgnoreCase));
                    matchedTopicId = t?.Id;
                    if (t != null && !matchedUnitId.HasValue) matchedUnitId = t.UnitId;
                }

                string correctAnswer = r.CorrectOption ?? "";
                if (r.QuestionType.Equals("MCQ", StringComparison.OrdinalIgnoreCase))
                {
                    if (correctAnswer.Equals("A", StringComparison.OrdinalIgnoreCase)) correctAnswer = r.OptionA ?? "";
                    else if (correctAnswer.Equals("B", StringComparison.OrdinalIgnoreCase)) correctAnswer = r.OptionB ?? "";
                    else if (correctAnswer.Equals("C", StringComparison.OrdinalIgnoreCase)) correctAnswer = r.OptionC ?? "";
                    else if (correctAnswer.Equals("D", StringComparison.OrdinalIgnoreCase)) correctAnswer = r.OptionD ?? "";
                }

                var qBank = new QuestionBank
                {
                    CourseId = courseId,
                    SemesterId = semesterId,
                    SubjectId = subjectId,
                    UnitId = matchedUnitId,
                    TopicId = matchedTopicId,
                    LearningOutcome = r.LearningOutcome,
                    NormalizedText = normText,
                    QuestionType = r.QuestionType,
                    DifficultyLevel = r.DifficultyLevel,
                    QuestionText = r.QuestionText.Trim(),
                    Marks = r.Marks,
                    NegativeMarks = r.NegativeMarks,
                    CorrectAnswer = correctAnswer,
                    Explanation = r.Explanation,
                    Status = "Active",
                    FacultyId = facultyId,
                    IsActive = true,
                    CreatedBy = facultyId,
                    CreatedOn = DateTime.UtcNow
                };

                await _db.QuestionBanks.AddAsync(qBank);
                await _db.SaveChangesAsync();

                if (r.QuestionType.Equals("MCQ", StringComparison.OrdinalIgnoreCase))
                {
                    var options = new List<(string Text, bool Correct)>();
                    if (!string.IsNullOrWhiteSpace(r.OptionA)) options.Add((r.OptionA, r.OptionA == correctAnswer));
                    if (!string.IsNullOrWhiteSpace(r.OptionB)) options.Add((r.OptionB, r.OptionB == correctAnswer));
                    if (!string.IsNullOrWhiteSpace(r.OptionC)) options.Add((r.OptionC, r.OptionC == correctAnswer));
                    if (!string.IsNullOrWhiteSpace(r.OptionD)) options.Add((r.OptionD, r.OptionD == correctAnswer));

                    foreach (var opt in options)
                    {
                        _db.QuestionOptions.Add(new QuestionOption
                        {
                            QuestionId = qBank.Id,
                            OptionText = opt.Text.Trim(),
                            IsCorrect = opt.Correct
                        });
                    }
                    await _db.SaveChangesAsync();
                }

                existingNorms.Add(normText);
                imported++;
            }

            result.Success = imported > 0;
            result.ImportedCount = imported;
            result.DuplicateCount = duplicate;
            result.Message = $"Bulk import complete: {imported} questions imported, {duplicate} duplicate(s) skipped, {result.ErrorCount} error(s).";
            return Json(result);
        }

        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var current = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            result.Add(current.ToString());
            return result;
        }

        // =========================================================================
        // --- LEGACY QUIZ BUILDER COMPATIBILITY ---
        // =========================================================================

        // GET: Admin/Quiz/Item/{id?}
        [HttpGet]
        public async Task<IActionResult> Item(string? iId, int? courseId, int? quadrantId)
        {
            QuizMasterViewModel model = new();

            if (!string.IsNullOrEmpty(iId))
            {
                iId = iId.Decrypt(true);
                if (string.IsNullOrWhiteSpace(iId))
                    return NotFound();

                int quizId = Convert.ToInt32(iId);
                var existing = await _quizManager.GetAsync(
                    quizId,
                    new[] { "Course", "CourseQuadrant", "Questions", "Questions.Options" }
                );

                if (existing != null)
                    model = existing;
            }
            else
            {
                if (courseId.HasValue && courseId.Value > 0)
                    model.CourseId = courseId.Value;
                if (quadrantId.HasValue && quadrantId.Value > 0)
                    model.CourseQuadrantId = quadrantId.Value;

                model.Questions = new List<QuizQuestionViewModel>
                {
                    new QuizQuestionViewModel
                    {
                        QuestionType = TMS.Models.Masters.QuestionType.Objective,
                        MaxMarks = 1,
                        Options = new List<QuizOptionViewModel>
                        {
                            new QuizOptionViewModel(),
                            new QuizOptionViewModel()
                        }
                    }
                };
            }

            await SetDropdowns(model);
            return View(model);
        }

        // POST: Admin/Quiz/Item
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Item(QuizMasterViewModel model)
        {
            int userId = GetUserId();

            if (model.Id == 0)
            {
                bool duplicate = await ValidateModel(model);
                if (duplicate)
                {
                    await SetDropdowns(model);
                    return View(model);
                }
            }

            bool result = await _quizManager.AddUpdateQuizAsync(model, userId);

            if (result)
            {
                TempData["Success"] = "Quiz saved successfully.";
                SetApplicationResult(true, "Quiz saved successfully.");
                return RedirectToAction(nameof(Index));
            }

            SetApplicationResult(false, "Error while saving quiz.");
            await SetDropdowns(model);
            return View(model);
        }

        // POST: Admin/Quiz/Delete
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var quiz = await _quizManager.GetAsync(id);
            if (quiz == null)
                return Json(new { success = false, message = "Quiz not found." });

            if (IsFacultyRole())
            {
                int userId = GetUserId();
                var mappings = await _courseFacultyMapManager.GetAsync(null, x => x.FacultyId == userId && x.CourseId == quiz.CourseId && x.IsActive);
                if (!mappings.Any())
                    return Json(new { success = false, message = "You do not have permission to delete this quiz." });
            }

            bool result = await _quizManager.DeleteAsync(id);
            return Json(new { success = result, message = result ? "Quiz deleted successfully." : "Failed to delete quiz." });
        }

        private async Task SetDropdowns(QuizMasterViewModel model)
        {
            int userId = GetUserId();
            bool isFaculty = IsFacultyRole();

            List<CourseMasterViewModel> courses;

            if (isFaculty)
            {
                var mappings = await _courseFacultyMapManager.GetAsync(new[] { "Course" }, x => x.FacultyId == userId && x.IsActive);
                var courseIds = mappings.Select(m => m.CourseId).Distinct().ToList();

                var allCourses = await _courseManager.GetAsync(null, t => t.IsActive);
                courses = allCourses.Where(c => courseIds.Contains(c.Id) || c.Id == model.CourseId).ToList();
            }
            else
            {
                courses = await _courseManager.GetAsync(null, x => x.IsActive || x.Id == model.CourseId);
            }

            ViewBag.CourseId = courses ?? new List<CourseMasterViewModel>();

            var quadrants = await _quadrantManager.GetAsync(null, q => q.IsActive);
            ViewBag.QuadrantId = quadrants ?? new List<CourseQuadrantViewModel>();
        }

        private async Task<bool> ValidateModel(QuizMasterViewModel model)
        {
            var exists = await _quizManager.CheckExpression(t =>
                (model.Id == 0 || t.Id != model.Id) &&
                t.CourseId == model.CourseId &&
                t.CourseQuadrantId == model.CourseQuadrantId &&
                t.Title == model.Title
            );

            if (exists)
                ModelState.AddModelError("Title", "A quiz with this title already exists for the selected course/quadrant.");

            return exists;
        }

        [HttpGet]
        public async Task<JsonResult> GetQuadrantsByCourse(int courseId)
        {
            var quadrants = await _quadrantManager.GetAsync(null, q => q.IsActive);
            var list = quadrants.Select(q => new { q.Id, q.Name }).ToList();
            return Json(list);
        }

        [HttpGet]
        public IActionResult RenderQuestionPartial(int index)
        {
            var model = new QuizQuestionViewModel
            {
                QuestionText = string.Empty,
                QuestionType = TMS.Models.Masters.QuestionType.Objective,
                MaxMarks = 1,
                Options = new List<QuizOptionViewModel>
                {
                    new QuizOptionViewModel(),
                    new QuizOptionViewModel()
                },
                CorrectOptionId = null
            };

            ViewData["qIndex"] = index;
            return PartialView("_QuestionTemplate", model);
        }
    }
}
