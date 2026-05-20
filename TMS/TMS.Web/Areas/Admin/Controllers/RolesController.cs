using TMS.Repository.Managers;
using TMS.Web.Controllers;
using TMS.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TMS.ViewModels.Account;

namespace TMS.Web.Areas.Admin.Controllers
{
    [ValidateFormAccess(Common.FormDefination.Role)]
    [Area("Admin")]
    public class RolesController : MastersController<RoleMasterViewModel>
    {
        private readonly IAccountManager _accountManager;

        public RolesController(IMasterManager<RoleMasterViewModel> manager, IAccountManager accountManager)
            : base(manager, "Role", "Role", new[] { "UserType" })
        {
            _accountManager = accountManager;
        }
        protected override async Task SetDropdownViewBag(RoleMasterViewModel model)
        {
            //State binding
            var stateList = await _accountManager.GetAllUserTypes();
            ViewBag.UserTypeId = stateList.GetSelectList(model.UserTypeId);
        }
    }
}
