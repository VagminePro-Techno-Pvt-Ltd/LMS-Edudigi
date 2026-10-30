using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TMS.Repository;
using TMS.ViewModels;
using TMS.ViewModels.Email;
using TMS.ViewModels.Integration;
using TMS.Web.Infrastructure.Http;
using TMS.Web.Services.Proctoring;

namespace TMS.Web.Services.Integration
{
    public class IntegrationHealthService : IIntegrationHealthService
    {
        private readonly IConfiguration _config;
        private readonly IOptionsMonitor<EmailSettings> _emailOptions;
        private readonly IOptionsMonitor<ZoomSettings> _zoomOptions;
        private readonly IOptionsMonitor<ProctoringApiOptions> _proctoringOptions;
        private readonly ApplicationDBContext _db;
        private readonly ILogger<IntegrationHealthService> _logger;

        public IntegrationHealthService(
            IConfiguration config,
            IOptionsMonitor<EmailSettings> emailOptions,
            IOptionsMonitor<ZoomSettings> zoomOptions,
            IOptionsMonitor<ProctoringApiOptions> proctoringOptions,
            ApplicationDBContext db,
            ILogger<IntegrationHealthService> logger)
        {
            _config = config;
            _emailOptions = emailOptions;
            _zoomOptions = zoomOptions;
            _proctoringOptions = proctoringOptions;
            _db = db;
            _logger = logger;
        }

        public async Task<IntegrationHubDashboardViewModel> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
        {
            var vm = new IntegrationHubDashboardViewModel();

            // 1. SMTP Email Integration
            vm.Providers.Add(await InspectEmailIntegrationAsync(cancellationToken));

            // 2. Zoom Virtual Classroom Integration
            vm.Providers.Add(InspectZoomIntegration());

            // 3. Google Drive Content Sync Integration
            vm.Providers.Add(await InspectContentSyncIntegrationAsync(cancellationToken));

            // 4. Google Gemini AI Integration
            vm.Providers.Add(InspectGeminiIntegration());

            // 5. AI Proctoring Platform Integration
            vm.Providers.Add(InspectProctoringIntegration());

            // 6. Azure Blob Storage Integration
            vm.Providers.Add(InspectAzureStorageIntegration());

            // Aggregate counts
            vm.ConfiguredCount = vm.Providers.Count(p => p.Status == IntegrationOperationalStatus.Configured);
            vm.DisabledCount = vm.Providers.Count(p => p.Status == IntegrationOperationalStatus.Disabled);
            vm.MisconfiguredCount = vm.Providers.Count(p => p.Status == IntegrationOperationalStatus.Misconfigured);
            vm.UnverifiedCount = vm.Providers.Count(p => p.Status == IntegrationOperationalStatus.Unverified);

            // Fetch recent sanitized logs from EmailDeliveryLog or internal jobs
            try
            {
                var recentEmailLogs = await _db.EmailDeliveryLogs
                    .Include(l => l.EmailOutboxJob)
                    .OrderByDescending(l => l.AttemptedAtUtc)
                    .Take(10)
                    .Select(l => new IntegrationSanitizedLogDto
                    {
                        TimestampUtc = l.AttemptedAtUtc,
                        ProviderKey = "SMTP",
                        Operation = "EmailDispatch",
                        Level = l.Status == "Success" ? "Information" : "Warning",
                        Message = l.Status == "Success" 
                            ? $"Dispatched email to {MaskEmail(l.EmailOutboxJob != null ? l.EmailOutboxJob.RecipientEmail : "recipient")} in {l.DurationMs}ms"
                            : $"Delivery issue: {l.ErrorMessage}",
                        CorrelationId = l.EmailOutboxJob != null ? l.EmailOutboxJob.CorrelationId : null
                    })
                    .ToListAsync(cancellationToken);

                vm.RecentActivityLogs.AddRange(recentEmailLogs);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Unable to query delivery logs for Integration Hub: {Message}", ex.Message);
            }

            return vm;
        }

        public async Task<(bool Success, string Message)> CheckProviderHealthAsync(string providerKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(providerKey))
                return (false, "Provider key is required.");

