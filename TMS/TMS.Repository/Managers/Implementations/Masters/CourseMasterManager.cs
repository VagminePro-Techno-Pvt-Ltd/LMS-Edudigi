using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMS.Models.Masters;
using TMS.Repository.Repositories;
using TMS.ViewModels.Masters;

namespace TMS.Repository.Managers.Implementations.Masters
{
    internal class CourseMasterManager : MasterManager<CourseMasterViewModel, CourseMaster>
    {
        public CourseMasterManager(IRepository<CourseMaster> repository, AutoMapper.IMapper mapper) : base(repository, mapper)
        {

        }

        public async override Task<bool> AddUpdateAsync(CourseMasterViewModel model, int userId)
        {
            //Add mode
            if (model.Id == 0)
            {
                //var createModel = model.Map<CourseMasterViewModel, CourseMaster>();
                var createModel = _mapper.Map<CourseMaster>(model);
                createModel.IsActive = true;
                createModel.CreatedOn = DateTime.Now;
                createModel.CreatedBy = userId;
               
                var result = await _repository.AddAsync(createModel);
                if (!result)
                    return false;
            }
            else
            {  //Edit mode
                var oldModel = await _repository.GetAsync(model.Id, new[] { "Semester", "CourseCategory", "CourseQuadrants", "Enrollments" });
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
