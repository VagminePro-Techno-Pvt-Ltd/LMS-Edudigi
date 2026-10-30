using System;
using System.Collections.Generic;

namespace TMS.ViewModels.Email
{
    public class EmailEnqueueRequest
    {
        public string RecipientEmail { get; set; } = string.Empty;
        public string? RecipientName { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public Dictionary<string, string> TemplateData { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public string? IdempotencyKey { get; set; }
        public string? EventType { get; set; }
        public string? CorrelationId { get; set; }
        public DateTime? ScheduledForUtc { get; set; }
    }

    public class EmailMessage
    {
        public string RecipientEmail { get; set; } = string.Empty;
        public string? RecipientName { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string HtmlBody { get; set; } = string.Empty;
        public string? PlainTextBody { get; set; }
        public string? TemplateName { get; set; }
        public string? EventType { get; set; }
        public string? CorrelationId { get; set; }
    }

    public class EmailSendResult
    {
        public bool Success { get; set; }
        public bool IsPermanentFailure { get; set; }
        public string? ErrorMessage { get; set; }
        public int DurationMs { get; set; }
        public string? ProviderMessageId { get; set; }

        public static EmailSendResult Succeeded(int durationMs = 0, string? providerMessageId = null)
        {
            return new EmailSendResult
            {
                Success = true,
                IsPermanentFailure = false,
                DurationMs = durationMs,
                ProviderMessageId = providerMessageId
            };
        }

        public static EmailSendResult Failed(string errorMessage, bool isPermanent, int durationMs = 0)
        {
            return new EmailSendResult
            {
                Success = false,
                IsPermanentFailure = isPermanent,
                ErrorMessage = errorMessage,
                DurationMs = durationMs
            };
        }
    }
}
