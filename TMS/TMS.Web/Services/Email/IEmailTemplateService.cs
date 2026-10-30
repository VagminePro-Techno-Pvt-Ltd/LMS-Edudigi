using System.Collections.Generic;

namespace TMS.Web.Services.Email
{
    public class RenderedEmail
    {
        public string Subject { get; set; } = string.Empty;
        public string HtmlBody { get; set; } = string.Empty;
        public string PlainTextBody { get; set; } = string.Empty;
    }

    public interface IEmailTemplateService
    {
        /// <summary>
        /// Renders an email by template name with parameters.
        /// </summary>
        RenderedEmail Render(string templateName, IDictionary<string, string> data);
    }
}
