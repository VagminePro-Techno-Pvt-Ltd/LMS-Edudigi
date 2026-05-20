using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using TMS.Models.Training;
using TMS.Repository.Helpers;
using TMS.ViewModels.Training;

namespace TMS.Repository.Repositories.Implementations
{
	internal class ApprovalRepository : BaseModelRepository<TrainingScheduleApprovalHistory>, IApprovalRepository
	{
		private readonly IDBHelper _dBHelper;
		private readonly ApplicationDBContext _context;
		public ApprovalRepository(IDBHelper dBHelper, ApplicationDBContext context) :base(context)
		{
			_dBHelper = dBHelper;
			_context = context;
		}
		public DataSet ApprovalPendingWithMe(int UserId, string username)
		{
			throw new NotImplementedException();
		}

		public int GetApprovalId(TrainingScheduleApprovalHistoryViewModel history)
		{
			try
			{
				var approvalHistory = new TrainingScheduleApprovalHistory
				{
					TrainingScheduleId = history.TrainingScheduleId,
					StatusId = history.StatusId,
					Remarks = history.Remarks,
					OldFromDate = history.OldFromDateTime,
					OldToDate = history.OldToDateTime,
					NewFromDate = history.NewFromDateTime,
					NewToDate = history.NewToDateTime,
					CreatedBy = Convert.ToInt32(history.CreatedBy),
					CreatedOn=DateTime.Now,
					

                };

				_context.TrainingScheduleApprovalHistory.Add(approvalHistory);
				_context.SaveChanges();

				return approvalHistory.Id;
			}
			catch (Exception)
			{

				throw;
			}
		}

		public bool UpdateapprovalId(TrainingScheduleApprovalHistory history)
		{
			try
			{
				//var approvalHistoryModel = _context.TrainingScheduleApprovalHistory.ToList();
				//var approvalHistory = _context.TrainingScheduleApprovalHistory
				//   .SingleOrDefault(a => a.TrainingScheduleId == history.TrainingScheduleId && a.StatusId == history.StatusId);

				//if (approvalHistory != null)
				//{
				//	approvalHistory.CreatedBy = Convert.ToInt16(history.CreatedBy);
				//	approvalHistory.Remarks = history.Remarks;
				//	_context.SaveChanges();
				//}

				_context.TrainingScheduleApprovalHistory.Add(history);
				_context.SaveChanges();
				 return true;
			}
			catch (Exception)
			{
				return false;
				//throw;
			}
		}

		public async Task<List<TrainingScheduleApprovalHistory>> GetApprovalStatus(TrainingScheduleApprovalHistoryViewModel model)
		{
			var list = await _context.TrainingScheduleApprovalHistory.Include(t=>t.CreatedByUser).Include(t=>t.TrainingSchedule)
				.Where(t => t.TrainingScheduleId == model.TrainingScheduleId)
				.ToListAsync();
			
			return list;
		}

        public async Task<List<TrainingScheduleApprovalHistory>>GetStatusById(TrainingScheduleApprovalHistory model)
		{
			var list = await _context.TrainingScheduleApprovalHistory.Where(t=>t.TrainingScheduleId==model.TrainingScheduleId).ToListAsync();
			return list;
		}
		public TrainingScheduleAttachment GetAttachment(int id)
		{
			throw new NotImplementedException();
		}

		public bool SaveApprovalAttachment(TrainingScheduleAttachment model)
		{
			_context.TrainingScheduleAttachments.Add(model);
			_context.SaveChanges();
			return true;
		}

		
	}
}
