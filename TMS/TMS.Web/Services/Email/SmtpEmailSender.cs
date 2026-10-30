using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using TMS.ViewModels.Email;

namespace TMS.Web.Services.Email
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IOptions<EmailSettings> options, ILogger<SmtpEmailSender> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public async Task<EmailSendResult> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();

            var (isValid, errorReason) = _settings.ValidateConfiguration();
            if (!isValid)
            {
                _logger.LogWarning("Email dispatch skipped: {Reason}", errorReason);
                return EmailSendResult.Failed(errorReason ?? "Email dispatch is disabled or misconfigured.", isPermanent: true, durationMs: 0);
            }

            if (string.IsNullOrWhiteSpace(message.RecipientEmail))
            {
                return EmailSendResult.Failed("Recipient email address is missing or empty.", isPermanent: true, durationMs: 0);
            }

            string actualRecipient = message.RecipientEmail.Trim();
            string actualSubject = message.Subject;
            string actualHtml = message.HtmlBody;
            string? actualText = message.PlainTextBody;

            // Enforce TestMode isolation
            if (_settings.TestMode)
            {
                if (string.IsNullOrWhiteSpace(_settings.TestRecipientEmail))
                {
                    _logger.LogInformation("TestMode active: delivery suppressed because no TestRecipientEmail is set. Intended recipient: {Recipient}", actualRecipient);
                    return EmailSendResult.Succeeded(0, "TestMode-Suppressed");
                }

                _logger.LogInformation("TestMode active: redirecting email intended for {OriginalRecipient} to test recipient {TestRecipient}", actualRecipient, _settings.TestRecipientEmail);
                actualSubject = $"[TEST MODE] {actualSubject}";
                string testBanner = $"<div style=\"background-color:#fffbeb;border:1px solid #f59e0b;color:#92400e;padding:12px 16px;border-radius:6px;margin-bottom:16px;font-family:system-ui,-apple-system,sans-serif;font-size:13px;line-height:1.4;\"><strong>[LMS TEST MODE]</strong> This email was originally generated for <code>{actualRecipient}</code>. Live delivery is constrained to authorized test recipients.</div>";
                actualHtml = testBanner + actualHtml;
                actualRecipient = _settings.TestRecipientEmail.Trim();
            }

            var mimeMessage = new MimeMessage();
            try
            {
                mimeMessage.From.Add(new MailboxAddress(_settings.SenderDisplayName, _settings.SenderEmail));
                mimeMessage.To.Add(new MailboxAddress(message.RecipientName ?? actualRecipient, actualRecipient));
                mimeMessage.Subject = actualSubject;

                var builder = new BodyBuilder
                {
                    HtmlBody = actualHtml,
                    TextBody = !string.IsNullOrWhiteSpace(actualText) ? actualText : HtmlToPlainText(actualHtml)
                };
                mimeMessage.Body = builder.ToMessageBody();

                if (!string.IsNullOrWhiteSpace(message.CorrelationId))
                {
                    mimeMessage.Headers.Add("X-Correlation-ID", message.CorrelationId);
                }
                if (!string.IsNullOrWhiteSpace(message.EventType))
                {
                    mimeMessage.Headers.Add("X-LMS-Event-Type", message.EventType);
                }
            }
            catch (ParseException ex)
            {
                _logger.LogError("Invalid email syntax encountered for {Recipient}: {Error}", actualRecipient, ex.Message);
                return EmailSendResult.Failed($"Invalid email syntax: {ex.Message}", isPermanent: true, (int)sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to construct MIME message for {Recipient}", actualRecipient);
                return EmailSendResult.Failed($"Failed to construct email: {ex.Message}", isPermanent: true, (int)sw.ElapsedMilliseconds);
            }

            using var client = new SmtpClient();
            client.CheckCertificateRevocation = false;
            try
            {
                client.Timeout = Math.Max(5, _settings.TimeoutSeconds) * 1000;

                var socketOption = _settings.Port == 465 
                    ? SecureSocketOptions.SslOnConnect 
                    : (_settings.EnableSsl ? SecureSocketOptions.StartTlsWhenAvailable : SecureSocketOptions.None);

                await client.ConnectAsync(_settings.Host, _settings.Port, socketOption, cancellationToken);

                if (!string.IsNullOrWhiteSpace(_settings.UserName) && !string.IsNullOrWhiteSpace(_settings.Password))
                {
                    await client.AuthenticateAsync(_settings.UserName, _settings.Password, cancellationToken);
                }

                await client.SendAsync(mimeMessage, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);

                sw.Stop();
                _logger.LogInformation("Email dispatched successfully to {Recipient} via {Host}:{Port} ({ElapsedMs} ms)", actualRecipient, _settings.Host, _settings.Port, sw.ElapsedMilliseconds);

                return EmailSendResult.Succeeded((int)sw.ElapsedMilliseconds, mimeMessage.MessageId);
            }
            catch (SmtpCommandException ex)
            {
                sw.Stop();
                bool isPermanent = ex.StatusCode switch
                {
                    SmtpStatusCode.MailboxBusy => false,
                    SmtpStatusCode.InsufficientStorage => false,
                    SmtpStatusCode.ServiceNotAvailable => false,
                    SmtpStatusCode.MailboxUnavailable => true,
                    SmtpStatusCode.ExceededStorageAllocation => false,
                    SmtpStatusCode.SyntaxError => true,
                    SmtpStatusCode.CommandNotImplemented => true,
                    SmtpStatusCode.BadCommandSequence => true,
                    _ => (int)ex.StatusCode >= 500 && (int)ex.StatusCode < 600
                };

                _logger.LogWarning("SMTP command error ({StatusCode}) when sending to {Recipient}: {Error} (Permanent={IsPermanent})", ex.StatusCode, actualRecipient, ex.Message, isPermanent);
                return EmailSendResult.Failed($"SMTP error ({ex.StatusCode}): {ex.Message}", isPermanent, (int)sw.ElapsedMilliseconds);
            }
            catch (AuthenticationException ex)
            {
                sw.Stop();
                _logger.LogError("SMTP Authentication failed for host {Host}:{Port}: {Error}", _settings.Host, _settings.Port, ex.Message);
                return EmailSendResult.Failed($"SMTP Authentication failed: {ex.Message}", isPermanent: true, (int)sw.ElapsedMilliseconds);
            }
            catch (Exception ex) when (ex is SocketException || ex is IOException || ex is TimeoutException)
            {
                sw.Stop();
                _logger.LogWarning("Transient network error connecting to SMTP server {Host}:{Port}: {Error}", _settings.Host, _settings.Port, ex.Message);
                return EmailSendResult.Failed($"Network error: {ex.Message}", isPermanent: false, (int)sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Unexpected error dispatching email to {Recipient}", actualRecipient);
                return EmailSendResult.Failed($"Unexpected dispatch error: {ex.Message}", isPermanent: false, (int)sw.ElapsedMilliseconds);
            }
        }

        public async Task<EmailSendResult> SendTestEmailAsync(string testRecipient, CancellationToken cancellationToken = default)
        {
            var testMessage = new EmailMessage
            {
                RecipientEmail = testRecipient,
                RecipientName = "LMS Administrator",
                Subject = "EduDigiClass LMS — SMTP Configuration Test",
                EventType = "AdminTestEmail",
                CorrelationId = Guid.NewGuid().ToString("N"),
                HtmlBody = $@"
<div style=""font-family:system-ui,-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;max-width:600px;margin:0 auto;background:#ffffff;border:1px solid #e2e8f0;border-radius:10px;overflow:hidden;box-shadow:0 4px 6px -1px rgba(0,0,0,0.1);"">
    <div style=""background:linear-gradient(135deg,#1e3a8a 0%,#3b82f6 100%);color:#ffffff;padding:24px 30px;"">
        <h1 style=""margin:0;font-size:22px;font-weight:700;letter-spacing:-0.5px;"">EduDigiClass LMS</h1>
        <p style=""margin:4px 0 0 0;font-size:13px;opacity:0.9;"">Automated Email Notification System</p>
    </div>
    <div style=""padding:30px;color:#1e293b;line-height:1.6;"">
        <h2 style=""margin:0 0 16px 0;font-size:18px;color:#0f172a;font-weight:600;"">✅ SMTP Configuration Verification Succeeded!</h2>
        <p>This is a live test notification sent by the administrator to verify that the EduDigiClass LMS email provider connectivity, authentication, and dispatch pipeline are functioning properly.</p>
        <div style=""background:#f8fafc;border-left:4px solid #3b82f6;padding:16px;margin:20px 0;border-radius:0 6px 6px 0;font-size:14px;"">
            <div><strong>Timestamp (UTC):</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</div>
            <div><strong>Configured SMTP Host:</strong> {_settings.Host}:{_settings.Port}</div>
            <div><strong>Sender Address:</strong> {_settings.SenderEmail} ({_settings.SenderDisplayName})</div>
            <div><strong>SSL Enabled:</strong> {_settings.EnableSsl}</div>
            <div><strong>Test Recipient:</strong> {testRecipient}</div>
        </div>
        <p style=""font-size:13px;color:#64748b;"">If you received this message, your SMTP credentials and TLS handshake have been successfully validated by the LMS background infrastructure.</p>
    </div>
    <div style=""background:#f1f5f9;padding:16px 30px;text-align:center;font-size:12px;color:#64748b;border-top:1px solid #e2e8f0;"">
        EduDigiClass LMS Platform &bull; Automated System &bull; Please do not reply directly to this automated email.
    </div>
</div>"
            };

            return await SendEmailAsync(testMessage, cancellationToken);
        }

        private static string HtmlToPlainText(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;
            var text = System.Text.RegularExpressions.Regex.Replace(html, "<style[\\s\\S]*?</style>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            text = System.Text.RegularExpressions.Regex.Replace(text, "<script[\\s\\S]*?</script>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            text = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]+>", " ");
            text = System.Web.HttpUtility.HtmlDecode(text);
            return System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
        }
    }
}