            switch (providerKey.Trim().ToLowerInvariant())
            {
                case "smtp":
                case "email":
                    var emailSettings = GetEffectiveEmailSettings();
                    var (valid, reason) = emailSettings.ValidateConfiguration();
                    if (!valid) return (false, reason ?? "Email configuration invalid.");
                    if (!emailSettings.IsEnabled) return (false, "Email dispatch is disabled in settings.");
                    return (true, $"SMTP configuration is valid ({emailSettings.Host}:{emailSettings.Port}, SSL={emailSettings.EnableSsl}).");

                case "zoom":
                    var zoom = GetEffectiveZoomSettings();
                    if (string.IsNullOrWhiteSpace(zoom.AccountId) || string.IsNullOrWhiteSpace(zoom.ClientId) || string.IsNullOrWhiteSpace(zoom.ClientSecret))
                        return (false, "Zoom AccountId, ClientId, or ClientSecret is missing.");
                    return (true, "Zoom OAuth Server-to-Server credentials are configured.");

                case "gemini":
                    var geminiKey = GetEffectiveGeminiApiKey();
                    if (string.IsNullOrWhiteSpace(geminiKey))
                        return (false, "Gemini API key is not configured.");
                    var model = _config["Gemini:Model"] ?? "gemini-1.5-flash";
                    return (true, $"Gemini API key is configured for model '{model}'.");

                case "contentsync":
                case "googledrive":
                    try
                    {
                        var config = await _db.ContentSyncConfigs.FirstOrDefaultAsync(cancellationToken);
                        if (config == null) return (false, "ContentSync database configuration not found.");
                        return (true, $"ContentSync active: LocalDrive={config.LocalDriveEnabled}, GoogleDrive={config.GoogleDriveEnabled}.");
                    }
                    catch (Exception ex)
                    {
                        return (false, $"ContentSync database read error: {ex.Message}");
                    }

                case "proctoring":
                    var proctoring = _proctoringOptions.CurrentValue;
                    if (string.IsNullOrWhiteSpace(proctoring.ApiBaseUrl))
                        return (false, "Proctoring Platform ApiBaseUrl is missing.");
                    return (true, $"Proctoring API configured targeting {proctoring.ApiBaseUrl}");

                default:
                    return (false, $"Provider '{providerKey}' does not support live health diagnostics.");
            }
        }

        // =========================================================================
        // Private Providers Inspection Helpers
        // =========================================================================

        private async Task<IntegrationProviderItemDto> InspectEmailIntegrationAsync(CancellationToken cancellationToken)
        {
            var settings = GetEffectiveEmailSettings();
            var (isValid, errorReason) = settings.ValidateConfiguration();

            int pendingCount = 0;
            int failedCount = 0;
            try
            {
                pendingCount = await _db.EmailOutboxJobs.CountAsync(j => j.Status == "Pending" || j.Status == "Processing", cancellationToken);
                failedCount = await _db.EmailOutboxJobs.CountAsync(j => j.Status == "Failed", cancellationToken);
            }
            catch { }

            var item = new IntegrationProviderItemDto
            {
                ProviderKey = "SMTP",
                DisplayName = "SMTP Email Delivery (MailKit)",
                Category = "Communications",
                Description = "Dispatches institutional emails, reminders, absence notices, and examination alerts via background outbox queue.",
                AdminManagementUrl = "/Admin/EmailAdmin",
                DocumentationHint = "Configure SMTP credentials or Gmail App Password in EmailSettings section or environment variables.",
                IconClass = "fa-solid fa-envelope",
                BadgeColor = "#2563eb",
                SupportsLiveCheck = true
            };

            if (!settings.IsEnabled)
            {
                item.Status = IntegrationOperationalStatus.Disabled;
                item.HealthLevel = IntegrationHealthLevel.Unknown;
                item.StatusSummary = "Disabled (IsEnabled = false). Email dispatch loop will skip sending.";
            }
            else if (!isValid)
            {
                item.Status = IntegrationOperationalStatus.Misconfigured;
                item.HealthLevel = IntegrationHealthLevel.Critical;
                item.StatusSummary = errorReason ?? "Configuration incomplete.";
                item.Warnings.Add(errorReason ?? "Invalid configuration.");
            }
            else
            {
                item.Status = IntegrationOperationalStatus.Configured;
                item.HealthLevel = failedCount > 10 ? IntegrationHealthLevel.Degraded : IntegrationHealthLevel.Healthy;
                item.StatusSummary = $"Operational ({settings.Host}:{settings.Port}, Queue: {pendingCount} pending, {failedCount} failed).";
            }

            item.MaskedConfig["Host"] = settings.Host ?? "[Not set]";
            item.MaskedConfig["Port"] = settings.Port.ToString();
            item.MaskedConfig["Sender"] = settings.SenderEmail ?? "[Not set]";
            item.MaskedConfig["Username"] = MaskEmail(settings.UserName);
            item.MaskedConfig["Password"] = IntegrationUrlSanitizer.MaskSecret(settings.Password);
            item.MaskedConfig["TestMode"] = settings.TestMode ? $"True (Constrained to {settings.TestRecipientEmail})" : "False (Live Delivery)";

            return item;
        }

