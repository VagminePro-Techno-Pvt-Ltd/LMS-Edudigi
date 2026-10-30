using System.Threading;
using System.Threading.Tasks;
using TMS.ViewModels.Email;

namespace TMS.Web.Services.Email
{
    public interface IEmailSender
    {
        /// <summary>
        /// Sends an email message asynchronously via SMTP.
        /// </summary>
        Task<EmailSendResult> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends an administrative test email to the designated test recipient to verify configuration.
        /// </summary>
        Task<EmailSendResult> SendTestEmailAsync(string testRecipient, CancellationToken cancellationToken = default);
    }
}
