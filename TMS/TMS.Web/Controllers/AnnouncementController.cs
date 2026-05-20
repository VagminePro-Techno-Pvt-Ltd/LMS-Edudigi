using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TMS.Repository.Managers;
using TMS.ViewModels.Academics;
using TMS.ViewModels.Account;
using TMS.ViewModels.Masters;

namespace TMS.Web.Controllers
{
    [Authorize]
    public class AnnouncementController : Controller
    {
        private readonly IAnnouncementManager _announcementManager;
        private readonly IMasterManager<CourseMasterViewModel> _courseManager;
        private readonly IMasterManager<RoleMasterViewModel> _roleManager;
        private readonly IMasterManager<DepartmentMasterViewModel> _departmentManager;
        private readonly IMasterBaseManager<CourseEnrollmentViewModel> _enrollmentManager;

        public AnnouncementController(
            IAnnouncementManager announcementManager,
            IMasterManager<CourseMasterViewModel> courseManager,
            IMasterManager<RoleMasterViewModel> roleManager,
            IMasterManager<DepartmentMasterViewModel> departmentManager,
            IMasterBaseManager<CourseEnrollmentViewModel> enrollmentManager)
        {
            _announcementManager = announcementManager;
            _courseManager = courseManager;
            _roleManager = roleManager;
            _departmentManager = departmentManager;
            _enrollmentManager = enrollmentManager;
        }

        /// <summary>
        /// Admin/Faculty management view — CRUD for announcements.
        /// </summary>
        [Authorize(Roles = "Admin,Super Admin,Faculty")]
        public async Task<IActionResult> Index()
        {
            var announcements = await _announcementManager.GetAllForManagementAsync();
            return View(announcements);
        }

        /// <summary>
        /// Student/All roles — view published announcements.
        /// </summary>
        public async Task<IActionResult> Board()
        {
            var userId = GetUserId();
            var roleId = GetRoleId();
            
            List<int>? courseIds = null;
            if (roleId.HasValue) // Only fetch enrollments if user is logged in
            {
                var enrollments = await _enrollmentManager.GetAsync(
                    predicate: e => e.Status == 1 && e.StudentId == userId
                );
                courseIds = enrollments?.Select(e => e.CourseId).Distinct().ToList();
            }

            var announcements = await _announcementManager.GetVisibleAnnouncementsAsync(userId, roleId, courseIds);
            return View(announcements);
        }

        /// <summary>
        /// Create announcement form.
        /// </summary>
        [Authorize(Roles = "Admin,Super Admin,Faculty")]
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            var model = new AnnouncementViewModel
            {
                PublishDate = DateTime.Now,
                IsActive = true
            };
            return View(model);
        }

        /// <summary>
        /// Save new announcement.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Super Admin,Faculty")]
        public async Task<IActionResult> Create(AnnouncementViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns();
                return View(model);
            }

            var userId = GetUserId();
            model.IsActive = true;
            var result = await _announcementManager.AddUpdateAsync(model, userId);

            if (result)
            {
                TempData["SuccessMessage"] = "Announcement published successfully!";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "Failed to create announcement.";
            await PopulateDropdowns();
            return View(model);
        }

        /// <summary>
        /// Edit announcement form.
        /// </summary>
        [Authorize(Roles = "Admin,Super Admin,Faculty")]
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _announcementManager.GetAsync(id);
            if (model == null) return NotFound();

            await PopulateDropdowns();
            return View(model);
        }

        /// <summary>
        /// Save edited announcement.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Super Admin,Faculty")]
        public async Task<IActionResult> Edit(AnnouncementViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns();
                return View(model);
            }

            var userId = GetUserId();
            var result = await _announcementManager.AddUpdateAsync(model, userId);

            if (result)
            {
                TempData["SuccessMessage"] = "Announcement updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "Failed to update announcement.";
            await PopulateDropdowns();
            return View(model);
        }

        /// <summary>
        /// Soft-delete an announcement via AJAX.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Super Admin,Faculty")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();
            var result = await _announcementManager.SoftDeleteAsync(id, userId);
            return Json(new { success = result });
        }

        /// <summary>
        /// Toggle pin status via AJAX.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Super Admin,Faculty")]
        public async Task<IActionResult> TogglePin(int id)
        {
            var userId = GetUserId();
            var result = await _announcementManager.TogglePinAsync(id, userId);
            return Json(new { success = result });
        }

        /// <summary>
        /// Get target items (roles/courses/departments) by type for dynamic dropdown.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTargetItems(int targetType)
        {
            var items = new List<SelectListItem>();

            switch (targetType)
            {
                case 1: // Roles
                    var roles = await _roleManager.GetAsync();
                    items = roles.Select(r => new SelectListItem { Value = r.Id.ToString(), Text = r.Name }).ToList();
                    break;
                case 2: // Courses
                    var courses = await _courseManager.GetAsync();
                    items = courses.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToList();
                    break;
                case 3: // Departments
                    var depts = await _departmentManager.GetAsync();
                    items = depts.Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name }).ToList();
                    break;
            }

            return Json(items);
        }

        /// <summary>
        /// Emergency fix for SqlException if Announcements table is missing.
        /// </summary>
        [Authorize(Roles = "Admin,Super Admin")]
        public async Task<IActionResult> FixDatabase()
        {
            var result = await _announcementManager.FixDatabaseAsync();
            if (result) return Content("Database table 'Announcements' created or verified successfully.");
            return Content("Failed to create Announcements table. Check server logs.");
        }

        // ─── Helper Methods ───

        private int GetUserId()
        {
            return Convert.ToInt32(User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value ?? "0");
        }

        private int? GetRoleId()
        {
            var roleIdStr = User.Claims.FirstOrDefault(c => c.Type == "roleId")?.Value;
            if (!string.IsNullOrEmpty(roleIdStr)) return Convert.ToInt32(roleIdStr);
            
            var roleName = User.Claims.FirstOrDefault(c => c.Type == "role" || c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(roleName)) return null;

            var roles = _roleManager.GetAsync(predicate: r => r.Name != null && r.Name.ToLower() == roleName.ToLower()).Result;
            return roles.FirstOrDefault()?.Id;
        }

        private async Task PopulateDropdowns()
        {
            var roles = await _roleManager.GetAsync();
            var courses = await _courseManager.GetAsync();
            var depts = await _departmentManager.GetAsync();

            ViewBag.Roles = new SelectList(roles, "Id", "Name");
            ViewBag.Courses = new SelectList(courses, "Id", "Name");
            ViewBag.Departments = new SelectList(depts, "Id", "Name");
        }
    }
}