        private IntegrationProviderItemDto InspectZoomIntegration()
        {
            var zoom = GetEffectiveZoomSettings();
            var hasAccount = !string.IsNullOrWhiteSpace(zoom.AccountId);
            var hasClient = !string.IsNullOrWhiteSpace(zoom.ClientId);
            var hasSecret = !string.IsNullOrWhiteSpace(zoom.ClientSecret);

            var item = new IntegrationProviderItemDto
            {
                ProviderKey = "Zoom",
                DisplayName = "Zoom REST API v2 (Virtual Classroom)",
                Category = "Video Conferencing",
                Description = "Provisions scheduled live virtual classrooms, interactive webinars, and generates secure host/participant URLs.",
                AdminManagementUrl = "/VirtualClassroom",
                DocumentationHint = "Requires Zoom Server-to-Server OAuth app credentials in ZoomSettings section or environment variables.",
                IconClass = "fa-solid fa-video",
                BadgeColor = "#0b5cff",
                SupportsLiveCheck = true
            };

            if (hasAccount && hasClient && hasSecret)
            {
                item.Status = IntegrationOperationalStatus.Configured;
                item.HealthLevel = IntegrationHealthLevel.Healthy;
                item.StatusSummary = "Server-to-Server OAuth credentials configured.";
            }
            else
            {
                item.Status = IntegrationOperationalStatus.Misconfigured;
                item.HealthLevel = IntegrationHealthLevel.Critical;
                item.StatusSummary = "Missing Zoom OAuth credentials.";
                item.Warnings.Add("Zoom ClientId, ClientSecret, or AccountId is not configured.");
            }

            item.MaskedConfig["AccountId"] = IntegrationUrlSanitizer.MaskSecret(zoom.AccountId);
            item.MaskedConfig["ClientId"] = IntegrationUrlSanitizer.MaskSecret(zoom.ClientId);
            item.MaskedConfig["ClientSecret"] = IntegrationUrlSanitizer.MaskSecret(zoom.ClientSecret);

            return item;
        }

        private async Task<IntegrationProviderItemDto> InspectContentSyncIntegrationAsync(CancellationToken cancellationToken)
        {
            var item = new IntegrationProviderItemDto
            {
                ProviderKey = "GoogleDrive",
                DisplayName = "Google Drive & Local Content Sync",
                Category = "Cloud Storage",
                Description = "Two-way folder provisioning and lecture material synchronization between LMS and Google Drive / Local storage.",
                AdminManagementUrl = "/Admin/ContentSync",
                DocumentationHint = "Manage storage provider status, OAuth tokens, and synchronization schedules in the ContentSync Hub.",
                IconClass = "fa-brands fa-google-drive",
                BadgeColor = "#059669",
                SupportsLiveCheck = true
            };

            try
            {
                var config = await _db.ContentSyncConfigs.FirstOrDefaultAsync(cancellationToken);
                var clientId = _config["ContentSync:GoogleDriveClientId"] ?? Environment.GetEnvironmentVariable("GOOGLE_DRIVE_CLIENT_ID");
                var keyPath = _config["ContentSync:GoogleServiceAccountKeyPath"] ?? Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");

                item.MaskedConfig["LocalDrive"] = config?.LocalDriveEnabled == true ? $"Enabled ({config.LocalDriveRootPath})" : "Disabled";
                item.MaskedConfig["GoogleDrive"] = config?.GoogleDriveEnabled == true ? "Enabled" : "Disabled (Default)";
                item.MaskedConfig["ClientId"] = IntegrationUrlSanitizer.MaskSecret(clientId);
                item.MaskedConfig["ServiceAccount"] = !string.IsNullOrWhiteSpace(keyPath) ? keyPath : "[None]";

                if (config?.GoogleDriveEnabled == true)
                {
                    item.Status = IntegrationOperationalStatus.Configured;
                    item.HealthLevel = config.GoogleDriveNetworkDisabled ? IntegrationHealthLevel.Degraded : IntegrationHealthLevel.Healthy;
                    item.StatusSummary = config.GoogleDriveNetworkDisabled ? "Network Disabled (Offline mode)" : "Active two-way synchronization.";
                }
                else
                {
                    item.Status = IntegrationOperationalStatus.Configured;
                    item.HealthLevel = IntegrationHealthLevel.Healthy;
                    item.StatusSummary = "Local Drive active. Google Drive is disabled.";
                }
            }
            catch (Exception ex)
            {
                item.Status = IntegrationOperationalStatus.Unverified;
                item.HealthLevel = IntegrationHealthLevel.Unknown;
                item.StatusSummary = "Unable to read configuration: " + ex.Message;
            }

            return item;
        }

