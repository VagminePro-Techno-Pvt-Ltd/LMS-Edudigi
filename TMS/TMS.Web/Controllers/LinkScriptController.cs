using TMS.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text;
using System.Runtime.InteropServices;

namespace TMS.Web.Controllers
{
	public class LinkScriptController : Controller
	{
		//private readonly StorageSettings _storageSettings;
		//public LinkScriptController(IOptions<StorageSettings> options)
		public LinkScriptController()
		{
			// _storageSettings = options.Value;
		}


		public JavaScriptResult PermissionScript(string q)
		{
			if (string.IsNullOrWhiteSpace(q))
				return new JavaScriptResult(string.Empty);
			return new JavaScriptResult(q.Decrypt());
		}


		public JavaScriptResult AdminRoleAccessMapping()
		{
			StringBuilder stringBuilder = new();
			stringBuilder.Append($"var updateRoleAccessMappingUrl='{Url.Action("UpdateRoleAccessMapping", "RoleAccessMapping", new { area = "Admin" })}';");
			return new JavaScriptResult(stringBuilder.ToString());
		}

		public JavaScriptResult AdminRolePermissions()
		{
			StringBuilder stringBuilder = new();
			stringBuilder.Append($"var getRolePermissionsUrl='{Url.Action("GetRolePermissions", "RolePermissions", new { area = "Admin" })}';");
			stringBuilder.Append($"var updateRolePermissionsUrl='{Url.Action("UpdateRolePermissions", "RolePermissions", new { area = "Admin" })}';");
			return new JavaScriptResult(stringBuilder.ToString());
		}
		public JavaScriptResult AdminRoleControlsPermissions()
		{
			StringBuilder stringBuilder = new();
			stringBuilder.Append($"var getRoleControlsPermissionsUrl='{Url.Action("GetRoleControlsPermissions", "RoleControlPermission", new { area = "Admin" })}';");
			stringBuilder.Append($"var updateRoleControlsPermissionsUrl='{Url.Action("UpdateRoleControlsPermissions", "RoleControlPermission", new { area = "Admin" })}';");
			return new JavaScriptResult(stringBuilder.ToString());
		}
		public JavaScriptResult AdminUser()
		{
			StringBuilder stringBuilder = new();
			stringBuilder.Append($"var getRolesOptionsUrl='{Url.Action("GetRolesOptions", "User", new { area = "Admin" })}';");
			return new JavaScriptResult(stringBuilder.ToString());
		}
		public JavaScriptResult AdminVenueMaster()
		{
			StringBuilder stringBuilder = new();
			stringBuilder.Append($"var getListItemUrl='{Url.Action("List", "Venue", new { area = "Admin" })}';");
			stringBuilder.Append($"var getListPageUrl='{Url.Action("IndexSearchDataTables", "Venue", new { area = "Admin" })}';");
			stringBuilder.Append($"var getItemUrl='{Url.Action("Get", "Venue", new { area = "Admin" })}';");
			stringBuilder.Append($"var saveItemUrl='{Url.Action("Item", "Venue", new { area = "Admin" })}';");
			return new JavaScriptResult(stringBuilder.ToString());
		}
		public JavaScriptResult AdminSchedule()
		{
			StringBuilder stringBuilder = new();
			stringBuilder.Append($"var getScheduleUrl='{Url.Action("UpdateToSchedule", "TrainingSchedule", new { area = "Admin" })}';");
			stringBuilder.Append($"var getSApprovalStatusUrl='{Url.Action("GetApprovalStatusById", "TrainingSchedule", new { area = "Admin" })}';");
			stringBuilder.Append($"var getNominationRequestUrl='{Url.Action("GetNominationRequest", "TrainingSchedule", new { area = "Admin" })}';");
            stringBuilder.Append($"var getWithdrawlRequestUrl='{Url.Action("withdrawNominationRequest", "TrainingSchedule", new { area = "Admin" })}';");
            stringBuilder.Append($"var getCancelUrl='{Url.Action("CancelTraining", "TrainingSchedule", new { area = "Admin" })}';");


            //stringBuilder.Append($"var getNominationRequestItemUrl='{Url.Action("GetNominationRequest", "TrainingSchedule", new { area = "Admin" })}';");




            return new JavaScriptResult(stringBuilder.ToString());


		}
		public JavaScriptResult AdminReject()
		{
			StringBuilder stringBuilder = new();
			stringBuilder.Append($"var getRejectUrl='{Url.Action("UpdateToRescheduleAndReject", "TrainingSchedule", new { area = "Admin" })}';");
			stringBuilder.Append($"var getReScheduleUrl='{Url.Action("GetRescheduleModal", "TrainingSchedule", new { area = "Admin" })}';");

			return new JavaScriptResult(stringBuilder.ToString());


		}

		public JavaScriptResult Global()
		{
			StringBuilder stringBuilder = new();
			stringBuilder.Append($"var getUserMenuUrl='{Url.Action("LoadMenu", "Account", new { area = "" })}';");
			stringBuilder.Append($"var userNotificationGetUrl='{Url.Action("UserNotificationGet", "Account", new { area = "" })}';");
			return new JavaScriptResult(stringBuilder.ToString());
		}

