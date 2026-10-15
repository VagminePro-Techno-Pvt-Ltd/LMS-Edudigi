using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TMS.Web.Services;

namespace TMS.Web.Controllers
{
    /// <summary>
    /// Student Unit Controller — Unit Details Hub.
    /// Displays Unit info + 4 Quadrant Cards with material counts.
    /// Part of the new Unit-first hierarchy.
    /// </summary>
    [Authorize]
    public class StudentUnitController : BaseController
    {
        private readonly IStudentLearningService _learningService;

        public StudentUnitController(IStudentLearningService learningService)
        {
            _learningService = learningService;
        }

        // ═══════════════════════════════════════════════════
        //  GET: Details — Unit Hub (4 Quadrant Cards)
        // ═══════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Details(int courseId, int unitId)
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

            var model = await _learningService.GetUnitDetailsAsync(studentId, courseId, unitId);
            if (model == null) return NotFound();

            return View(model);
        }
    }
}
