using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using TMS.Repository.Managers.Implementations.Masters;
using TMS.ViewModels.Masters;
using TMS.ViewModels.Training;

namespace TMS.Repository.Managers
{
    public interface ITrainingScheduleManager: IMasterBaseManager<TrainingScheduleViewModel>
    {
        Task<DataTableResponse<TrainingScheduleViewModel>> GetByUser(int userId,DataTableRequest request);

         Task<List<NominationRequestViewModel>> GetLatestNominationRequests(int userId, int trainingId);
    }
}
