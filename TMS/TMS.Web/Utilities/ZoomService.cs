using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TMS.ViewModels;

namespace TMS.Utilities
{
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
            return doc.RootElement.GetProperty("access_token").GetString();
        }

        public async Task<string> CreateMeetingAsync(string topic, DateTime startTime, int durationMinutes, string timezone = "Asia/Kolkata")
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
                    join_before_host = false,
                    mute_upon_entry = true
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var response = await _client.PostAsync("users/me/meetings",
                new StringContent(json, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            return doc.RootElement.GetProperty("join_url").GetString();
        }
    }
}
