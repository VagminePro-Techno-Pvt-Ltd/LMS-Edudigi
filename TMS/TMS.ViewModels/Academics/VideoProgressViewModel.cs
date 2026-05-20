using System.ComponentModel.DataAnnotations;
using TMS.ViewModels;
using TMS.ViewModels.Masters;

namespace TMS.ViewModels.Academics
{
    public class VideoProgressViewModel : BaseViewModel
    {
        [Required]
        public int StudentId { get; set; }

        [Required]
        public int VideoId { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Current time must be non-negative")]
        public int CurrentTimeSeconds { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Max watched seconds must be non-negative")]
        public int MaxWatchedSeconds { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Total watch time must be non-negative")]
        public int TotalWatchTimeSeconds { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Skip violations must be non-negative")]
        public int SkipViolations { get; set; }

        public DateTime SessionStartTime { get; set; }

        public DateTime LastUpdated { get; set; }

        [Range(0, 100, ErrorMessage = "Completion percentage must be between 0 and 100")]
        public decimal CompletionPercentage { get; set; }

        public bool IsCompleted { get; set; }

        // Navigation properties
        public virtual UserViewModel? Student { get; set; }
        public virtual LectureMaterialViewModel? Video { get; set; }

        // Calculated properties
        public TimeSpan CurrentTimeSpan => TimeSpan.FromSeconds(CurrentTimeSeconds);
        public TimeSpan MaxWatchedTimeSpan => TimeSpan.FromSeconds(MaxWatchedSeconds);
        public TimeSpan TotalWatchTimeSpan => TimeSpan.FromSeconds(TotalWatchTimeSeconds);

        public string CurrentTimeFormatted => 
            $"{(int)CurrentTimeSpan.TotalMinutes:D2}:{CurrentTimeSpan.Seconds:D2}";

        public string MaxWatchedTimeFormatted => 
            $"{(int)MaxWatchedTimeSpan.TotalMinutes:D2}:{MaxWatchedTimeSpan.Seconds:D2}";

        public string TotalWatchTimeFormatted => 
            $"{(int)TotalWatchTimeSpan.TotalMinutes:D2}:{TotalWatchTimeSpan.Seconds:D2}";

        // Validation methods
        public bool IsProgressValid()
        {
            return CurrentTimeSeconds >= 0 && 
                   MaxWatchedSeconds >= CurrentTimeSeconds && 
                   TotalWatchTimeSeconds >= 0 &&
                   SkipViolations >= 0;
        }

        public bool HasSuspiciousActivity()
        {
            // Check for excessive skip violations
            if (SkipViolations > 15) return true;

            // Check for unrealistic watch time ratio
            var sessionDuration = DateTime.UtcNow - SessionStartTime;
            if (TotalWatchTimeSeconds > sessionDuration.TotalSeconds * 1.2) return true;

            return false;
        }

        public void UpdateProgress(int currentSeconds, int maxSeconds, int watchTime, int violations)
        {
            CurrentTimeSeconds = currentSeconds;
            MaxWatchedSeconds = Math.Max(MaxWatchedSeconds, maxSeconds);
            TotalWatchTimeSeconds = watchTime;
            SkipViolations = violations;
            LastUpdated = DateTime.UtcNow;
        }

        public void CalculateCompletion(int videoDurationSeconds)
        {
            if (videoDurationSeconds > 0)
            {
                CompletionPercentage = Math.Min(100, (decimal)MaxWatchedSeconds / videoDurationSeconds * 100);
                IsCompleted = CompletionPercentage >= 90; // Consider 90% as completed
            }
        }
    }
}