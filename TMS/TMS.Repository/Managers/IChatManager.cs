using System.Collections.Generic;
using System.Threading.Tasks;
using TMS.ViewModels.Chat;

namespace TMS.Repository.Managers
{
    public interface IChatManager
    {
        /// <summary>
        /// Gets all users the current user can chat with, based on shared courses.
        /// </summary>
        Task<List<ChatPartnerViewModel>> GetChatPartnersAsync(int userId, string role);

        /// <summary>
        /// Gets conversation history between two users within a course context.
        /// </summary>
        Task<List<ChatMessageViewModel>> GetConversationAsync(int userId, int partnerId, int courseId, int take = 50);

        /// <summary>
        /// Sends a text message (with optional attachments handled separately).
        /// </summary>
        Task<ChatMessageViewModel?> SendMessageAsync(int senderId, SendMessageRequest request);

        /// <summary>
        /// Saves file attachment metadata for a given message.
        /// </summary>
        Task<ChatAttachmentViewModel?> SaveAttachmentAsync(int messageId, string fileName, string filePath, string fileType, long fileSize);

        /// <summary>
        /// Marks all messages from a partner as read.
        /// </summary>
        Task MarkAsReadAsync(int userId, int partnerId, int courseId);

        /// <summary>
        /// Gets total unread message count for a user.
        /// </summary>
        Task<int> GetUnreadCountAsync(int userId);
    }
}
