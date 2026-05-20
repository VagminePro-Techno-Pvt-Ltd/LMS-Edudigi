using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using TMS.Models;
using TMS.Models.Masters;
using TMS.Repository.Managers;
using TMS.Utilities;
using TMS.ViewModels;
using TMS.ViewModels.Account;
using TMS.ViewModels.Masters;
using TMS.ViewModels.Training;

namespace TMS.Web.Areas.Admin.Controllers
{
   // [ValidateFormAccess(Common.FormDefination.TrainingSchedule)]
    [Area("Admin")]
    public class TrainingScheduleController : MastersAjaxBaseController<TrainingScheduleViewModel>
    {
        private readonly IMasterManager<CourseMasterViewModel> _courseManager;
        private readonly IMasterManager<DivisionMasterViewModel> _divisionManager;
        private readonly IMasterManager<VenueMasterViewModel> _venueManager;
        private readonly IMasterManager<UserViewModel> _userManager;
        private readonly IMasterManager<LanguageMasterViewModel> _languageManager;
        private readonly StorageSettings _storageSettings;
        private readonly IMasterManager<TrainingScheduleStatusMasterViewModel> _trainingScheduleStatusManager;
		private readonly IMasterBaseManager<TrainingScheduleViewModel> _trainingScheduleManager;
		private readonly IMasterBaseManager<TrainingMatrixViewModel> _TrainingMatrixManager;
        private readonly IApprovalManager _approvalManager;
		private readonly IMasterBaseManager<NominationRequestViewModel> _nominationManager;
		private readonly IMasterBaseManager<NominationRequestRemarksViewModel> _nominationReamrksManager;


		public TrainingScheduleController(ITrainingScheduleManager manager, IMasterManager<CourseMasterViewModel> courseManager
            , IMasterManager<DivisionMasterViewModel> divisionManager, IMasterManager<VenueMasterViewModel> venueManager
            , IMasterManager<UserViewModel> userManager, IMasterManager<LanguageMasterViewModel> languageManager
            , IOptions<StorageSettings> options, IMasterManager<TrainingScheduleStatusMasterViewModel> trainingScheduleStatusManager
			,IMasterBaseManager<TrainingScheduleViewModel> trainingScheduleManager, IMasterBaseManager<TrainingMatrixViewModel> TrainingMatrixManager
			,IApprovalManager approvalManager, IMasterBaseManager<NominationRequestViewModel> nominationManager, IMasterBaseManager<NominationRequestRemarksViewModel> nominationReamrksManager
            
			)
            : base(manager, "Training Schedule", "Training Schedule"
                  , new[] { "Status", "Venue" }
                  , new[] { "TrainingScheduleLanguages", "TrainingScheduleAttachment" })
        {
            _courseManager = courseManager;
            _divisionManager = divisionManager;
            _venueManager = venueManager;
            _userManager = userManager;
            _languageManager = languageManager;
            _storageSettings = options.Value;
            _trainingScheduleStatusManager = trainingScheduleStatusManager;
			_trainingScheduleManager = trainingScheduleManager;
            _TrainingMatrixManager = TrainingMatrixManager;
            _approvalManager = approvalManager;
            _nominationManager = nominationManager;
            _nominationReamrksManager = nominationReamrksManager;
		}


		public virtual async Task<IActionResult> GetRescheduleModal(string iId)
		{
			ViewData["Title"] = _masterType;
	
			iId = iId.Decrypt(true);
			if (string.IsNullOrWhiteSpace(iId))
				return Json(GetResultModelFail("Record not found"));
			var idInt = Convert.ToInt32(iId);
			var item = await _manager.GetAsync(idInt, _masterEditInclude);

			SetProperties(ref item!);
			await SetDropdownViewBag(item!);
			return PartialView("ReScheduleRemarks", item);
		}


		public async Task<IActionResult> UpdateToSchedule(int Id, string Remarks)

