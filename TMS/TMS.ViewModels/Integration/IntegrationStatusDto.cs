using System;
using System.Collections.Generic;

namespace TMS.ViewModels.Integration
{
    public enum IntegrationOperationalStatus
    {
        Configured,
        Disabled,
        Misconfigured,
        Unverified
    }

    public enum IntegrationHealthLevel
    {
        Healthy,
        Degraded,
        Critical,
        Unknown
    }

    public class IntegrationProviderItemDto
    {
        public string ProviderKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // Communication, Video, Cloud Storage, AI, Proctoring
        public string Description { get; set; } = string.Empty;
        public IntegrationOperationalStatus Status { get; set; } = IntegrationOperationalStatus.Unverified;
        public IntegrationHealthLevel HealthLevel { get; set; } = IntegrationHealthLevel.Unknown;
        public string StatusSummary { get; set; } = string.Empty;
        public Dictionary<string, string> MaskedConfig { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public List<string> Warnings { get; set; } = new();
        public string? AdminManagementUrl { get; set; }
        public string? DocumentationHint { get; set; }
        public DateTime? LastCheckedUtc { get; set; }
        public bool SupportsLiveCheck { get; set; }
        public string IconClass { get; set; } = "fa-solid fa-plug";
        public string BadgeColor { get; set; } = "#3b82f6";
    }

    public class IntegrationHubDashboardViewModel
    {
        public int TotalIntegrations => Providers.Count;
        public int ConfiguredCount { get; set; }
        public int DisabledCount { get; set; }
        public int MisconfiguredCount { get; set; }
        public int UnverifiedCount { get; set; }

        public List<IntegrationProviderItemDto> Providers { get; set; } = new();
        public List<IntegrationSanitizedLogDto> RecentActivityLogs { get; set; } = new();
        public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class IntegrationSanitizedLogDto
    {
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public string ProviderKey { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public string Level { get; set; } = "Information";
        public string Message { get; set; } = string.Empty;
        public string? CorrelationId { get; set; }
    }
}
