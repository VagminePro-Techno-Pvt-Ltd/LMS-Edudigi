using System.Threading;
using System.Threading.Tasks;
using TMS.ViewModels.Integration;

namespace TMS.Web.Services.Integration
{
    public interface IIntegrationHealthService
    {
        Task<IntegrationHubDashboardViewModel> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);
        Task<(bool Success, string Message)> CheckProviderHealthAsync(string providerKey, CancellationToken cancellationToken = default);
    }
}
