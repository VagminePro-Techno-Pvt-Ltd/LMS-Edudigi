using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using TMS.Common;
using TMS.Repository.Managers;
using TMS.ViewModels;
using TMS.ViewModels.Masters;
using TMS.Web.Controllers;
using System.Globalization;
using ExcelDataReader;
using System.Data;

namespace TMS.Web.Areas.Admin.Controllers
{
    [ValidateFormAccess(Common.FormDefination.Enrollment)]
    [Area("Admin")]
    [Authorize]
    public class CourseEnrollmentController : BaseController
    {
        private readonly IEnrollmentManager _enrollmentManager;
        private readonly IMasterManager<CourseMasterViewModel> _courseManager;
        private readonly IMasterManager<UserViewModel> _userManager;
        private readonly IMasterManager<CourseCategoryMasterViewModel> _programManager;

        public CourseEnrollmentController(
            IEnrollmentManager enrollmentManager,
            IMasterManager<CourseMasterViewModel> courseManager,
            IMasterManager<UserViewModel> userManager,
            IMasterManager<CourseCategoryMasterViewModel> programManager)
        {
            _enrollmentManager = enrollmentManager;
            _courseManager = courseManager;
            _userManager = userManager;
            _programManager = programManager;
        }

        // =============================
        // GET: Index - Show all PROGRAMS with enrollment counts
        // =============================
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Program Enrollment Management";

            // 1) Fetch all active Programs (CourseCategories)
            var programs = await _programManager.GetAsync(predicate: p => p.IsActive);

            // 2) Fetch all courses (to count per-program and map enrollments)
            var allCourses = await _courseManager.GetAsync(new[] { "Semester", "CourseCategory" }, c => c.IsActive);

            // 3) Fetch all active enrollments
            var allEnrollments = await _enrollmentManager.GetAsync(new[] { "Student" },
                e => e.Status == (int)EnrollmentStatus.Active);

            // 4) Build summary per program
            var programSummaries = programs.Select(p =>
            {
                var programCourseIds = allCourses
                    .Where(c => c.CourseCategoryId == p.Id)
                    .Select(c => c.Id)
                    .ToList();

                var enrolledStudentIds = allEnrollments
                    .Where(e => programCourseIds.Contains(e.CourseId))
                    .Select(e => e.StudentId)
                    .Distinct()
                    .ToList();

                return new ProgramEnrollmentSummaryVM
                {
                    ProgramId = p.Id,
                    ProgramName = p.Name ?? "Unknown",
                    ProgramDescription = p.Description,
                    NoOfSemester = p.NoOfSemester ?? 0,
                    CourseCount = programCourseIds.Count,
                    EnrolledStudentCount = enrolledStudentIds.Count
                };
            }).ToList();

            return View(programSummaries);
        }

