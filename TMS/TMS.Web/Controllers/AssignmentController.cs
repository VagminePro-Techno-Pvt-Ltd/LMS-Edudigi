using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using TMS.Models.Academics;
using TMS.Models.Account;
using TMS.Models.Masters;
using TMS.Models.Training;
using TMS.Repository;
using TMS.ViewModels.Academics;
using TMS.Web.Services;

namespace TMS.Web.Controllers
{
    [Authorize]
    public class AssignmentController : BaseController
    {
        private readonly ApplicationDBContext _context;
        private readonly IAiAssignmentService _aiAssignmentService;
        private readonly ILogger<AssignmentController> _logger;

        public AssignmentController(
            ApplicationDBContext context,
            IAiAssignmentService aiAssignmentService,
            ILogger<AssignmentController> logger)
        {
            _context = context;
            _aiAssignmentService = aiAssignmentService;
            _logger = logger;
        }

        private static bool IsAllowedUpload(IFormFile file)
        {
            if (file.Length <= 0 || file.Length > 10 * 1024 * 1024) return false;
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            return new[] { ".pdf", ".docx", ".pptx", ".jpg", ".jpeg", ".png" }.Contains(ext);
        }

        // ═══════════════════════════════════════════════════
        //  MAIN ROUTER: Directs based on Role
        // ═══════════════════════════════════════════════════

        public IActionResult Index()
        {
            var role = (GetUserRole() ?? "").ToLower().Trim();
            var userId = GetUserId();

            if (User.IsInRole("Admin") || User.IsInRole("Super Admin") || User.IsInRole("super admin") || role.Contains("admin"))
            {
                return RedirectToAction(nameof(AdminDashboard));
            }
            else if (User.IsInRole("Faculty") || role.Contains("faculty"))
            {
                return RedirectToAction(nameof(FacultyDashboard));
            }
            else if (User.IsInRole("Student") || role.Contains("student") || string.IsNullOrEmpty(role))
            {
                return RedirectToAction(nameof(StudentDashboard));
            }

            return RedirectToAction(nameof(StudentDashboard));
        }


        // ═══════════════════════════════════════════════════
        //  FACULTY APIs & VIEWS
        // ═══════════════════════════════════════════════════

        [Authorize(Roles = "Faculty,Admin,Super Admin,super admin,Exam Coordinator")]
        public async Task<IActionResult> FacultyDashboard(int? programId)
        {
            var userId = GetUserId();
            bool isStaff = User.IsInRole("Admin") || User.IsInRole("Super Admin") || User.IsInRole("super admin") || User.IsInRole("Exam Coordinator");

            List<int> authorizedCourseIds = new();
            if (!isStaff)
            {
                authorizedCourseIds = await _context.CourseFacultyMaps
                    .Where(m => m.FacultyId == userId && m.IsActive)
                    .Select(m => m.CourseId)
                    .Distinct()
                    .ToListAsync();
            }

            var programsQuery = _context.CourseCategoryMasters.Where(t => t.IsActive);
            var coursesQuery = _context.CourseMasters.Where(c => c.IsActive);
            if (!isStaff)
            {
                coursesQuery = coursesQuery.Where(c => authorizedCourseIds.Contains(c.Id));
                var progIds = await coursesQuery.Select(c => c.CourseCategoryId).Distinct().ToListAsync();
                programsQuery = programsQuery.Where(p => progIds.Contains(p.Id));
            }

            var programs = await programsQuery.OrderBy(t => t.Name).ToListAsync();
            var courses = await coursesQuery.OrderBy(c => c.Name).ToListAsync();

            // Filter assignments based on program and authorization
            var assignmentsQuery = _context.AssignmentMasters
                .Include(a => a.Program)
                .Include(a => a.Course)
                .Where(a => !a.IsDeleted);

            if (!isStaff)
            {
                assignmentsQuery = assignmentsQuery.Where(a => (a.CourseId.HasValue && authorizedCourseIds.Contains(a.CourseId.Value)) || a.CreatedBy == userId);
            }

            if (programId.HasValue && programId > 0)
            {
                assignmentsQuery = assignmentsQuery.Where(a => a.ProgramId == programId.Value);
            }

            var assignments = await assignmentsQuery.OrderByDescending(a => a.CreatedOn).ToListAsync();

            ViewBag.Programs = programs;
            ViewBag.Courses = courses;
            ViewBag.SelectedProgramId = programId;

            return View("FacultyDashboard", assignments);
        }

        // ═══════════════════════════════════════════════════
        //  FEATURE 53: AI ASSIGNMENT GENERATOR ACTIONS
        // ═══════════════════════════════════════════════════