        {

            ViewData["Title"] = _masterType;

            var old = await _manager.GetAsync(Id);
            var user = GetUserId();


            var model = new TrainingScheduleApprovalHistoryViewModel();

            if (old != null)

            {

                model.TrainingScheduleId = old.Id;

                model.StatusId = old.StatusId;

                model.Remarks = Remarks;
                //model.UpdatedByUser.= user;

                model.OldFromDateTime = old.FromDate;

                model.OldToDateTime = old.ToDate;

                model.CreatedBy = old.CreatedBy;
                model.UpdatedByUser = old.UpdatedByUser;


                var result = _approvalManager.AddApprovalHistory(model);

                if (result)

                {

                    if (old.StatusId == 2||old.StatusId==8)

                    {

                        old.StatusId = 5;

                        await _manager.UpdateMyStatusAsync(old, this.GetUserId());

                        return Json(GetResultModelSuccess($"{_masterType} {(model.Id == 1 ? "Planned" : "Sent for Approval")} successfully"));

                    }

                    if (old.StatusId == 5)

                    {

                        old.StatusId = 6;

                        await _manager.UpdateMyStatusAsync(old, this.GetUserId());

                        return Json(GetResultModelSuccess($"{_masterType} {(model.Id == 5 ? "Pending For Approval" : "Sent to be planned")} successfully"));//updated JSon from scheduled to planned

                    }

                    if (old.StatusId == 6)

                    {

                        old.StatusId = 1;

                        await _manager.UpdateMyStatusAsync(old, this.GetUserId());

                        return Json(GetResultModelSuccess($"{_masterType}{(model.Id == 6 ? "Planned" : "Planned")} successfully"));

                    }

                }

                if (old.StatusId == 3)

                {

                    return Json(new { message = "Can not schedule rejected training" });

                }

                else

                    return Json(new { message = "Status is already Scheduled " });

            }

            else

            {

                return Json(new { message = "Data Not Found" });

            }

        }
        public async Task<IActionResult> UpdateToRescheduleAndReject(int Id, string Remarks)

        {
			//UpdateToReject

			var old = await _manager.GetAsync(Id);

            var model = new TrainingScheduleApprovalHistoryViewModel();

            if (old != null)

            {

                model.TrainingScheduleId = old.Id;

                model.StatusId = old.StatusId;

                model.Remarks = Remarks;

                model.OldFromDateTime = old.FromDate;

                model.OldToDateTime = old.ToDate;

                model.CreatedBy = old.CreatedBy;
                model.UpdatedByUser= old.UpdatedByUser;

                var result = _approvalManager.AddApprovalHistory(model);

                if (old.StatusId == 2 || old.StatusId == 5 || old.StatusId == 6||old.StatusId==1)

                {

                    old.StatusId = 3;

                    await _manager.UpdateMyStatusAsync(old, this.GetUserId());

                    return Json(new { message = "Status Rejected successfully" });


                }
                if (old.StatusId == 3)
                {
                    old.StatusId = 8;
                    old.IsReScheduled= true;
                    await _manager.UpdateMyStatusAsync(old,this.GetUserId());
                    return Json(new { message = "Training Rescheduled Successfully" });
                }
                else

                {

                    return Json(new { message = "Status is already Rejected" });

                }

            }

            else

            {

                return Json(new { message = "Data Not Found" });

            }

        }
        public async Task<IActionResult> CancelTraining(int Id, string Remarks)

        {

            var old = await _manager.GetAsync(Id);

            var model = new TrainingScheduleApprovalHistoryViewModel();

            if (old != null)

            {

                model.TrainingScheduleId = old.Id;

                model.StatusId = old.StatusId;

                model.Remarks = Remarks;

                model.OldFromDateTime = old.FromDate;

                model.OldToDateTime = old.ToDate;

                model.CreatedBy = old.CreatedBy;
                model.UpdatedByUser = old.UpdatedByUser;

                var result = _approvalManager.AddApprovalHistory(model);


                if (result)
                {
                    old.StatusId = 7;

                    await _manager.UpdateMyStatusAsync(old, this.GetUserId());

                    return Json(new { message = "Training Cancelled successfully" });
                }
                else
                {
                    return Json(new { message = "Data Not Found" });
                }

                


                
            }

            else

            {

                return Json(new { message = "Data Not Found" });

            }

        }


