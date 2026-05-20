using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TMS.Models;
using TMS.Repository.Extensions;
using TMS.Repository.Repositories;
using TMS.ViewModels;

namespace TMS.Repository.Managers.Implementations.Masters
{
    internal class MasterManager<TViewModel, TMasterModel> : IMasterManager<TViewModel>
        where TViewModel : BaseMasterViewModel, new()
        where TMasterModel : BaseMasterModel, new()
    {
        protected readonly IRepository<TMasterModel> _repository;
        protected readonly AutoMapper.IMapper _mapper;
        public MasterManager(IRepository<TMasterModel> repository, AutoMapper.IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }
        public virtual async Task<List<TViewModel>> GetAsync(string[]? includes = null, Expression<Func<TViewModel, bool>>? predicate = null)
        {
            var query = _repository.GetAsync(includes);
            if (predicate != null)
            {
                var masterExpression = predicate.Convert<TViewModel, TMasterModel>();
                query = query.Where(masterExpression);
            }
            var list = await query.ToListAsync();
            var finalList = _mapper.Map<List<TViewModel>>(list);
            return finalList;
        }
        public async Task<DataTableResponse<TViewModel>> GetAsync(DataTableRequest request, string[]? includes = null, Expression<Func<TViewModel, bool>>? predicate = null, List<DataTableColumnsOrder>? orderColumns = null)
        {
            var totalCount = await _repository.CountAsync();
            var query = _repository.GetAsync(includes);                                                                                                                      //includes got removed. add it back in the GetAsync(),
            if (predicate != null)
            {
                var masterExpression = predicate.Convert<TViewModel, TMasterModel>();
                query = query.Where(masterExpression);
            }
            if (orderColumns != null && orderColumns.Any())
            {
                query = query.OrderBy(orderColumns);
            }
            var list = await query.Skip(request.Start).Take(request.Length).ToListAsync();                                                                                      //skips the first element and the next element
            var finalList = _mapper.Map<List<TViewModel>>(list);
            var filteredCount = await query.CountAsync();
            DataTableResponse<TViewModel> response = new()
            {
                Draw = request.Draw,
                RecordsTotal = totalCount,
                RecordsFiltered = filteredCount,
                Data = finalList.ToArray()
            };
            return response;
        }
        public virtual async Task<TViewModel?> GetAsync(int id, string[]? includes = null)
        {
            var model = await _repository.GetAsync(id, includes);
            return _mapper.Map<TViewModel>(model);
        }
        public virtual async Task<bool> AddUpdateAsync(TViewModel model, int userId)
        {
            //Add mode
            if (model.Id == 0)
            {
                var createModel = model.Map<TViewModel, TMasterModel>();
                createModel.IsActive = true;
                createModel.CreatedOn = DateTime.Now;
                createModel.CreatedBy = userId;
                var result = await _repository.AddAsync(createModel);
                if (!result)
                    return false;

                await _repository.SaveChangesAsync();
                model.Id = createModel.Id; // Map the new ID back to the ViewModel
                return true;
            }
            else
            {  //Edit mode
                var oldModel = await _repository.GetAsync(model.Id);
                var updatedModel = model.MapToDTO(oldModel!);
                updatedModel.UpdatedOn = DateTime.Now;
                updatedModel.UpdatedBy = userId;
                var editResult = await _repository.UpdateAsync(updatedModel);
                if (!editResult)
                    return false;
                
                await _repository.SaveChangesAsync();
                return true;
            }
        }

        public virtual async Task<bool> IsValidName(TViewModel model)
        {
            return !(await _repository.Find(t => (model.Id == 0 || t.Id != model.Id) && t.Name == model.Name)).Any();
        }

        public virtual async Task<bool> CheckExpression(Expression<Func<TViewModel, bool>> expression)
        {
            var masterExpression = expression.Convert<TViewModel, TMasterModel>();
            return (await _repository.Find(masterExpression)).Any();
        }

        public Task<int> CountAsync(Expression<Func<TViewModel, bool>>? predicate = null)
        {
            if (predicate == null)
                return _repository.CountAsync();
            var expression = predicate.Convert<TViewModel, TMasterModel>();
            return _repository.CountAsync(expression);
        }
    }
}
