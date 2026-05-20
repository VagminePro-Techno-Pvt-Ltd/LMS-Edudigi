using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

namespace TMS.Web.Controllers
{
    [Authorize]
    public class AssignmentController : BaseController
    {
        private readonly ApplicationDBContext _context;

        public AssignmentController(ApplicationDBContext context)
        {
            _context = context;
        }

        // ═══════════════════════════════════════════════════
        //  MAIN ROUTER: Directs based on Role
        // ═══════════════════════════════════════════════════

        public async Task<IActionResult> Index()
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

        [Authorize]
        public async Task<IActionResult> FacultyDashboard(int? programId)
        {
            var userId = GetUserId();
            var programs = await _context.CourseCategoryMasters
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .ToListAsync();

            // Filter assignments based on program
            var assignmentsQuery = _context.AssignmentMasters
                .Include(a => a.Program)
                .Include(a => a.Course)
                .Where(a => !a.IsDeleted);

            if (programId.HasValue && programId > 0)
            {
                assignmentsQuery = assignmentsQuery.Where(a => a.ProgramId == programId.Value);
            }

            var assignments = await assignmentsQuery.OrderByDescending(a => a.CreatedOn).ToListAsync();

            var courses = await _context.CourseMasters
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Programs = programs;
            ViewBag.Courses = courses;
            ViewBag.SelectedProgramId = programId;

            return View("FacultyDashboard", assignments);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateAssignment(AssignmentMaster model, IFormFile? attachment)
        {
            try
            {
                var userId = GetUserId();
                model.CreatedBy = userId;
                model.CreatedOn = DateTime.UtcNow;
                model.IsActive = true;
                model.IsDeleted = false;

                if (attachment != null && attachment.Length > 0)
                {
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

        [Authorize]
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
        [Authorize]
        public async Task<IActionResult> GradeSubmission(AssignmentGrade grade)
        {
            try
            {
                var userId = GetUserId();
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

        [Authorize]
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

            // Fetch assignments mapped to enrolled programs OR specific courses
            var assignments = await _context.AssignmentMasters
                .Include(a => a.Program)
                .Include(a => a.Course)
                .Where(a => !a.IsDeleted && (enrolledPrograms.Contains(a.ProgramId) || (a.CourseId.HasValue && enrolledCourseIds.Contains(a.CourseId.Value))))
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
        [Authorize]
        public async Task<IActionResult> SubmitAssignment(int assignmentId, string? submissionText, IFormFile? attachment)
        {
            try
            {
                var studentId = GetUserId();
                var assignment = await _context.AssignmentMasters.FindAsync(assignmentId);

                if (assignment == null) return NotFound();

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

        [Authorize]
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
