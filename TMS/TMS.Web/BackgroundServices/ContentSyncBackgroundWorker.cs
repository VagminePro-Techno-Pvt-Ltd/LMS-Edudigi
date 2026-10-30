using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TMS.Web.Services.ContentSync;

namespace TMS.Web.BackgroundServices
{
    public class ContentSyncBackgroundWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ContentSyncBackgroundWorker> _logger;
        private DateTime _lastScanTime = DateTime.MinValue;

        public ContentSyncBackgroundWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<ContentSyncBackgroundWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ContentSyncBackgroundWorker started. Monitoring outbox queue for academic folder provisioning...");

            // Initial short delay to allow web app & db to complete bootstrap
            await Task.Delay(2000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var provisioner = scope.ServiceProvider.GetRequiredService<IContentFolderProvisioner>();

                        // 1. Process pending outbox jobs (LMS -> Drive)
                        await provisioner.ProcessPendingJobsAsync(stoppingToken);

                        // 2. Periodically scan for Drive -> LMS file synchronization (every 60 seconds)
                        if ((DateTime.UtcNow - _lastScanTime).TotalSeconds >= 60)
                        {
                            await provisioner.SyncDriveFilesToLmsAsync(stoppingToken);
                            _lastScanTime = DateTime.UtcNow;
                        }
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in ContentSyncBackgroundWorker loop: {Message}", ex.Message);
                }

                // Wait 5 seconds before next polling iteration
                try
                {
                    await Task.Delay(5000, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("ContentSyncBackgroundWorker stopped gracefully.");
        }
    }
}
