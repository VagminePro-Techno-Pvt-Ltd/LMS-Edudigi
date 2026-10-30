using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Account;
using TMS.Models.Masters;

namespace TMS.Models.Academics
{
    [Table(nameof(ContentModerationFlag))]
    public class ContentModerationFlag : BaseModel
    {
        [Required, StringLength(50)]
        public string ContentType { get; set; } = default!; // "DiscussionThread", "DiscussionReply"

        [Required]
        public int ContentId { get; set; }

        [Required]
        public int AuthorUserId { get; set; }

        [ForeignKey(nameof(AuthorUserId))]
        public virtual UserMaster? AuthorUser { get; set; }

        [StringLength(255)]
        public string? AuthorName { get; set; }

        public int? ProgramId { get; set; }

        [ForeignKey(nameof(ProgramId))]
        public virtual CourseCategoryMaster? Program { get; set; }

        public int? CourseId { get; set; }

        [ForeignKey(nameof(CourseId))]
        public virtual CourseMaster? Course { get; set; }

        [Required]
        public string FlaggedText { get; set; } = default!;

        [StringLength(500)]
        public string DetectedKeywords { get; set; } = default!;

        [Required, StringLength(50)]
        public string Status { get; set; } = "PendingReview"; // "PendingReview", "Confirmed", "Dismissed"

        public int? ReviewedByUserId { get; set; }

        [ForeignKey(nameof(ReviewedByUserId))]
        public virtual UserMaster? ReviewedByUser { get; set; }

        public DateTime? ReviewedOn { get; set; }

        public string? ReviewRemarks { get; set; }
    }
}
