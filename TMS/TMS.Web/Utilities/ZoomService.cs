using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TMS.ViewModels;

namespace TMS.Utilities
{
    public class ZoomMeetingResult
    {
        public string JoinUrl { get; set; } = string.Empty;
        public string HostUrl { get; set; } = string.Empty;
        public string? MeetingId { get; set; }
        public string? Password { get; set; }
    }

    public class ZoomService
    {
        private readonly HttpClient _client;
        private readonly ZoomSettings _zoomSettings;

        public ZoomService(ZoomSettings zoomSettings)
        {
            _zoomSettings = zoomSettings;
            _client = new HttpClient { BaseAddress = new Uri("https://api.zoom.us/v2/") };
        }

        private async Task<string> GetAccessTokenAsync()
        {
            using var tokenClient = new HttpClient();
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_zoomSettings.ClientId}:{_zoomSettings.ClientSecret}"));

            tokenClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);

            var content = new StringContent($"account_id={_zoomSettings.AccountId}&grant_type=account_credentials",
                Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await tokenClient.PostAsync("https://zoom.us/oauth/token", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("access_token").GetString() ?? string.Empty;
        }

        public async Task<ZoomMeetingResult> CreateZoomMeetingAsync(string topic, DateTime startTime, int durationMinutes, string timezone = "Asia/Kolkata")
        {
            var token = await GetAccessTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

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
            var response = await _client.PostAsync("users/me/meetings",
                new StringContent(json, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            string joinUrl = doc.RootElement.TryGetProperty("join_url", out var joinEl) ? (joinEl.GetString() ?? "") : "";
            string startUrl = doc.RootElement.TryGetProperty("start_url", out var startEl) ? (startEl.GetString() ?? "") : "";
            string meetingId = doc.RootElement.TryGetProperty("id", out var idEl) ? idEl.ToString() : "";
            string password = doc.RootElement.TryGetProperty("password", out var pwdEl) ? (pwdEl.GetString() ?? "") : "";

            return new ZoomMeetingResult
            {
                JoinUrl = string.IsNullOrWhiteSpace(joinUrl) ? startUrl : joinUrl,
                HostUrl = string.IsNullOrWhiteSpace(startUrl) ? joinUrl : startUrl,
                MeetingId = meetingId,
                Password = password
            };
        }

        public async Task<string> CreateMeetingAsync(string topic, DateTime startTime, int durationMinutes, string timezone = "Asia/Kolkata")
        {
            var result = await CreateZoomMeetingAsync(topic, startTime, durationMinutes, timezone);
            return result.JoinUrl;
        }
    }
}