        public async Task<IActionResult> GetNominationRequest(string Id,string Remarks)// for user request
		{
			ViewData["Title"] = _masterType;

            var user = this.GetUserId();

			//Id = Id.Decrypt(true);
			
			var idInt = Convert.ToInt32(Id);
			var old = await _manager.GetAsync(idInt);
			var CreatedById = old.CreatedBy;
			int CreatedByIdInt = System.Convert.ToInt32(CreatedById);
			var item = await _manager.GetAsync(idInt,_masterGetInclude);
			var model = new NominationRequestViewModel();

            if (old != null)
            {
                model.TrainingId = old.Id;
                model.CandidateId = user;
                //model.NominationRemarks = Remarks;
                model.NominationRemarks = new List<NominationRequestRemarksViewModel> { new NominationRequestRemarksViewModel { Remarks = Remarks } };
			}
         
            var result = await _nominationManager.AddUpdateAsync(model, CreatedByIdInt);

            if (old.NominationLeft == 0)
            {
                return Json("No Nomination left");
            }

            return Json(GetResultModelSuccess($"{_masterType} {(model.Id == 0 ? "Sent for Approval" : "Sent for Approval")} successfully"));

        }

         public async Task<IActionResult> withdrawNominationRequest(string Id, string Remarks)// for user request

        {
            ViewData["Title"] = _masterType;
            var user = this.GetUserId();
            //Id = Id.Decrypt(true);
            var idInt = Convert.ToInt32(Id);
            var old = await _manager.GetAsync(idInt); //return TrainingScheduleViewModel
            var list = await ((ITrainingScheduleManager)_manager).GetLatestNominationRequests(user, idInt);
            var models = new NominationRequestViewModel();

            foreach (var item in list)

            {

                if (list != null)

                {

                    models.Id = item.Id;

                    models.TrainingId = item.TrainingId;

                    models.CandidateId = item.CandidateId;

                    // model.NominationRemarks = Remarks;

                    models.Status = Common.NominationRequestStatus.WithdrawRequested;

                    models.NominationRemarks = new List<NominationRequestRemarksViewModel> { new NominationRequestRemarksViewModel { Remarks = Remarks } };

                }


            }

            var CreatedById = old.CreatedBy;

            int CreatedByIdInt = System.Convert.ToInt32(CreatedById);

            var result1 = await _nominationManager.AddUpdateAsync(models, CreatedByIdInt);


            return Json(GetResultModelSuccess($"{_masterType} {(models.Id == 0 ? "Withdraw" : "Withdraw")} successfully"));

            //return Json(new { message = "Sent For Approval" });

        }

        public async Task<IActionResult> SaveTrainingNomination(List<int> selectedUsers, string Id, string remarks)// nominate multiple users by admin.
		{
			ViewData["Title"] = _masterType;
			var user = this.GetUserId();
			Id = Id.Decrypt(true);
			var idInt = Convert.ToInt32(Id);
			var old = await _manager.GetAsync(idInt);
			var CreatedById = old.CreatedBy;
			int CreatedByIdInt = System.Convert.ToInt32(CreatedById);
			var modelList = new List<NominationRequestViewModel>();
			foreach (var selectedUser in selectedUsers)
			{
				var model = new NominationRequestViewModel
				{
					TrainingId = old.Id,
					CandidateId = selectedUser,
					NominationRemarks = new List<NominationRequestRemarksViewModel> { new NominationRequestRemarksViewModel { Remarks = remarks } }
				};
				modelList.Add(model);
			}
			var result = await _nominationManager.AddUpdateRangeAsync(modelList, CreatedByIdInt);
			return Json(new { message = "Users Nominate successfully" });
		}

        protected override Expression<Func<TrainingScheduleViewModel, bool>>? GetFilter(DataTableRequest request)
        {

            if (string.IsNullOrWhiteSpace(request.Search?.Value))
                return null;

            Expression<Func<TrainingScheduleViewModel, bool>> predicate = t => (t.IsActive ? "Active" : "Inactive").Contains(request.Search.Value)
            || (!string.IsNullOrWhiteSpace(t.Status.Name) && t.Status.Name.Contains(request.Search.Value))
            || (!string.IsNullOrWhiteSpace(t.TrainingTitle) && t.TrainingTitle.Contains(request.Search.Value))
            || (t.IsOnline ? "Online" : "Offline").Contains(request.Search.Value);
            return predicate;
        }

		protected override List<DataTableColumnsOrder>? GetOrderColumns(DataTableRequest request)
        {
            var columns = new[] { "", "Status.Description", "TrainingTitle", "FromDate", "FromTime", "IsOnline", "Venue.Name", "UpdatedByUser.Name,CreatedByUser.Name", "UpdatedOn,CreatedOn", "NominationLeft" };
            return GetOrderedColumns(columns, request);
        }

        public async override Task<DataTableResponse<TrainingScheduleViewModel>> IndexSearchDataTables()

