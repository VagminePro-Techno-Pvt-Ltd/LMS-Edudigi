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
        public async Task<IActionResult> Index(DateTime? date)
        {
            var role = User.Claims.FirstOrDefault(c => c.Type == "role")?.Value?.ToLower() ?? "";
            bool isStaff = role.Contains("admin") || role.Contains("faculty") || role.Contains("teacher") || role.Contains("management") || role.Contains("coordinator") || User.IsInRole("Admin") || User.IsInRole("Faculty");

            if (!isStaff)
            {
                return RedirectToAction(nameof(MyAttendance));
            }

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
        public async Task<IActionResult> UpdateStatus(int id, int studentId, DateTime date, int status, string? remarks)
        {
            var userId = Convert.ToInt32(User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value);
            DailyAttendanceViewModel? record = null;

            if (id > 0)
            {
                record = await _attendanceManager.GetAsync(id);
            }

            if (record == null)
            {
                record = new DailyAttendanceViewModel
                {
                    StudentId = studentId,
                    Date = date == default ? DateTime.Today : date.Date,
                    CreatedOn = DateTime.Now,
                    CreatedBy = userId
                };
            }

            record.AttendanceStatus = status;
            record.Remarks = remarks;
            if (status == 1 && record.LoginTime == null)
            {
                record.LoginTime = DateTime.Now;
            }

            var result = await _attendanceManager.AddUpdateAsync(record, userId);
            return Json(new { success = result });
        }

        /// <summary>
        /// Admin manual dispatch for nightly absence notifications.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Super Admin,super admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DispatchNightlyAbsenceEmails(DateTime? date)
        {
            var targetDate = date ?? DateTime.Today;
            using var scope = HttpContext.RequestServices.CreateScope();
            int queued = await BackgroundServices.AttendanceBackgroundJob.DispatchNightlyAbsenceEmailsAsync(scope, targetDate);
            return Json(new { success = true, queued, message = $"Queued {queued} absence email(s) for {targetDate:dd MMM yyyy}." });
        }

        /// <summary>
        /// Admin manual dispatch for weekly attendance shortage warnings.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Super Admin,super admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DispatchAttendanceShortageAlerts()
        {
            using var scope = HttpContext.RequestServices.CreateScope();
            int queued = await BackgroundServices.AttendanceBackgroundJob.DispatchWeeklyShortageAlertsAsync(scope, DateTime.Today);
            return Json(new { success = true, queued, message = $"Queued {queued} attendance shortage warning(s)." });
        }
    }
}

