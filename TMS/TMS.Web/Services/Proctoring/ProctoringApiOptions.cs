using System;

namespace TMS.Web.Services.Proctoring
{
    public class ProctoringApiOptions
    {
        public const string SectionName = "ProctoringPlatform";

        /// <summary>
        /// Base URL of the ProctoringService (default http://localhost:5004)
        /// </summary>
        public string ApiBaseUrl { get; set; } = "http://localhost:5004";

        /// <summary>
        /// Base URL of the AuthService (default http://localhost:5003)
        /// </summary>
        public string AuthBaseUrl { get; set; } = "http://localhost:5003";

        /// <summary>
        /// Base URL of the ApiGateway (default http://localhost:5001)
        /// </summary>
        public string GatewayBaseUrl { get; set; } = "http://localhost:5001";

        /// <summary>
        /// Base URL of the React Web Frontend (default http://localhost:5173 or :80)
        /// </summary>
        public string FrontendBaseUrl { get; set; } = "http://localhost:5173";

        /// <summary>
        /// HMAC-SHA256 signing secret shared with AI Proctoring services
        /// </summary>
        public string JwtSecret { get; set; } = "AIProctoringSuperSecretKey2026!@#$%^&*()";

        /// <summary>
        /// Default admin issuer & audience
        /// </summary>
        public string JwtIssuer { get; set; } = "AIProctoring";
        public string JwtAudience { get; set; } = "AIProctoring";

        public int TimeoutSeconds { get; set; } = 30;
    }
}
