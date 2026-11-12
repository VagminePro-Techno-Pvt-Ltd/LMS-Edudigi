using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TMS.Models.Academics;
using TMS.Repository;
using TMS.Web.Services;

namespace TMS.Web.BackgroundServices
{
    /// <summary>
    /// Background queue consumer that processes video lecture transcription tasks asynchronously.
    /// Runs continuously with bounded concurrency without blocking web requests or file uploads.
    /// Recovers pending tasks on application startup.
    /// </summary>
    public class LectureTranscriptionBackgroundService : BackgroundService
    {
        private readonly ILectureTranscriptionQueue _queue;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LectureTranscriptionBackgroundService> _logger;
        private readonly SemaphoreSlim _concurrencyLimiter = new(2, 2); // Bounded worker concurrency (max 2 parallel STT tasks)

        public LectureTranscriptionBackgroundService(
            ILectureTranscriptionQueue queue,
            IServiceProvider serviceProvider,
            ILogger<LectureTranscriptionBackgroundService> logger)
        {
            _queue = queue;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Lecture Transcription Background Service started.");

            // Recover any pending transcription tasks on startup
            try
            {
                using var startupScope = _serviceProvider.CreateScope();
                var db = startupScope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                var pendingIds = await db.LectureTranscripts
                    .AsNoTracking()
                    .Where(t => t.Status == TranscriptStatus.Pending && t.IsActive)
                    .Select(t => t.LectureMaterialId)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                if (pendingIds.Any())
                {
                    _logger.LogInformation("Recovered {Count} pending transcription jobs on startup.", pendingIds.Count);
                    foreach (var id in pendingIds)
                    {
                        _queue.Queue(id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not recover pending transcription jobs on startup: {Message}", ex.Message);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    int materialId = await _queue.DequeueAsync(stoppingToken);
                    if (materialId <= 0) continue;

                    await _concurrencyLimiter.WaitAsync(stoppingToken);

                    // Fire transcription task with bounded concurrency so background worker can continue
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            using var scope = _serviceProvider.CreateScope();
                            var transcriptionService = scope.ServiceProvider.GetRequiredService<ILectureTranscriptionService>();

                            _logger.LogInformation("Processing transcription for material {Id}...", materialId);
                            var result = await transcriptionService.TranscribeVideoAsync(materialId, stoppingToken);
                            _logger.LogInformation("Transcription finished for material {Id}: Success={Success}, Status={Status}", materialId, result.Success, result.Status);
                        }
                        catch (Exception taskEx)
                        {
                            _logger.LogError(taskEx, "Error processing background transcription for material {Id}", materialId);
                        }
                        finally
                        {
                            _concurrencyLimiter.Release();
                        }
                    }, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in LectureTranscriptionBackgroundService loop.");
                    await Task.Delay(2000, stoppingToken); // Backoff on error
                }
            }

            _logger.LogInformation("Lecture Transcription Background Service stopped.");
        }
    }
}
