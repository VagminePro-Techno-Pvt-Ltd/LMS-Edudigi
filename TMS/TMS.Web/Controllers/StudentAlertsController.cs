using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TMS.Web.Services;

namespace TMS.Web.Controllers
{
    [Authorize]
    public class StudentAlertsController : BaseController
    {
        private readonly IStudentAtRiskService _atRiskService;

        public StudentAlertsController(IStudentAtRiskService atRiskService)
        {
            _atRiskService = atRiskService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? filter = "All")
        {
            int studentId = GetUserId();
            var model = await _atRiskService.GetStudentAlertsAsync(studentId, filter ?? "All");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id, bool isRead = true)
        {
            int studentId = GetUserId();
            bool result = await _atRiskService.MarkAlertReadAsync(id, studentId, isRead);
            return Json(new { success = result });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            int studentId = GetUserId();
            bool result = await _atRiskService.MarkAllAlertsReadAsync(studentId);
            return Json(new { success = result });
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            int studentId = GetUserId();
            int count = await _atRiskService.GetStudentUnreadAlertsCountAsync(studentId);
            return Json(new { count });
        }
    }
}
