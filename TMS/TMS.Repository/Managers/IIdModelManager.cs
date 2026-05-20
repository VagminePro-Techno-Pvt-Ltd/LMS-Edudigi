using System.Linq.Expressions;
using TMS.ViewModels;

namespace TMS.Repository.Managers
{
    public interface IIdModelManager<TViewModel> where TViewModel : IdIntViewModel
    {
        Task<List<TViewModel>> GetAsync(string[]? includes = null, System.Linq.Expressions.Expression<Func<TViewModel, bool>>? predicate = null);
        Task<TViewModel?> GetAsync(int id, string[]? includes = null);
        Task<bool> AddUpdateAsync(TViewModel model, int userId);
        Task<bool> CheckExpression(System.Linq.Expressions.Expression<Func<TViewModel, bool>> predicate);
        Task<int> CountAsync(Expression<Func<TViewModel, bool>>? predicate = null);
    }
}
