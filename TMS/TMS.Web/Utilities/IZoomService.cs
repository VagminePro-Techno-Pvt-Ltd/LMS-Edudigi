using System;
using System.Threading;
using System.Threading.Tasks;

namespace TMS.Utilities
{
    public interface IZoomService
    {
        Task<ZoomMeetingResult> CreateZoomMeetingAsync(
            string topic,
            DateTime startTime,
            int durationMinutes,
            string timezone = "Asia/Kolkata",
            CancellationToken cancellationToken = default);

        Task<string> CreateMeetingAsync(
            string topic,
            DateTime startTime,
            int durationMinutes,
            string timezone = "Asia/Kolkata",
            CancellationToken cancellationToken = default);

        Task<bool> ValidateCredentialsAsync(CancellationToken cancellationToken = default);
    }
}
