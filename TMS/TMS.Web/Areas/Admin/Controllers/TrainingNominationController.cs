using TMS.Repository.Managers;
using TMS.ViewModels;
using TMS.Web.Controllers;
using TMS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc.Rendering;
using TMS.ViewModels.Masters;
using TMS.ViewModels.Training;
using System.Security.Cryptography;
using TMS.Models.Account;
using System.Runtime.InteropServices;
using TMS.Repository.Managers.Implementations;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TMS.Web.Areas.Admin.Controllers
{


    [Area("Admin")]
    public class TrainingNominationController : MastersAjaxBaseController<NominationRequestViewModel>
	{
        private readonly IMasterBaseManager<NominationRequestViewModel> _manager;
        private readonly IMasterManager<VenueMasterViewModel> _venueManager;
        private readonly IMasterManager<LocationMasterViewModel> _locationManager;
        private readonly IMasterManager<DepartmentMasterViewModel> _departmentManager;
        private readonly IMasterManager<DivisionMasterViewModel> _divisionManager;
        private readonly IMasterManager<DesignationMasterViewModel> _designationManager;
        private readonly IMasterManager<CourseMasterViewModel> _courseManager;
        private readonly IMasterManager<CourseCategoryMasterViewModel> _courseCategoryManager;
        private readonly IMasterManager<UserViewModel> _userManager;
		private readonly IMasterBaseManager<NominationRequestViewModel> _nominationManager;
        private readonly IMasterBaseManager<TrainingScheduleViewModel> _trainingScheduleManager;


        public TrainingNominationController(IMasterBaseManager<NominationRequestViewModel> manager, IMasterManager<LocationMasterViewModel> locationManager
            , IMasterManager<DepartmentMasterViewModel> departmentManager, IMasterManager<DivisionMasterViewModel> divisionManager
            , IMasterManager<DesignationMasterViewModel> designationManager, IMasterManager<CourseMasterViewModel> courseManager
            , IMasterManager<CourseCategoryMasterViewModel> courseCategoryManager
            , IMasterBaseManager<NominationRequestViewModel> nominationManager
            , IMasterBaseManager<TrainingScheduleViewModel> trainingScheduleManager
            , IMasterManager<UserViewModel> userManager
            , IMasterManager<VenueMasterViewModel> venueManager)
            :base(manager, "TrainingNomination", "TrainingNomination", new[] { "Candidate" }, new[] {"TrainingSchedules"})
        {
            _manager = manager;
            _locationManager = locationManager;
            _departmentManager = departmentManager;
            _divisionManager = divisionManager;
            _designationManager = designationManager;
            _courseManager = courseManager;
            _courseCategoryManager = courseCategoryManager;
            _userManager = userManager;
			_nominationManager= nominationManager;
            _trainingScheduleManager= trainingScheduleManager;
            _venueManager= venueManager;

        }
        protected override List<DataTableColumnsOrder>? GetOrderColumns(DataTableRequest request)
        {
            //var columns = new[] { "","CandidateId","Status", "Candidate.Name", "Candidate.Email" };
            var columns = new[] { "", "Status", "Candidate.Name", "Training.TrainingTitle", "Training.FromDate", "Training.ToDate", "Candidate.Email", "CreatedByUser.Name", "UpdatedOn,CreatedOn" };
            return GetOrderedColumns(columns, request);
        }
        //public override async Task<DataTableResponse<NominationRequestViewModel>> IndexSearchDataTables()
        //{
        //    var request = JqueryDataTableUtility.GetDataTableRequest(Request);
        //    var CurrentUser = this.GetUserId();
        //    var UserDetails = await _userManager.GetAsync(CurrentUser, new[] { "Role" });
        //    var UserType = UserDetails.Role.UserTypeId;
        //    return await _manager.GetAsync(request, _masterGetInclude, GetFilter(request), GetOrderColumns(request));
        //}

        public override async Task<DataTableResponse<NominationRequestViewModel>> IndexSearchDataTables()// doubt send al 
        {
            var request = JqueryDataTableUtility.GetDataTableRequest(Request);
            var CurrentUser = this.GetUserId();
            var UserDetails = await _userManager.GetAsync(CurrentUser, new[] { "Role" });
            var nominations = await _nominationManager.GetAsync(new[] { "Candidate","Training" });
        
            //var nominatedUserIds = filteredNominations.Select(n => n.Candidate.SupervisorId).ToList();
            //var allUsers = await _userManager.GetAsync(new[] { "Role" });
            //var finalList = allUsers.Where(u => u.SupervisorId == CurrentUser && !nominatedUserIds.Contains(u.Id)).ToList();

            return await _manager.GetAsync(request, _masterGetInclude, GetFilter(request), GetOrderColumns(request));
        }

        //public override async Task<DataTableResponse<NominationRequestViewModel>> IndexSearchDataTables()
        //{
        //    var request = JqueryDataTableUtility.GetDataTableRequest(Request);
        //    var CurrentUser = this.GetUserId();
        //    var UserDetails = await _userManager.GetAsync(CurrentUser, new[] { "Role" });
        //    var nominations = await _nominationManager.GetAsync(new[] { "Candidate" });
        //    var filteredNominations = nominations.Where(n => n.Candidate.SupervisorId == CurrentUser).ToList();
        //    if (filteredNominations.Any())
        //    {
        //        return await _manager.GetAsync(request, _masterGetInclude, GetFilter(request), GetOrderColumns(request));
        //    }
        //    else
        //    {
        //        return null; 
        //    }
        //}


        protected override Expression<Func<NominationRequestViewModel, bool>>? GetFilter(DataTableRequest request)
        {

            if (string.IsNullOrWhiteSpace(request.Search?.Value))
                return null;

            Expression<Func<NominationRequestViewModel, bool>> predicate = t => (t.IsActive ? "Active" : "Inactive").Contains(request.Search.Value)
            || (!string.IsNullOrWhiteSpace(t.Candidate.Name) && t.Candidate.Email.Contains(request.Search.Value));

            return predicate;
        }
        public async Task<IActionResult> GetNominationModel( string iId)
        {
            iId = iId.Decrypt(true);
            if (string.IsNullOrWhiteSpace(iId))
                return Json(GetResultModelFail("Record not found"));
            var idInt = Convert.ToInt32(iId);
            var locations = await _locationManager.GetAsync();
            ViewBag.LocationId = locations.GetSelectList(dataTextField: "NameStatus");
            var courceCategories = await _courseCategoryManager.GetAsync();
            ViewBag.CourseCategoryId = courceCategories.GetSelectList(dataTextField: "NameStatus");
            var item = await _trainingScheduleManager.GetAsync(idInt, new[] { "Instructor", "Venue" , "Course", "ConductedByDivision" });
            
            return PartialView("_NominationPartial",item);
        }

       

        public async Task<IActionResult> GetDepartments(int id)
        {
            var departments = await _departmentManager.GetAsync(null, t => t.LocationId == id && t.IsActive);
            return GetDropdownList(departments.GetSelectList(dataTextField: "NameStatus"), true);
        }
        public async Task<IActionResult> GetDivision(int id)
        {
            var departments = await _divisionManager.GetAsync(null, t => t.DepartmentId == id && t.IsActive);
            return GetDropdownList(departments.GetSelectList(dataTextField: "NameStatus"), true);
        }
        public async Task<IActionResult> GetDesignation(int id)
        {
            var departments = await _designationManager.GetAsync(null, t => t.DivisionId == id && t.IsActive);
            return GetDropdownList(departments.GetSelectList(dataTextField: "NameStatus"), true);
        }
        protected virtual IActionResult GetDropdownList(List<SelectListItem> selectList, bool excludeDefault = false)
        {
            ViewBag.ExcludeDefault = excludeDefault;
            return PartialView("~/Views/Shared/_OptionsPartial.cshtml", selectList);
        }
        //public async Task<IActionResult> GetTrainingNomination()
        //{

        //    var nominatedUserIds = await _nominationManager.GetAsync(new[] { "Candidate" });
        //    var nominatedUserIds = nominatedUsers.Select(u => u.Id);
        //    var userList = await _userManager.GetAsync(new[] { "Role" }, u => u.Role.UserTypeId == 3 && u.IsActive);



        //    var filteredUserList = userList.Where(u => !nominatedUserIds.Contains(u.Id));

        //    return PartialView("_NominationUserPartial", filteredUserList);

        //}

        //public async Task<IActionResult> GetTrainingNomination()
        //{
        //    var nominatedUsers = await _nominationManager.GetAsync(new[] { "Candidate" });
        //    var userList = await _userManager.GetAsync(new[] { "Role" }, u => u.Role.UserTypeId == 3 && u.IsActive);
        //    var nominatedUserIds = nominatedUsers.Select(u => u.Id).ToList();
        //    //var filteredUserList = userList.Where(u => !nominatedUsers.Contains(u.Id)).ToList();

        //    return PartialView("_NominationUserPartial", userList);
        //}
        //public async Task<IActionResult> GetTrainingNomination()
        //{
        //    var nominatedUsers = await _nominationManager.GetAsync(new[] { "Candidate" });
        //    var userList = await _userManager.GetAsync(new[] { "Role" }, u => u.Role.UserTypeId == 3 && u.IsActive);
        //    return PartialView("_NominationUserPartial",userList);
        //}
        public async Task<IActionResult> GetTrainingNomination()
        {
            var nominatedUsers = await _nominationManager.GetAsync(new[] { "Candidate" });
            var userList = await _userManager.GetAsync(new[] { "Role" }, u => u.Role.UserTypeId == 3 && u.IsActive);
            var finalList = userList.Where(u => !nominatedUsers.Any(n => n.CandidateId == u.Id)).ToList();
            return PartialView("_NominationUserPartial", finalList);
        }
		public async Task<IActionResult> GetTrainingDetailsOnNomination(string iId)
		{
			iId = iId.Decrypt(true);
			if (string.IsNullOrWhiteSpace(iId))
				return Json(GetResultModelFail("Record not found"));  
			var idInt = Convert.ToInt32(iId);
			var item = await _nominationManager.GetAsync(idInt, new[] { "Training" });
			return PartialView("");
		}

		//public async Task<IActionResult> GetTrainingNomination()
		//{
		//    var nominatedUsers = await _nominationManager.GetAsync(new[] { "Candidate" });
		//    var nominatedUserIds = nominatedUsers.Select(u => u.Id).ToList();
		//    var userList = await _userManager.GetAsync(new[] { "Role" }, u => u.Role.UserTypeId == 3 && u.IsActive);


		//    // Filter out nominated users from the user list
		//    var filteredUserList = new List<UserViewModel>();
		//    foreach (var user in userList)
		//    {
		//        if (!nominatedUserIds.Contains(user.Id))
		//        {
		//            filteredUserList.Add(user);
		//        }
		//    }

		//    return PartialView("_NominationUserPartial", filteredUserList);
		//}


		public async Task<IActionResult> ApproveNomination(string Id, string Remarks)
		{
			Id = Id.Decrypt(true);
			var idInt = Convert.ToInt32(Id);
			ViewData["Title"] = _masterType;
			var old = await _manager.GetAsync(idInt);
			var user = GetUserId();

			if (old.Status == Common.NominationRequestStatus.Requested)
			{
				old.Status = Common.NominationRequestStatus.Approved;
                old.NominationRemarks = new List<NominationRequestRemarksViewModel> { new NominationRequestRemarksViewModel { Remarks = Remarks } };
				await _manager.AddUpdateAsync(old, this.GetUserId());
				return Json(GetResultModelSuccess($"{_masterType} {(idInt == 0 ? "Planned" : "Approved")} successfully"));
			}
            if(old.Status == Common.NominationRequestStatus.WithdrawRequested)
            {
                old.Status = Common.NominationRequestStatus.WithdrawApproved;
                old.NominationRemarks = new List<NominationRequestRemarksViewModel> { new NominationRequestRemarksViewModel { Remarks = Remarks } };
                await _manager.AddUpdateAsync(old, this.GetUserId());
                return Json(GetResultModelSuccess($"{_masterType} {(idInt == 0 ? "Planned" : "WithdrawApproved")} successfully"));
            }
			else
			{
				return Json(new { message = "Data Not Found" });
			}
		}
		
		public async Task<IActionResult> RejectNomination(string Id, string Remarks)
		{
			Id = Id.Decrypt(true);
			var idInt = Convert.ToInt32(Id);
			ViewData["Title"] = _masterType;
			var old = await _manager.GetAsync(idInt);
			var user = GetUserId();

			if (old.Status == Common.NominationRequestStatus.Requested)
			{
				old.Status = Common.NominationRequestStatus.Rejected;
                old.NominationRemarks = new List<NominationRequestRemarksViewModel> { new NominationRequestRemarksViewModel { Remarks = Remarks } };
                await _manager.AddUpdateAsync(old, this.GetUserId());
				return Json(GetResultModelSuccess($"{_masterType} {(idInt == 0 ? "Planned" : "Rejected")} successfully"));
			}
            if (old.Status == Common.NominationRequestStatus.WithdrawRequested)
            {
                old.Status = Common.NominationRequestStatus.WithdrawRejected;
                old.NominationRemarks = new List<NominationRequestRemarksViewModel> { new NominationRequestRemarksViewModel { Remarks = Remarks } };
                await _manager.AddUpdateAsync(old, this.GetUserId());
                return Json(GetResultModelSuccess($"{_masterType} {(idInt == 0 ? "Planned" : "WithDraw Rejected")} successfully"));
            }
            else
			{
				return Json(new { message = "Data Not Found" });
			}
		}

        public virtual async Task<IActionResult> GetNominationRemarkByApprover(string iId)// for admin
        {
            iId = iId.Decrypt(true);
            if (string.IsNullOrWhiteSpace(iId))
                return Json(GetResultModelFail("Record not found"));
            var idInt = Convert.ToInt32(iId);
            var item = await _nominationManager.GetAsync(idInt, new[] { "Training", "Training.Course", "Training.Instructor", "Training.Venue", "Training.ConductedByDivision" });
            SetProperties(ref item!);
            await SetDropdownViewBag(item!);
            return PartialView("_NominationRemarkPartial",item);
        }
        protected override async Task SetDropdownViewBag(NominationRequestViewModel model)
        {
            var courseIdList = await _courseManager.GetAsync(null, t => t.IsActive || t.Id == model.Training.CourseId);
            ViewBag.CourseId = courseIdList.GetSelectList(model.Training.CourseId);


            ViewBag.CourseId = await _courseManager.GetAsync(new[] { "CourseCategory", "CourseEmploymentTypeMappings" }, t => t.IsActive || t.Id == model.Training.CourseId);
            var divisionList = await _divisionManager.GetAsync(null, t => t.IsActive || t.Id == model.Training.ConductedByDivisionId);
            ViewBag.ConductedByDivisionId = divisionList.GetSelectList(model.Training.ConductedByDivisionId, dataTextField: _nameWithStatusField);
            var venueList = await _venueManager.GetAsync(null, t => t.IsActive || t.Id == model.Training.VenueId);
            ViewBag.VenueSeats = venueList.Where(t => t.Id == model.Training.VenueId).Select(t => t.VenueSeat).FirstOrDefault();
            ViewBag.VenueId = venueList.GetSelectList(model.Training.VenueId, dataTextField: _nameWithStatusField);
            ViewBag.Venues = await _venueManager.GetAsync(null, t => t.IsActive || t.Id == model.Training.VenueId);
            var instructorList = await _userManager.GetAsync(new[] { "Role" }, t => t.Role!.UserTypeId == 4 && (t.IsActive || t.Id == model.Training.InstructorId));
            ViewBag.InstructorId = instructorList.GetSelectList(model.Training.InstructorId, dataTextField: _nameWithStatusField);
      
        }

        

    }
}