        // =============================
        // GET: ManageMapping - Enroll students in a PROGRAM (all its courses)
        // =============================
        [HttpGet]
        public async Task<IActionResult> ManageMapping(int programId)
        {
            // 1) Fetch all students with "Student" role
            var students = await _userManager.GetAsync(new[] { "Role" },
                u => u.IsActive && u.Role != null && u.Role.Name == "Student");

            // 2) Get all courses under this program
            var programCourses = await _courseManager.GetAsync(null, c => c.IsActive && c.CourseCategoryId == programId);
            var programCourseIds = programCourses.Select(c => c.Id).ToList();

            // 3) Get existing ACTIVE enrollments for any course in this program
            var existingEnrollments = await _enrollmentManager.GetAsync(null,
                e => e.Status == (int)EnrollmentStatus.Active);

            // A student is "enrolled in program" if enrolled in ALL courses of the program
            // For display, show anyone enrolled in ANY course of the program
            var enrolledStudentIds = existingEnrollments
                .Where(e => programCourseIds.Contains(e.CourseId))
                .Select(e => e.StudentId)
                .Distinct()
                .ToList();

            // 4) Fetch program info
            var program = await _programManager.GetAsync(programId);
            ViewBag.ProgramName = program?.Name ?? "Unknown Program";
            ViewBag.ProgramId = programId;
            ViewBag.CourseCount = programCourseIds.Count;

            // 5) Prepare select list for checkboxes
            ViewBag.StudentList = students.Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.Name,
                Selected = enrolledStudentIds.Contains(s.Id)
            }).ToList();

            ViewBag.StudentDetails = students;

            var viewModel = new CourseEnrollmentViewModel
            {
                CourseId = programId, // Reuse CourseId field to carry ProgramId
                SelectedStudentIds = enrolledStudentIds
            };

            return PartialView("_ManageMapping", viewModel);
        }

        // =============================
        // POST: SaveMapping - Enroll/Drop students for ALL courses in a PROGRAM
        // =============================
        [HttpPost]
        public async Task<IActionResult> SaveMapping(CourseEnrollmentViewModel model)
        {
            // model.CourseId here is actually ProgramId (reusing the field)
            int programId = model.CourseId;
            var selectedStudentIds = model.SelectedStudentIds ?? new List<int>();

            // 1) Get ALL active courses under this program
            var programCourses = await _courseManager.GetAsync(null,
                c => c.IsActive && c.CourseCategoryId == programId);

            if (!programCourses.Any())
            {
                SetApplicationResult(false, "No courses found under this program. Add courses first.");
                return RedirectToAction(nameof(Index));
            }

            // 2) For each course, run SaveMappingAsync (enroll/drop students)
            int totalEnrolled = 0, totalDropped = 0, totalReactivated = 0;
            var errors = new List<string>();

            foreach (var course in programCourses)
            {
                var result = await _enrollmentManager.SaveMappingAsync(
                    course.Id,
                    selectedStudentIds,
                    GetUserId(),
                    "Program"
                );

                totalEnrolled += result.SuccessCount;
                totalDropped += result.DroppedCount;
                totalReactivated += result.ReactivatedCount;
                if (result.Errors.Any())
                    errors.AddRange(result.Errors);
            }

            if (errors.Any())
            {
                SetApplicationResult(false, string.Join("; ", errors));
            }
            else
            {
                SetApplicationResult(true,
                    $"Program enrollment saved! {selectedStudentIds.Count} students × {programCourses.Count()} courses. " +
                    $"Enrolled: {totalEnrolled}, Dropped: {totalDropped}, Re-activated: {totalReactivated}.");
            }

            return RedirectToAction(nameof(Index));
        }

        // =============================
        // GET: ViewMapping - View enrolled students for a PROGRAM
        // =============================
        [HttpGet]
        public async Task<IActionResult> ViewMapping(int programId)
        {
            // Get all courses under program
            var programCourses = await _courseManager.GetAsync(new[] { "Semester" },
                c => c.IsActive && c.CourseCategoryId == programId);
            var programCourseIds = programCourses.Select(c => c.Id).ToList();

            // Get all enrollments for these courses
            var enrollments = await _enrollmentManager.GetAsync(
                new[] { "Student", "Course" });
            var programEnrollments = enrollments
                .Where(e => programCourseIds.Contains(e.CourseId))
                .ToList();

            // Group by student - show unique students with their status
            var studentGroups = programEnrollments
                .GroupBy(e => e.StudentId)
                .Select(g => new ProgramStudentEnrollmentVM
                {
                    StudentId = g.Key,
                    StudentName = g.First().Student?.Name ?? "Unknown",
                    StudentEmail = g.First().Student?.Email ?? "",
                    EnrolledOn = g.Min(e => e.EnrolledOn),
                    ActiveCourseCount = g.Count(e => e.Status == (int)EnrollmentStatus.Active),
                    TotalCourseCount = programCourseIds.Count,
                    Status = g.All(e => e.Status == (int)EnrollmentStatus.Active) ? 1
                           : g.All(e => e.Status == (int)EnrollmentStatus.Dropped) ? 3
                           : g.All(e => e.Status == (int)EnrollmentStatus.Completed) ? 4
                           : 1, // Partial = still active
                    Source = g.First().Source ?? "Admin"
                })
                .OrderByDescending(s => s.Status == 1) // Active first
                .ThenBy(s => s.StudentName)
                .ToList();

            var program = await _programManager.GetAsync(programId);
            ViewBag.ProgramName = program?.Name ?? "Unknown Program";
            ViewBag.ProgramId = programId;
            ViewBag.ProgramCourses = programCourses;

            return PartialView("_ViewMapping", studentGroups);
        }

        // =============================
        // POST: DropStudent - Drop from ALL courses in program
        // =============================
        [HttpPost]
        public async Task<IActionResult> DropStudent(int programId, int studentId)
        {
            var programCourses = await _courseManager.GetAsync(null,
                c => c.IsActive && c.CourseCategoryId == programId);

            int dropped = 0;
            foreach (var course in programCourses)
            {
                var success = await _enrollmentManager.DropStudentAsync(course.Id, studentId, GetUserId());
                if (success) dropped++;
            }

            return Json(new
            {
                success = dropped > 0,
                message = dropped > 0
                    ? $"Student dropped from {dropped} course(s) in this program."
                    : "Student was not actively enrolled."
            });
        }

        // =============================
        // POST: CompleteEnrollment - Mark completed for ALL courses in program
        // =============================
        [HttpPost]
        public async Task<IActionResult> CompleteEnrollment(int programId, int studentId)
        {
            var programCourses = await _courseManager.GetAsync(null,
                c => c.IsActive && c.CourseCategoryId == programId);

            int completed = 0;
            foreach (var course in programCourses)
            {
                var success = await _enrollmentManager.CompleteEnrollmentAsync(course.Id, studentId, GetUserId());
                if (success) completed++;
            }

            return Json(new
            {
                success = completed > 0,
                message = completed > 0
                    ? $"Enrollment completed for {completed} course(s)."
                    : "No active enrollments found."
            });
        }

        // =============================
        // GET: DownloadTemplate - Download sample CSV
        // =============================
        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            var csv = "Name,Email,Phone,CourseCode\n\"John Doe\",\"john@example.com\",\"9876543210\",\"MBA-101\"\n\"Jane Smith\",\"jane@test.com\",\"9123456789\",\"BBA-202\"";
            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", "Enrollment_Template.csv");
        }

        // =============================
        // GET: BulkEnroll - Show Excel upload page
        // =============================
        [HttpGet]
        public async Task<IActionResult> BulkEnroll(int? courseId)
        {
            ViewData["Title"] = "Mass Student Enrollment (Excel/CSV)";
            
            var courses = await _courseManager.GetAsync(null, c => c.IsActive);
            ViewBag.CourseList = new SelectList(courses, "Id", "Name", courseId);
            ViewBag.SelectedCourseId = courseId;

            return View();
        }

        // =============================
        // POST: BulkEnroll - Process Excel/CSV file
        // =============================
        [HttpPost]
        public async Task<IActionResult> BulkEnroll(int courseId, IFormFile bulkFile)
        {
            if (bulkFile == null || bulkFile.Length == 0)
            {
                SetApplicationResult(false, "Please upload a valid Excel or CSV file.");
                return RedirectToAction(nameof(BulkEnroll), new { courseId });
            }

            var rows = new List<StudentUploadRow>();
            var fileName = bulkFile.FileName;

            try
            {
                using (var stream = bulkFile.OpenReadStream())
                {
                    if (fileName.EndsWith(".csv"))
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            int line = 0;
                            string? content;
                            while ((content = await reader.ReadLineAsync()) is not null)
                            {
                                line++;
                                if (line == 1 || string.IsNullOrWhiteSpace(content)) continue;

                                var parts = content.Split(',');
                                rows.Add(new StudentUploadRow { 
                                    Name = parts.Length > 0 ? parts[0].Trim() : "", 
                                    Email = parts.Length > 1 ? parts[1].Trim() : "", 
                                    ContactNo = parts.Length > 2 ? parts[2].Trim() : null,
                                    CourseCode = parts.Length > 3 ? parts[3].Trim() : null
                                });
                            }
                        }
                    }
                    else // Excel
                    {
                        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                        using (var reader = ExcelReaderFactory.CreateReader(stream))
                        {
                            var resultDs = reader.AsDataSet(new ExcelDataSetConfiguration()
                            {
                                ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true }
                            });

                            var dt = resultDs.Tables[0];
                            foreach (DataRow dr in dt.Rows)
                            {
                                rows.Add(new StudentUploadRow {
                                    Name = dr[0]?.ToString()?.Trim() ?? "",
                                    Email = dr[1]?.ToString()?.Trim() ?? "",
                                    ContactNo = dr[2]?.ToString()?.Trim() ?? "",
                                    CourseCode = dr[3]?.ToString()?.Trim() ?? ""
                                });
                            }
                        }
                    }
                }

                if (!rows.Any())
                {
                    SetApplicationResult(false, "No data found in file.");
                    return RedirectToAction(nameof(BulkEnroll), new { courseId });
                }

                // Process the rows (Create students + Enroll)
                var result = await _enrollmentManager.BulkEnrollWithAutoCreateAsync(courseId, rows, GetUserId());

                ViewBag.BulkResult = result;
                var courses = await _courseManager.GetAsync(null, c => c.IsActive);
                ViewBag.CourseList = new SelectList(courses, "Id", "Name", courseId);
                
                SetApplicationResult(result.FailedCount == 0, $"Process Complete. Success: {result.SuccessCount}, Failed: {result.FailedCount}");
                
                return View();
            }
            catch (Exception ex)
            {
                SetApplicationResult(false, "Error processing file: " + ex.Message);
                return RedirectToAction(nameof(BulkEnroll), new { courseId });
            }
        }
    }

    // ─── View Models for Program Enrollment ───

    public class ProgramEnrollmentSummaryVM
    {
        public int ProgramId { get; set; }
        public string ProgramName { get; set; } = string.Empty;
        public string? ProgramDescription { get; set; }
        public int NoOfSemester { get; set; }
        public int CourseCount { get; set; }
        public int EnrolledStudentCount { get; set; }
    }

    public class ProgramStudentEnrollmentVM
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public DateTime EnrolledOn { get; set; }
        public int ActiveCourseCount { get; set; }
        public int TotalCourseCount { get; set; }
        public int Status { get; set; }
        public string Source { get; set; } = "Admin";

        public string StatusText => Status switch
        {
            1 => "Active",
            2 => "Pending",
            3 => "Dropped",
            4 => "Completed",
            _ => "Unknown"
        };

        public string StatusBadgeClass => Status switch
        {
            1 => "bg-success",
            2 => "bg-warning text-dark",
            3 => "bg-danger",
            4 => "bg-info",
            _ => "bg-dark"
        };
    }
}