        {

            var request = JqueryDataTableUtility.GetDataTableRequest(Request);

            var CurrentUser = GetUserId();

            if (CurrentUser != 1)

            {

                var UserDetails = await _userManager.GetAsync(CurrentUser, new[] { "Role" });

                var UserType = UserDetails.Role.UserTypeId;


                List<TrainingScheduleViewModel> CommonTraining = new List<TrainingScheduleViewModel>();

                List<TrainingScheduleViewModel> AsPerMatrixTraining = new List<TrainingScheduleViewModel>();

                List<TrainingScheduleViewModel> combinedTrainings = new List<TrainingScheduleViewModel>();

                List<TrainingScheduleViewModel> finalTrainings = new List<TrainingScheduleViewModel>();
                List<TrainingScheduleViewModel> superList = new List<TrainingScheduleViewModel>();           // No use 
                // var response = new DataTableResponse<TrainingScheduleViewModel>();
                if (UserType == 2)
                {
                    finalTrainings = await _trainingScheduleManager.GetAsync(new[] { "Status", "Venue" });
                }

                if (UserType == 3)

                {

                    //var Trainings = await _trainingScheduleManager.GetAsync(new[] { "Status", "Venue" });

                    //var MatrixDetails = await _TrainingMatrixManager.GetAsync();

                    //CommonTraining = Trainings.Where(item => !item.IsAsPerMatrix && item.ConductedByDivisionId == UserDetails.DivisionId).ToList();

                    //// var StatusId = Trainings.Where(item => item.StatusId == 2).ToList();

                    //AsPerMatrixTraining = Trainings.Where(item => item.IsAsPerMatrix && MatrixDetails.Any(item2 => item.CourseId == item2.CourseId)).ToList();


                    //combinedTrainings = CommonTraining.Concat(AsPerMatrixTraining).ToList();

                    //finalTrainings = combinedTrainings.Where(item => item.StatusId == 1).OrderByDescending(n=>n.LastActionOn).ToList();//change from 2 to 1

                    //// filteredViewModel.CommonTrainings = combinedTrainings;
                    ///
                    try
                    {
                        return await ((ITrainingScheduleManager)_manager).GetByUser(GetUserId(), request);
                    }
                    catch (Exception ex )
                    {
                      

                        throw ex;
                    }

                }

                if (UserType == 4 || UserType == 5)
                {
                    var Trainings = await _trainingScheduleManager.GetAsync(new[] { "Status", "Venue" });
                    finalTrainings = Trainings.Where(item => item.StatusId == 1|| item.StatusId == 3 || item.StatusId == 5 || item.StatusId == 6).OrderByDescending(n => n.LastActionOn).ToList();
                }
                if (UserType == 6)
                {
                    var Trainings = await _trainingScheduleManager.GetAsync(new[] { "Status", "Venue" });
                    finalTrainings = Trainings.Where(item => item.StatusId == 5).OrderByDescending(n => n.LastActionOn).OrderByDescending(n => n.LastActionOn).ToList();

                }

                return new DataTableResponse<TrainingScheduleViewModel>

                {

                    Data = finalTrainings.ToArray(),

                    Draw = request.Draw,

                    RecordsFiltered = finalTrainings.Count,

                    RecordsTotal = finalTrainings.Count

                };

            }

            // For other UserTypes, proceed with the usual response

            return await _manager.GetAsync(request, _masterGetInclude, GetFilter(request), GetOrderColumns(request));

        }
        [HttpPost]
        public virtual async Task<IActionResult> GetRemark(string iId)
        {
            iId = iId.Decrypt(true);
            if (string.IsNullOrWhiteSpace(iId))
                return Json(GetResultModelFail("Record not found"));
            var idInt = Convert.ToInt32(iId);
            var item = await _manager.GetAsync(idInt, new []{ "ConductedByDivision", "Course", "Venue", "Instructor" });
            SetProperties(ref item!);
            await SetDropdownViewBag(item!);
            return PartialView("_RemarkPartial", item);
        }

