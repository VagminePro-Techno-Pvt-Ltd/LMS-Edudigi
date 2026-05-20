using System.Linq.Expressions;
using TMS.ViewModels;
using TMS.ViewModels.Academics;
using TMS.ViewModels.Masters;

namespace TMS.Repository.Managers
{
    public interface IMasterBaseManager<TViewModel>
        where TViewModel : BaseViewModel
    {
        Task<List<TViewModel>> GetAsync(string[]? includes = null, Expression<Func<TViewModel, bool>>? predicate = null);
        Task<DataTableResponse<TViewModel>> GetAsync(DataTableRequest request, string[]? includes = null, Expression<Func<TViewModel, bool>>? predicate = null, List<DataTableColumnsOrder>? orderColumns = null);
		Task<DataTableResponse<TViewModel>> GetallData(DataTableRequest request, string[]? includes = null, Expression<Func<TViewModel, bool>>? predicate = null, List<DataTableColumnsOrder>? orderColumns = null);

		Task<TViewModel?> GetAsync(int id, string[]? includes = null);
        Task<bool> AddUpdateAsync(TViewModel model, int userId);
		Task<bool> AddUpdateRangeAsync(List<TViewModel> model, int userId);
		Task<bool> UpdateNominationStatusAsync(TViewModel model, int userId);

		Task<bool> UpdateMyStatusAsync(TViewModel model, int userId);
		//Task<bool> AddUpdateNomination(NominationRequestViewModel model, int userId);//add NOminationview

        //Task<NominationRequestViewModel> AddupdateModelLast(NominationRequestViewModel model, int userId);
		Task<bool> AddUpdateAsync(List<TViewModel> model, int userId);
        Task<bool> CheckExpression(Expression<Func<TViewModel, bool>> predicate);
        Task<ModelResultViewModel<TViewModel>> AddUpdateResultAsync(TViewModel model, int userId);
        Task<int> CountAsync(Expression<Func<TViewModel, bool>>? predicate = null);
          Task<bool> DeleteAsync(int id);
        Task<bool> AddUpdateQuizAsync(QuizMasterViewModel model, int userId);
        Task<bool> AddUpdateStudentQuizAttemptAsync(StudentQuizAttemptViewModel model, int userId);
        Task<bool> AddUpdateQuizAssessmentAsync(FacultyQuizAssessmentViewModel model, int userId);
    }
}
