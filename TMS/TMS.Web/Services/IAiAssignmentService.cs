using System.Threading;
using System.Threading.Tasks;
using TMS.ViewModels.Academics;

namespace TMS.Web.Services
{
    /// <summary>
    /// Service for generating curriculum-grounded assignments and rubrics using Gemini AI.
    /// </summary>
    public interface IAiAssignmentService
    {
        /// <summary>
        /// Generates a complete assignment containing questions, marks, student instructions, marking criteria, and rubrics.
        /// </summary>
        Task<AiAssignmentGenerateResult> GenerateAssignmentAsync(AiAssignmentGenerateRequest request, CancellationToken cancellationToken = default);
    }
}
