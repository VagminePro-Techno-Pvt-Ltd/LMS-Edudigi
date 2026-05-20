using System;
using System.Collections.Generic;

namespace TMS.ViewModels.Chat
{
    /// <summary>
    /// Represents a single chat message in the conversation view.
    /// </summary>
    public class ChatMessageViewModel
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string? CourseName { get; set; }
        public int SenderId { get; set; }
        public string? SenderName { get; set; }
        public string? SenderRole { get; set; }
        public int ReceiverId { get; set; }
        public string? ReceiverName { get; set; }
        public string? MessageText { get; set; }
        public DateTime SentOn { get; set; }
        public bool IsRead { get; set; }
        public List<ChatAttachmentViewModel> Attachments { get; set; } = new();
    }

    /// <summary>
    /// Represents a file attachment on a message.
    /// </summary>
    public class ChatAttachmentViewModel
    {
        public int Id { get; set; }
        public int MessageId { get; set; }
        public string FileName { get; set; } = default!;
        public string FilePath { get; set; } = default!;
        public string? FileType { get; set; }
        public long FileSize { get; set; }
    }

    /// <summary>
    /// Represents a user you can chat with (shown in the sidebar contact list).
    /// </summary>
    public class ChatPartnerViewModel
    {
        public int UserId { get; set; }
        public string? Name { get; set; }
        public string? Role { get; set; }
        public string? CourseName { get; set; }
        public int CourseId { get; set; }
        public string? LastMessage { get; set; }
        public DateTime? LastMessageTime { get; set; }
        public int UnreadCount { get; set; }
    }

    /// <summary>
    /// Payload sent from the client when composing a new message.
    /// </summary>
    public class SendMessageRequest
    {
        public int CourseId { get; set; }
        public int ReceiverId { get; set; }
        public string? MessageText { get; set; }
    }
}