        [HttpGet]
        [Authorize(Roles = "Faculty,Admin,Super Admin,super admin,Exam Coordinator")]
        public async Task<IActionResult> AiGenerator(int? editId)
        {
            var userId = GetUserId();
            bool isStaff = User.IsInRole("Admin") || User.IsInRole("Super Admin") || User.IsInRole("super admin") || User.IsInRole("Exam Coordinator");

            List<int> authorizedCourseIds = new();
            if (!isStaff)
            {
                authorizedCourseIds = await _context.CourseFacultyMaps
                    .Where(m => m.FacultyId == userId && m.IsActive)
                    .Select(m => m.CourseId)
                    .Distinct()
                    .ToListAsync();
            }

            var programsQuery = _context.CourseCategoryMasters.Where(t => t.IsActive);
            var coursesQuery = _context.CourseMasters.Where(c => c.IsActive);
            if (!isStaff)
            {
                coursesQuery = coursesQuery.Where(c => authorizedCourseIds.Contains(c.Id));
                var progIds = await coursesQuery.Select(c => c.CourseCategoryId).Distinct().ToListAsync();
                programsQuery = programsQuery.Where(p => progIds.Contains(p.Id));
            }

            ViewBag.Programs = await programsQuery.OrderBy(t => t.Name).ToListAsync();
            ViewBag.Courses = await coursesQuery.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Semesters = await _context.SemesterMasters.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();

            SaveAiAssignmentRequest? model = null;

            if (editId.HasValue && editId.Value > 0)
            {
                var assignment = await _context.AssignmentMasters
                    .Include(a => a.Program)
                    .Include(a => a.Course)
                    .FirstOrDefaultAsync(a => a.Id == editId.Value && !a.IsDeleted);

                if (assignment != null)
                {
                    if (!isStaff && assignment.CourseId.HasValue && !authorizedCourseIds.Contains(assignment.CourseId.Value) && assignment.CreatedBy != userId)
                    {
                        SetApplicationResult(false, "You are not authorized to edit this assignment.");
                        return RedirectToAction(nameof(FacultyDashboard));
                    }

                    var (cleanText, metadata) = AiAssignmentMetadata.ParseDescription(assignment.Description);

                    model = new SaveAiAssignmentRequest
                    {
                        AssignmentId = assignment.Id,
                        ProgramId = assignment.ProgramId,
                        CourseId = assignment.CourseId ?? 0,
                        SemesterId = metadata?.SemesterId ?? assignment.Course?.SemesterId,
                        SubjectId = metadata?.SubjectId,
                        UnitId = metadata?.UnitId,
                        TopicId = metadata?.TopicId,
                        Difficulty = metadata?.Difficulty ?? "Medium",
                        AssignmentType = metadata?.AssignmentType ?? "Theory / Descriptive",
                        Title = assignment.Title,
                        Description = cleanText,
                        Instructions = metadata?.Instructions ?? "",
                        DueDate = assignment.DueDate,
                        TotalMarks = assignment.MaxMarks,
                        PassMarks = assignment.PassMarks,
                        AllowLateSubmission = assignment.AllowLateSubmission,
                        LatePenalty = assignment.LatePenalty,
                        Questions = metadata?.Questions ?? new List<AiAssignmentQuestionItem>(),
                        MarkingCriteria = metadata?.MarkingCriteria ?? new List<AiMarkingCriterionItem>(),
                        Rubrics = metadata?.Rubrics ?? new List<AiAssignmentRubricRow>(),
                        Publish = assignment.IsActive
                    };

                    ViewBag.EditAssignment = assignment;
                }
            }

            return View("AiGenerator", model);
        }

        [HttpGet]
        [Authorize(Roles = "Faculty,Admin,Super Admin,super admin,Exam Coordinator")]
        public async Task<IActionResult> AiAssignment(int? editId)
        {
            return await AiGenerator(editId);
        }

        [HttpPost]
        [Authorize(Roles = "Faculty,Admin,Super Admin,super admin,Exam Coordinator")]
        public async Task<IActionResult> GenerateAiAssignment([FromBody] AiAssignmentGenerateRequest request)
        {
            if (request == null)
            {
                return Json(new { success = false, message = "Invalid request payload." });
            }

            var userId = GetUserId();
            bool isStaff = User.IsInRole("Admin") || User.IsInRole("Super Admin") || User.IsInRole("super admin") || User.IsInRole("Exam Coordinator");

            if (!isStaff)
            {
                if (request.CourseId > 0)
                {
                    var isAuthorized = await _context.CourseFacultyMaps
                        .AnyAsync(m => m.CourseId == request.CourseId && m.FacultyId == userId && m.IsActive);
                    if (!isAuthorized)
                    {
                        var fallbackCourse = await _context.CourseFacultyMaps
                            .Where(m => m.FacultyId == userId && m.IsActive)
                            .Select(m => m.CourseId)
                            .FirstOrDefaultAsync();
                        if (fallbackCourse > 0) request.CourseId = fallbackCourse;
                    }
                }
                else
                {
                    var fallbackCourse = await _context.CourseFacultyMaps
                        .Where(m => m.FacultyId == userId && m.IsActive)
                        .Select(m => m.CourseId)
                        .FirstOrDefaultAsync();
                    if (fallbackCourse > 0) request.CourseId = fallbackCourse;
                }
            }
            else if (request.CourseId <= 0)
            {
                var anyCourse = await _context.CourseMasters
                    .Where(c => c.IsActive)
                    .Select(c => c.Id)
                    .FirstOrDefaultAsync();
                if (anyCourse > 0) request.CourseId = anyCourse;
            }

            if (request.ProgramId <= 0 && request.CourseId > 0)
            {
                var derivedProgId = await _context.CourseMasters
                    .Where(c => c.Id == request.CourseId)
                    .Select(c => c.CourseCategoryId)
                    .FirstOrDefaultAsync();
                if (derivedProgId > 0) request.ProgramId = derivedProgId;
            }

            var result = await _aiAssignmentService.GenerateAssignmentAsync(request);
            if (!result.Success)
            {
                return Json(new { success = false, message = result.ErrorMessage ?? "Generation failed." });
            }

            return Json(new { success = true, data = result });
        }

