using AutoMapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMS.Models.Training;
using TMS.Repository.Repositories;
using TMS.ViewModels.Training;

namespace TMS.Repository.Managers.Implementations
{
	internal class ApprovalManager : IApprovalManager
	{

		private readonly IApprovalRepository _repository;
		public readonly IMapper _mapper;
		public ApprovalManager(IApprovalRepository repository, IMapper mapper)
		{
			_repository = repository;
			_mapper = mapper;
		}

		public bool AddApprovalHistory(TrainingScheduleApprovalHistoryViewModel model)
		{
			//var result = false;

			model.CreatedOn=DateTime.Now;

			var approvalId = _repository.GetApprovalId(model);
			
			
			return true;

		}
		public bool Updateapproval(TrainingScheduleApprovalHistoryViewModel model)
		{

			var approvalHistory = new TrainingScheduleApprovalHistory
			{
				TrainingScheduleId = model.TrainingScheduleId,
				StatusId = model.StatusId,
				Remarks = model.Remarks,
				OldFromDate = model.OldFromDateTime,
				OldToDate = model.OldToDateTime,
				NewFromDate = model.NewFromDateTime,
				NewToDate = model.NewToDateTime,
				CreatedBy = Convert.ToInt32(model.CreatedBy),
				CreatedOn = DateTime.Now,

			};

			var approval = _repository.UpdateapprovalId(approvalHistory);
			
			return approval;

		}




		public DataSet ApprovalPendingWithMe(int UserId, string username)
		{
			return _repository.ApprovalPendingWithMe(UserId, username);
		}
		public async Task<List<TrainingScheduleApprovalHistoryViewModel>> GetApprovalStatus(TrainingScheduleApprovalHistoryViewModel model, string[]? includes = null)
		{
			var list = await _repository.GetApprovalStatus(model);
			
			return _mapper.Map<List<TrainingScheduleApprovalHistoryViewModel>>(list);
		}

		public TrainingScheduleApprovalHistoryViewModel GetAttachment(int id)
		{
			var doc = _repository.GetAttachment(id);
			return _mapper.Map<TrainingScheduleApprovalHistoryViewModel>(doc);
		}

		
	}
}
