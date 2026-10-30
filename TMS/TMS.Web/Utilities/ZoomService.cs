using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TMS.ViewModels;
using TMS.Web.Infrastructure.Http;

namespace TMS.Utilities
{
    public class ZoomMeetingResult
    {
        public string JoinUrl { get; set; } = string.Empty;
        public string HostUrl { get; set; } = string.Empty;
        public string? MeetingId { get; set; }
        public string? Password { get; set; }
    }

    public class ZoomService : IZoomService
    {
        private readonly HttpClient _client;
        private readonly ZoomSettings _zoomSettings;
        private readonly ILogger<ZoomService>? _logger;

        // In-memory token cache with thread-safe acquisition
        private static string? _cachedToken;
        private static DateTimeOffset _tokenExpiresUtc = DateTimeOffset.MinValue;
        private static readonly SemaphoreSlim _tokenLock = new(1, 1);

        /// <summary>
        /// Primary DI constructor using pooled HttpClient and configured resilience pipeline
        /// </summary>
        public ZoomService(
            HttpClient client,
            IOptions<ZoomSettings> zoomSettings,
            ILogger<ZoomService>? logger = null)
        {
            _zoomSettings = zoomSettings.Value ?? new ZoomSettings();
            _client = client;
            _logger = logger;

            if (_client.BaseAddress == null)
            {
                _client.BaseAddress = new Uri("https://api.zoom.us/v2/");
            }
        }

        /// <summary>
        /// Backward-compatible constructor for legacy callers passing ZoomSettings
        /// </summary>
        public ZoomService(ZoomSettings zoomSettings)
        {
            _zoomSettings = zoomSettings ?? new ZoomSettings();
            _client = new HttpClient { BaseAddress = new Uri("https://api.zoom.us/v2/") };
            _logger = null;
        }

        /// <summary>
        /// Backward-compatible constructor for legacy callers passing IOptions
        /// </summary>
        public ZoomService(IOptions<ZoomSettings> zoomSettings) : this(zoomSettings?.Value ?? new ZoomSettings())
        {
        }

        public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            // 1. Fast-path check from cached token
            if (!string.IsNullOrEmpty(_cachedToken) && DateTimeOffset.UtcNow < _tokenExpiresUtc)
            {
                return _cachedToken;
            }

            await _tokenLock.WaitAsync(cancellationToken);
            try
            {
                // Double-check after acquiring lock
                if (!string.IsNullOrEmpty(_cachedToken) && DateTimeOffset.UtcNow < _tokenExpiresUtc)
                {
                    return _cachedToken;
                }

                // Resolve effective credentials (environment variables take precedence)
                var accountId = Environment.GetEnvironmentVariable("ZOOM_ACCOUNT_ID") ?? _zoomSettings.AccountId;
                var clientId = Environment.GetEnvironmentVariable("ZOOM_CLIENT_ID") ?? _zoomSettings.ClientId;
                var clientSecret = Environment.GetEnvironmentVariable("ZOOM_CLIENT_SECRET") ?? _zoomSettings.ClientSecret;

                if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
                {
                    throw new InvalidOperationException("Zoom credentials incomplete. AccountId, ClientId, and ClientSecret are required.");
                }

                var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

                using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://zoom.us/oauth/token");
                tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
                tokenRequest.Headers.Add(ResilientRetryDelegatingHandler.RetrySafeHeader, "true");
                tokenRequest.Content = new StringContent(
                    $"account_id={Uri.EscapeDataString(accountId)}&grant_type=account_credentials",
                    Encoding.UTF8,
                    "application/x-www-form-urlencoded");

                _logger?.LogInformation("[Zoom] Requesting new OAuth access token for AccountId: {AccountId}",
                    IntegrationUrlSanitizer.MaskSecret(accountId));

                // Use the pooled _client or token request
                using var response = await _client.SendAsync(tokenRequest, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);

                var token = doc.RootElement.GetProperty("access_token").GetString() ?? string.Empty;
                var expiresInSeconds = doc.RootElement.TryGetProperty("expires_in", out var expEl) ? expEl.GetInt32() : 3600;

                // Cache with 3-minute safety buffer prior to expiration
                _cachedToken = token;
                _tokenExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, expiresInSeconds - 180));

                _logger?.LogInformation("[Zoom] OAuth access token acquired and cached for {Sec} seconds.", expiresInSeconds);
                return _cachedToken;
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        public async Task<ZoomMeetingResult> CreateZoomMeetingAsync(
            string topic,
            DateTime startTime,
            int durationMinutes,
            string timezone = "Asia/Kolkata",
            CancellationToken cancellationToken = default)
        {
            var token = await GetAccessTokenAsync(cancellationToken);

            var payload = new
            {
                topic = topic,
                type = 2, // Scheduled meeting
                start_time = startTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                duration = durationMinutes,
                timezone = timezone,
                settings = new
                {
                    host_video = true,
                    participant_video = true,
                    join_before_host = true,
                    waiting_room = false,
                    mute_upon_entry = false,
                    approval_type = 2, // Automatically approve / no registration required
                    audio = "both",
                    auto_recording = "none"
                }
            };

            var json = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, "users/me/meetings")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add(ResilientRetryDelegatingHandler.RetrySafeHeader, "true");

            _logger?.LogInformation("[Zoom] Creating live meeting: \"{Topic}\" scheduled at {StartTime} ({Duration} mins)",
                topic, startTime, durationMinutes);

            using var response = await _client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseJson);

            string joinUrl = doc.RootElement.TryGetProperty("join_url", out var joinEl) ? (joinEl.GetString() ?? "") : "";
            string startUrl = doc.RootElement.TryGetProperty("start_url", out var startEl) ? (startEl.GetString() ?? "") : "";
            string meetingId = doc.RootElement.TryGetProperty("id", out var idEl) ? idEl.ToString() : "";
            string password = doc.RootElement.TryGetProperty("password", out var pwdEl) ? (pwdEl.GetString() ?? "") : "";

            _logger?.LogInformation("[Zoom] Meeting created successfully: ID {MeetingId}", meetingId);

            return new ZoomMeetingResult
            {
                JoinUrl = string.IsNullOrWhiteSpace(joinUrl) ? startUrl : joinUrl,
                HostUrl = string.IsNullOrWhiteSpace(startUrl) ? joinUrl : startUrl,
                MeetingId = meetingId,
                Password = password
            };
        }

        public async Task<string> CreateMeetingAsync(
            string topic,
            DateTime startTime,
            int durationMinutes,
            string timezone = "Asia/Kolkata",
            CancellationToken cancellationToken = default)
        {
            var result = await CreateZoomMeetingAsync(topic, startTime, durationMinutes, timezone, cancellationToken);
            return result.JoinUrl;
        }

        public async Task<bool> ValidateCredentialsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var token = await GetAccessTokenAsync(cancellationToken);
                return !string.IsNullOrWhiteSpace(token);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "[Zoom] Credential validation failed: {Message}", ex.Message);
                return false;
            }
        }

        // Test helper to clear cache in unit tests
        public static void ClearTokenCacheForTesting()
        {
            _cachedToken = null;
            _tokenExpiresUtc = DateTimeOffset.MinValue;
        }
    }
}
