using Microsoft.AspNetCore.Mvc;
using TMS.Web.Services;

namespace TMS.Web.Controllers
{
    /// <summary>
    /// Student Quadrant Controller — Quadrant Content List.
    /// Displays the filtered material list for a specific unit + quadrant.
    /// Part of the new Unit-first hierarchy.
    /// </summary>
    public class StudentQuadrantController : BaseController
    {
        private readonly IStudentLearningService _learningService;

        public StudentQuadrantController(IStudentLearningService learningService)
        {
            _learningService = learningService;
        }

        // ═══════════════════════════════════════════════════
        //  GET: Content — Material List
        // ═══════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Content(int courseId, int unitId, int quadrantId)
        {
            int studentId = GetUserId();

            // Security: Verify enrollment
            bool enrolled = await _learningService.IsStudentEnrolledAsync(studentId, courseId);
            if (!enrolled)
            {
                return RedirectToAction("Index", "StudentCourse");
            }

            var model = await _learningService.GetQuadrantContentAsync(studentId, courseId, unitId, quadrantId);
            if (model == null) return NotFound();

            return View(model);
        }
    }
}
