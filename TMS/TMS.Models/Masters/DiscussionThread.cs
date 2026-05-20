using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Account;


namespace TMS.Models.Masters
{
    [Table(nameof(DiscussionThread))]
    public class DiscussionThread : BaseModel
    {
        public int? CourseQuadrantId { get; set; }
        [ForeignKey(nameof(CourseQuadrantId))]
        public virtual CourseQuadrantMaster? CourseQuadrant { get; set; }

        public int? ProgramId { get; set; }
        [ForeignKey(nameof(ProgramId))]
        public virtual CourseCategoryMaster? Program { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = default!;

        public int CreatedByUserId { get; set; }
        [ForeignKey(nameof(CreatedByUserId))]
        public virtual UserMaster? CreatedByUser { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public bool IsLocked { get; set; } = false;
        public bool IsPinned { get; set; } = false;
        public int ViewCount { get; set; } = 0;

        public virtual List<DiscussionReply>? Replies { get; set; }
        public virtual List<DiscussionAttachment>? Attachments { get; set; }
    }

    [Table(nameof(DiscussionReply))]
    public class DiscussionReply : BaseModel
    {
        public int ThreadId { get; set; }
        [ForeignKey(nameof(ThreadId))]
        public virtual DiscussionThread? Thread { get; set; }

        public int UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public virtual UserMaster? User { get; set; }

        public int? ParentReplyId { get; set; }
        [ForeignKey(nameof(ParentReplyId))]
        public virtual DiscussionReply? ParentReply { get; set; }

        [Required]
        public string CommentText { get; set; } = default!;

        public bool IsAccepted { get; set; } = false;
        public DateTime CommentedOn { get; set; } = DateTime.UtcNow;

        public virtual List<DiscussionReply>? ChildReplies { get; set; }
        public virtual List<DiscussionAttachment>? Attachments { get; set; }
    }

    [Table(nameof(DiscussionAttachment))]
    public class DiscussionAttachment : BaseModel
    {
        public int? ThreadId { get; set; }
        [ForeignKey(nameof(ThreadId))]
        public virtual DiscussionThread? Thread { get; set; }

        public int? ReplyId { get; set; }
        [ForeignKey(nameof(ReplyId))]
        public virtual DiscussionReply? Reply { get; set; }

        [Required, StringLength(255)]
        public string FileName { get; set; } = default!;

        [Required, StringLength(500)]
        public string FilePath { get; set; } = default!;

        [StringLength(50)]
        public string? FileType { get; set; }

        public long FileSize { get; set; }

        public int UploadedBy { get; set; }
        public DateTime UploadedOn { get; set; } = DateTime.UtcNow;
    }
}



