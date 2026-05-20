using Azure.Core;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMS.Models.Training;
using TMS.Repository.Helpers;

namespace TMS.Repository.Repositories.Implementations
{
    internal class TrainingScheduleRepository : BaseModelRepository<TrainingSchedule>, ITrainingScheduleRepository
    {
        private readonly IDBHelper _dBHelper;

        public TrainingScheduleRepository(ApplicationDBContext context, IDBHelper dBHelper) : base(context)
        {
            _dBHelper = dBHelper;
        }

        public DataSet GetByUser(int userId,DataTableRequest request)
        {

            return _dBHelper.ExecuteProc("TrainingSchedulGetByUser",
           new SqlParameter("UserId", userId),
           new SqlParameter("PageNo", request.Start),
          new SqlParameter("PageSize", request.Length),
          new SqlParameter("SortColumn", request.Order?[0].Column),
          new SqlParameter("SortDirection", request.Order?[0].Dir),
          new SqlParameter("SearchText", request.Search?.Value));
        }
        public DataSet GetLatestNominationRequests(int userId, int traningId)
        {
            return _dBHelper.ExecuteProc("GetLatestNominationRequests",
                 new SqlParameter("UserId", userId),
                 new SqlParameter("TringId", traningId));
        }
    }
}