        [HttpPost]
        [Authorize(Roles = "Faculty,Admin,Super Admin,super admin,Exam Coordinator")]
        public async Task<IActionResult> SaveAiAssignment([FromBody] SaveAiAssignmentRequest request)
        {
            if (request == null)
            {
                return Json(new { success = false, message = "Invalid assignment data." });
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Json(new { success = false, message = "Assignment title is required." });
            }

            if (request.ProgramId <= 0 && request.CourseId > 0)
            {
                var derivedProgId = await _context.CourseMasters
                    .Where(c => c.Id == request.CourseId)
                    .Select(c => c.CourseCategoryId)
                    .FirstOrDefaultAsync();
                if (derivedProgId > 0) request.ProgramId = derivedProgId;
            }

            if (request.TotalMarks <= 0)
            {
                return Json(new { success = false, message = "Total marks must be greater than zero." });
            }

            if (request.PassMarks < 0 || request.PassMarks > request.TotalMarks)
            {
                return Json(new { success = false, message = "Pass marks must be between 0 and total marks." });
            }

            if (request.DueDate <= DateTime.UtcNow)
            {
                return Json(new { success = false, message = "Due date must be in the future." });
            }

            var userId = GetUserId();
            bool isStaff = User.IsInRole("Admin") || User.IsInRole("Super Admin") || User.IsInRole("super admin") || User.IsInRole("Exam Coordinator");

            if (!isStaff)
            {
                if (request.CourseId > 0)
                {
                    var isAuthorized = await _context.CourseFacultyMaps
                        .AnyAsync(m => m.CourseId == request.CourseId && m.FacultyId == userId && m.IsActive);
                    if (!isAuthorized)
                    {
                        var fallbackCourse = await _context.CourseFacultyMaps
                            .Where(m => m.FacultyId == userId && m.IsActive)
                            .Select(m => m.CourseId)
                            .FirstOrDefaultAsync();
                        if (fallbackCourse > 0) request.CourseId = fallbackCourse;
                        else return Json(new { success = false, message = "You are not authorized to manage assignments for this course." });
                    }
                }
                else
                {
                    var fallbackCourse = await _context.CourseFacultyMaps
                        .Where(m => m.FacultyId == userId && m.IsActive)
                        .Select(m => m.CourseId)
                        .FirstOrDefaultAsync();
                    if (fallbackCourse > 0) request.CourseId = fallbackCourse;
                }
            }
            else if (request.CourseId <= 0)
            {
                var anyCourse = await _context.CourseMasters
                    .Where(c => c.IsActive)
                    .Select(c => c.Id)
                    .FirstOrDefaultAsync();
                if (anyCourse > 0) request.CourseId = anyCourse;
            }

            // Normalization: Ensure sum of question marks strictly equals request.TotalMarks
            if (request.Questions != null && request.Questions.Any())
            {
                GeminiAiAssignmentService.ValidateAndNormalizeMarks(request.Questions, request.TotalMarks);
            }

            // Academic metadata names
            string? semName = null;
            if (request.SemesterId.HasValue && request.SemesterId.Value > 0)
            {
                semName = await _context.SemesterMasters.Where(s => s.Id == request.SemesterId.Value).Select(s => s.Name).FirstOrDefaultAsync();
            }

            string? subName = null;
            if (request.SubjectId.HasValue && request.SubjectId.Value > 0)
            {
                subName = await _context.SubjectMasters.Where(s => s.Id == request.SubjectId.Value).Select(s => s.Name).FirstOrDefaultAsync();
            }

            string? uName = null;
            if (request.UnitId.HasValue && request.UnitId.Value > 0)
            {
                uName = await _context.UnitMasters.Where(u => u.Id == request.UnitId.Value).Select(u => u.Name).FirstOrDefaultAsync();
            }

            string? tName = null;
            if (request.TopicId.HasValue && request.TopicId.Value > 0)
            {
                tName = await _context.TopicMasters.Where(t => t.Id == request.TopicId.Value).Select(t => t.Name).FirstOrDefaultAsync();
            }

            var metadata = new AiAssignmentMetadata
            {
                SemesterId = request.SemesterId,
                SemesterName = semName,
                SubjectId = request.SubjectId,
                SubjectName = subName,
                UnitId = request.UnitId,
                UnitName = uName,
                TopicId = request.TopicId,
                TopicName = tName,
                Difficulty = request.Difficulty,
                AssignmentType = request.AssignmentType,
                Instructions = request.Instructions,
                Questions = request.Questions ?? new(),
                MarkingCriteria = request.MarkingCriteria ?? new(),
                Rubrics = request.Rubrics ?? new(),
                IsAiGenerated = true,
                Status = request.Publish ? "Published" : "Draft",
                PublishedOn = request.Publish ? DateTime.UtcNow : null
            };

            var fullDescription = AiAssignmentMetadata.BuildDescriptionWithMetadata(
                request.Description,
                request.Instructions,
                request.Questions ?? new(),
                request.MarkingCriteria ?? new(),
                request.Rubrics ?? new(),
                metadata);

            AssignmentMaster assignment;

            if (request.AssignmentId.HasValue && request.AssignmentId.Value > 0)
            {
                assignment = await _context.AssignmentMasters.FirstOrDefaultAsync(a => a.Id == request.AssignmentId.Value && !a.IsDeleted);
                if (assignment == null)
                {
                    return Json(new { success = false, message = "Assignment not found." });
                }

                // Prevent duplicate publishing if already active
                if (assignment.IsActive && request.Publish)
                {
                    return Json(new { success = false, message = "This assignment is already published." });
                }

                assignment.Title = request.Title.Trim();
                assignment.Description = fullDescription;
                assignment.ProgramId = request.ProgramId;
                assignment.CourseId = request.CourseId;
                assignment.DueDate = request.DueDate;
                assignment.MaxMarks = request.TotalMarks;
                assignment.PassMarks = request.PassMarks;
                assignment.AllowLateSubmission = request.AllowLateSubmission;
                assignment.LatePenalty = request.LatePenalty;
                assignment.IsActive = request.Publish; // true if publishing, false if draft
                assignment.UpdatedBy = userId;
                assignment.UpdatedOn = DateTime.UtcNow;
            }
            else
            {
                assignment = new AssignmentMaster
                {
                    Title = request.Title.Trim(),
                    Description = fullDescription,
                    ProgramId = request.ProgramId,
                    CourseId = request.CourseId,
                    DueDate = request.DueDate,
                    MaxMarks = request.TotalMarks,
                    PassMarks = request.PassMarks,
                    AllowLateSubmission = request.AllowLateSubmission,
                    LatePenalty = request.LatePenalty,
                    IsActive = request.Publish, // true if publishing, false if draft
                    IsDeleted = false,
                    CreatedBy = userId,
                    CreatedOn = DateTime.UtcNow
                };
                _context.AssignmentMasters.Add(assignment);
            }

            await _context.SaveChangesAsync();

            var message = request.Publish
                ? "Assignment published successfully! It is now visible to enrolled students."
                : "Assignment saved as draft successfully (hidden from students).";

            return Json(new
            {
                success = true,
                assignmentId = assignment.Id,
                isPublished = assignment.IsActive,
                message = message
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Faculty,Admin,Super Admin,super admin,Exam Coordinator")]
        public async Task<IActionResult> PublishAssignment(int id)
        {
            var assignment = await _context.AssignmentMasters.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
            if (assignment == null)
            {
                return Json(new { success = false, message = "Assignment not found." });
            }

            var userId = GetUserId();
            bool isStaff = User.IsInRole("Admin") || User.IsInRole("Super Admin") || User.IsInRole("super admin") || User.IsInRole("Exam Coordinator");

            if (!isStaff && assignment.CourseId.HasValue)
            {
                var isAuthorized = await _context.CourseFacultyMaps
                    .AnyAsync(m => m.CourseId == assignment.CourseId.Value && m.FacultyId == userId && m.IsActive);
                if (!isAuthorized && assignment.CreatedBy != userId)
                {
                    return Json(new { success = false, message = "You are not authorized to publish this assignment." });
                }
            }

            if (assignment.IsActive)
            {
                return Json(new { success = false, message = "Assignment is already published." });
            }

            assignment.IsActive = true;
            assignment.UpdatedBy = userId;
            assignment.UpdatedOn = DateTime.UtcNow;

            // Update metadata if present
            var (cleanText, metadata) = AiAssignmentMetadata.ParseDescription(assignment.Description);
            if (metadata != null)
            {
                metadata.Status = "Published";
                metadata.PublishedOn = DateTime.UtcNow;
                assignment.Description = AiAssignmentMetadata.BuildDescriptionWithMetadata(
                    cleanText,
                    metadata.Instructions ?? "",
                    metadata.Questions,
                    metadata.MarkingCriteria,
                    metadata.Rubrics,
                    metadata);
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Assignment published successfully! Enrolled students can now view and submit." });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> PrintAssignment(int id)
        {
            var assignment = await _context.AssignmentMasters
                .Include(a => a.Program)
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

            if (assignment == null) return NotFound();

            var userId = GetUserId();
            var userRole = (GetUserRole() ?? "").ToLower().Trim();
            bool isAdmin = User.IsInRole("Admin") || User.IsInRole("Super Admin") || User.IsInRole("super admin") || User.IsInRole("Exam Coordinator") || userRole.Contains("admin") || userRole.Contains("coordinator");
            bool isFaculty = User.IsInRole("Faculty") || userRole.Contains("faculty");

            bool isFacultyView = false;

            if (isAdmin)
            {
                // Administrators get full access
                isFacultyView = true;
            }
            else if (isFaculty)
            {
                // Verify faculty authorization for this assignment
                bool isAuthorizedFaculty = false;
                if (assignment.CourseId.HasValue)
                {
                    isAuthorizedFaculty = await _context.CourseFacultyMaps
                        .AnyAsync(m => m.CourseId == assignment.CourseId.Value && m.FacultyId == userId && m.IsActive);
                }
                if (!isAuthorizedFaculty && assignment.CreatedBy == userId)
                {
                    isAuthorizedFaculty = true;
                }

                if (isAuthorizedFaculty)
                {
                    isFacultyView = true;
                }
                else
                {
                    // Check if faculty is enrolled as student in this course
                    if (!assignment.IsActive) return Forbid();

                    var enrollments = await _context.CourseEnrollments
                        .Where(e => e.StudentId == userId && e.IsActive && (e.Status == 0 || e.Status == 1))
                        .ToListAsync();
                    var enrolledCourseIds = enrollments.Select(e => e.CourseId).ToList();
                    var enrolledPrograms = await _context.CourseMasters
                        .Where(c => enrolledCourseIds.Contains(c.Id))
                        .Select(c => c.CourseCategoryId)
                        .Distinct()
                        .ToListAsync();

                    bool isEnrolled = (assignment.CourseId.HasValue && enrolledCourseIds.Contains(assignment.CourseId.Value))
                                      || enrolledPrograms.Contains(assignment.ProgramId);
                    if (!isEnrolled) return Forbid();

                    isFacultyView = false;
                }
            }
            else
            {
                // Student access: must be active and enrolled
                if (!assignment.IsActive) return Forbid();

                var enrollments = await _context.CourseEnrollments
                    .Where(e => e.StudentId == userId && e.IsActive && (e.Status == 0 || e.Status == 1))
                    .ToListAsync();
                var enrolledCourseIds = enrollments.Select(e => e.CourseId).ToList();
                var enrolledPrograms = await _context.CourseMasters
                    .Where(c => enrolledCourseIds.Contains(c.Id))
                    .Select(c => c.CourseCategoryId)
                    .Distinct()
                    .ToListAsync();

                bool isEnrolled = (assignment.CourseId.HasValue && enrolledCourseIds.Contains(assignment.CourseId.Value))
                                  || enrolledPrograms.Contains(assignment.ProgramId);
                if (!isEnrolled) return Forbid();

                isFacultyView = false;
            }

            var (cleanText, metadata) = AiAssignmentMetadata.ParseDescription(assignment.Description);
            var overviewOnly = AiAssignmentMetadata.ExtractOverview(assignment.Description);
            var instructions = !string.IsNullOrWhiteSpace(metadata?.Instructions)
                ? metadata.Instructions
                : AiAssignmentMetadata.ExtractInstructions(assignment.Description);

            // Backend visibility enforcement: Never expose Key Concepts / Guidelines to students
            var safeQuestions = new List<AiAssignmentQuestionItem>();
            var rawQuestions = metadata?.Questions ?? new List<AiAssignmentQuestionItem>();
            foreach (var q in rawQuestions)
            {
                safeQuestions.Add(new AiAssignmentQuestionItem
                {
                    QuestionNumber = q.QuestionNumber,
                    QuestionText = q.QuestionText,
                    Marks = q.Marks,
                    ExpectedAnswerGuidelines = isFacultyView ? q.ExpectedAnswerGuidelines : null
                });
            }

            var vm = new PrintAssignmentViewModel
            {
                AssignmentId = assignment.Id,
                Title = assignment.Title,
                ProgramName = assignment.Program?.Name ?? "Academic Program",
                CourseName = assignment.Course?.Name ?? "Course",
                CourseCode = assignment.Course?.CourseCode ?? "",
                SemesterName = metadata?.SemesterName,
                SubjectName = metadata?.SubjectName,
                UnitName = metadata?.UnitName,
                TopicName = metadata?.TopicName,
                DueDate = assignment.DueDate,
                TotalMarks = assignment.MaxMarks,
                PassMarks = assignment.PassMarks,
                Difficulty = metadata?.Difficulty ?? "Medium",
                AssignmentType = metadata?.AssignmentType ?? "Theory / Descriptive",
                Description = overviewOnly, // Only the overview text, eliminating repetition across sections
                Instructions = instructions,
                Questions = safeQuestions,
                // Backend visibility enforcement: Never expose Marking Criteria or Rubrics to students
                MarkingCriteria = isFacultyView ? (metadata?.MarkingCriteria ?? new List<AiMarkingCriterionItem>()) : new List<AiMarkingCriterionItem>(),
                Rubrics = isFacultyView ? (metadata?.Rubrics ?? new List<AiAssignmentRubricRow>()) : new List<AiAssignmentRubricRow>(),
                IsPublished = assignment.IsActive,
                IsFacultyView = isFacultyView
            };

            return View("PrintAssignment", vm);
        }

        // ───────────────────────────────────────────────────
        //  CASCADING DROPDOWN APIS
        // ───────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetCoursesByProgram(int programId)
        {
            var userId = GetUserId();
            bool isStaff = User.IsInRole("Admin") || User.IsInRole("Super Admin") || User.IsInRole("super admin") || User.IsInRole("Exam Coordinator");

            var query = _context.CourseMasters.Where(c => c.IsActive && c.CourseCategoryId == programId);
            if (!isStaff)
            {
                var mappedCourseIds = await _context.CourseFacultyMaps
                    .Where(m => m.FacultyId == userId && m.IsActive)
                    .Select(m => m.CourseId)
                    .Distinct()
                    .ToListAsync();
                query = query.Where(c => mappedCourseIds.Contains(c.Id));
            }

            var courses = await query.OrderBy(c => c.Name)
                .Select(c => new { id = c.Id, name = c.Name, code = c.CourseCode, semesterId = c.SemesterId, programId = c.CourseCategoryId })
                .ToListAsync();

            return Json(courses);
        }

        [HttpGet]
        public async Task<IActionResult> GetSemestersByProgram(int programId)
        {
            var semesters = await _context.CourseMasters
                .Where(c => c.IsActive && c.CourseCategoryId == programId && c.SemesterId.HasValue)
                .Select(c => c.Semester!)
                .Where(s => s != null && s.IsActive)
                .Distinct()
                .OrderBy(s => s.Name)
                .Select(s => new { id = s.Id, name = s.Name })
                .ToListAsync();

            if (!semesters.Any())
            {
                semesters = await _context.SemesterMasters
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.Name)
                    .Select(s => new { id = s.Id, name = s.Name })
                    .ToListAsync();
            }

            return Json(semesters);
        }

        [HttpGet]
        public async Task<IActionResult> GetSubjectsByCourse(int courseId)
        {
            var course = await _context.CourseMasters.FirstOrDefaultAsync(c => c.Id == courseId);
            if (course == null) return Json(new List<object>());

            var subjects = new List<object>();

            if (course.SemesterId.HasValue && course.SemesterId.Value > 0)
            {
                var semesterSubjects = await _context.SubjectMasters
                    .Where(s => s.SemesterId == course.SemesterId.Value && s.IsActive)
                    .OrderBy(s => s.Name)
                    .Select(s => new { id = s.Id, name = s.Name, code = s.SubjectCode })
                    .ToListAsync();
                subjects.AddRange(semesterSubjects);
            }

            if (!subjects.Any())
            {
                var allSubjects = await _context.SubjectMasters
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.Name)
                    .Take(50)
                    .Select(s => new { id = s.Id, name = s.Name, code = s.SubjectCode })
                    .ToListAsync();
                subjects.AddRange(allSubjects);
            }

            return Json(subjects);
        }

        [HttpGet]
        public async Task<IActionResult> GetUnitsBySubject(int subjectId)
        {
            var units = await _context.UnitMasters
                .Where(u => u.SubjectId == subjectId && u.IsActive)
                .OrderBy(u => u.SortOrder).ThenBy(u => u.Name)
                .Select(u => new { id = u.Id, name = u.Name })
                .ToListAsync();

            return Json(units);
        }

        [HttpGet]
        public async Task<IActionResult> GetTopicsByUnit(int unitId)
        {
            var topics = await _context.TopicMasters
                .Where(t => t.UnitId == unitId && t.IsActive)
                .OrderBy(t => t.SortOrder).ThenBy(t => t.Name)
                .Select(t => new { id = t.Id, name = t.Name })
                .ToListAsync();

            return Json(topics);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Faculty,Admin,Super Admin,super admin,Exam Coordinator")]
        public async Task<IActionResult> CreateAssignment(AssignmentMaster model, IFormFile? attachment)
        {
            try
            {
                var userId = GetUserId();
                if (userId <= 0 || string.IsNullOrWhiteSpace(model.Title) || model.MaxMarks <= 0 ||
                    model.PassMarks < 0 || model.PassMarks > model.MaxMarks || model.DueDate <= DateTime.UtcNow)
                {
                    SetApplicationResult(false, "Invalid title, marks or due date.");
                    return RedirectToAction(nameof(FacultyDashboard));
                }
                if (!await _context.CourseCategoryMasters.AnyAsync(p => p.Id == model.ProgramId && p.IsActive) ||
                    (model.CourseId.HasValue && !await _context.CourseMasters.AnyAsync(c => c.Id == model.CourseId.Value && c.CourseCategoryId == model.ProgramId && c.IsActive)))
                {
                    SetApplicationResult(false, "Invalid program/course mapping.");
                    return RedirectToAction(nameof(FacultyDashboard));
                }
                model.CreatedBy = userId;
                model.CreatedOn = DateTime.UtcNow;
                model.IsActive = true;
                model.IsDeleted = false;

                if (attachment != null && attachment.Length > 0)
                {
                    if (!IsAllowedUpload(attachment))
                    {
                        SetApplicationResult(false, "Only PDF, DOCX, PPTX, JPG or PNG files up to 10 MB are allowed.");
                        return RedirectToAction(nameof(FacultyDashboard));
                    }
                    var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "assignments");
                    if (!Directory.Exists(uploadsDir))
                    {
                        Directory.CreateDirectory(uploadsDir);
                    }

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(attachment.FileName);
                    var filePath = Path.Combine(uploadsDir, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await attachment.CopyToAsync(stream);
                    }

                    model.AttachmentPath = "/uploads/assignments/" + fileName;
                }

                _context.AssignmentMasters.Add(model);
                await _context.SaveChangesAsync();

                SetApplicationResult(true, "Assignment created successfully!");
            }
            catch (Exception ex)
            {
                SetApplicationResult(false, $"Error creating assignment: {ex.Message}");
            }

            return RedirectToAction(nameof(FacultyDashboard));
        }

        [Authorize(Roles = "Faculty,Admin,Super Admin,super admin,Exam Coordinator")]
        public async Task<IActionResult> ViewSubmissions(int assignmentId)
        {
            var assignment = await _context.AssignmentMasters
                .Include(a => a.Program)
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.Id == assignmentId && !a.IsDeleted);

            if (assignment == null) return NotFound();

            var submissions = await _context.AssignmentSubmissions
                .Include(s => s.Student)
                .Where(s => s.AssignmentId == assignmentId && !s.IsDeleted)
                .OrderByDescending(s => s.SubmittedOn)
                .ToListAsync();

            // Fetch evaluations for these submissions
            var submissionIds = submissions.Select(s => s.Id).ToList();
            var grades = await _context.AssignmentGrades
                .Where(g => submissionIds.Contains(g.SubmissionId) && !g.IsDeleted)
                .ToListAsync();

            ViewBag.Assignment = assignment;
            ViewBag.Grades = grades;

            return View("ViewSubmissions", submissions);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Faculty,Admin,Super Admin,super admin,Exam Coordinator")]
        public async Task<IActionResult> GradeSubmission(AssignmentGrade grade)
        {
            try
            {
                var userId = GetUserId();
                var submissionForGrade = await _context.AssignmentSubmissions
                    .Include(s => s.Assignment)
                    .FirstOrDefaultAsync(s => s.Id == grade.SubmissionId && !s.IsDeleted);
                if (submissionForGrade?.Assignment == null || submissionForGrade.Assignment.IsDeleted ||
                    grade.Marks < 0 || grade.Marks > submissionForGrade.Assignment.MaxMarks)
                    return Json(new { success = false, message = "Invalid submission or marks." });
                grade.CreatedBy = userId;
                grade.CreatedOn = DateTime.UtcNow;
                grade.EvaluatedBy = userId;
                grade.EvaluatedOn = DateTime.UtcNow;
                grade.IsActive = true;

                // Update submission status
                var submission = await _context.AssignmentSubmissions.FindAsync(grade.SubmissionId);
                if (submission != null)
                {
                    submission.Status = "Graded";
                    submission.UpdatedBy = userId;
                    submission.UpdatedOn = DateTime.UtcNow;
                }

                // Check if already graded
                var existingGrade = await _context.AssignmentGrades
                    .FirstOrDefaultAsync(g => g.SubmissionId == grade.SubmissionId && !g.IsDeleted);

                if (existingGrade != null)
                {
                    existingGrade.Marks = grade.Marks;
                    existingGrade.Feedback = grade.Feedback;
                    existingGrade.EvaluatedBy = grade.EvaluatedBy;
                    existingGrade.EvaluatedOn = grade.EvaluatedOn;
                    existingGrade.UpdatedBy = userId;
                    existingGrade.UpdatedOn = DateTime.UtcNow;
                }
                else
                {
                    _context.AssignmentGrades.Add(grade);
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Submission graded successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.ToString() });
            }
        }

        [Authorize(Roles = "Faculty,Admin,Super Admin,super admin,Exam Coordinator")]
        public async Task<IActionResult> DownloadAllSubmissions(int assignmentId)
        {
            var submissions = await _context.AssignmentSubmissions
                .Include(s => s.Student)
                .Where(s => s.AssignmentId == assignmentId && !string.IsNullOrEmpty(s.AttachmentPath) && !s.IsDeleted)
                .ToListAsync();

            if (!submissions.Any())
            {
                SetApplicationResult(false, "No attachments found to download.");
                return RedirectToAction(nameof(ViewSubmissions), new { assignmentId });
            }

            var tempZipPath = Path.Combine(Path.GetTempPath(), $"Assignment_{assignmentId}_{Guid.NewGuid()}.zip");

            using (var archive = ZipFile.Open(tempZipPath, ZipArchiveMode.Create))
            {
                foreach (var sub in submissions)
                {
                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", sub.AttachmentPath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                    {
                        var studentName = (sub.Student?.Name ?? $"Student_{sub.StudentId}").Replace(" ", "_");
                        var entryName = $"{studentName}_Attempt{sub.AttemptNumber}_{Path.GetFileName(fullPath)}";
                        archive.CreateEntryFromFile(fullPath, entryName);
                    }
                }
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(tempZipPath);
            System.IO.File.Delete(tempZipPath);

            return File(bytes, "application/zip", $"Submissions_Assignment_{assignmentId}.zip");
        }

        // ═══════════════════════════════════════════════════
        //  STUDENT APIs & VIEWS
        // ═══════════════════════════════════════════════════

        [Authorize]
        public async Task<IActionResult> StudentDashboard()
        {
            var studentId = GetUserId();

            // Fetch enrolled course IDs
            var enrollments = await _context.CourseEnrollments
                .Where(e => e.StudentId == studentId && e.Status == 1) // 1 = Active
                .ToListAsync();

            var enrolledCourseIds = enrollments.Select(e => e.CourseId).ToList();
            
            // ProgramIds are also linked via Course
            var enrolledPrograms = await _context.CourseMasters
                .Where(c => enrolledCourseIds.Contains(c.Id))
                .Select(c => c.CourseCategoryId)
                .Distinct()
                .ToListAsync();

            // Fetch assignments mapped to enrolled programs OR specific courses (published/active only)
            var assignments = await _context.AssignmentMasters
                .Include(a => a.Program)
                .Include(a => a.Course)
                .Where(a => !a.IsDeleted && a.IsActive && (enrolledPrograms.Contains(a.ProgramId) || (a.CourseId.HasValue && enrolledCourseIds.Contains(a.CourseId.Value))))
                .OrderByDescending(a => a.DueDate)
                .ToListAsync();

            // Fetch student submissions
            var submissions = await _context.AssignmentSubmissions
                .Where(s => s.StudentId == studentId && !s.IsDeleted)
                .ToListAsync();

            // Fetch grades
            var submissionIds = submissions.Select(s => s.Id).ToList();
            var grades = await _context.AssignmentGrades
                .Where(g => submissionIds.Contains(g.SubmissionId) && !g.IsDeleted)
                .ToListAsync();

            ViewBag.Submissions = submissions;
            ViewBag.Grades = grades;

            return View("StudentDashboard", assignments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> SubmitAssignment(int assignmentId, string? submissionText, IFormFile? attachment)
        {
            try
            {
                var studentId = GetUserId();
                var assignment = await _context.AssignmentMasters.FindAsync(assignmentId);

                if (assignment == null || assignment.IsDeleted || !assignment.IsActive) return NotFound();

                bool isStaff = User.IsInRole("Admin") || User.IsInRole("Super Admin") || User.IsInRole("Faculty") || User.IsInRole("Exam Coordinator");
                if (!isStaff)
                {
                    var enrollments = await _context.CourseEnrollments
                        .Where(e => e.StudentId == studentId && e.IsActive && (e.Status == 0 || e.Status == 1))
                        .ToListAsync();
                    var enrolledCourseIds = enrollments.Select(e => e.CourseId).ToList();
                    var enrolledPrograms = await _context.CourseMasters
                        .Where(c => enrolledCourseIds.Contains(c.Id))
                        .Select(c => c.CourseCategoryId)
                        .Distinct()
                        .ToListAsync();

                    bool isEnrolled = (assignment.CourseId.HasValue && enrolledCourseIds.Contains(assignment.CourseId.Value))
                                      || enrolledPrograms.Contains(assignment.ProgramId);
                    if (!isEnrolled)
                    {
                        return Forbid();
                    }
                }

                if (string.IsNullOrWhiteSpace(submissionText) && (attachment == null || attachment.Length == 0))
                {
                    SetApplicationResult(false, "Provide submission text or a file.");
                    return RedirectToAction(nameof(StudentDashboard));
                }
                // Validate due date logic
                var isLate = DateTime.UtcNow > assignment.DueDate;
                if (isLate && !assignment.AllowLateSubmission)
                {
                    SetApplicationResult(false, "Late submissions are not allowed for this assignment.");
                    return RedirectToAction(nameof(StudentDashboard));
                }

                // Determine attempt number
                var previousSubmissions = await _context.AssignmentSubmissions
                    .Where(s => s.AssignmentId == assignmentId && s.StudentId == studentId && !s.IsDeleted)
                    .ToListAsync();

                var attemptNumber = previousSubmissions.Count + 1;

                var submission = new AssignmentSubmission
                {
                    AssignmentId = assignmentId,
                    StudentId = studentId,
                    SubmissionText = submissionText,
                    SubmittedOn = DateTime.UtcNow,
                    AttemptNumber = attemptNumber,
                    Status = isLate ? "LateSubmitted" : "Submitted",
                    IsActive = true,
                    CreatedBy = studentId,
                    CreatedOn = DateTime.UtcNow
                };

                if (attachment != null && attachment.Length > 0)
                {
                    if (!IsAllowedUpload(attachment))
                    {
                        SetApplicationResult(false, "Only PDF, DOCX, PPTX, JPG or PNG files up to 10 MB are allowed.");
                        return RedirectToAction(nameof(StudentDashboard));
                    }
                    var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "submissions");
                    if (!Directory.Exists(uploadsDir))
                    {
                        Directory.CreateDirectory(uploadsDir);
                    }

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(attachment.FileName);
                    var filePath = Path.Combine(uploadsDir, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await attachment.CopyToAsync(stream);
                    }

                    submission.AttachmentPath = "/uploads/submissions/" + fileName;
                }

                _context.AssignmentSubmissions.Add(submission);
                await _context.SaveChangesAsync();

                SetApplicationResult(true, "Assignment submitted successfully!");
            }
            catch (Exception ex)
            {
                SetApplicationResult(false, $"Error submitting assignment: {ex.Message}");
            }

            return RedirectToAction(nameof(StudentDashboard));
        }

        [Authorize]
        public async Task<IActionResult> GetSubmissionHistory(int assignmentId)
        {
            var studentId = GetUserId();
            var assignment = await _context.AssignmentMasters.FindAsync(assignmentId);
            if (assignment == null) return NotFound();

            bool isStaff = User.IsInRole("Admin") || User.IsInRole("Super Admin") || User.IsInRole("Faculty") || User.IsInRole("Exam Coordinator");
            if (!isStaff)
            {
                var enrollments = await _context.CourseEnrollments
                    .Where(e => e.StudentId == studentId && e.IsActive && (e.Status == 0 || e.Status == 1))
                    .ToListAsync();
                var enrolledCourseIds = enrollments.Select(e => e.CourseId).ToList();
                var enrolledPrograms = await _context.CourseMasters
                    .Where(c => enrolledCourseIds.Contains(c.Id))
                    .Select(c => c.CourseCategoryId)
                    .Distinct()
                    .ToListAsync();

                bool isEnrolled = (assignment.CourseId.HasValue && enrolledCourseIds.Contains(assignment.CourseId.Value))
                                  || enrolledPrograms.Contains(assignment.ProgramId);
                if (!isEnrolled)
                {
                    return Forbid();
                }
            }

            var submissions = await _context.AssignmentSubmissions
                .Where(s => s.AssignmentId == assignmentId && s.StudentId == studentId && !s.IsDeleted)
                .OrderByDescending(s => s.AttemptNumber)
                .ToListAsync();

            var submissionIds = submissions.Select(s => s.Id).ToList();
            var grades = await _context.AssignmentGrades
                .Where(g => submissionIds.Contains(g.SubmissionId) && !g.IsDeleted)
                .ToListAsync();

            ViewBag.Grades = grades;

            return PartialView("_SubmissionHistory", submissions);
        }

        // ═══════════════════════════════════════════════════
        //  ADMIN APIs & VIEWS
        // ═══════════════════════════════════════════════════

        [Authorize(Roles = "Admin,Super Admin,super admin,Exam Coordinator,Management")]
        public async Task<IActionResult> AdminDashboard()
        {
            var assignments = await _context.AssignmentMasters
                .Include(a => a.Program)
                .Include(a => a.Course)
                .Where(a => !a.IsDeleted)
                .ToListAsync();

            var submissions = await _context.AssignmentSubmissions
                .Include(s => s.Student)
                .Include(s => s.Assignment)
                .Where(s => !s.IsDeleted)
                .ToListAsync();

            var grades = await _context.AssignmentGrades
                .Where(g => !g.IsDeleted)
                .ToListAsync();

            ViewBag.Submissions = submissions;
            ViewBag.Grades = grades;

            return View("AdminDashboard", assignments);
        }
    }
}
