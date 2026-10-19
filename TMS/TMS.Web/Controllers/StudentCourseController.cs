using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.Common;
using TMS.Repository;
using TMS.Repository.Managers;
using TMS.ViewModels;
using TMS.ViewModels.Academics;
using TMS.ViewModels.Masters;
using TMS.Web.Services;

namespace TMS.Web.Controllers
{
    /// <summary>
    /// Student Course Controller — The Subject Hub.
    /// Handles: My Courses listing + Subject Details (Unit Grid).
    /// Part of the new Unit-first hierarchy.
    /// </summary>
    [Authorize]
    public class StudentCourseController : BaseController
    {
        private readonly IStudentLearningService _learningService;
        private readonly IMasterManager<CourseMasterViewModel> _courseManager;
        private readonly IMasterBaseManager<CourseEnrollmentViewModel> _enrollmentManager;
        private readonly IMasterBaseManager<CourseQuadrantViewModel> _quadrantManager;
        private readonly ApplicationDBContext _db;

        public StudentCourseController(
            IStudentLearningService learningService,
            IMasterManager<CourseMasterViewModel> courseManager,
            IMasterBaseManager<CourseEnrollmentViewModel> enrollmentManager,
            IMasterBaseManager<CourseQuadrantViewModel> quadrantManager,
            ApplicationDBContext db)
        {
            _learningService = learningService;
            _courseManager = courseManager;
            _enrollmentManager = enrollmentManager;
            _quadrantManager = quadrantManager;
            _db = db;
        }

        // ═══════════════════════════════════════════════════
        //  GET: Index — My Courses
        // ═══════════════════════════════════════════════════
        public async Task<IActionResult> Index()
        {
            int studentId = GetUserId();

            var enrollments = await _enrollmentManager.GetAsync(
                includes: new[] { "Course", "Course.CourseCategory", "Course.Semester" },
                predicate: e => e.StudentId == studentId && e.IsActive && (e.Status == 0 || e.Status == (int)EnrollmentStatus.Active)
            );

            var enrolledCourses = enrollments?
                .Select(e => e.Course)
                .Where(c => c != null)
                .ToList() ?? new List<CourseMasterViewModel>();

            if (!enrolledCourses.Any())
            {
                ViewBag.Message = "You are not enrolled in any courses.";
                return View(new StudentCourseCategoryViewModel());
            }

            var firstCategory = enrolledCourses.FirstOrDefault(c => c.CourseCategory != null)?.CourseCategory;
            var courseIds = enrolledCourses.Select(c => c.Id).Distinct().ToList();
            var progressMap = await _learningService.GetCoursesProgressMapAsync(studentId, courseIds);

            var viewModel = new StudentCourseCategoryViewModel
            {
                CategoryName = firstCategory?.Name ?? "No Category",
                CategoryDescription = firstCategory?.Description ?? "No description available.",
                Semesters = enrolledCourses
                    .GroupBy(c => c.Semester != null ? c.Semester.Id : (c.SemesterId ?? 0))
                    .OrderBy(g => g.Key)
                    .Select(g => new SemesterCourseGroupViewModel
                    {
                        Semester = g.Key,
                        Courses = g.Select(c => {
                            var stat = progressMap.TryGetValue(c.Id, out var s) ? s : (Total: 0, Completed: 0, Percent: 0);
                            return new CourseSummaryViewModel
                            {
                                CourseId = c.Id,
                                CourseCode = c.CourseCode ?? "N/A",
                                CourseName = c.Name ?? "Untitled Course",
                                CategoryId = c.CourseCategoryId,
                                TotalMaterials = stat.Total,
                                CompletedMaterials = stat.Completed,
                                ProgressPercent = stat.Percent
                            };
                        }).ToList()
                    }).ToList()
            };

            return View(viewModel);
        }

        // ═══════════════════════════════════════════════════
        //  GET: Details — Subject Hub (Unit Grid)
        // ═══════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Details(int courseId)
        {
            int studentId = GetUserId();

            // Security: Verify enrollment
            bool isStaff = User.IsInRole("Admin") || User.IsInRole("Super Admin") || User.IsInRole("Faculty") || User.IsInRole("Exam Coordinator");
            if (!isStaff)
            {
                bool enrolled = await _learningService.IsStudentEnrolledAsync(studentId, courseId);
                if (!enrolled)
                {
                    return Forbid();
                }
            }

            var model = await _learningService.GetSubjectHubAsync(studentId, courseId);
            if (model == null) return NotFound();

            return View(model);
        }
    }
}
