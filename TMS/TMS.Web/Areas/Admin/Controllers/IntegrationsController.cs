using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TMS.ViewModels.Integration;
using TMS.Web.Controllers;
using TMS.Web.Services.Integration;

namespace TMS.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Super Admin,Admin,SuperAdmin,super admin,Management")]
    public class IntegrationsController : BaseController
    {
        private readonly IIntegrationHealthService _healthService;
        private readonly ILogger<IntegrationsController> _logger;

        public IntegrationsController(
            IIntegrationHealthService healthService,
            ILogger<IntegrationsController> logger)
        {
            _healthService = healthService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            ViewData["Title"] = "Integration Hub — Platform Services & Connectors";

            try
            {
                var model = await _healthService.GetDashboardSummaryAsync(cancellationToken);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load Integration Hub summary.");
                TempData["ErrorMessage"] = "Failed to load integration status: " + ex.Message;
                return View(new IntegrationHubDashboardViewModel());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckHealth(string providerKey, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(providerKey))
            {
                TempData["ErrorMessage"] = "Provider key is required for health check.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var (success, message) = await _healthService.CheckProviderHealthAsync(providerKey, cancellationToken);
                if (success)
                {
                    TempData["SuccessMessage"] = $"[{providerKey}] Diagnostic Success: {message}";
                }
                else
                {
                    TempData["ErrorMessage"] = $"[{providerKey}] Diagnostic Warning: {message}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking health for provider {Provider}", providerKey);
                TempData["ErrorMessage"] = $"Diagnostic exception for {providerKey}: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> DiagnosticDetails(string providerKey, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(providerKey))
                return Json(new { success = false, message = "Provider key required" });

            try
            {
                var (success, message) = await _healthService.CheckProviderHealthAsync(providerKey, cancellationToken);
                return Json(new { success, message, checkedAt = DateTime.UtcNow.ToString("dd MMM yyyy, HH:mm:ss UTC") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
