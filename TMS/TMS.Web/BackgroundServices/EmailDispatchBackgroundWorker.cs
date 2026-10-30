using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TMS.ViewModels.Email;
using TMS.Web.Services.Email;

namespace TMS.Web.BackgroundServices
{
    public class EmailDispatchBackgroundWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptionsMonitor<EmailSettings> _optionsMonitor;
        private readonly ILogger<EmailDispatchBackgroundWorker> _logger;

        public EmailDispatchBackgroundWorker(
            IServiceProvider serviceProvider,
            IOptionsMonitor<EmailSettings> optionsMonitor,
            ILogger<EmailDispatchBackgroundWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _optionsMonitor = optionsMonitor;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("EmailDispatchBackgroundWorker started.");

            // Delay initial tick to let web application start up cleanly
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var settings = _optionsMonitor.CurrentValue;

                try
                {
                    // Check if dispatch is enabled
                    if (settings.IsEnabled)
                    {
                        await ProcessOutboxBatchAsync(settings, stoppingToken);
                    }
                    else
                    {
                        _logger.LogTrace("Email dispatch is currently disabled in EmailSettings. Skipping poll.");
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in EmailDispatchBackgroundWorker loop.");
                }

                // Poll interval
                int pollInterval = Math.Max(3, settings.DispatchIntervalSeconds);
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(pollInterval), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            _logger.LogInformation("EmailDispatchBackgroundWorker gracefully stopped.");
        }

        private async Task ProcessOutboxBatchAsync(EmailSettings settings, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var outboxService = scope.ServiceProvider.GetRequiredService<IEmailOutboxService>();
            var templateService = scope.ServiceProvider.GetRequiredService<IEmailTemplateService>();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

            int batchSize = Math.Max(1, Math.Min(100, settings.BatchSize));
            var leaseDuration = TimeSpan.FromMinutes(3);

            var jobs = await outboxService.ClaimDueJobsAsync(batchSize, leaseDuration, cancellationToken);
            if (jobs.Count == 0) return;

            _logger.LogInformation("Claimed {Count} due email job(s) for processing.", jobs.Count);

            // Rate limiting: compute minimum delay between messages (ms) based on RateLimitPerMinute
            int minDelayMs = settings.RateLimitPerMinute > 0 ? (60000 / settings.RateLimitPerMinute) : 0;

            foreach (var job in jobs)
            {
                if (cancellationToken.IsCancellationRequested) break;

                try
                {
                    // Deserialize safe payload
                    var payload = !string.IsNullOrWhiteSpace(job.SafePayloadJson)
                        ? JsonSerializer.Deserialize<Dictionary<string, string>>(job.SafePayloadJson) ?? new Dictionary<string, string>()
                        : new Dictionary<string, string>();

                    // Ensure standard keys exist if missing
                    if (!payload.ContainsKey("RecipientEmail")) payload["RecipientEmail"] = job.RecipientEmail;
                    if (!payload.ContainsKey("RecipientName") && !string.IsNullOrWhiteSpace(job.RecipientName)) payload["RecipientName"] = job.RecipientName;

                    // Render template
                    var rendered = templateService.Render(job.TemplateName, payload);

                    var emailMessage = new EmailMessage
                    {
                        RecipientEmail = job.RecipientEmail,
                        RecipientName = job.RecipientName,
                        Subject = !string.IsNullOrWhiteSpace(job.Subject) ? job.Subject : rendered.Subject,
                        HtmlBody = rendered.HtmlBody,
                        PlainTextBody = rendered.PlainTextBody,
                        TemplateName = job.TemplateName,
                        EventType = job.EventType,
                        CorrelationId = job.CorrelationId
                    };

                    // Send email
                    var sendResult = await emailSender.SendEmailAsync(emailMessage, cancellationToken);

                    if (sendResult.Success)
                    {
                        await outboxService.MarkJobSentAsync(job.Id, sendResult.DurationMs, sendResult.ProviderMessageId, cancellationToken);
                    }
                    else
                    {
                        await outboxService.MarkJobFailedAsync(job.Id, sendResult.ErrorMessage ?? "Unknown send error", sendResult.IsPermanentFailure, sendResult.DurationMs, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled exception processing email job #{JobId}", job.Id);
                    await outboxService.MarkJobFailedAsync(job.Id, ex.Message, isPermanent: false, durationMs: 0, cancellationToken);
                }

                // Respect per-provider rate limit delay
                if (minDelayMs > 0)
                {
                    await Task.Delay(minDelayMs, cancellationToken);
                }
            }
        }
    }
}
