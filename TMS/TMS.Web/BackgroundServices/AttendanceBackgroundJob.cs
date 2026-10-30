using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TMS.Repository;
using TMS.Repository.Managers;
using TMS.ViewModels.Email;
using TMS.Web.Services.Email;

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
                _logger.LogInformation("Attendance Job scheduled to run at {time} (in {delay:F2} hours)", nextRunTime, delay.TotalHours);

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

                        // Phase 4: Queue Nightly Absence Notifications
                        await DispatchNightlyAbsenceEmailsAsync(scope, DateTime.Today, _logger);

                        // Phase 4: Weekly Attendance Shortage Check (Sundays)
                        if (DateTime.Today.DayOfWeek == DayOfWeek.Sunday)
                        {
                            await DispatchWeeklyShortageAlertsAsync(scope, DateTime.Today, _logger);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing daily absentees.");
                }
            }
        }

        public static async Task<int> DispatchNightlyAbsenceEmailsAsync(IServiceScope scope, DateTime targetDate, ILogger? logger = null)
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            var outbox = scope.ServiceProvider.GetRequiredService<IEmailOutboxService>();

            var date = targetDate.Date;
            var absentees = await db.DailyAttendances
                .Include(a => a.Student)
                .Where(a => a.Date == date && a.AttendanceStatus == 0 && a.Student != null && a.Student.IsActive && !string.IsNullOrEmpty(a.Student.Email))
                .ToListAsync();

            if (!absentees.Any()) return 0;

            var batch = absentees.Select(r => new EmailEnqueueRequest
            {
                RecipientEmail = r.Student!.Email.Trim(),
                RecipientName = r.Student.Name,
                Subject = $"Attendance Alert: Marked Absent on {date:dd MMM yyyy}",
                TemplateName = "AttendanceAbsence",
                TemplateData = new Dictionary<string, string>
                {
                    ["RecipientName"] = r.Student.Name ?? "Student",
                    ["Date"] = date.ToString("dd MMM yyyy")
                },
                EventType = "AttendanceAbsence",
                IdempotencyKey = $"absence-{r.StudentId}-{date:yyyyMMdd}"
            }).ToList();

            await outbox.EnqueueBatchAsync(batch);
            logger?.LogInformation("Enqueued {count} nightly absence notifications for {date:yyyy-MM-dd}.", batch.Count, date);
            return batch.Count;
        }

        public static async Task<int> DispatchWeeklyShortageAlertsAsync(IServiceScope scope, DateTime referenceDate, ILogger? logger = null)
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            var outbox = scope.ServiceProvider.GetRequiredService<IEmailOutboxService>();

            var today = referenceDate.Date;
            int weekNumber = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(today, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            // Group DailyAttendances by StudentId for active students
            var activeStudentIds = await db.UserMasters
                .Where(u => u.IsActive && u.Role != null && u.Role.Name.ToLower() == "student" && !string.IsNullOrEmpty(u.Email))
                .Select(u => u.Id)
                .ToListAsync();

            if (!activeStudentIds.Any()) return 0;

            var attendanceSummaries = await db.DailyAttendances
                .Where(a => activeStudentIds.Contains(a.StudentId) && a.IsActive)
                .GroupBy(a => a.StudentId)
                .Select(g => new
                {
                    StudentId = g.Key,
                    TotalSessions = g.Count(),
                    AttendedSessions = g.Count(x => x.AttendanceStatus == 1)
                })
                .Where(s => s.TotalSessions >= 5) // Meaningful minimum session count
                .ToListAsync();

            var activeStudentsMap = await db.UserMasters
                .Where(u => activeStudentIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            var batch = new List<EmailEnqueueRequest>();
            foreach (var summary in attendanceSummaries)
            {
                decimal percentage = Math.Round((decimal)summary.AttendedSessions * 100m / summary.TotalSessions, 1);
                if (percentage < 75.0m && activeStudentsMap.TryGetValue(summary.StudentId, out var student))
                {
                    batch.Add(new EmailEnqueueRequest
                    {
                        RecipientEmail = student.Email.Trim(),
                        RecipientName = student.Name,
                        Subject = $"Urgent: Attendance Shortage Warning ({percentage:0.#}% Attendance)",
                        TemplateName = "AttendanceShortage",
                        TemplateData = new Dictionary<string, string>
                        {
                            ["RecipientName"] = student.Name ?? "Student",
                            ["CurrentPercentage"] = percentage.ToString("0.#"),
                            ["Threshold"] = "75.0",
                            ["TotalSessions"] = summary.TotalSessions.ToString(),
                            ["AttendedSessions"] = summary.AttendedSessions.ToString()
                        },
                        EventType = "AttendanceShortage",
                        IdempotencyKey = $"shortage-{student.Id}-{today.Year}-W{weekNumber}"
                    });
                }
            }

            if (batch.Any())
            {
                await outbox.EnqueueBatchAsync(batch);
                logger?.LogInformation("Enqueued {count} weekly attendance shortage warnings for week {week}.", batch.Count, weekNumber);
            }

            return batch.Count;
        }
    }
}
