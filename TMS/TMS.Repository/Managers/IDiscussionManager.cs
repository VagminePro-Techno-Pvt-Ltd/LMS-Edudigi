using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMS.Models.Masters;

namespace TMS.Repository.Managers
{
    public interface IDiscussionManager
    {
        Task<IEnumerable<DiscussionThread>> GetThreadsByProgramAsync(int programId, string filter);
        Task<IEnumerable<CourseCategoryMaster>> GetUserProgramsAsync(int userId, string userRole);
        Task<DiscussionThread> GetThreadDetailsAsync(int threadId);
        Task<int> CreateThreadAsync(DiscussionThread thread, string body, int userId);
        Task<int> AddReplyAsync(DiscussionReply reply, int userId);
        Task<bool> SaveAttachmentsAsync(List<DiscussionAttachment> attachments);
        Task<bool> MarkReplyAsAcceptedAsync(int replyId, int userId);
        Task<bool> CanUserAccessForumAsync(int programId, int userId, string userRole);
        Task<bool> ToggleLockAsync(int threadId, int userId);
        Task<bool> TogglePinAsync(int threadId, int userId);
    }
}

