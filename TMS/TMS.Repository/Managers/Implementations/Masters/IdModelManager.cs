using TMS.Models;
using TMS.Repository.Repositories;
using TMS.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace TMS.Repository.Managers.Implementations.Masters
{
    internal class IdModelManager<TViewModel, TMasterModel> : IIdModelManager<TViewModel>
        where TViewModel : IdIntViewModel, new()
        where TMasterModel : BaseIdModel, new()
    {
        protected readonly IBaseIdModelRepository<TMasterModel> _repository;
        protected readonly AutoMapper.IMapper _mapper;
        public IdModelManager(IBaseIdModelRepository<TMasterModel> repository, AutoMapper.IMapper mapper)
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
                var result = await _repository.AddAsync(createModel);
                if (!result)
                    return false;
            }
            else
            {  //Edit mode
                var oldModel = await _repository.GetAsync(model.Id);
                var updatedModel = model.MapToDTO(oldModel!);
                var editResult = await _repository.UpdateAsync(updatedModel);
                if (!editResult)
                    return false;
            }
            await _repository.SaveChangesAsync();
            return true;
        }

        public virtual async Task<bool> CheckExpression(Expression<Func<TViewModel, bool>> expression)
        {
            var masterExpression = expression.Convert<TViewModel, TMasterModel>(true);
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
