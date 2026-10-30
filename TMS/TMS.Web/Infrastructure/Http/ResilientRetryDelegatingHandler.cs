using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TMS.ViewModels.Integration;

namespace TMS.Web.Infrastructure.Http
{
    public class IntegrationCircuitBreakerException : HttpRequestException
    {
        public string Host { get; }
        public DateTime OpenUntilUtc { get; }

        public IntegrationCircuitBreakerException(string host, DateTime openUntilUtc)
            : base($"Circuit breaker for host '{host}' is OPEN until {openUntilUtc:yyyy-MM-dd HH:mm:ss} UTC due to repeated failures.")
        {
            Host = host;
            OpenUntilUtc = openUntilUtc;
        }
    }

    public class HostCircuitState
    {
        public int ConsecutiveFailures;
        public DateTime? CircuitOpenUntilUtc;
    }

    public class ResilientRetryDelegatingHandler : DelegatingHandler
    {
        public const string IdempotentHeader = "X-Idempotent";
        public const string RetrySafeHeader = "X-Retry-Safe";

        private readonly IntegrationHttpOptions _options;
        private readonly ILogger<ResilientRetryDelegatingHandler> _logger;
        private static readonly ConcurrentDictionary<string, HostCircuitState> CircuitStates = new(StringComparer.OrdinalIgnoreCase);

