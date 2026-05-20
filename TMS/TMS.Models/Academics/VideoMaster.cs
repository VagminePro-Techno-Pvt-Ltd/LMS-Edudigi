using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Academics
{
    /// <summary>
    /// A video asset linked to a Topic.
    /// Source can be "Local", "YouTube", or "Vimeo".
    /// </summary>
    [Table(nameof(VideoMaster))]
    public class VideoMaster : BaseModel
    {
        /// <summary>Topic this video belongs to.</summary>
        [Required]
        public int TopicId { get; set; }

        [ForeignKey(nameof(TopicId))]
        public virtual TopicMaster? Topic { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = default!;

        /// <summary>Local server path (used when Source = "Local").</summary>
        [StringLength(500)]
        public string? FilePath { get; set; }

        /// <summary>External video URL (used when Source = "YouTube" or "Vimeo").</summary>
        [StringLength(500)]
        public string? VideoUrl { get; set; }

        /// <summary>Total duration of the video in seconds.</summary>
        [Required]
        public int DurationSeconds { get; set; } = 0;

        /// <summary>Video source type: "Local", "YouTube", "Vimeo".</summary>
        [Required, StringLength(20)]
        public string Source { get; set; } = "Local";
    }
}
