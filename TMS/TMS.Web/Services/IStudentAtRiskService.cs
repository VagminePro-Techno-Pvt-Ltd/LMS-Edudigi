using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMS.Models.Masters;
using TMS.ViewModels.Academics;

namespace TMS.Web.Services
{
    public interface IStudentAtRiskService
    {
        Task EnsureSchemaAsync(CancellationToken ct = default);

        Task<List<CourseMaster>> GetFacultyAssignedCoursesAsync(int facultyUserId, bool isStaff);

        Task<FacultyCourseActivitiesViewModel> GetCourseActivitiesAsync(int courseId, int requestingUserId, bool isStaff, string activityTypeFilter = "All");

        Task<bool> SaveActivityDeadlineAsync(SaveDeadlineRequest request, int userId, bool isStaff);

        Task<FacultyStudentRiskRosterViewModel> GetFacultyStudentRiskRosterAsync(int courseId, int requestingUserId, bool isStaff, string riskFilter = "All", string? search = null);

        Task<StudentRiskDetailViewModel?> GetStudentRiskDetailAsync(int courseId, int studentId, int requestingUserId, bool isStaff);

        Task<StudentAlertsPageViewModel> GetStudentAlertsAsync(int studentId, string filter = "All");

        Task<bool> MarkAlertReadAsync(int alertId, int studentId, bool isRead);

        Task<bool> MarkAllAlertsReadAsync(int studentId);

        Task<int> GetStudentUnreadAlertsCountAsync(int studentId);

        Task<AdminAtRiskAnalyticsViewModel> GetAdminAnalyticsAsync(int? programId = null, int? courseId = null, string riskFilter = "All", string? search = null);

        Task SyncStudentAlertsForCourseAsync(int courseId);
    }
}
