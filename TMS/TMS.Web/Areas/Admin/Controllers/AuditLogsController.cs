using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TMS.ViewModels.Audit;
using TMS.Web.Controllers;
using TMS.Web.Services;

namespace TMS.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class AuditLogsController : BaseController
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogsController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        private bool IsAdminUser()
        {
            var role = (GetUserRole() ?? "").ToLower().Trim();
            return User.IsInRole("Admin") ||
                   User.IsInRole("Super Admin") ||
                   User.IsInRole("super admin") ||
                   User.IsInRole("Management") ||
                   role == "admin" ||
                   role == "super admin" ||
                   role == "management";
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] AuditLogFilterViewModel filter)
        {
            if (!IsAdminUser())
            {
                return Forbid();
            }

            var model = await _auditLogService.GetLogsAsync(filter);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (!IsAdminUser())
            {
                return Forbid();
            }

            var detail = await _auditLogService.GetLogByIdAsync(id);
            if (detail == null)
            {
                return NotFound(new { success = false, message = "Audit log not found." });
            }

            return Json(new { success = true, data = detail });
        }

        [HttpGet]
        public async Task<IActionResult> ExportCsv([FromQuery] AuditLogFilterViewModel filter)
        {
            if (!IsAdminUser())
            {
                return Forbid();
            }

            var csvBytes = await _auditLogService.ExportCsvAsync(filter);
            var filename = $"AuditLogs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
            return File(csvBytes, "text/csv; charset=utf-8", filename);
        }
    }
}

