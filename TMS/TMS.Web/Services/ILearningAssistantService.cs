using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMS.ViewModels.LearningAssistant;

namespace TMS.Web.Services
{
    public interface ILearningAssistantService
    {
        Task<LearningAssistantResponse> AskAssistantAsync(
            int userId,
            string role,
            LearningAssistantRequest request,
            CancellationToken cancellationToken = default);

        Task<List<EnrolledCourseSummaryDto>> GetStudentEnrolledCoursesAsync(int studentId);

        Task<LearningAssistantContextDto> ResolveContextAsync(
            int userId,
            string role,
            string? pageUrl,
            int? courseId,
            int? unitId,
            int? materialId);
    }
}
