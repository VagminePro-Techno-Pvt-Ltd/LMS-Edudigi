
    using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TMS.Common;
using TMS.Models;
using TMS.ViewModels.Masters;

namespace TMS.ViewModels.Academics
    {
        public class CourseMeetingViewModel : BaseViewModel
        {
        [Required]
        [MapToDTO]
        public string? MeetingTitle { get; set; }
        [MapToDTO]
        [Required]
        public int CourseId { get; set; }
        [MapToDTO]
        [Required]
        public int CourseQuadrantId { get; set; }

        [MapToDTO]
        [Required]
        public DateTime ScheduledAt { get; set; }

        [MapToDTO]
        [Required]
        public string MeetingUrl { get; set; } = string.Empty;
        [Required]
        [MapToDTO]
        public int Duration { get; set; }

        [MapToDTO]
        public string? MeetingProvider { get; set; } = "External";

        public string? CourseName { get; set; }
        public string? Description { get; set; }
    }
}


