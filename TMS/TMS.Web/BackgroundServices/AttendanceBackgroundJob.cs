using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using TMS.Repository.Managers;

namespace TMS.Web.BackgroundServices
{
    public class AttendanceBackgroundJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AttendanceBackgroundJob> _logger;

        public AttendanceBackgroundJob(IServiceProvider serviceProvider, ILogger<AttendanceBackgroundJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                // Schedule for 11:59 PM
                var nextRunTime = DateTime.Today.AddHours(23).AddMinutes(59);
                
                if (now > nextRunTime)
                {
                    nextRunTime = nextRunTime.AddDays(1);
                }

                var delay = nextRunTime - now;
                _logger.LogInformation("Attendance Job scheduled to run at {time} (in {delay} hours)", nextRunTime, delay.TotalHours);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var attendanceManager = scope.ServiceProvider.GetRequiredService<IDailyAttendanceManager>();
                        _logger.LogInformation("Processing daily absentees for {date}...", DateTime.Today);
                        int count = await attendanceManager.ProcessDailyAbsentees();
                        _logger.LogInformation("Successfully marked {count} students as absent.", count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing daily absentees.");
                }
            }
        }
    }
}
