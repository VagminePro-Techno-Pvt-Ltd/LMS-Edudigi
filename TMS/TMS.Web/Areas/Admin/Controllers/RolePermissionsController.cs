using TMS.Repository.Managers;
using TMS.ViewModels.Account;
using TMS.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TMS.Web.Areas.Admin.Controllers
{
    [ValidateFormAccess(Common.FormDefination.RolePermissions)]
    [Area("Admin")]
    public class RolePermissionsController : MastersController<RoleMasterViewModel>
    {
        private readonly IAccountManager _accountManager;
        public RolePermissionsController(IMasterManager<RoleMasterViewModel> manager, IAccountManager accountManager)
            : base(manager, "Role Permission", "Role Permission")
        {
            _accountManager = accountManager;
        }
        public override async Task<IActionResult> Index()
        {
            var roles = await _manager.GetAsync();
            ViewBag.Roles = roles.GetSelectList();
            return View();
        }

        public async Task<IActionResult> GetRolePermissions(int roleId)
        {
            var model = await _accountManager.GetRolePermissionsById(roleId);
            return PartialView("_RolesPermissionsPartial", model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateRolePermissions(List<RolePermissionsViewModel> model, int roleId)
        {
            var result= await _accountManager.UpdateRolePermissions(model, roleId);
            if(result==1)
            return Json(new { status = true, message = "Request processed successfully" });

            return Json(new { status = false, message = "Something went wrong, please contact support!" });
        }
    }
}
