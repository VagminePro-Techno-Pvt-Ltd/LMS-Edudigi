using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TMS.Models.Email;
using TMS.Repository;
using TMS.ViewModels.Email;

namespace TMS.Web.Services.Email
{
    public class EmailOutboxService : IEmailOutboxService
    {
        private readonly ApplicationDBContext _db;
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailOutboxService> _logger;

        public EmailOutboxService(
            ApplicationDBContext db,
            IOptions<EmailSettings> options,
            ILogger<EmailOutboxService> logger)
        {
            _db = db;
            _settings = options.Value;
            _logger = logger;
        }

        public async Task<EmailOutboxJob?> EnqueueAsync(EmailEnqueueRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.RecipientEmail))
            {
                _logger.LogWarning("Email enqueue ignored: recipient email is missing.");
                return null;
            }

            // Check idempotency if a key is provided
            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                var existing = await _db.EmailOutboxJobs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(j => j.IdempotencyKey == request.IdempotencyKey && j.Status != "Cancelled", cancellationToken);

                if (existing != null)
                {
                    _logger.LogDebug("Idempotent email enqueue skipped: job with key {Key} already exists (ID: {Id}, Status: {Status})", 
                        request.IdempotencyKey, existing.Id, existing.Status);
                    return existing;
                }
            }

            // Clean payload to ensure no passwords or credentials are stored
            var safePayload = new Dictionary<string, string>(request.TemplateData, StringComparer.OrdinalIgnoreCase);
            if (safePayload.ContainsKey("Password")) safePayload.Remove("Password");
            if (safePayload.ContainsKey("CurrentPassword")) safePayload.Remove("CurrentPassword");
            if (safePayload.ContainsKey("NewPassword")) safePayload.Remove("NewPassword");

            var job = new EmailOutboxJob
            {
                RecipientEmail = request.RecipientEmail.Trim(),
                RecipientName = request.RecipientName?.Trim(),
                Subject = request.Subject,
                TemplateName = request.TemplateName,
                SafePayloadJson = JsonSerializer.Serialize(safePayload),
                Status = "Pending",
                CreatedAtUtc = DateTime.UtcNow,
                ScheduledForUtc = request.ScheduledForUtc ?? DateTime.UtcNow,
                AttemptCount = 0,
                MaxRetries = _settings.MaxRetries > 0 ? _settings.MaxRetries : 5,
                IdempotencyKey = request.IdempotencyKey,
                EventType = request.EventType,
                CorrelationId = request.CorrelationId ?? Guid.NewGuid().ToString("N")
            };

            await _db.EmailOutboxJobs.AddAsync(job, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Enqueued email outbox job #{JobId} for {Recipient} (Event: {Event}, Key: {Key})",
                job.Id, job.RecipientEmail, job.EventType, job.IdempotencyKey);

            return job;
        }

        public async Task<int> EnqueueBatchAsync(IEnumerable<EmailEnqueueRequest> requests, CancellationToken cancellationToken = default)
        {
            var requestList = requests.ToList();
            if (requestList.Count == 0) return 0;

            var keys = requestList
                .Where(r => !string.IsNullOrWhiteSpace(r.IdempotencyKey))
                .Select(r => r.IdempotencyKey!)
                .Distinct()
                .ToList();

            var existingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (keys.Count > 0)
            {
                var found = await _db.EmailOutboxJobs
                    .Where(j => keys.Contains(j.IdempotencyKey!) && j.Status != "Cancelled")
                    .Select(j => j.IdempotencyKey!)
                    .ToListAsync(cancellationToken);

                foreach (var k in found)
                {
                    existingKeys.Add(k);
                }
            }

            int addedCount = 0;
            var jobsToAdd = new List<EmailOutboxJob>();

            foreach (var req in requestList)
            {
                if (string.IsNullOrWhiteSpace(req.RecipientEmail)) continue;

                if (!string.IsNullOrWhiteSpace(req.IdempotencyKey) && existingKeys.Contains(req.IdempotencyKey))
                {
                    continue; // Skip duplicate
                }

                var safePayload = new Dictionary<string, string>(req.TemplateData, StringComparer.OrdinalIgnoreCase);
                if (safePayload.ContainsKey("Password")) safePayload.Remove("Password");
                if (safePayload.ContainsKey("CurrentPassword")) safePayload.Remove("CurrentPassword");
                if (safePayload.ContainsKey("NewPassword")) safePayload.Remove("NewPassword");

                var job = new EmailOutboxJob
                {
                    RecipientEmail = req.RecipientEmail.Trim(),
                    RecipientName = req.RecipientName?.Trim(),
                    Subject = req.Subject,
                    TemplateName = req.TemplateName,
                    SafePayloadJson = JsonSerializer.Serialize(safePayload),
                    Status = "Pending",
                    CreatedAtUtc = DateTime.UtcNow,
                    ScheduledForUtc = req.ScheduledForUtc ?? DateTime.UtcNow,
                    AttemptCount = 0,
                    MaxRetries = _settings.MaxRetries > 0 ? _settings.MaxRetries : 5,
                    IdempotencyKey = req.IdempotencyKey,
                    EventType = req.EventType,
                    CorrelationId = req.CorrelationId ?? Guid.NewGuid().ToString("N")
                };

                jobsToAdd.Add(job);
                if (!string.IsNullOrWhiteSpace(req.IdempotencyKey))
                {
                    existingKeys.Add(req.IdempotencyKey);
                }
                addedCount++;
            }

            if (jobsToAdd.Count > 0)
            {
                await _db.EmailOutboxJobs.AddRangeAsync(jobsToAdd, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Enqueued batch of {Count} email outbox jobs.", jobsToAdd.Count);
            }

            return addedCount;
        }

        public async Task<List<EmailOutboxJob>> ClaimDueJobsAsync(int batchSize, TimeSpan leaseDuration, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            // Strategy: query eligible jobs and lease atomically
            // Eligible if:
            // 1. Status == "Pending" AND ScheduledForUtc <= now AND (NextAttemptAtUtc == null OR NextAttemptAtUtc <= now)
            // 2. OR Status == "Processing" AND LockedUntilUtc != null AND LockedUntilUtc < now (abandoned worker recovery)
            var candidateJobs = await _db.EmailOutboxJobs
                .Where(j => (j.Status == "Pending" && j.ScheduledForUtc <= now && (j.NextAttemptAtUtc == null || j.NextAttemptAtUtc <= now))
                         || (j.Status == "Processing" && j.LockedUntilUtc != null && j.LockedUntilUtc < now))
                .OrderBy(j => j.ScheduledForUtc)
                .ThenBy(j => j.Id)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (candidateJobs.Count == 0) return candidateJobs;

            var leaseUntil = now.Add(leaseDuration);
            foreach (var job in candidateJobs)
            {
                job.Status = "Processing";
                job.LockedUntilUtc = leaseUntil;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return candidateJobs;
        }

        public async Task MarkJobSentAsync(int jobId, int durationMs, string? messageId = null, CancellationToken cancellationToken = default)
        {
            var job = await _db.EmailOutboxJobs.FindAsync(new object[] { jobId }, cancellationToken);
            if (job == null) return;

            job.Status = "Sent";
            job.SentAtUtc = DateTime.UtcNow;
            job.LockedUntilUtc = null;
            job.LastError = null;

            var log = new EmailDeliveryLog
            {
                EmailOutboxJobId = jobId,
                AttemptNumber = job.AttemptCount + 1,
                AttemptedAtUtc = DateTime.UtcNow,
                Status = "Success",
                ErrorMessage = null,
                DurationMs = durationMs
            };

            await _db.EmailDeliveryLogs.AddAsync(log, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Outbox Job #{JobId} marked Sent (duration: {DurationMs} ms)", jobId, durationMs);
        }

        public async Task MarkJobFailedAsync(int jobId, string errorMessage, bool isPermanent, int durationMs, CancellationToken cancellationToken = default)
        {
            var job = await _db.EmailOutboxJobs.FindAsync(new object[] { jobId }, cancellationToken);
            if (job == null) return;

            job.AttemptCount++;
            job.LastError = errorMessage;
            job.LockedUntilUtc = null;

            bool deadLetter = isPermanent || job.AttemptCount >= job.MaxRetries;

            if (deadLetter)
            {
                job.Status = "Failed";
                _logger.LogWarning("Outbox Job #{JobId} permanently failed: {Error} (Attempt {Count}/{Max})", 
                    jobId, errorMessage, job.AttemptCount, job.MaxRetries);
            }
            else
            {
                job.Status = "Pending";
                // Exponential backoff with jitter: 2^attempt * 15s + jitter
                int jitter = Random.Shared.Next(1, 10);
                double backoffSec = Math.Min(3600, Math.Pow(2, job.AttemptCount) * 15 + jitter);
                job.NextAttemptAtUtc = DateTime.UtcNow.AddSeconds(backoffSec);

                _logger.LogInformation("Outbox Job #{JobId} scheduled for retry at {RetryTime} (Attempt {Count}/{Max}): {Error}",
                    jobId, job.NextAttemptAtUtc, job.AttemptCount, job.MaxRetries, errorMessage);
            }

            var log = new EmailDeliveryLog
            {
                EmailOutboxJobId = jobId,
                AttemptNumber = job.AttemptCount,
                AttemptedAtUtc = DateTime.UtcNow,
                Status = isPermanent ? "PermanentFailure" : (deadLetter ? "DeadLetter" : "TransientFailure"),
                ErrorMessage = errorMessage,
                DurationMs = durationMs
            };

            await _db.EmailDeliveryLogs.AddAsync(log, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> CancelScheduledJobsAsync(string idempotencyKeyPrefix, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKeyPrefix)) return 0;

            var pendingJobs = await _db.EmailOutboxJobs
                .Where(j => j.Status == "Pending" && j.IdempotencyKey != null && j.IdempotencyKey.StartsWith(idempotencyKeyPrefix))
                .ToListAsync(cancellationToken);

            foreach (var job in pendingJobs)
            {
                job.Status = "Cancelled";
                job.LastError = "Cancelled by business event update or completion";
            }

            if (pendingJobs.Count > 0)
            {
                await _db.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Cancelled {Count} scheduled reminder jobs matching prefix '{Prefix}'", pendingJobs.Count, idempotencyKeyPrefix);
            }

            return pendingJobs.Count;
        }
    }
}
