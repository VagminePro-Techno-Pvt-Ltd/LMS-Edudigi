using System.Threading;
using System.Threading.Tasks;
using TMS.Models.Academics;

namespace TMS.Web.Services;

public interface IAiEvaluationService
{
    Task<AiEvaluationResult> EvaluateQuestionAsync(AiEvaluationRequest request, CancellationToken cancellationToken = default);
}
