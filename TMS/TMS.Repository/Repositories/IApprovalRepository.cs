using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMS.Models.Training;
using TMS.ViewModels.Training;

namespace TMS.Repository.Repositories
{
	internal interface IApprovalRepository:IBaseModelRepository<TrainingScheduleApprovalHistory>
    {
		int GetApprovalId(TrainingScheduleApprovalHistoryViewModel history);
		bool UpdateapprovalId(TrainingScheduleApprovalHistory history);
		bool SaveApprovalAttachment(TrainingScheduleAttachment model);
		

		Task<List<TrainingScheduleApprovalHistory>> GetApprovalStatus(TrainingScheduleApprovalHistoryViewModel model);
		DataSet ApprovalPendingWithMe(int UserId, string username);
		TrainingScheduleAttachment GetAttachment(int id);
	}
}