        private IntegrationProviderItemDto InspectGeminiIntegration()
        {
            var key = GetEffectiveGeminiApiKey();
            var model = _config["Gemini:Model"] ?? Environment.GetEnvironmentVariable("GEMINI_MODEL") ?? "gemini-1.5-flash";
            var timeout = _config.GetValue<int>("Gemini:TimeoutSeconds", 45);

            var item = new IntegrationProviderItemDto
            {
                ProviderKey = "Gemini",
                DisplayName = "Google Gemini AI (Curriculum & Evaluation)",
                Category = "Artificial Intelligence",
                Description = "Powers descriptive internal assessment evaluations, curriculum question bank generation, and student learning companion.",
                AdminManagementUrl = "/InternalAssessment/AdminDashboard",
                DocumentationHint = "Set Gemini:ApiKey in appsettings.json or GEMINI_API_KEY environment variable.",
                IconClass = "fa-solid fa-brain",
                BadgeColor = "#7c3aed",
                SupportsLiveCheck = true
            };

            item.MaskedConfig["Model"] = model;
            item.MaskedConfig["Timeout"] = $"{timeout} seconds";
            item.MaskedConfig["ApiKey"] = IntegrationUrlSanitizer.MaskSecret(key, 6, 4);

            if (!string.IsNullOrWhiteSpace(key))
            {
                item.Status = IntegrationOperationalStatus.Configured;
                item.HealthLevel = IntegrationHealthLevel.Healthy;
                item.StatusSummary = $"Active (Model: {model}, Timeout: {timeout}s).";
            }
            else
            {
                item.Status = IntegrationOperationalStatus.Misconfigured;
                item.HealthLevel = IntegrationHealthLevel.Critical;
                item.StatusSummary = "Gemini API key is not configured.";
                item.Warnings.Add("AI evaluation and suggested questions will fail until an API key is provided.");
            }

            return item;
        }

        private IntegrationProviderItemDto InspectProctoringIntegration()
        {
            var opts = _proctoringOptions.CurrentValue;
            var item = new IntegrationProviderItemDto
            {
                ProviderKey = "Proctoring",
                DisplayName = "AI Proctoring Platform Microservice",
                Category = "Assessment Security",
                Description = "External proctoring platform managing secure browser enforcement, webcam monitoring, and incident auditing.",
                AdminManagementUrl = "/Admin/ProctoringExam",
                DocumentationHint = "Set ProctoringPlatform in appsettings.json with ApiBaseUrl and JwtSecret.",
                IconClass = "fa-solid fa-shield-halved",
                BadgeColor = "#16a34a",
                SupportsLiveCheck = true
            };

            item.MaskedConfig["ApiBaseUrl"] = opts.ApiBaseUrl ?? "[Not set]";
            item.MaskedConfig["FrontendUrl"] = opts.FrontendBaseUrl ?? "[Not set]";
            item.MaskedConfig["JwtIssuer"] = opts.JwtIssuer ?? "[Not set]";
            item.MaskedConfig["JwtSecret"] = IntegrationUrlSanitizer.MaskSecret(opts.JwtSecret);

            if (!string.IsNullOrWhiteSpace(opts.ApiBaseUrl) && !string.IsNullOrWhiteSpace(opts.JwtSecret))
            {
                item.Status = IntegrationOperationalStatus.Configured;
                item.HealthLevel = IntegrationHealthLevel.Healthy;
                item.StatusSummary = $"Configured targeting {opts.ApiBaseUrl}.";
            }
            else
            {
                item.Status = IntegrationOperationalStatus.Misconfigured;
                item.HealthLevel = IntegrationHealthLevel.Critical;
                item.StatusSummary = "Proctoring Platform API configuration incomplete.";
                item.Warnings.Add("ApiBaseUrl or JwtSecret is missing.");
            }

            return item;
        }

