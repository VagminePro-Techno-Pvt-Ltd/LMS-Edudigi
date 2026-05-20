using TMS.Models;
using TMS.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace TMS.Repository.Managers
{
    public interface IMasterManager<TViewModel>
        where TViewModel : BaseMasterViewModel
    {
        Task<List<TViewModel>> GetAsync(string[]? includes = null, Expression<Func<TViewModel, bool>>? predicate = null);
        Task<DataTableResponse<TViewModel>> GetAsync(DataTableRequest request, string[]? includes = null, System.Linq.Expressions.Expression<Func<TViewModel, bool>>? predicate = null, List<DataTableColumnsOrder>? orderColumns = null);
        Task<TViewModel?> GetAsync(int id, string[]? includes = null);
        Task<bool> AddUpdateAsync(TViewModel model, int userId);
        Task<bool> IsValidName(TViewModel model);
        Task<bool> CheckExpression(Expression<Func<TViewModel, bool>> predicate);
        Task<int> CountAsync(Expression<Func<TViewModel, bool>>? predicate = null);
    }
}
