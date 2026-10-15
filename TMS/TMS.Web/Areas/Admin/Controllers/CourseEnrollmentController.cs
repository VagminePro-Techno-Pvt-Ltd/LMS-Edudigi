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
        private readonly IAccountManager _accountManager;

        public CourseEnrollmentController(
            IEnrollmentManager enrollmentManager,
            IMasterManager<CourseMasterViewModel> courseManager,
            IMasterManager<UserViewModel> userManager,
            IMasterManager<CourseCategoryMasterViewModel> programManager,
            IAccountManager accountManager)
        {
            _enrollmentManager = enrollmentManager;
            _courseManager = courseManager;
            _userManager = userManager;
            _programManager = programManager;
            _accountManager = accountManager;
        }

        // =============================
        // GET: Index - Academic Enrollment Center Dashboard
        // =============================
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Academic Enrollment Center";

            // 1) Fetch all active Programs (CourseCategories)
            var programs = await _programManager.GetAsync(predicate: p => p.IsActive);

            // 2) Fetch all courses with Semester and Category
            var allCourses = await _courseManager.GetAsync(new[] { "Semester", "CourseCategory" }, c => c.IsActive);

            // 3) Fetch all active enrollments
            var allEnrollments = await _enrollmentManager.GetAsync(new[] { "Student" },
                e => e.Status == (int)EnrollmentStatus.Active);

            // 4) Fetch all students
            var allStudents = await _userManager.GetAsync(new[] { "Role" },
                u => u.IsActive && u.Role != null && u.Role.Name == "Student");

            // 5) Build summary per program
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
            }).OrderBy(p => p.ProgramName).ToList();

            // 6) Build summary per individual course
            var courseSummaries = allCourses.Select(c =>
            {
                var enrolledCount = allEnrollments.Count(e => e.CourseId == c.Id);
                return new CourseEnrollmentDetailVM
                {
                    CourseId = c.Id,
                    CourseCode = c.CourseCode ?? "N/A",
                    CourseName = c.Name,
                    ProgramId = c.CourseCategoryId,
                    ProgramName = c.CourseCategory?.Name ?? "General / Unassigned",
                    SemesterId = c.SemesterId,
                    SemesterName = c.Semester?.Name ?? "General",
                    EnrolledStudentCount = enrolledCount
                };
            }).OrderBy(c => c.ProgramName).ThenBy(c => c.CourseName).ToList();

            var dashboardVm = new AcademicEnrollmentDashboardVM
            {
                Programs = programSummaries,
                Courses = courseSummaries,
                TotalActiveEnrollments = allEnrollments.Count,
                TotalStudents = allStudents.Count()
            };

            return View(dashboardVm);
        }

        // =============================
        // GET: ManageMapping - Enroll students in a PROGRAM (all its courses)
        // =============================
        [HttpGet]
        public async Task<IActionResult> ManageMapping(int programId)
        {
            // 1) Fetch all students with "Student" role
            var students = (await _userManager.GetAsync(new[] { "Role" },
                u => u.IsActive && u.Role != null && u.Role.Name == "Student")).OrderBy(s => s.Name).ToList();

            // 2) Get all courses under this program
            var programCourses = await _courseManager.GetAsync(new[] { "Semester" }, c => c.IsActive && c.CourseCategoryId == programId);
            var programCourseIds = programCourses.Select(c => c.Id).ToList();

            // 3) Get ALL active enrollments across the system (including Course & Category)
            var allActiveEnrollments = await _enrollmentManager.GetAsync(new[] { "Course", "Course.CourseCategory" },
                e => e.Status == (int)EnrollmentStatus.Active);

            // A student is "enrolled in program" if enrolled in ANY course of the program
            var enrolledStudentIds = allActiveEnrollments
                .Where(e => programCourseIds.Contains(e.CourseId))
                .Select(e => e.StudentId)
                .Distinct()
                .ToList();

            // 4) Map each student to their enrolled courses/programs across the platform
            var studentEnrollmentInfo = students.ToDictionary(
                s => s.Id,
                s => allActiveEnrollments
                    .Where(e => e.StudentId == s.Id && e.Course != null)
                    .Select(e => new StudentEnrolledCourseBadgeVM
                    {
                        CourseId = e.CourseId,
                        CourseCode = e.Course?.CourseCode ?? "",
                        CourseName = e.Course?.Name ?? "",
                        ProgramId = e.Course?.CourseCategoryId ?? 0,
                        ProgramName = e.Course?.CourseCategory?.Name ?? "General"
                    }).ToList()
            );

            // 5) Fetch program info
            var program = await _programManager.GetAsync(programId);
            ViewBag.ProgramName = program?.Name ?? "Unknown Program";
            ViewBag.ProgramId = programId;
            ViewBag.CourseCount = programCourseIds.Count;
            ViewBag.ProgramCourses = programCourses;
            ViewBag.StudentEnrollments = studentEnrollmentInfo;

            // 6) Prepare select list for checkboxes
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
        // POST: EnrollSingleStudent - Enroll a student in a specific course
        // =============================
        [HttpPost]
        public async Task<IActionResult> EnrollSingleStudent(int courseId, int studentId)
        {
            var result = await _enrollmentManager.EnrollStudentAsync(courseId, studentId, GetUserId(), "Admin");
            return Json(new
            {
                success = result.Success,
                message = result.Message,
                wasReactivated = result.WasReactivated
            });
        }

        // =============================
        // POST: DropSingleStudent - Remove/Drop student from a specific course
        // =============================
        [HttpPost]
        public async Task<IActionResult> DropSingleStudent(int courseId, int studentId)
        {
            var success = await _enrollmentManager.DropStudentAsync(courseId, studentId, GetUserId());
            return Json(new
            {
                success,
                message = success ? "Student dropped from course successfully." : "Student was not actively enrolled."
            });
        }

        // =============================
        // GET: Studio - Unified Enrollment Studio Modal (Program or Course)
        // =============================
        [HttpGet]
        public async Task<IActionResult> Studio(int? programId, int? courseId)
        {
            var studioVm = new EnrollmentStudioVM();

            // Fetch all students with Student role
            var allStudents = (await _userManager.GetAsync(new[] { "Role" },
                u => u.IsActive && u.Role != null && u.Role.Name == "Student")).OrderBy(s => s.Name).ToList();
            studioVm.AllStudents = allStudents;

            // All active enrollments across system for student course mapping
            var allActiveEnrollments = await _enrollmentManager.GetAsync(new[] { "Course", "Course.CourseCategory" },
                e => e.Status == (int)EnrollmentStatus.Active);

            studioVm.StudentEnrollments = allStudents.ToDictionary(
                s => s.Id,
                s => allActiveEnrollments
                    .Where(e => e.StudentId == s.Id && e.Course != null)
                    .Select(e => new StudentEnrolledCourseBadgeVM
                    {
                        CourseId = e.CourseId,
                        CourseCode = e.Course?.CourseCode ?? "",
                        CourseName = e.Course?.Name ?? "",
                        ProgramId = e.Course?.CourseCategoryId ?? 0,
                        ProgramName = e.Course?.CourseCategory?.Name ?? "General"
                    }).ToList()
            );

            if (courseId.HasValue && courseId.Value > 0)
            {
                var course = await _courseManager.GetAsync(courseId.Value, new[] { "Semester", "CourseCategory" });
                if (course == null) return NotFound("Course not found.");

                studioVm.CourseId = course.Id;
                studioVm.ProgramId = course.CourseCategoryId;
                studioVm.Title = course.Name;
                studioVm.Subtitle = $"Code: {course.CourseCode} | Semester: {course.Semester?.Name ?? "General"} | Program: {course.CourseCategory?.Name ?? "General"}";
                studioVm.CategoryName = course.CourseCategory?.Name ?? "General";
                studioVm.ProgramCourses = new List<CourseMasterViewModel> { course };

                var enrollments = await _enrollmentManager.GetAsync(new[] { "Student" }, e => e.CourseId == course.Id);
                studioVm.ActiveEnrolledStudentIds = enrollments.Where(e => e.Status == (int)EnrollmentStatus.Active).Select(e => e.StudentId).ToList();

                studioVm.EnrolledStudents = enrollments.Select(e => new ProgramStudentEnrollmentVM
                {
                    StudentId = e.StudentId,
                    StudentName = e.Student?.Name ?? "Unknown",
                    StudentEmail = e.Student?.Email ?? "",
                    EnrolledOn = e.EnrolledOn,
                    ActiveCourseCount = e.Status == (int)EnrollmentStatus.Active ? 1 : 0,
                    TotalCourseCount = 1,
                    Status = e.Status,
                    Source = e.Source ?? "Admin"
                }).OrderByDescending(s => s.Status == 1).ThenBy(s => s.StudentName).ToList();
            }
            else if (programId.HasValue && programId.Value > 0)
            {
                var program = await _programManager.GetAsync(programId.Value);
                if (program == null) return NotFound("Program not found.");

                studioVm.ProgramId = program.Id;
                studioVm.Title = program.Name;
                studioVm.Subtitle = $"Degree Program | {program.NoOfSemester ?? 0} Semesters";
                studioVm.CategoryName = program.Name;
                studioVm.NoOfSemester = program.NoOfSemester ?? 0;

                var progCourses = (await _courseManager.GetAsync(new[] { "Semester" },
                    c => c.IsActive && c.CourseCategoryId == program.Id)).OrderBy(c => c.SemesterId).ThenBy(c => c.Name).ToList();
                studioVm.ProgramCourses = progCourses;
                var progCourseIds = progCourses.Select(c => c.Id).ToList();

                var enrollments = await _enrollmentManager.GetAsync(new[] { "Student", "Course" });
                var progEnrollments = enrollments.Where(e => progCourseIds.Contains(e.CourseId)).ToList();

                var activeStudents = progEnrollments.Where(e => e.Status == (int)EnrollmentStatus.Active).Select(e => e.StudentId).Distinct().ToList();
                studioVm.ActiveEnrolledStudentIds = activeStudents;

                studioVm.EnrolledStudents = progEnrollments
                    .GroupBy(e => e.StudentId)
                    .Select(g => new ProgramStudentEnrollmentVM
                    {
                        StudentId = g.Key,
                        StudentName = g.First().Student?.Name ?? "Unknown",
                        StudentEmail = g.First().Student?.Email ?? "",
                        EnrolledOn = g.Min(e => e.EnrolledOn),
                        ActiveCourseCount = g.Count(e => e.Status == (int)EnrollmentStatus.Active),
                        TotalCourseCount = progCourseIds.Count,
                        Status = g.All(e => e.Status == (int)EnrollmentStatus.Active) ? 1
                               : g.All(e => e.Status == (int)EnrollmentStatus.Dropped) ? 3
                               : g.All(e => e.Status == (int)EnrollmentStatus.Completed) ? 4
                               : 1,
                        Source = g.First().Source ?? "Admin"
                    })
                    .OrderByDescending(s => s.Status == 1)
                    .ThenBy(s => s.StudentName)
                    .ToList();
            }

            return PartialView("_EnrollmentStudio", studioVm);
        }

        // =============================
        // POST: SaveStudioMapping - AJAX batch enrollment save
        // =============================
        [HttpPost]
        public async Task<IActionResult> SaveStudioMapping([FromBody] StudioSaveRequest req)
        {
            if (req == null || req.StudentIds == null || !req.StudentIds.Any())
            {
                return Json(new { success = false, message = "Please select at least one student." });
            }

            var targetCourseIds = new List<int>();

            if (req.CourseId.HasValue && req.CourseId.Value > 0)
            {
                targetCourseIds.Add(req.CourseId.Value);
            }
            else if (req.ProgramId.HasValue && req.ProgramId.Value > 0)
            {
                if (req.SelectedCourseIds != null && req.SelectedCourseIds.Any())
                {
                    targetCourseIds.AddRange(req.SelectedCourseIds);
                }
                else
                {
                    var progCourses = await _courseManager.GetAsync(null, c => c.IsActive && c.CourseCategoryId == req.ProgramId.Value);
                    targetCourseIds.AddRange(progCourses.Select(c => c.Id));
                }
            }

            if (!targetCourseIds.Any())
            {
                return Json(new { success = false, message = "No courses identified for enrollment. Please add courses to this program first." });
            }

            int enrolledCount = 0;
            int reactivatedCount = 0;

            foreach (var cid in targetCourseIds)
            {
                foreach (var sid in req.StudentIds)
                {
                    var res = await _enrollmentManager.EnrollStudentAsync(cid, sid, GetUserId(), "Studio");
                    if (res.Success)
                    {
                        if (res.WasReactivated) reactivatedCount++;
                        else enrolledCount++;
                    }
                }
            }

            return Json(new
            {
                success = true,
                message = $"Successfully enrolled {req.StudentIds.Count} student(s) across {targetCourseIds.Count} course(s)!"
            });
        }

        // =============================
        // POST: QuickAddAndEnroll - Create new student & enroll immediately
        // =============================
        [HttpPost]
        public async Task<IActionResult> QuickAddAndEnroll([FromBody] QuickAddStudentEnrollVM req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Email))
            {
                return Json(new { success = false, message = "Name and Email are required." });
            }

            req.Email = req.Email.Trim();

            var existingUser = (await _userManager.GetAsync(null, u => u.Email == req.Email)).FirstOrDefault();
            int studentId;

            if (existingUser != null)
            {
                studentId = existingUser.Id;
            }
            else
            {
                var roles = await _accountManager.GetRolesByCompanyId(0, 0);
                var studentRole = roles.FirstOrDefault(r => r.Name != null && r.Name.Equals("Student", StringComparison.OrdinalIgnoreCase));
                int roleId = studentRole != null ? int.Parse(studentRole.Id) : 3;

                var newUser = new UserViewModel
                {
                    Name = req.Name.Trim(),
                    Email = req.Email,
                    ContactNo = req.ContactNo?.Trim(),
                    Password = string.IsNullOrWhiteSpace(req.Password) ? "Welcome@123" : req.Password.Trim(),
                    RoleId = roleId,
                    IsActive = true
                };

                var created = await _userManager.AddUpdateAsync(newUser, GetUserId());
                if (!created)
                {
                    return Json(new { success = false, message = "Failed to create student account." });
                }

                var createdUser = (await _userManager.GetAsync(null, u => u.Email == req.Email)).FirstOrDefault();
                if (createdUser == null)
                {
                    return Json(new { success = false, message = "Could not retrieve created student record." });
                }
                studentId = createdUser.Id;
            }

            var targetCourseIds = new HashSet<int>();
            if (req.CourseIds != null && req.CourseIds.Any())
            {
                foreach (var cid in req.CourseIds) targetCourseIds.Add(cid);
            }
            else if (req.ProgramId.HasValue && req.ProgramId.Value > 0)
            {
                var progCourses = await _courseManager.GetAsync(null, c => c.IsActive && c.CourseCategoryId == req.ProgramId.Value);
                foreach (var pc in progCourses) targetCourseIds.Add(pc.Id);
            }

            int enrolledCourses = 0;
            foreach (var cid in targetCourseIds)
            {
                var enrollRes = await _enrollmentManager.EnrollStudentAsync(cid, studentId, GetUserId(), "Quick Add");
                if (enrollRes.Success) enrolledCourses++;
            }

            return Json(new
            {
                success = true,
                message = $"Student '{req.Name}' created & enrolled into {enrolledCourses} course(s) successfully!"
            });
        }

        // =============================
        // POST: ReactivateStudent - Re-activate a dropped student
        // =============================
        [HttpPost]
        public async Task<IActionResult> ReactivateStudent(int? programId, int? courseId, int studentId)
        {
            var targetCourses = new List<int>();
            if (courseId.HasValue && courseId.Value > 0) targetCourses.Add(courseId.Value);
            else if (programId.HasValue && programId.Value > 0)
            {
                var pc = await _courseManager.GetAsync(null, c => c.IsActive && c.CourseCategoryId == programId.Value);
                targetCourses.AddRange(pc.Select(c => c.Id));
            }

            int reactivated = 0;
            foreach (var cid in targetCourses)
            {
                var res = await _enrollmentManager.EnrollStudentAsync(cid, studentId, GetUserId(), "Reactivation");
                if (res.Success) reactivated++;
            }

            return Json(new { success = reactivated > 0, message = $"Student reactivated in {reactivated} course(s)." });
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

    // ─── View Models for Enrollment Studio ───

    public class AcademicEnrollmentDashboardVM
    {
        public List<ProgramEnrollmentSummaryVM> Programs { get; set; } = new();
        public List<CourseEnrollmentDetailVM> Courses { get; set; } = new();
        public int TotalPrograms => Programs.Count;
        public int TotalCourses => Courses.Count;
        public int TotalActiveEnrollments { get; set; }
        public int TotalStudents { get; set; }
    }

    public class CourseEnrollmentDetailVM
    {
        public int CourseId { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public int ProgramId { get; set; }
        public string ProgramName { get; set; } = string.Empty;
        public int? SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        public int EnrolledStudentCount { get; set; }
    }

    public class EnrollmentStudioVM
    {
        public int? ProgramId { get; set; }
        public int? CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int NoOfSemester { get; set; }
        public List<CourseMasterViewModel> ProgramCourses { get; set; } = new();
        public List<ProgramStudentEnrollmentVM> EnrolledStudents { get; set; } = new();
        public List<UserViewModel> AllStudents { get; set; } = new();
        public List<int> ActiveEnrolledStudentIds { get; set; } = new();
        public Dictionary<int, List<StudentEnrolledCourseBadgeVM>> StudentEnrollments { get; set; } = new();
    }

    public class StudentEnrolledCourseBadgeVM
    {
        public int CourseId { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public int ProgramId { get; set; }
        public string ProgramName { get; set; } = string.Empty;
    }

    public class StudioSaveRequest
    {
        public int? ProgramId { get; set; }
        public int? CourseId { get; set; }
        public List<int> StudentIds { get; set; } = new();
        public List<int>? SelectedCourseIds { get; set; }
    }

    public class QuickAddStudentEnrollVM
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ContactNo { get; set; }
        public string? Password { get; set; }
        public int? ProgramId { get; set; }
        public List<int>? CourseIds { get; set; }
    }

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
