using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Chat
{
    [Table(nameof(ChatAttachment))]
    public class ChatAttachment : BaseModel
    {
        public int MessageId { get; set; }
        [ForeignKey(nameof(MessageId))]
        public virtual ChatMessage? Message { get; set; }

        [Required, StringLength(300)]
        public string FileName { get; set; } = default!;

        [Required, StringLength(500)]
        public string FilePath { get; set; } = default!;

        [StringLength(50)]
        public string? FileType { get; set; }

        public long FileSize { get; set; }

        public DateTime UploadedOn { get; set; } = DateTime.UtcNow;
    }
}
