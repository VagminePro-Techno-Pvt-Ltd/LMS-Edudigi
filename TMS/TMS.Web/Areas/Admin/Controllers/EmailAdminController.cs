using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TMS.Repository;
using TMS.ViewModels.Email;
using TMS.Web.Controllers;
using TMS.Web.Services.Email;

namespace TMS.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Super Admin,Admin,SuperAdmin,super admin,Management")]
    public class EmailAdminController : BaseController
    {
        private readonly ApplicationDBContext _db;
        private readonly IEmailSender _emailSender;
        private readonly IOptionsMonitor<EmailSettings> _optionsMonitor;
        private readonly ILogger<EmailAdminController> _logger;

        public EmailAdminController(
            ApplicationDBContext db,
            IEmailSender emailSender,
            IOptionsMonitor<EmailSettings> optionsMonitor,
            ILogger<EmailAdminController> logger)
        {
            _db = db;
            _emailSender = emailSender;
            _optionsMonitor = optionsMonitor;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Automated Email Infrastructure & Outbox";

            var settings = _optionsMonitor.CurrentValue;
            var (isValid, validationError) = settings.ValidateConfiguration();

            ViewBag.Settings = settings;
            ViewBag.IsValidConfig = isValid;
            ViewBag.ValidationError = validationError;

            // Safe mask for user
            ViewBag.MaskedUser = !string.IsNullOrWhiteSpace(settings.UserName)
                ? (settings.UserName.Length > 4 ? settings.UserName.Substring(0, 3) + "***@" + settings.UserName.Split('@').LastOrDefault() : "***")
                : "Not Configured";

            // Outbox counts
            var counts = await _db.EmailOutboxJobs
                .GroupBy(j => j.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            ViewBag.PendingCount = counts.GetValueOrDefault("Pending", 0);
            ViewBag.ProcessingCount = counts.GetValueOrDefault("Processing", 0);
            ViewBag.SentCount = counts.GetValueOrDefault("Sent", 0);
            ViewBag.FailedCount = counts.GetValueOrDefault("Failed", 0);
            ViewBag.CancelledCount = counts.GetValueOrDefault("Cancelled", 0);

            // Recent 50 jobs
            var recentJobs = await _db.EmailOutboxJobs
                .OrderByDescending(j => j.Id)
                .Take(50)
                .ToListAsync();

            return View(recentJobs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendTestEmail(string testRecipient)
        {
            if (string.IsNullOrWhiteSpace(testRecipient))
            {
                TempData["ErrorMessage"] = "Please provide a valid test recipient email address.";
                return RedirectToAction(nameof(Index));
            }

            var settings = _optionsMonitor.CurrentValue;
            if (!settings.IsEnabled)
            {
                TempData["ErrorMessage"] = "Email dispatch is currently disabled (IsEnabled = false). Enable it in configuration before sending test emails.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var result = await _emailSender.SendTestEmailAsync(testRecipient.Trim());
                if (result.Success)
                {
                    TempData["SuccessMessage"] = $"Test email successfully accepted by SMTP server for {testRecipient} in {result.DurationMs} ms!";
                }
                else
                {
                    TempData["ErrorMessage"] = $"SMTP test dispatch failed: {result.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin test email dispatch exception");
                TempData["ErrorMessage"] = $"Exception during test email dispatch: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RetryJob(int id)
        {
            var job = await _db.EmailOutboxJobs.FindAsync(id);
            if (job == null)
            {
                TempData["ErrorMessage"] = "Job not found.";
                return RedirectToAction(nameof(Index));
            }

            job.Status = "Pending";
            job.AttemptCount = 0;
            job.NextAttemptAtUtc = DateTime.UtcNow;
            job.LockedUntilUtc = null;
            job.LastError = null;

            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Job #{id} reset to Pending for immediate retry.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelJob(int id)
        {
            var job = await _db.EmailOutboxJobs.FindAsync(id);
            if (job == null)
            {
                TempData["ErrorMessage"] = "Job not found.";
                return RedirectToAction(nameof(Index));
            }

            job.Status = "Cancelled";
            job.LastError = "Cancelled by administrator";
            job.LockedUntilUtc = null;

            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Job #{id} cancelled.";
            return RedirectToAction(nameof(Index));
        }
    }
}
