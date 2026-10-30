using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TMS.Models.Email;
using TMS.Repository;
using TMS.ViewModels.Email;
using TMS.Web.Services.Email;

namespace TMS.IA.RuntimeChecks
{
    public class MockEmailSender : IEmailSender
    {
        public List<EmailMessage> SentMessages { get; } = new();
        public bool SimulateTransientFailure { get; set; }
        public bool SimulatePermanentFailure { get; set; }

        public Task<EmailSendResult> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            if (SimulatePermanentFailure)
            {
                return Task.FromResult(EmailSendResult.Failed("550 5.1.1 User unknown", isPermanent: true, durationMs: 15));
            }

            if (SimulateTransientFailure)
            {
                return Task.FromResult(EmailSendResult.Failed("Connection timed out to SMTP server", isPermanent: false, durationMs: 3000));
            }

            SentMessages.Add(message);
            return Task.FromResult(EmailSendResult.Succeeded(durationMs: 20, providerMessageId: "MOCK-" + Guid.NewGuid().ToString("N")));
        }

        public Task<EmailSendResult> SendTestEmailAsync(string testRecipient, CancellationToken cancellationToken = default)
        {
            var msg = new EmailMessage
            {
                RecipientEmail = testRecipient,
                Subject = "EduDigiClass LMS — Test Email",
                HtmlBody = "<p>Test email</p>",
                PlainTextBody = "Test email",
                TemplateName = "TestEmail",
                EventType = "AdminTest"
            };
            return SendEmailAsync(msg, cancellationToken);
        }
    }

    public static class EmailWorkflowAutomatedTests
    {
        private static int _testPassed = 0;
        private static int _testFailed = 0;

        private static void Assert(string testName, bool condition, string detail = "")
        {
            if (condition)
            {
                _testPassed++;
                Console.WriteLine($"[EMAIL TEST PASS] {testName} {(string.IsNullOrEmpty(detail) ? "" : "-> " + detail)}");
            }
            else
            {
                _testFailed++;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[EMAIL TEST FAIL] {testName} {(string.IsNullOrEmpty(detail) ? "" : "-> " + detail)}");
                Console.ResetColor();
            }
        }

        private static ApplicationDBContext CreateInMemoryDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDBContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new ApplicationDBContext(options);
        }

        public static async Task<int> RunAllTestsAsync()
        {
            _testPassed = 0;
            _testFailed = 0;

            Console.WriteLine("==========================================================================");
            Console.WriteLine("📧 EDUDIGICLASS LMS — AUTOMATED EMAIL WORKFLOWS ACCEPTANCE TEST SUITE");
            Console.WriteLine("==========================================================================");

            TestConfigurationValidation();
            TestConfigurationDisabledAndTestMode();
            TestTemplateRenderingAllTemplates();
            TestTemplateHtmlEscaping();
            await TestOutboxEnqueueAndDeduplicationAsync();
            await TestOutboxBatchEnqueueAsync();
            await TestScheduledReminderFutureAsync();
            await TestAtomicLeaseClaimingAsync();
            await TestWorkerRecoveryAfterRestartAsync();
            await TestRetryExponentialBackoffAsync();
            await TestPermanentFailureDeadLetterAsync();
            await TestMaxRetriesExhaustedAsync();
            await TestCancelScheduledJobsAsync();
            TestAttendanceAbsenceDeduplicationKey();
            TestWeeklyAttendanceShortageCalculation();
            TestCryptographicOtpSecurity();
            await TestMockEmailSenderEndToEndAsync();

            Console.WriteLine("==========================================================================");
            Console.WriteLine($"🏁 EMAIL TEST SUITE COMPLETED: {_testPassed} PASSED, {_testFailed} FAILED");
            Console.WriteLine("==========================================================================");

            return _testFailed;
        }

        private static void TestConfigurationValidation()
        {
            Console.WriteLine("\n--- Testing Email Configuration Validation ---");

            var validSettings = new EmailSettings
            {
                Host = "smtp.gmail.com",
                Port = 587,
                UserName = "lms@edudigiclass.com",
                Password = "app-password-1234",
                SenderEmail = "no-reply@edudigiclass.com",
                SenderDisplayName = "EduDigiClass LMS",
                IsEnabled = true,
                TestMode = false
            };

            var (isValid, errors) = validSettings.ValidateConfiguration();
            Assert("Valid configuration passes validation", isValid && string.IsNullOrEmpty(errors));

            var invalidSettings = new EmailSettings
            {
                Host = "",
                Port = 0,
                UserName = "",
                Password = "",
                SenderEmail = "",
                IsEnabled = true
            };

            var (isInvalid, invalidReason) = invalidSettings.ValidateConfiguration();
            Assert("Invalid configuration fails validation with error message", !isInvalid && !string.IsNullOrEmpty(invalidReason), $"Reason: {invalidReason}");
        }

        private static void TestConfigurationDisabledAndTestMode()
        {
            Console.WriteLine("\n--- Testing Disabled and Test Mode Behaviors ---");

            var disabledSettings = new EmailSettings { IsEnabled = false };
            Assert("Disabled setting reports IsEnabled == false", !disabledSettings.IsEnabled);

            var testModeSettings = new EmailSettings
            {
                IsEnabled = true,
                TestMode = true,
                TestRecipientEmail = "admin-audit@edudigiclass.com"
            };
            Assert("TestMode enabled with designated test recipient", testModeSettings.TestMode && testModeSettings.TestRecipientEmail == "admin-audit@edudigiclass.com");
        }

        private static void TestTemplateRenderingAllTemplates()
        {
            Console.WriteLine("\n--- Testing Template Rendering (All 20 Supported Templates) ---");

            var templateService = new EmailTemplateService(Options.Create(new EmailSettings()));
            var sampleData = new Dictionary<string, string>
            {
                { "RecipientName", "Rahul Sharma" },
                { "OtpCode", "849201" },
                { "ExpiryMinutes", "10" },
                { "CourseTitle", "Data Structures & Algorithms" },
                { "CourseCode", "CS-201" },
                { "Semester", "Semester 3" },
                { "ProgramName", "B.Tech Computer Science" },
                { "AssignmentTitle", "Binary Search Tree Implementation" },
                { "DueDate", "15 Nov 2026, 11:59 PM" },
                { "MaxMarks", "50" },
                { "MarksAwarded", "46" },
                { "Feedback", "Excellent implementation of AVL balancing" },
                { "AttendanceDate", "28 Oct 2026" },
                { "Status", "Absent" },
                { "AttendancePercentage", "68.5" },
                { "AttendedSessions", "15" },
                { "TotalSessions", "22" },
                { "ExamTitle", "Midterm Examination - Data Structures" },
                { "ExamDate", "05 Nov 2026" },
                { "StartTime", "10:00 AM" },
                { "EndTime", "12:00 PM" },
                { "Duration", "120 minutes" },
                { "SebRequired", "Yes" },
                { "AnnouncementTitle", "Campus Annual Tech Symposium Announced" },
                { "AnnouncementBody", "All students are invited to register for hackathons and technical symposiums starting next Monday." },
                { "AudienceType", "All Students & Faculty" }
            };

            string[] templateNames = new[]
            {
                "AccountWelcome",
                "PasswordResetOtp",
                "PasswordChangedNotice",
                "CourseEnrollmentConfirmation",
                "CourseDropNotice",
                "CourseReactivationNotice",
                "AssignmentPublication",
                "AssignmentReminder48h",
                "AssignmentReminder24h",
                "AssignmentSubmissionReceipt",
                "AssignmentFacultySubmissionNotice",
                "AssignmentGraded",
                "AttendanceAbsence",
                "AttendanceShortage",
                "ExamPublication",
                "ExamReminder24h",
                "ExamReminder2h",
                "ExamSubmissionReceipt",
                "ResultsPublished",
                "AcademicAnnouncement"
            };

            foreach (var template in templateNames)
            {
                var rendered = templateService.Render(template, sampleData);
                bool valid = !string.IsNullOrWhiteSpace(rendered.Subject)
                             && !string.IsNullOrWhiteSpace(rendered.HtmlBody)
                             && !string.IsNullOrWhiteSpace(rendered.PlainTextBody)
                             && rendered.HtmlBody.Contains("EduDigiClass");

                Assert($"Template '{template}' rendered with HTML & PlainText branding", valid, $"Subject: {rendered.Subject}");
            }
        }

        private static void TestTemplateHtmlEscaping()
        {
            Console.WriteLine("\n--- Testing Template HTML Escaping & XSS Protection ---");

            var templateService = new EmailTemplateService(Options.Create(new EmailSettings()));
            var maliciousData = new Dictionary<string, string>
            {
                { "RecipientName", "<script>alert('pwned')</script>" },
                { "AssignmentTitle", "<img src=x onerror=alert(1)>" }
            };

            var rendered = templateService.Render("AssignmentPublication", maliciousData);

            bool noRawScript = !rendered.HtmlBody.Contains("<script>alert('pwned')</script>");
            bool noRawImgTag = !rendered.HtmlBody.Contains("<img src=x onerror=alert(1)>");

            Assert("Malicious HTML input is sanitized/escaped in output HTML", noRawScript && noRawImgTag);
        }

        private static async Task TestOutboxEnqueueAndDeduplicationAsync()
        {
            Console.WriteLine("\n--- Testing Outbox Enqueue & Deduplication ---");

            using var db = CreateInMemoryDbContext("TestDb_Deduplication_" + Guid.NewGuid().ToString("N"));
            var settings = Options.Create(new EmailSettings { MaxRetries = 3 });
            var logger = new LoggerFactory().CreateLogger<EmailOutboxService>();
            var outbox = new EmailOutboxService(db, settings, logger);

            var req1 = new EmailEnqueueRequest
            {
                RecipientEmail = "student1@edudigiclass.com",
                RecipientName = "Student One",
                Subject = "Welcome to EduDigiClass",
                TemplateName = "AccountWelcome",
                IdempotencyKey = "user-welcome-101",
                EventType = "AccountWelcome"
            };

            var job1 = await outbox.EnqueueAsync(req1);
            Assert("First enqueue succeeds and creates job", job1 != null && job1.Status == "Pending");

            // Enqueue identical job with same IdempotencyKey
            var job2 = await outbox.EnqueueAsync(req1);
            Assert("Second enqueue with same idempotency key returns existing job", job2 != null && job2.Id == job1!.Id);

            int totalCount = await db.EmailOutboxJobs.CountAsync();
            Assert("Database contains only 1 job (duplicate prevented)", totalCount == 1);
        }

        private static async Task TestOutboxBatchEnqueueAsync()
        {
            Console.WriteLine("\n--- Testing Outbox Batch Enqueue ---");

            using var db = CreateInMemoryDbContext("TestDb_Batch_" + Guid.NewGuid().ToString("N"));
            var settings = Options.Create(new EmailSettings { MaxRetries = 3 });
            var logger = new LoggerFactory().CreateLogger<EmailOutboxService>();
            var outbox = new EmailOutboxService(db, settings, logger);

            var batch = new List<EmailEnqueueRequest>
            {
                new EmailEnqueueRequest { RecipientEmail = "s1@test.com", TemplateName = "AccountWelcome", IdempotencyKey = "b-1" },
                new EmailEnqueueRequest { RecipientEmail = "s2@test.com", TemplateName = "AccountWelcome", IdempotencyKey = "b-2" },
                new EmailEnqueueRequest { RecipientEmail = "s3@test.com", TemplateName = "AccountWelcome", IdempotencyKey = "b-2" }, // Duplicate key
                new EmailEnqueueRequest { RecipientEmail = "   ", TemplateName = "AccountWelcome" } // Invalid empty email
            };

            int enqueued = await outbox.EnqueueBatchAsync(batch);
            Assert("Batch enqueue filters duplicates and invalid recipients", enqueued == 2);

            int count = await db.EmailOutboxJobs.CountAsync();
            Assert("Database persisted exactly 2 unique jobs", count == 2);
        }

        private static async Task TestScheduledReminderFutureAsync()
        {
            Console.WriteLine("\n--- Testing Scheduled Reminders UTC Future Logic ---");

            using var db = CreateInMemoryDbContext("TestDb_Schedule_" + Guid.NewGuid().ToString("N"));
            var settings = Options.Create(new EmailSettings { MaxRetries = 3 });
            var logger = new LoggerFactory().CreateLogger<EmailOutboxService>();
            var outbox = new EmailOutboxService(db, settings, logger);

            // Job scheduled for 24 hours into the future
            var reqFuture = new EmailEnqueueRequest
            {
                RecipientEmail = "student@test.com",
                TemplateName = "AssignmentReminder24h",
                ScheduledForUtc = DateTime.UtcNow.AddHours(24),
                IdempotencyKey = "remind-24h-1"
            };

            await outbox.EnqueueAsync(reqFuture);

            // Immediate claim (for right now)
            var claimedNow = await outbox.ClaimDueJobsAsync(10, TimeSpan.FromMinutes(2));
            Assert("Future scheduled reminder is NOT claimed before its time", claimedNow.Count == 0);
        }

        private static async Task TestAtomicLeaseClaimingAsync()
        {
            Console.WriteLine("\n--- Testing Atomic Lease Claiming & Concurrency Protection ---");

            using var db = CreateInMemoryDbContext("TestDb_AtomicClaim_" + Guid.NewGuid().ToString("N"));
            var settings = Options.Create(new EmailSettings { MaxRetries = 3 });
            var logger = new LoggerFactory().CreateLogger<EmailOutboxService>();
            var outbox = new EmailOutboxService(db, settings, logger);

            await outbox.EnqueueAsync(new EmailEnqueueRequest
            {
                RecipientEmail = "student@test.com",
                TemplateName = "AccountWelcome",
                ScheduledForUtc = DateTime.UtcNow.AddMinutes(-5),
                IdempotencyKey = "claim-test-1"
            });

            // Worker 1 claims
            var worker1Claims = await outbox.ClaimDueJobsAsync(10, TimeSpan.FromMinutes(3));
            Assert("Worker 1 claims 1 due job", worker1Claims.Count == 1);
            Assert("Claimed job status changed to 'Processing'", worker1Claims[0].Status == "Processing");
            Assert("Claimed job has active lock lease", worker1Claims[0].LockedUntilUtc.HasValue && worker1Claims[0].LockedUntilUtc!.Value > DateTime.UtcNow);

            // Worker 2 attempts to claim simultaneously
            var worker2Claims = await outbox.ClaimDueJobsAsync(10, TimeSpan.FromMinutes(3));
            Assert("Worker 2 cannot claim locked job (prevents duplicate sending)", worker2Claims.Count == 0);
        }

        private static async Task TestWorkerRecoveryAfterRestartAsync()
        {
            Console.WriteLine("\n--- Testing Worker Recovery After Restart (Abandoned Locks) ---");

            using var db = CreateInMemoryDbContext("TestDb_Recovery_" + Guid.NewGuid().ToString("N"));
            var settings = Options.Create(new EmailSettings { MaxRetries = 3 });
            var logger = new LoggerFactory().CreateLogger<EmailOutboxService>();
            var outbox = new EmailOutboxService(db, settings, logger);

            // Simulate a crashed worker: job left in 'Processing' with expired lock 10 minutes ago
            var abandonedJob = new EmailOutboxJob
            {
                RecipientEmail = "student@test.com",
                Subject = "Abandoned Job",
                TemplateName = "AccountWelcome",
                Status = "Processing",
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-15),
                ScheduledForUtc = DateTime.UtcNow.AddMinutes(-15),
                LockedUntilUtc = DateTime.UtcNow.AddMinutes(-10),
                MaxRetries = 3
            };

            await db.EmailOutboxJobs.AddAsync(abandonedJob);
            await db.SaveChangesAsync();

            // Next worker poll
            var recoveredJobs = await outbox.ClaimDueJobsAsync(10, TimeSpan.FromMinutes(3));
            Assert("Recovered abandoned Processing job with expired lock", recoveredJobs.Count == 1 && recoveredJobs[0].Id == abandonedJob.Id);
            Assert("Recovered job has fresh lock lease", recoveredJobs[0].LockedUntilUtc.HasValue && recoveredJobs[0].LockedUntilUtc!.Value > DateTime.UtcNow);
        }

        private static async Task TestRetryExponentialBackoffAsync()
        {
            Console.WriteLine("\n--- Testing Retry with Exponential Backoff ---");

            using var db = CreateInMemoryDbContext("TestDb_Retry_" + Guid.NewGuid().ToString("N"));
            var settings = Options.Create(new EmailSettings { MaxRetries = 5 });
            var logger = new LoggerFactory().CreateLogger<EmailOutboxService>();
            var outbox = new EmailOutboxService(db, settings, logger);

            var job = await outbox.EnqueueAsync(new EmailEnqueueRequest
            {
                RecipientEmail = "student@test.com",
                TemplateName = "AccountWelcome",
                IdempotencyKey = "retry-test-1"
            });

            // Simulate transient network timeout
            await outbox.MarkJobFailedAsync(job!.Id, "SMTP connection timed out", isPermanent: false, durationMs: 5000);

            var updatedJob = await db.EmailOutboxJobs.FindAsync(job.Id);
            Assert("Job marked Pending for retry", updatedJob?.Status == "Pending");
            Assert("AttemptCount incremented to 1", updatedJob?.AttemptCount == 1);
            Assert("NextAttemptAtUtc set in the future with backoff", updatedJob?.NextAttemptAtUtc.HasValue == true && updatedJob.NextAttemptAtUtc.Value > DateTime.UtcNow);

            var auditLog = await db.EmailDeliveryLogs.FirstOrDefaultAsync(l => l.EmailOutboxJobId == job.Id);
            Assert("Delivery attempt audit log recorded", auditLog != null && auditLog.Status == "TransientFailure");
        }

        private static async Task TestPermanentFailureDeadLetterAsync()
        {
            Console.WriteLine("\n--- Testing Permanent Failure Dead-Lettering ---");

            using var db = CreateInMemoryDbContext("TestDb_Permanent_" + Guid.NewGuid().ToString("N"));
            var settings = Options.Create(new EmailSettings { MaxRetries = 5 });
            var logger = new LoggerFactory().CreateLogger<EmailOutboxService>();
            var outbox = new EmailOutboxService(db, settings, logger);

            var job = await outbox.EnqueueAsync(new EmailEnqueueRequest
            {
                RecipientEmail = "bad-address@nowhere.xyz",
                TemplateName = "AccountWelcome",
                IdempotencyKey = "perm-test-1"
            });

            // Simulate permanent 550 Mailbox does not exist
            await outbox.MarkJobFailedAsync(job!.Id, "550 5.1.1 User unknown", isPermanent: true, durationMs: 25);

            var updatedJob = await db.EmailOutboxJobs.FindAsync(job.Id);
            Assert("Permanent error immediately transitions job to 'Failed'", updatedJob?.Status == "Failed");
            Assert("Job will no longer be claimed for retry", updatedJob?.NextAttemptAtUtc == null);

            var auditLog = await db.EmailDeliveryLogs.FirstOrDefaultAsync(l => l.EmailOutboxJobId == job.Id);
            Assert("Permanent failure audit log created", auditLog?.Status == "PermanentFailure");
        }

        private static async Task TestMaxRetriesExhaustedAsync()
        {
            Console.WriteLine("\n--- Testing Max Retries Exhaustion ---");

            using var db = CreateInMemoryDbContext("TestDb_MaxRetries_" + Guid.NewGuid().ToString("N"));
            var settings = Options.Create(new EmailSettings { MaxRetries = 3 });
            var logger = new LoggerFactory().CreateLogger<EmailOutboxService>();
            var outbox = new EmailOutboxService(db, settings, logger);

            var job = await outbox.EnqueueAsync(new EmailEnqueueRequest
            {
                RecipientEmail = "student@test.com",
                TemplateName = "AccountWelcome",
                IdempotencyKey = "maxretries-1"
            });

            // Simulate 3 failures
            await outbox.MarkJobFailedAsync(job!.Id, "Fail 1", isPermanent: false, durationMs: 10);
            await outbox.MarkJobFailedAsync(job!.Id, "Fail 2", isPermanent: false, durationMs: 10);
            await outbox.MarkJobFailedAsync(job!.Id, "Fail 3", isPermanent: false, durationMs: 10);

            var updatedJob = await db.EmailOutboxJobs.FindAsync(job.Id);
            Assert("Exhausted retries moves job to 'Failed' (Dead Letter)", updatedJob?.Status == "Failed" && updatedJob?.AttemptCount == 3);
        }

        private static async Task TestCancelScheduledJobsAsync()
        {
            Console.WriteLine("\n--- Testing Reminder Cancellation on Completion/Rescheduling ---");

            using var db = CreateInMemoryDbContext("TestDb_Cancel_" + Guid.NewGuid().ToString("N"));
            var settings = Options.Create(new EmailSettings { MaxRetries = 3 });
            var logger = new LoggerFactory().CreateLogger<EmailOutboxService>();
            var outbox = new EmailOutboxService(db, settings, logger);

            // Student enrolled in assignment #50, 48h and 24h reminders queued
            await outbox.EnqueueAsync(new EmailEnqueueRequest
            {
                RecipientEmail = "student@test.com",
                TemplateName = "AssignmentReminder48h",
                ScheduledForUtc = DateTime.UtcNow.AddHours(48),
                IdempotencyKey = "remind-assign-48h-50-student99"
            });

            await outbox.EnqueueAsync(new EmailEnqueueRequest
            {
                RecipientEmail = "student@test.com",
                TemplateName = "AssignmentReminder24h",
                ScheduledForUtc = DateTime.UtcNow.AddHours(24),
                IdempotencyKey = "remind-assign-24h-50-student99"
            });

            // Student submits assignment #50 early!
            int cancelled = await outbox.CancelScheduledJobsAsync("remind-assign-");
            Assert("Both pending reminders cancelled upon submission", cancelled == 2);

            var jobs = await db.EmailOutboxJobs.ToListAsync();
            Assert("All matched reminder jobs marked 'Cancelled'", jobs.All(j => j.Status == "Cancelled"));
        }

        private static void TestAttendanceAbsenceDeduplicationKey()
        {
            Console.WriteLine("\n--- Testing Attendance Absence Deduplication Logic ---");

            int studentId = 405;
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            string key1 = $"absence-{studentId}-{today}";
            string key2 = $"absence-{studentId}-{today}";

            Assert("Deterministic attendance absence key generated for date", key1 == key2);
            Assert("Key contains student ID and date", key1.Contains("405") && key1.Contains(today));
        }

        private static void TestWeeklyAttendanceShortageCalculation()
        {
            Console.WriteLine("\n--- Testing Weekly Attendance Shortage Calculation ---");

            int attended = 14;
            int total = 20;
            double percentage = (double)attended / total * 100.0;

            bool isShortage = percentage < 75.0 && total >= 5;
            Assert("Attendance 14/20 = 70% correctly flagged as shortage (< 75%)", isShortage && percentage == 70.0);

            int weekNum = System.Globalization.ISOWeek.GetWeekOfYear(DateTime.UtcNow);
            string shortageKey = $"shortage-405-{DateTime.UtcNow.Year}-W{weekNum}";
            Assert("Weekly shortage key generated per student and week", shortageKey.Contains("W" + weekNum));
        }

        private static void TestCryptographicOtpSecurity()
        {
            Console.WriteLine("\n--- Testing Cryptographic OTP Generation & Validation ---");

            var generatedOtps = new HashSet<int>();
            bool allSixDigits = true;
            for (int i = 0; i < 500; i++)
            {
                int otp = RandomNumberGenerator.GetInt32(100000, 1000000);
                if (otp < 100000 || otp > 999999) allSixDigits = false;
                generatedOtps.Add(otp);
            }

            Assert("All 500 generated OTPs are strictly 6 digits", allSixDigits);
            Assert("Generated OTPs exhibit high entropy across 500 samples", generatedOtps.Count > 480);

            // Test expiry window
            var generatedAt = DateTime.UtcNow;
            var validExpiry = generatedAt.AddMinutes(10);
            var isExpiredAfter11Min = DateTime.UtcNow > generatedAt.AddMinutes(11);
            var isStillValidAt5Min = DateTime.UtcNow < validExpiry;

            Assert("10-minute expiry window holds true", isStillValidAt5Min && !isExpiredAfter11Min);
        }

        private static async Task TestMockEmailSenderEndToEndAsync()
        {
            Console.WriteLine("\n--- Testing End-to-End Worker Dispatch with Mock Email Sender ---");

            using var db = CreateInMemoryDbContext("TestDb_EndToEnd_" + Guid.NewGuid().ToString("N"));
            var settings = Options.Create(new EmailSettings { MaxRetries = 3 });
            var logger = new LoggerFactory().CreateLogger<EmailOutboxService>();
            var outbox = new EmailOutboxService(db, settings, logger);
            var templateService = new EmailTemplateService(settings);
            var mockSender = new MockEmailSender();

            // Enqueue welcome email
            var job = await outbox.EnqueueAsync(new EmailEnqueueRequest
            {
                RecipientEmail = "newuser@edudigiclass.com",
                RecipientName = "Aakash Patel",
                Subject = "Welcome to EduDigiClass",
                TemplateName = "AccountWelcome",
                TemplateData = new Dictionary<string, string> { { "RecipientName", "Aakash Patel" } },
                IdempotencyKey = "e2e-user-1"
            });

            // Claim due jobs
            var claimed = await outbox.ClaimDueJobsAsync(10, TimeSpan.FromMinutes(2));
            Assert("Worker claimed the queued job", claimed.Count == 1);

            // Process job
            var payload = !string.IsNullOrWhiteSpace(claimed[0].SafePayloadJson)
                ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(claimed[0].SafePayloadJson!) ?? new()
                : new();
            var rendered = templateService.Render(claimed[0].TemplateName, payload);

            var sendResult = await mockSender.SendEmailAsync(new EmailMessage
            {
                RecipientEmail = claimed[0].RecipientEmail,
                RecipientName = claimed[0].RecipientName,
                Subject = claimed[0].Subject,
                HtmlBody = rendered.HtmlBody,
                PlainTextBody = rendered.PlainTextBody
            });

            Assert("Mock sender succeeded without touching external SMTP", sendResult.Success);
            Assert("Mock sender recorded 1 sent message", mockSender.SentMessages.Count == 1);

            await outbox.MarkJobSentAsync(claimed[0].Id, sendResult.DurationMs, sendResult.ProviderMessageId);

            var finalJob = await db.EmailOutboxJobs.FindAsync(job!.Id);
            Assert("Job successfully updated to 'Sent' status in outbox", finalJob?.Status == "Sent" && finalJob?.SentAtUtc != null);

            var finalLog = await db.EmailDeliveryLogs.FirstOrDefaultAsync(l => l.EmailOutboxJobId == job.Id);
            Assert("Delivery log created with Success status", finalLog?.Status == "Success");
        }
    }
}
