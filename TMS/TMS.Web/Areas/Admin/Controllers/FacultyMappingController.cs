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
            var courses = await _courseManager.GetAsync(null, c => c.IsActive);

            // Get existing mappings (only active ones for display count)
            var allMappings = await _mappingManager.GetAsync(new[] { "Course", "Faculty" }, m => m.IsActive);

            // Attach mapped faculty list to each course
            foreach (var course in courses)
            {
                course.MappedFaculties = allMappings
                    .Where(m => m.CourseId == course.Id)
                    .Select(m => m.Faculty)
                    .ToList();
            }

            return View(courses);
        }

        [HttpGet]
        public async Task<IActionResult> ManageMapping(int courseId)
        {
            var faculties = await _userManager.GetAsync(new[] { "Role" }, u => u.IsActive && u.Role!.Name == "Faculty");
            
            // Get existing active mappings
            var mappings = await _mappingManager.GetAsync(null, m => m.CourseId == courseId && m.IsActive);

            var selectedIds = mappings.Select(m => m.FacultyId).ToList();

            var viewModel = new CourseFacultyMapViewModel
            {
                CourseId = courseId,
                FacultyIds = selectedIds
            };

            ViewBag.CourseName = (await _courseManager.GetAsync(courseId))?.Name ?? "Unknown Course";
            ViewBag.FacultyList = faculties.Select(f => new SelectListItem
            {
                Value = f.Id.ToString(),
                Text = f.Name,
                Selected = selectedIds.Contains(f.Id)
            }).ToList();

            return PartialView("_ManageMapping", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SaveMapping(CourseFacultyMapViewModel model)
        {
            if (model.CourseId == 0)
                return Json(new { success = false, message = "Invalid Course ID." });

            // ✅ NEW: Smart Sync logic - No more hard deletes
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
                message = $"Faculty mappings updated. Added/Updated: {result.SuccessCount}, Removed: {result.DroppedCount}" 
            });
        }

        [HttpGet]
        public async Task<IActionResult> ViewMapping(int courseId)
        {
            // Show only active faculty mappings
            var mappings = await _mappingManager.GetAsync(new[] { "Faculty" }, m => m.CourseId == courseId && m.IsActive);
            ViewBag.CourseName = (await _courseManager.GetAsync(courseId))?.Name ?? "Unknown Course";
            return PartialView("_ViewMapping", mappings);
        }
    }
}