        private IntegrationProviderItemDto InspectAzureStorageIntegration()
        {
            var connStr = _config["StorageSettings:StorageConnectionString"];
            var isPlaceholder = string.IsNullOrWhiteSpace(connStr) || connStr.Equals("sas", StringComparison.OrdinalIgnoreCase);

            var item = new IntegrationProviderItemDto
            {
                ProviderKey = "AzureStorage",
                DisplayName = "Azure Blob Storage (Legacy Media)",
                Category = "Cloud Storage",
                Description = "Legacy storage for company logos and certificate templates.",
                DocumentationHint = "Configure Azure StorageConnectionString in StorageSettings section if Azure storage is required.",
                IconClass = "fa-solid fa-cloud-arrow-up",
                BadgeColor = "#0284c7",
                SupportsLiveCheck = false
            };

            item.MaskedConfig["ConnectionString"] = isPlaceholder ? "[Placeholder 'sas' - Inactive]" : IntegrationUrlSanitizer.MaskSecret(connStr);
            item.MaskedConfig["Container"] = _config["StorageSettings:ContainerName"] ?? "[None]";

            if (isPlaceholder)
            {
                item.Status = IntegrationOperationalStatus.Disabled;
                item.HealthLevel = IntegrationHealthLevel.Unknown;
                item.StatusSummary = "Inactive (placeholder credentials). LMS is operating on local storage.";
            }
            else
            {
                item.Status = IntegrationOperationalStatus.Configured;
                item.HealthLevel = IntegrationHealthLevel.Healthy;
                item.StatusSummary = "Configured for Azure Blob storage.";
            }

            return item;
        }

        // =========================================================================
        // Effective Settings Resolvers (Env Var > Config)
        // =========================================================================

        private EmailSettings GetEffectiveEmailSettings()
        {
            var s = _emailOptions.CurrentValue;
            var clone = new EmailSettings
            {
                Host = Environment.GetEnvironmentVariable("EMAIL_HOST") ?? s.Host,
                Port = int.TryParse(Environment.GetEnvironmentVariable("EMAIL_PORT"), out int p) ? p : s.Port,
                UserName = Environment.GetEnvironmentVariable("EMAIL_USERNAME") ?? s.UserName,
                Password = Environment.GetEnvironmentVariable("EMAIL_PASSWORD") ?? s.Password,
                SenderEmail = Environment.GetEnvironmentVariable("EMAIL_SENDER_EMAIL") ?? s.SenderEmail,
                SenderDisplayName = Environment.GetEnvironmentVariable("EMAIL_SENDER_DISPLAY_NAME") ?? s.SenderDisplayName,
                EnableSsl = bool.TryParse(Environment.GetEnvironmentVariable("EMAIL_ENABLE_SSL"), out bool ssl) ? ssl : s.EnableSsl,
                IsEnabled = bool.TryParse(Environment.GetEnvironmentVariable("EMAIL_IS_ENABLED"), out bool en) ? en : s.IsEnabled,
                TestMode = bool.TryParse(Environment.GetEnvironmentVariable("EMAIL_TEST_MODE"), out bool tm) ? tm : s.TestMode,
                TestRecipientEmail = Environment.GetEnvironmentVariable("EMAIL_TEST_RECIPIENT") ?? s.TestRecipientEmail,
                MaxRetries = s.MaxRetries,
                BatchSize = s.BatchSize,
                DispatchIntervalSeconds = s.DispatchIntervalSeconds,
                RateLimitPerMinute = s.RateLimitPerMinute,
                TimeoutSeconds = s.TimeoutSeconds
            };
            return clone;
        }

        private ZoomSettings GetEffectiveZoomSettings()
        {
            var s = _zoomOptions.CurrentValue;
            return new ZoomSettings
            {
                AccountId = Environment.GetEnvironmentVariable("ZOOM_ACCOUNT_ID") ?? s.AccountId,
                ClientId = Environment.GetEnvironmentVariable("ZOOM_CLIENT_ID") ?? s.ClientId,
                ClientSecret = Environment.GetEnvironmentVariable("ZOOM_CLIENT_SECRET") ?? s.ClientSecret
            };
        }

        private string? GetEffectiveGeminiApiKey()
        {
            return Environment.GetEnvironmentVariable("GEMINI_API_KEY") 
                   ?? _config["Gemini:ApiKey"] 
                   ?? _config["GEMINI_API_KEY"];
        }

        private static string MaskEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return "[Not configured]";
            var parts = email.Split('@');
            if (parts.Length != 2) return IntegrationUrlSanitizer.MaskSecret(email);
            var user = parts[0];
            var domain = parts[1];
            var maskedUser = user.Length <= 2 ? user + "***" : user.Substring(0, 2) + "***";
            return $"{maskedUser}@{domain}";
        }
    }
}
