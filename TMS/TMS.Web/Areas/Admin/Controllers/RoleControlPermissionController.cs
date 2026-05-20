using TMS.Repository.Managers;
using TMS.ViewModels.Account;
using TMS.Web.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace TMS.Web.Areas.Admin.Controllers
{
    [ValidateFormAccess(Common.FormDefination.RoleControlPermission)]
    [Area("Admin")]
    public class RoleControlPermissionController : BaseController
    {
        private readonly IAccountManager _accountManager;
        private readonly IMasterManager<RoleMasterViewModel> _roleManager;

        public RoleControlPermissionController(IAccountManager accountManager, IMasterManager<RoleMasterViewModel> roleManager)
        {
            _accountManager = accountManager;
            _roleManager = roleManager;
        }
        public async Task<IActionResult> Index()
        {
            await SetDropdownViewBag();
            return View();
        }

        public async Task<IActionResult> GetRoleControlsPermissions(int roleId, int formId)
        {
            var permissions = await _accountManager.GetFormControlPermissionByRole(formId, roleId);
            return PartialView("_ControlPermissionsPartial", permissions);
        }
        public async Task<IActionResult> UpdateRoleControlsPermissions(List<FormControlViewModel> model, int roleId)
        {
            var result = await _accountManager.UpdateFormControlPermissionByRole(model, roleId, this.GetUserId());
            if (result == 1)
                return Json(new { status = true, message = "Permission updated successfully" });
            return Json(new { status = false, message = "Something went wrong, please try again!" });
        }
        private async Task SetDropdownViewBag()
        {
            var roles = await _roleManager.GetAsync();
            ViewBag.Roles = roles.GetSelectList();
        }
    }
}
