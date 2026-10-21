using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMS.ViewModels.Academics;

namespace TMS.Web.Services
{
    public class SuggestedQuestionResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public bool RequiresTopicContext { get; set; }
        public List<SuggestedQuestionItemViewModel> Questions { get; set; } = new();
    }

    public interface ISuggestedQuestionService
    {
        Task<SuggestedQuestionResult> GenerateSuggestedQuestionsAsync(
            SuggestedQuestionRequestViewModel request, 
            int facultyId, 
            CancellationToken cancellationToken = default);

        Task<SuggestedQuestionResult> RegenerateSingleQuestionAsync(
            SuggestedQuestionItemViewModel currentQuestion, 
            SuggestedQuestionRequestViewModel context, 
            int facultyId, 
            CancellationToken cancellationToken = default);

        Task<List<QuestionClassificationSuggestionDto>> ClassifyLegacyQuestionsAsync(
            int courseId,
            int subjectId,
            List<LegacyQuestionToClassifyDto> questions,
            CancellationToken cancellationToken = default);
    }
}
