using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Academics
{
    [Table("CourseMeetings")]
    public class CourseMeeting:BaseModel
    {
        [Required]
        public string? MeetingTitle { get; set; }
        [Required]
        public int CourseId { get; set; }
        [Required]
        public int Duration { get; set; }
        [Required]
        public int CourseQuadrantId { get; set; }

        [Required]
        public DateTime ScheduledAt { get; set; }

        [Required]
        public string MeetingUrl { get; set; } = string.Empty;

         
    }
}