		[HttpPost]
		public virtual async Task<IActionResult> GetRemarkForNomination(string iId)
		{

			iId = iId.Decrypt(true);
			if (string.IsNullOrWhiteSpace(iId))
				return Json(GetResultModelFail("Record not found"));
			var idInt = Convert.ToInt32(iId);
			var item = await _manager.GetAsync(idInt, new[] { "ConductedByDivision", "Course", "Venue", "Instructor" });
            //item.IsShowNomination=true;


			SetProperties(ref item!);
			await SetDropdownViewBag(item!);
			return PartialView("_NominationRemarkPartial", item);
		}
		public async Task<IActionResult> GetApprovalStatusById(string iId, TrainingScheduleApprovalHistoryViewModel model)
        {
            iId = iId.Decrypt(true);
            var idInt = Convert.ToInt32(iId);
            model.TrainingScheduleId = idInt;
            var item = await _approvalManager.GetApprovalStatus(model);
            return PartialView("_ApprovalPartial", item);
        }

        [HttpPost]
		public async override Task<IActionResult> Item(TrainingScheduleViewModel model)
		{
			RemoveFromModelState();

			ViewData["Title"] = _masterType;
			if (!ModelState.IsValid)
			{
				return Json(GetFirstModelError(ModelState));
			}
			var validationResult = await IsValidModel(model);
			if (!validationResult.Status)
			{
				return Json(validationResult);
			}
			SetEditProperties(ref model);
          
            var result = await _manager.AddUpdateAsync(model, this.GetUserId());
			if (result)
			{
				return Json(GetResultModelSuccess($"{_masterType} {(model.Id == 0 ? "added" : "updated")} successfully"));
			}
			return Json(GetResultModelFail("Please validate the data or Time slot not available"));
		}
		protected override async Task SetDropdownViewBag(TrainingScheduleViewModel model)
        {
            //var courseIdList = await _courseManager.GetAsync(null, t => t.IsActive || t.Id == model.CourseId);
            //ViewBag.CourseId = courseIdList.GetSelectList(model.CourseId);
          

            ViewBag.CourseId = await _courseManager.GetAsync(new[] { "CourseCategory", "CourseEmploymentTypeMappings" }, t => t.IsActive || t.Id == model.CourseId);
            var divisionList = await _divisionManager.GetAsync(null, t => t.IsActive || t.Id == model.ConductedByDivisionId);
            ViewBag.ConductedByDivisionId = divisionList.GetSelectList(model.ConductedByDivisionId, dataTextField: _nameWithStatusField);
            var venueList = await _venueManager.GetAsync(null, t => t.IsActive || t.Id == model.VenueId);
			ViewBag.VenueSeats = venueList.Where(t=>t.Id == model.VenueId).Select(t=>t.VenueSeat).FirstOrDefault();
			ViewBag.VenueId = venueList.GetSelectList(model.VenueId, dataTextField: _nameWithStatusField);
           ViewBag.Venues = await _venueManager.GetAsync(null, t => t.IsActive || t.Id == model.VenueId);
            var instructorList = await _userManager.GetAsync(new[] { "Role" }, t => t.Role!.UserTypeId == 4 && (t.IsActive || t.Id == model.InstructorId));
            ViewBag.InstructorId = instructorList.GetSelectList(model.InstructorId, dataTextField: _nameWithStatusField);
            ViewBag.trainingStatus = await _trainingScheduleStatusManager.GetAsync();
          



            //Language List
            var languageMasterList = await _languageManager.GetAsync();
            List<TrainingScheduleLanguageViewModel> langList = new();
            foreach (var lang in languageMasterList)
            {
                var objLang = model.TrainingScheduleLanguages.Where(t => t.LanguageId == lang.Id).FirstOrDefault();
                if (objLang != null)
                {
                    objLang.Language = lang;
                    objLang.IsSelected = true;
                    langList.Add(objLang);
                }
                else
                {
                    langList.Add(new() { LanguageId = lang.Id, Language = lang, TraningScheduleId = model.Id });
                }
            }
            model.TrainingScheduleLanguages = langList;
        }

		protected override void SetEditProperties(ref TrainingScheduleViewModel model)
        {
            if (model.Files != null && model.Files.Any())
            {
                FileUtility fileUtility = new(_storageSettings);
                foreach (var file in model.Files)
                {
                    var attachmentModel = fileUtility.UploadFile(file);
                    model.TrainingScheduleAttachment.Add(new()
                    {
                        FilePath = attachmentModel.FilePath,
                        FileName = attachmentModel.FileName
                    });
                }
            }
        }

        //public override async Task<IActionResult> Index()
        //{
        //   return await base.Index();
        //}
    }
}
