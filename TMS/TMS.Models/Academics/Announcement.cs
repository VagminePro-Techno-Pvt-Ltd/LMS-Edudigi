using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Account;

namespace TMS.Models.Academics
{
    /// <summary>
    /// Persistent announcements for university events, programs, notices, and general updates.
    /// Separate from the notification system — announcements are always visible and stored permanently.
    /// </summary>
    [Table("Announcements")]
    public class Announcement : BaseModel
    {
        [Required, StringLength(250)]
        public string Title { get; set; } = default!;

        /// <summary>
        /// Rich-text HTML description of the announcement.
        /// </summary>
        public string Description { get; set; } = default!;

        /// <summary>
        /// 0 = All Users, 1 = Specific Role, 2 = Specific Course, 3 = Specific Department
        /// </summary>
        [Required]
        public int TargetType { get; set; } = 0;

        /// <summary>
        /// ID of the target (RoleId / CourseId / DepartmentId). Null when TargetType = 0 (All).
        /// </summary>
        public int? TargetId { get; set; }

        /// <summary>
        /// 0 = General, 1 = Event, 2 = Urgent
        /// </summary>
        [Required]
        public int AnnouncementType { get; set; } = 0;

        /// <summary>
        /// When the announcement becomes visible. Supports scheduling.
        /// </summary>
        [Required]
        public DateTime PublishDate { get; set; }

        /// <summary>
        /// When the announcement auto-hides. Null means no expiry.
        /// </summary>
        public DateTime? ExpiryDate { get; set; }

        /// <summary>
        /// Optional file attachment path.
        /// </summary>
        [StringLength(500)]
        public string? AttachmentUrl { get; set; }

        /// <summary>
        /// Pinned announcements appear at the top of the list.
        /// </summary>
        public bool IsPinned { get; set; } = false;

        /// <summary>
        /// Soft delete flag.
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        // ─── Navigation ───
        [ForeignKey(nameof(CreatedBy))]
        public override UserMaster? CreatedByUser { get; set; }
    }
}