		public JavaScriptResult HomeIndex()
		{
			StringBuilder stringBuilder = new();
			stringBuilder.Append($"var dashboardUserUrl='{Url.Action("DashboardUser", "Home", new { area = "" })}';");
			stringBuilder.Append($"var getAllMastersUrl='{Url.Action("GetAllMasters", "Home", new { area = "" })}';");
			return new JavaScriptResult(stringBuilder.ToString());
		}
		public JavaScriptResult AdminGlobalMaster(string c)
		{
			StringBuilder stringBuilder = new();
			stringBuilder.Append($"var getListItemUrl='{Url.Action("List", c, new { area = "Admin" })}';");
			stringBuilder.Append($"var getListPageUrl='{Url.Action("IndexSearchDataTables", c, new { area = "Admin" })}';");
			stringBuilder.Append($"var getItemUrl='{Url.Action("Get", c, new { area = "Admin" })}';");
			stringBuilder.Append($"var getRemarkItemUrl='{Url.Action("GetRemark", c, new { area = "Admin" })}';");
			stringBuilder.Append($"var getNominationRemarkItemUrl='{Url.Action("GetRemarkForNomination", c, new { area = "Admin" })}';");
			stringBuilder.Append($"var saveItemUrl='{Url.Action("Item", c, new { area = "Admin" })}';");

			if (c == "Division")
			{
				stringBuilder.Append($"var getDepartmentOptionUrl='{Url.Action("GetDepartments", c, new { area = "Admin" })}';");
			}
			else if (c == "Designation")
			{
				stringBuilder.Append($"var getDepartmentOptionUrl='{Url.Action("GetDepartments", c, new { area = "Admin" })}';");
				stringBuilder.Append($"var getDivisionOptionUrl='{Url.Action("GetDivisions", c, new { area = "Admin" })}';");
			}
			else if (c == "Question")
			{
				stringBuilder.Append($"var getQuestionOptionsUrl='{Url.Action("GetQuestionOptions", c, new { area = "Admin" })}';");
			}
			else if (c == "User")
			{
				stringBuilder.Append($"var getDepartmentsURL='{Url.Action("GetDepartments", c, new { area = "Admin" })}';");
				stringBuilder.Append($"var getDivisionURL='{Url.Action("GetDivision", c, new { area = "Admin" })}';");
				stringBuilder.Append($"var getDesignationURL='{Url.Action("GetDesignation", c, new { area = "Admin" })}';");
			}

			return new JavaScriptResult(stringBuilder.ToString());
		}

		public JavaScriptResult AdminTrainingMatrixMaster()
		{
			StringBuilder stringBuilder = new();
			stringBuilder.Append($"var getDepartmentsURL='{Url.Action("GetDepartments", "TrainingMatrix", new { area = "Admin" })}';");
			stringBuilder.Append($"var getDivisionURL='{Url.Action("GetDivision", "TrainingMatrix", new { area = "Admin" })}';");
			stringBuilder.Append($"var getDesignationURL='{Url.Action("GetDesignation", "TrainingMatrix", new { area = "Admin" })}';");
			stringBuilder.Append($"var getTrainingMatrixURL='{Url.Action("GetTrainingMatrix", "TrainingMatrix", new { area = "Admin" })}';");
			stringBuilder.Append($"var saveTrainingMatrixURL='{Url.Action("SaveTrainingMatrix", "TrainingMatrix", new { area = "Admin" })}';");
			return new JavaScriptResult(stringBuilder.ToString());
		}
		public JavaScriptResult AdminQuizMaster()
		{
			StringBuilder stringBuilder = new();
			stringBuilder.Append($"var getQuestionOptionsUrl='{Url.Action("GetQuestionOptions", "Quiz", new { area = "Admin" })}';");
			stringBuilder.Append($"var saveQuestionsUrl='{Url.Action("SaveQuestions", "Quiz", new { area = "Admin" })}';");
			stringBuilder.Append($"var getQuestionListUrl='{Url.Action("GetQuestionList", "Quiz", new { area = "Admin" })}';");
			stringBuilder.Append($"var getQuizMappingListUrl='{Url.Action("GetQuizMappingList", "Quiz", new { area = "Admin" })}';");
			stringBuilder.Append($"var saveQuizMappingUrl='{Url.Action("SaveQuizMapping", "Quiz", new { area = "Admin" })}';");
			return new JavaScriptResult(stringBuilder.ToString());
		}

		public JavaScriptResult AdminNominationRequest()

		{

			StringBuilder stringBuilder = new();

			stringBuilder.Append($"var getNominationItemUrl='{Url.Action("GetNominationModel", "TrainingNomination", new { area = "Admin" })}';");

			stringBuilder.Append($"var getNomDepartmentsURL='{Url.Action("GetDepartments", "TrainingNomination", new { area = "Admin" })}';");

			stringBuilder.Append($"var getNomDivisionURL='{Url.Action("GetDivision", "TrainingNomination", new { area = "Admin" })}';");

			stringBuilder.Append($"var getNomDesignationURL='{Url.Action("GetDesignation", "TrainingNomination", new { area = "Admin" })}';");

			stringBuilder.Append($"var getTrainingNominationURL='{Url.Action("GetTrainingNomination", "TrainingNomination", new { area = "Admin" })}';");

			stringBuilder.Append($"var saveTrainingNominationURL='{Url.Action("SaveTrainingNomination", "TrainingSchedule", new { area = "Admin" })}';");


			stringBuilder.Append($"var getApproveNominationUrl='{Url.Action("ApproveNomination", "TrainingNomination", new { area = "Admin" })}';");
			stringBuilder.Append($"var getRejectNominationUrl='{Url.Action("RejectNomination", "TrainingNomination", new { area = "Admin" })}';");	
			stringBuilder.Append($"var getNominationRemarkUrl='{Url.Action("GetNominationRemarkByApprover", "TrainingNomination", new { area = "Admin" })}';");






			return new JavaScriptResult(stringBuilder.ToString());

		}
	}
}
