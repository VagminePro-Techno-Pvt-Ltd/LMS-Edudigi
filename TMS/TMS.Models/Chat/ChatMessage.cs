using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Account;
using TMS.Models.Masters;

namespace TMS.Models.Chat
{
    [Table(nameof(ChatMessage))]
    public class ChatMessage : BaseModel
    {
        public int CourseId { get; set; }
        [ForeignKey(nameof(CourseId))]
        public virtual CourseMaster? Course { get; set; }

        public int SenderId { get; set; }
        [ForeignKey(nameof(SenderId))]
        public virtual UserMaster? Sender { get; set; }

        public int ReceiverId { get; set; }
        [ForeignKey(nameof(ReceiverId))]
        public virtual UserMaster? Receiver { get; set; }

        public string? MessageText { get; set; }

        public DateTime SentOn { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        public bool IsDeleted { get; set; } = false;

        public virtual List<ChatAttachment>? Attachments { get; set; }
    }
}
