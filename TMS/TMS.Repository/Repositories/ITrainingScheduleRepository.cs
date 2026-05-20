using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMS.Models.Training;

namespace TMS.Repository.Repositories
{
    internal interface ITrainingScheduleRepository: IBaseModelRepository<TrainingSchedule>
    {
        DataSet GetByUser(int userId,DataTableRequest request);

        DataSet GetLatestNominationRequests(int userId, int traningId);
       
    }
}
