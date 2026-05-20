using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMS.ViewModels.Training;

namespace TMS.Repository.Managers
{
	public interface IApprovalManager
	{
		public bool AddApprovalHistory(TrainingScheduleApprovalHistoryViewModel model);
		public bool Updateapproval(TrainingScheduleApprovalHistoryViewModel model);
		public DataSet ApprovalPendingWithMe(int userId,  string username);
		public Task<List<TrainingScheduleApprovalHistoryViewModel>> GetApprovalStatus(TrainingScheduleApprovalHistoryViewModel model, string[]? includes = null);
		public TrainingScheduleApprovalHistoryViewModel GetAttachment(int id);
	}
}
