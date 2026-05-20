using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using TMS.ViewModels.Academics;

namespace TMS.Repository.Managers
{
    public interface IAnnouncementManager : IMasterBaseManager<AnnouncementViewModel>
    {
        /// <summary>
        /// Get all active, published, non-expired announcements visible to a specific user.
        /// Filters by role, course enrollment, and temporal constraints.
        /// </summary>
        Task<List<AnnouncementViewModel>> GetVisibleAnnouncementsAsync(int userId, int? roleId, List<int>? courseIds = null);

        /// <summary>
        /// Get all announcements for admin management view (including deleted/expired).
        /// </summary>
        Task<List<AnnouncementViewModel>> GetAllForManagementAsync();

        /// <summary>
        /// Soft-delete an announcement instead of hard delete.
        /// </summary>
        Task<bool> SoftDeleteAsync(int id, int userId);

        /// <summary>
        /// Toggle the pinned status of an announcement.
        /// </summary>
        Task<bool> TogglePinAsync(int id, int userId);
        Task<bool> FixDatabaseAsync();
    }
}
