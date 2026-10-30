using System;

namespace TMS.ViewModels.Integration
{
    public class IntegrationHttpOptions
    {
        public const string SectionName = "IntegrationHttpOptions";

        public int TimeoutSeconds { get; set; } = 30;
        public int MaxRetries { get; set; } = 3;
        public int BaseDelayMilliseconds { get; set; } = 400;
        public int MaxDelayMilliseconds { get; set; } = 8000;
        public bool EnableExponentialBackoff { get; set; } = true;
        public bool EnableJitter { get; set; } = true;
        public bool EnableCircuitBreaker { get; set; } = true;
        public int CircuitBreakerFailureThreshold { get; set; } = 5;
        public int CircuitBreakerDurationSeconds { get; set; } = 30;
    }
}
