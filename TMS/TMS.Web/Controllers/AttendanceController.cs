using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TMS.Repository.Managers;
using TMS.ViewModels.Academics;

namespace TMS.Web.Controllers
{
    [Authorize]
    public class AttendanceController : Controller
    {
        private readonly IDailyAttendanceManager _attendanceManager;

        public AttendanceController(IDailyAttendanceManager attendanceManager)
        {
            _attendanceManager = attendanceManager;
        }

        /// <summary>
        /// Admin/Faculty view for daily attendance records.
        /// </summary>
        [Authorize(Roles = "Admin,Super Admin,Faculty")]
        public async Task<IActionResult> Index(DateTime? date)
        {
            var selectedDate = date ?? DateTime.Today;
            var records = await _attendanceManager.GetAttendanceReport(selectedDate);
            ViewBag.SelectedDate = selectedDate;
            return View(records);
        }

        /// <summary>
        /// Student's personal attendance log.
        /// </summary>
        public async Task<IActionResult> MyAttendance()
        {
            var userId = Convert.ToInt32(User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value);
            var records = await _attendanceManager.GetAsync(predicate: t => t.StudentId == userId);
            return View(records.OrderByDescending(t => t.Date).ToList());
        }

        /// <summary>
        /// Override attendance status (Admin/Faculty only).
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Super Admin,Faculty")]
        public async Task<IActionResult> UpdateStatus(int id, int status, string remarks)
        {
            var userId = Convert.ToInt32(User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value);
            var record = await _attendanceManager.GetAsync(id);
            if (record == null) return NotFound();

            record.AttendanceStatus = status;
            record.Remarks = remarks;
            
            var result = await _attendanceManager.AddUpdateAsync(record, userId);
            return Json(new { success = result });
        }
    }
}
