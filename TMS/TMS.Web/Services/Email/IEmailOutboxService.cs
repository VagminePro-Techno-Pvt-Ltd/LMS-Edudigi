using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMS.Models.Email;
using TMS.ViewModels.Email;

namespace TMS.Web.Services.Email
{
    public interface IEmailOutboxService
    {
        /// <summary>
        /// Enqueues an email job into the transactional outbox with idempotency protection.
        /// </summary>
        Task<EmailOutboxJob?> EnqueueAsync(EmailEnqueueRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Enqueues a batch of email jobs (e.g. For whole course or bulk import) with idempotency protection.
        /// </summary>
        Task<int> EnqueueBatchAsync(IEnumerable<EmailEnqueueRequest> requests, CancellationToken cancellationToken = default);

        /// <summary>
        /// Claims due pending jobs atomically using a worker lease lock.
        /// </summary>
        Task<List<EmailOutboxJob>> ClaimDueJobsAsync(int batchSize, TimeSpan leaseDuration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks a job as successfully sent and logs the delivery attempt.
        /// </summary>
        Task MarkJobSentAsync(int jobId, int durationMs, string? messageId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Records an attempt failure with exponential backoff or dead-letter failure.
        /// </summary>
        Task MarkJobFailedAsync(int jobId, string errorMessage, bool isPermanent, int durationMs, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels scheduled reminder jobs matching the idempotency key or prefix (e.g. When assignment is submitted or exam rescheduled).
        /// </summary>
        Task<int> CancelScheduledJobsAsync(string idempotencyKeyPrefix, CancellationToken cancellationToken = default);
    }
}