        public ResilientRetryDelegatingHandler(
            IOptions<IntegrationHttpOptions> options,
            ILogger<ResilientRetryDelegatingHandler> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var host = request.RequestUri?.Host ?? "unknown";

            // 1. Circuit Breaker Check
            if (_options.EnableCircuitBreaker && IsCircuitOpen(host, out var openUntil))
            {
                _logger.LogWarning("[CIRCUIT BREAKER OPEN] Outbound request to {Host} rejected until {OpenUntil:O}", host, openUntil);
                throw new IntegrationCircuitBreakerException(host, openUntil);
            }

            // 2. Check Idempotency for Retry Eligibility
            bool isIdempotent = IsRequestEligibleForRetry(request);

            int maxAttempts = isIdempotent ? Math.Max(1, _options.MaxRetries + 1) : 1;
            int attempt = 0;
            HttpResponseMessage? lastResponse = null;
            Exception? lastException = null;

            while (attempt < maxAttempts)
            {
                attempt++;
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var response = await base.SendAsync(request, cancellationToken);

                    if (!IsTransientStatusCode(response.StatusCode))
                    {
                        // Success or non-transient status -> reset circuit failure count
                        RecordSuccess(host);
                        return response;
                    }

                    // Transient status received (e.g. 429, 500, 502, 503, 504)
                    lastResponse = response;

                    if (attempt >= maxAttempts)
                    {
                        RecordFailure(host);
                        return response;
                    }

                    var delay = CalculateDelay(attempt, response.Headers.RetryAfter);
                    _logger.LogWarning("[HTTP RETRY {Attempt}/{Max}] Transient status {StatusCode} from {Host}. Backing off for {DelayMs}ms...",
                        attempt, maxAttempts, (int)response.StatusCode, host, (int)delay.TotalMilliseconds);

                    await Task.Delay(delay, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
                {
                    lastException = ex;
                    RecordFailure(host);

                    if (attempt >= maxAttempts || !isIdempotent)
                    {
                        throw;
                    }

                    var delay = CalculateDelay(attempt, null);
                    _logger.LogWarning(ex, "[HTTP RETRY {Attempt}/{Max}] Network exception to {Host} ({Message}). Backing off for {DelayMs}ms...",
                        attempt, maxAttempts, host, ex.Message, (int)delay.TotalMilliseconds);

                    await Task.Delay(delay, cancellationToken);
                }
            }

            RecordFailure(host);

            if (lastResponse != null) return lastResponse;
            if (lastException != null) throw lastException;

            throw new HttpRequestException($"Outbound HTTP request to {host} exhausted retries.");
        }

        public static bool IsRequestEligibleForRetry(HttpRequestMessage request)
        {
            // Standard idempotent HTTP methods
            if (request.Method == HttpMethod.Get ||
                request.Method == HttpMethod.Head ||
                request.Method == HttpMethod.Options ||
                request.Method == HttpMethod.Put ||
                request.Method == HttpMethod.Delete)
            {
                return true;
            }

            // Explicit idempotency markers
            if (request.Headers.Contains(IdempotentHeader) || request.Headers.Contains(RetrySafeHeader))
            {
                return true;
            }

            return false;
        }

        public static bool IsTransientStatusCode(HttpStatusCode statusCode)
        {
            int code = (int)statusCode;
            return code == 408 // Request Timeout
                || code == 429 // Too Many Requests
                || code == 500 // Internal Server Error
                || code == 502 // Bad Gateway
                || code == 503 // Service Unavailable
                || code == 504; // Gateway Timeout
        }

        public TimeSpan CalculateDelay(int attempt, RetryConditionHeaderValue? retryAfter)
        {
            // Respect server-specified Retry-After if available
            if (retryAfter != null)
            {
                if (retryAfter.Delta.HasValue && retryAfter.Delta.Value > TimeSpan.Zero)
                {
                    return TimeSpan.FromMilliseconds(Math.Min(retryAfter.Delta.Value.TotalMilliseconds, _options.MaxDelayMilliseconds));
                }
                if (retryAfter.Date.HasValue)
                {
                    var diff = retryAfter.Date.Value - DateTimeOffset.UtcNow;
                    if (diff > TimeSpan.Zero)
                    {
                        return TimeSpan.FromMilliseconds(Math.Min(diff.TotalMilliseconds, _options.MaxDelayMilliseconds));
                    }
                }
            }

            if (!_options.EnableExponentialBackoff)
            {
                return TimeSpan.FromMilliseconds(_options.BaseDelayMilliseconds);
            }

            // Exponential backoff: BaseDelay * 2^(attempt - 1)
            double exponentialMs = _options.BaseDelayMilliseconds * Math.Pow(2, attempt - 1);
            exponentialMs = Math.Min(exponentialMs, _options.MaxDelayMilliseconds);

            // Full Jitter: random value between 0 and exponentialMs
            if (_options.EnableJitter)
            {
                var jitter = Random.Shared.NextDouble() * (exponentialMs * 0.5);
                exponentialMs = (exponentialMs * 0.5) + jitter;
            }

            return TimeSpan.FromMilliseconds(Math.Max(_options.BaseDelayMilliseconds, exponentialMs));
        }

        private bool IsCircuitOpen(string host, out DateTime openUntil)
        {
            openUntil = DateTime.MinValue;
            if (CircuitStates.TryGetValue(host, out var state))
            {
                if (state.CircuitOpenUntilUtc.HasValue)
                {
                    if (DateTime.UtcNow < state.CircuitOpenUntilUtc.Value)
                    {
                        openUntil = state.CircuitOpenUntilUtc.Value;
                        return true;
                    }
                    // Half-open: lease expired, allow trial request
                    state.CircuitOpenUntilUtc = null;
                }
            }
            return false;
        }

        private void RecordSuccess(string host)
        {
            var state = CircuitStates.GetOrAdd(host, _ => new HostCircuitState());
            Interlocked.Exchange(ref state.ConsecutiveFailures, 0);
            state.CircuitOpenUntilUtc = null;
        }

        private void RecordFailure(string host)
        {
            var state = CircuitStates.GetOrAdd(host, _ => new HostCircuitState());
            int failures = Interlocked.Increment(ref state.ConsecutiveFailures);

            if (_options.EnableCircuitBreaker && failures >= _options.CircuitBreakerFailureThreshold)
            {
                state.CircuitOpenUntilUtc = DateTime.UtcNow.AddSeconds(_options.CircuitBreakerDurationSeconds);
                _logger.LogCritical("[CIRCUIT BREAKER TRIPPED] Host '{Host}' reached {Failures} consecutive failures. Opening circuit for {Sec}s.",
                    host, failures, _options.CircuitBreakerDurationSeconds);
            }
        }

        public static void ResetAllCircuitBreakers()
        {
            CircuitStates.Clear();
        }
    }
}
