using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using TMS.Repository.Managers;
using TMS.ViewModels;
using TMS.ViewModels.Masters;
using TMS.Web.Controllers;

namespace TMS.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class FacultyMappingController : BaseController
    {
        private readonly IFacultyMappingManager _mappingManager;
        private readonly IMasterManager<CourseMasterViewModel> _courseManager;
        private readonly IMasterManager<UserViewModel> _userManager;

        public FacultyMappingController(
            IFacultyMappingManager mappingManager,
            IMasterManager<CourseMasterViewModel> courseManager,
            IMasterManager<UserViewModel> userManager)
        {
            _mappingManager = mappingManager;
            _courseManager = courseManager;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Faculty Course Mapping";
            var courses = await _courseManager.GetAsync(new[] { "CourseCategory", "Semester" }, c => c.IsActive);

            // Get existing active mappings
            var allMappings = await _mappingManager.GetAsync(new[] { "Course", "Faculty" }, m => m.IsActive);

            // Attach mapped faculty list to each course
            foreach (var course in courses)
            {
                course.MappedFaculties = allMappings
                    .Where(m => m.CourseId == course.Id && m.Faculty != null)
                    .Select(m => m.Faculty!)
                    .ToList();
            }

            return View(courses);
        }

        [HttpGet]
        public async Task<IActionResult> ManageMapping(int courseId)
        {
            if (courseId <= 0)
                return BadRequest("Invalid Course ID.");

            var allUsers = await _userManager.GetAsync(new[] { "Role" }, u => u.IsActive);
            var faculties = allUsers
                .Where(u => u.Role != null && (
                    u.Role.Name.Contains("Faculty", StringComparison.OrdinalIgnoreCase) ||
                    u.Role.Name.Contains("Teacher", StringComparison.OrdinalIgnoreCase) ||
                    u.Role.Name.Contains("Instructor", StringComparison.OrdinalIgnoreCase) ||
                    u.Role.Name.Contains("Trainer", StringComparison.OrdinalIgnoreCase) ||
                    u.Role.Name.Contains("Admin", StringComparison.OrdinalIgnoreCase)))
                .OrderBy(u => u.Name)
                .ToList();

            if (!faculties.Any())
            {
                faculties = allUsers.OrderBy(u => u.Name).ToList();
            }
            
            // Get existing active mappings
            var mappings = await _mappingManager.GetAsync(null, m => m.CourseId == courseId && m.IsActive);
            var selectedIds = mappings.Select(m => m.FacultyId).ToList();

            var viewModel = new CourseFacultyMapViewModel
            {
                CourseId = courseId,
                FacultyIds = selectedIds
            };

            var course = await _courseManager.GetAsync(courseId);
            ViewBag.CourseName = course?.Name ?? "Selected Course";
            ViewBag.CourseCode = course?.CourseCode ?? "";
            ViewBag.FacultyList = faculties.Select(f => new SelectListItem
            {
                Value = f.Id.ToString(),
                Text = $"{f.Name} ({f.Email})",
                Selected = selectedIds.Contains(f.Id)
            }).ToList();

            return PartialView("_ManageMapping", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SaveMapping(CourseFacultyMapViewModel model)
        {
            if (model.CourseId <= 0)
                return Json(new { success = false, message = "Invalid Course selected." });

            var result = await _mappingManager.SaveMappingAsync(
                model.CourseId, 
                model.FacultyIds ?? new List<int>(), 
                GetUserId()
            );

            if (result.Errors.Any())
            {
                return Json(new { success = false, message = string.Join("; ", result.Errors) });
            }

            return Json(new { 
                success = true, 
                message = $"Faculty mappings updated successfully ({result.SuccessCount} assigned, {result.DroppedCount} unassigned)." 
            });
        }

        [HttpGet]
        public async Task<IActionResult> ViewMapping(int courseId)
        {
            var mappings = await _mappingManager.GetAsync(new[] { "Faculty" }, m => m.CourseId == courseId && m.IsActive);
            var course = await _courseManager.GetAsync(courseId);
            ViewBag.CourseId = courseId;
            ViewBag.CourseName = course?.Name ?? "Selected Course";
            ViewBag.CourseCode = course?.CourseCode ?? "";
            return PartialView("_ViewMapping", mappings);
        }

        [HttpPost]
        public async Task<IActionResult> UnassignFaculty(int courseId, int facultyId)
        {
            if (courseId <= 0 || facultyId <= 0)
                return Json(new { success = false, message = "Invalid parameters." });

            var success = await _mappingManager.UnassignFacultyAsync(courseId, facultyId, GetUserId());

            return Json(new { 
                success = success, 
                message = success ? "Instructor successfully unassigned." : "Failed to unassign instructor."
            });
        }

        [HttpPost]
        public async Task<IActionResult> UnassignAll(int courseId)
        {
            if (courseId <= 0)
                return Json(new { success = false, message = "Invalid Course ID." });

            var count = await _mappingManager.UnassignAllAsync(courseId, GetUserId());

            return Json(new { 
                success = true, 
                message = $"All instructors ({count}) unassigned successfully."
            });
        }
    }
}


