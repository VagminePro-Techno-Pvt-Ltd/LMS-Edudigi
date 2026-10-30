using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TMS.Web.Controllers;
using TMS.Web.Services.ContentSync;

namespace TMS.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Super Admin,Admin,SuperAdmin,super admin,Management")]
    public class ContentSyncController : BaseController
    {
        private readonly IContentFolderProvisioner _provisioner;
        private readonly LocalContentStorageProvider _localProvider;
        private readonly GoogleDriveContentStorageProvider _googleDriveProvider;
        private readonly ILogger<ContentSyncController> _logger;

        public ContentSyncController(
            IContentFolderProvisioner provisioner,
            LocalContentStorageProvider localProvider,
            GoogleDriveContentStorageProvider googleDriveProvider,
            ILogger<ContentSyncController> logger)
        {
            _provisioner = provisioner;
            _localProvider = localProvider;
            _googleDriveProvider = googleDriveProvider;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Auto Content Sync & Cloud Storage Hub";

            var summary = await _provisioner.GetStatusSummaryAsync();
            var localTree = await _provisioner.GetFolderHierarchyAsync("LocalDrive");
            var gdriveTree = await _provisioner.GetFolderHierarchyAsync("GoogleDrive");

            ViewBag.LocalTree = localTree;
            ViewBag.GDriveTree = gdriveTree;

            return View(summary);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateConfig(
            bool localDriveEnabled,
            string localDriveRootPath,
            bool googleDriveEnabled,
            bool googleDriveNetworkDisabled)
        {
            try
            {
                await _provisioner.UpdateProviderConfigAsync(
                    localDriveEnabled,
                    localDriveRootPath,
                    googleDriveEnabled,
                    googleDriveNetworkDisabled);

                TempData["SuccessMessage"] = "Storage configuration saved successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update storage configuration: {Message}", ex.Message);
                TempData["ErrorMessage"] = $"Failed to save storage settings: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RunSyncNow()
        {
            try
            {
                await _provisioner.ProcessPendingJobsAsync();
                TempData["SuccessMessage"] = "Pending provisioning jobs processed successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running provisioning jobs: {Message}", ex.Message);
                TempData["ErrorMessage"] = $"Error running sync: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RetryFailed()
        {
            try
            {
                await _provisioner.RetryFailedJobsAsync();
                TempData["SuccessMessage"] = "Failed jobs re-queued and executed.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying failed jobs: {Message}", ex.Message);
                TempData["ErrorMessage"] = $"Error retrying jobs: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecheckHierarchy()
        {
            try
            {
                await _provisioner.TriggerFullHierarchyRecheckAsync();
                TempData["SuccessMessage"] = "Full academic hierarchy re-checked and missing folders provisioned.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rechecking hierarchy: {Message}", ex.Message);
                TempData["ErrorMessage"] = $"Error during hierarchy check: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScanDriveToLms()
        {
            try
            {
                await _provisioner.SyncDriveFilesToLmsAsync();
                TempData["SuccessMessage"] = "Drive scanned and detected content synchronized into LMS.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning drive: {Message}", ex.Message);
                TempData["ErrorMessage"] = $"Drive scan error: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> TestConnection(string providerKey)
        {
            bool online = false;
            string message;

            if (string.Equals(providerKey, "LocalDrive", StringComparison.OrdinalIgnoreCase))
            {
                online = await _localProvider.TestConnectionAsync();
                message = online ? "Local/UNC drive storage is reachable and write-verified." : "Local/UNC drive path could not be accessed.";
            }
            else
            {
                online = await _googleDriveProvider.TestConnectionAsync();
                message = online ? "Google Drive service connection successful." : "Google Drive service unreachable (Offline / Disconnected).";
            }

            return Json(new { success = online, message });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleNetworkAccess(bool online)
        {
            _googleDriveProvider.SetNetworkAccess(online);
            await _provisioner.UpdateProviderConfigAsync(
                _localProvider.IsEnabled,
                null!,
                _googleDriveProvider.IsEnabled,
                !online);

            return Json(new
            {
                success = true,
                online = online,
                statusText = online ? "Online" : "Simulated Offline (Network Disconnected)",
                message = online ? "Google Drive network access restored." : "Google Drive network access temporarily disconnected."
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetHierarchy(string providerKey)
        {
            var tree = await _provisioner.GetFolderHierarchyAsync(providerKey);
            return Json(tree);
        }
    }
}
