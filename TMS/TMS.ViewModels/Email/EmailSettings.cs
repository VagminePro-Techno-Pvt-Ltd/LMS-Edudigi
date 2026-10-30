using System;
using System.Net.Mail;

namespace TMS.ViewModels.Email
{
    public class EmailSettings
    {
        public const string SectionName = "EmailSettings";

        /// <summary>
        /// SMTP Host (e.g. smtp.gmail.com)
        /// </summary>
        public string Host { get; set; } = "smtp.gmail.com";

        /// <summary>
        /// SMTP Port (default: 587)
        /// </summary>
        public int Port { get; set; } = 587;

        /// <summary>
        /// SMTP Username / account email
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// SMTP App Password / credential
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Sender From address
        /// </summary>
        public string SenderEmail { get; set; } = "noreply@edudigi.com";

        /// <summary>
        /// Sender display name
        /// </summary>
        public string SenderDisplayName { get; set; } = "EduDigiClass LMS";

        /// <summary>
        /// SSL / TLS enabled
        /// </summary>
        public bool EnableSsl { get; set; } = true;

        /// <summary>
        /// Connection and socket timeout in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Master switch to enable email sending. Disabled by default until explicitly enabled by administrator.
        /// </summary>
        public bool IsEnabled { get; set; } = false;

        /// <summary>
        /// When TestMode is true, delivery to arbitrary real recipients is prevented.
        /// Messages are diverted to TestRecipientEmail or simulated.
        /// </summary>
        public bool TestMode { get; set; } = true;

        /// <summary>
        /// Explicit administrator-approved recipient for TestMode.
        /// </summary>
        public string? TestRecipientEmail { get; set; }

        /// <summary>
        /// Maximum retry attempts for transient failures before moving to dead-letter (Failed).
        /// </summary>
        public int MaxRetries { get; set; } = 5;

        /// <summary>
        /// Number of outbox jobs claimed per background poll.
        /// </summary>
        public int BatchSize { get; set; } = 20;

        /// <summary>
        /// Polling interval for background worker in seconds.
        /// </summary>
        public int DispatchIntervalSeconds { get; set; } = 10;

        /// <summary>
        /// Maximum messages dispatched per minute across workers to respect provider rate limits.
        /// </summary>
        public int RateLimitPerMinute { get; set; } = 60;

        /// <summary>
        /// Base URL of the LMS portal for action links in emails.
        /// </summary>
        public string LmsPortalBaseUrl { get; set; } = "http://localhost:5022";

        /// <summary>
        /// Official support contact email displayed in templates.
        /// </summary>
        public string SupportContact { get; set; } = "support@edudigi.com";

        /// <summary>
        /// Validates whether the email settings are configured sufficiently to attempt dispatch.
        /// </summary>
        public (bool IsValid, string? ErrorReason) ValidateConfiguration()
        {
            if (!IsEnabled)
            {
                return (false, "Email dispatch is currently disabled (IsEnabled = false).");
            }

            if (string.IsNullOrWhiteSpace(Host))
            {
                return (false, "SMTP Host is not configured.");
            }

            if (Port <= 0 || Port > 65535)
            {
                return (false, $"Invalid SMTP Port: {Port}.");
            }

            if (string.IsNullOrWhiteSpace(UserName))
            {
                return (false, "SMTP UserName is not configured.");
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                return (false, "SMTP Password is not configured.");
            }

            if (string.IsNullOrWhiteSpace(SenderEmail))
            {
                return (false, "SenderEmail is not configured.");
            }

            if (TestMode && string.IsNullOrWhiteSpace(TestRecipientEmail))
            {
                return (false, "TestMode is enabled but no TestRecipientEmail has been authorized by the administrator.");
            }

            return (true, null);
        }
    }
}
