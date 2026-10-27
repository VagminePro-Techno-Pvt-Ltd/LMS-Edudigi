using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMS.ViewModels.Academics;

namespace TMS.Web.Services
{
    public interface IAutomatedResultService
    {
        Task<ResultCalculationSummaryDto> CalculateAssessmentResultsAsync(
            int assessmentId, 
            int calculatedBy, 
            int? policyId = null, 
            CancellationToken cancellationToken = default);

        Task<ResultCalculationSummaryDto> GetAssessmentResultsAsync(
            int assessmentId, 
            CancellationToken cancellationToken = default);

        Task<bool> PublishAssessmentResultsAsync(
            int assessmentId, 
            int publishedBy, 
            bool publish = true, 
            CancellationToken cancellationToken = default);

        Task<List<StudentProcessedResultDto>> GetEligibleCompartmentalStudentsAsync(
            int assessmentId, 
            CancellationToken cancellationToken = default);

        Task<int> CreateCompartmentalAssessmentAsync(
            CompartmentalAssessmentCreateDto dto, 
            int createdBy, 
            CancellationToken cancellationToken = default);

        Task<InstitutionalGradingPolicyViewModel> GetActiveGradingPolicyAsync(
            CancellationToken cancellationToken = default);

        Task<bool> SaveGradingPolicyAsync(
            InstitutionalGradingPolicyViewModel model, 
            int updatedBy, 
            CancellationToken cancellationToken = default);

        Task<ResultCalculationSummaryDto> CalculateExamMasterResultsAsync(
            int examId, 
            int calculatedBy, 
            int? policyId = null, 
            CancellationToken cancellationToken = default);

        Task<bool> ApplyAuthorizedGraceMarksAsync(
            int resultId, 
            decimal graceMarks, 
            string approvalReason, 
            int authorizedBy, 
            CancellationToken cancellationToken = default);
    }
}
