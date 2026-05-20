using TMS.Repository.Repositories;
using TMS.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMS.Models.Account;
using TMS.Repository.Managers.Implementations.Masters;
using TMS.Models.Masters;

namespace TMS.Repository.Managers.Implementations
{
    internal class UserManager : MasterManager<UserViewModel, UserMaster>
    {
        public UserManager(IRepository<UserMaster> repository, AutoMapper.IMapper mapper) : base(repository, mapper)
        {
             
        }
        public override async Task<bool> AddUpdateAsync(UserViewModel model, int userId)
        {
            //Add mode
            if (model.Id == 0)
            {
                var createModel = _mapper.Map<UserMaster>(model);
                createModel.IsActive = true;
                createModel.CreatedOn = DateTime.Now;
                createModel.CreatedBy = userId;
                var result = await _repository.AddAsync(createModel);
                if (!result)
                    return false;
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
            }
            await _repository.SaveChangesAsync();
            return true;
        }
    }
}
