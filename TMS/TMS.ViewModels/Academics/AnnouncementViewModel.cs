using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Common;
using TMS.ViewModels.Masters;

namespace TMS.ViewModels.Academics
{
    public class AnnouncementViewModel : BaseViewModel
    {
        [MapToDTO, Required, StringLength(250)]
        [Display(Name = "Title")]
        public string Title { get; set; } = default!;

        [MapToDTO, Required]
        [Display(Name = "Description")]
        public string Description { get; set; } = default!;

        /// <summary>
        /// 0 = All Users, 1 = Specific Role, 2 = Specific Course, 3 = Specific Department
        /// </summary>
        [MapToDTO, Required]
        [Display(Name = "Target Audience")]
        public int TargetType { get; set; } = 0;

        [MapToDTO]
        [Display(Name = "Target")]
        public int? TargetId { get; set; }

        /// <summary>
        /// 0 = General, 1 = Event, 2 = Urgent
        /// </summary>
        [MapToDTO, Required]
        [Display(Name = "Announcement Type")]
        public int AnnouncementType { get; set; } = 0;

        [MapToDTO, Required]
        [Display(Name = "Publish Date")]
        public DateTime PublishDate { get; set; } = DateTime.Now;

        [MapToDTO]
        [Display(Name = "Expiry Date")]
        public DateTime? ExpiryDate { get; set; }

        [MapToDTO, StringLength(500)]
        [Display(Name = "Attachment")]
        public string? AttachmentUrl { get; set; }

        [MapToDTO]
        [Display(Name = "Pinned")]
        public bool IsPinned { get; set; } = false;

        [MapToDTO]
        public bool IsDeleted { get; set; } = false;

        // ─── Display name for the target ───
        [NotMapped]
        public string? TargetName { get; set; }

        // ─── Computed Properties ───
        [NotMapped]
        public string TargetTypeText => TargetType switch
        {
            0 => "All Users",
            1 => "Specific Role",
            2 => "Specific Course",
            3 => "Specific Department",
            _ => "Unknown"
        };

        [NotMapped]
        public string AnnouncementTypeText => AnnouncementType switch
        {
            0 => "General",
            1 => "Event",
            2 => "Urgent",
            _ => "Unknown"
        };

        [NotMapped]
        public string AnnouncementTypeBadgeClass => AnnouncementType switch
        {
            1 => "ann-badge-event",
            2 => "ann-badge-urgent",
            _ => "ann-badge-general"
        };

        [NotMapped]
        public string AnnouncementTypeIcon => AnnouncementType switch
        {
            1 => "fa-solid fa-calendar-star",
            2 => "fa-solid fa-triangle-exclamation",
            _ => "fa-solid fa-circle-info"
        };

        [NotMapped]
        public string PublishDateStr => PublishDate.ToString("dd MMM yyyy, hh:mm tt");

        [NotMapped]
        public string? ExpiryDateStr => ExpiryDate?.ToString("dd MMM yyyy, hh:mm tt");

        [NotMapped]
        public bool IsPublished => PublishDate <= DateTime.Now && (ExpiryDate == null || ExpiryDate >= DateTime.Now);

        [NotMapped]
        public bool IsScheduled => PublishDate > DateTime.Now;

        [NotMapped]
        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate < DateTime.Now;

        [NotMapped]
        public string StatusText => IsExpired ? "Expired" : IsScheduled ? "Scheduled" : "Published";

        [NotMapped]
        public string StatusBadgeClass => IsExpired ? "ann-status-expired" : IsScheduled ? "ann-status-scheduled" : "ann-status-published";

        [NotMapped]
        public string AuthorName => CreatedByUser?.Name ?? "System";

        [NotMapped]
        public string TimeAgo
        {
            get
            {
                var span = DateTime.Now - PublishDate;
                if (span.TotalMinutes < 1) return "Just now";
                if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
                if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
                if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
                return PublishDate.ToString("dd MMM yyyy");
            }
        }
    }
}
