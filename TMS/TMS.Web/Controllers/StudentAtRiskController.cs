using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TMS.ViewModels.Academics;
using TMS.Web.Services;

namespace TMS.Web.Controllers
{
    [Authorize]
    public class StudentAtRiskController : BaseController
    {
        private readonly IStudentAtRiskService _atRiskService;
        private readonly IContentModerationService _moderationService;
        private readonly ILogger<StudentAtRiskController>? _logger;

        public StudentAtRiskController(
            IStudentAtRiskService atRiskService,
            IContentModerationService moderationService,
            ILogger<StudentAtRiskController>? logger = null)
        {
            _atRiskService = atRiskService;
            _moderationService = moderationService;
            _logger = logger;
        }

        // =========================================================================
        // FACULTY DASHBOARD: Course activities, deadlines, student risk roster
        // =========================================================================

        [HttpGet]
        public async Task<IActionResult> FacultyDashboard(int? courseId, string? riskFilter = "All", string? search = null, string? activityType = "All")
        {
            var userId = GetUserId();
            var role = (GetUserRole() ?? "").ToLower().Trim();
            bool isStaff = User.IsInRole("Admin") || User.IsInRole("Super Admin") || role.Contains("admin") || User.IsInRole("Exam Coordinator");
            bool isFaculty = IsFacultyRole() || User.IsInRole("Faculty") || role.Contains("faculty");

            if (!isStaff && !isFaculty)
            {
                SetApplicationResult(false, "You are not authorized to access Faculty Student At-Risk.");
                return RedirectToAction("Index", "Home");
            }

            var assignedCourses = await _atRiskService.GetFacultyAssignedCoursesAsync(userId, isStaff);
            if (!assignedCourses.Any())
            {
                ViewBag.NoCourses = true;
                return View(new FacultyStudentRiskRosterViewModel());
            }

            int selectedCourseId = courseId ?? assignedCourses.First().Id;
            if (!assignedCourses.Any(c => c.Id == selectedCourseId))
            {
                selectedCourseId = assignedCourses.First().Id;
            }

            var model = await _atRiskService.GetFacultyStudentRiskRosterAsync(selectedCourseId, userId, isStaff, riskFilter ?? "All", search);
            ViewBag.SelectedActivityType = activityType ?? "All";
            ViewBag.AssignedCourses = assignedCourses;
            ViewBag.SelectedCourseId = selectedCourseId;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDeadline([FromBody] SaveDeadlineRequest request)
        {
            if (request == null || request.CourseId <= 0 || request.ActivityId <= 0 || string.IsNullOrWhiteSpace(request.ActivityType))
            {
                return Json(new { success = false, message = "Invalid deadline request parameters." });
            }

            var userId = GetUserId();
            var role = (GetUserRole() ?? "").ToLower().Trim();
            bool isStaff = User.IsInRole("Admin") || User.IsInRole("Super Admin") || role.Contains("admin");

            try
            {
                bool success = await _atRiskService.SaveActivityDeadlineAsync(request, userId, isStaff);
                if (success)
                {
                    return Json(new { success = true, message = "Activity deadline saved and student alerts updated." });
                }
                return Json(new { success = false, message = "Could not save deadline. Verify course authorization." });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error saving deadline");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetStudentDetails(int courseId, int studentId)
        {
            if (courseId <= 0 || studentId <= 0)
            {
                return BadRequest("Invalid course or student ID.");
            }

            var userId = GetUserId();
            var role = (GetUserRole() ?? "").ToLower().Trim();
            bool isStaff = User.IsInRole("Admin") || User.IsInRole("Super Admin") || role.Contains("admin");

            var detail = await _atRiskService.GetStudentRiskDetailAsync(courseId, studentId, userId, isStaff);
            if (detail == null)
            {
                return NotFound("Student details not found or access denied.");
            }

            return Json(new { success = true, data = detail });
        }

        // =========================================================================
        // ADMIN DASHBOARD: Institution-wide Learning Analytics & Moderation
        // =========================================================================

        [HttpGet]
        public async Task<IActionResult> AdminDashboard(int? programId, int? courseId, string? riskFilter = "All", string? search = null)
        {
            var role = (GetUserRole() ?? "").ToLower().Trim();
            bool isStaff = User.IsInRole("Admin") || User.IsInRole("Super Admin") || role.Contains("admin") || User.IsInRole("Exam Coordinator");

            if (!isStaff)
            {
                SetApplicationResult(false, "Access restricted to Administrator role.");
                return RedirectToAction("Index", "Home");
            }

            var model = await _atRiskService.GetAdminAnalyticsAsync(programId, courseId, riskFilter ?? "All", search);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewModeration([FromBody] ReviewModerationRequest request)
        {
            if (request == null || request.FlagId <= 0 || string.IsNullOrWhiteSpace(request.Decision))
            {
                return Json(new { success = false, message = "Invalid moderation review request." });
            }

            var userId = GetUserId();
            var role = (GetUserRole() ?? "").ToLower().Trim();
            bool isStaff = User.IsInRole("Admin") || User.IsInRole("Super Admin") || role.Contains("admin") || IsFacultyRole();

            if (!isStaff)
            {
                return Json(new { success = false, message = "Unauthorized to review moderation flags." });
            }

            bool result = await _moderationService.ReviewFlagAsync(request.FlagId, request.Decision, request.Remarks, userId);
            if (result)
            {
                return Json(new { success = true, message = $"Content flag successfully {request.Decision.ToLower()}ed." });
            }
            return Json(new { success = false, message = "Failed to update moderation flag." });
        }
    }
}
