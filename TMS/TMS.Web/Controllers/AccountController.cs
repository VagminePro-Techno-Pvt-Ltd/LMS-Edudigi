using TMS.Repository.Managers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TMS.Web.Controllers
{
    [Authorize]
    public class AccountController : BaseController
    {
        private readonly IAccountManager _accountManager;
        private readonly IUserManager _userManager;

        public AccountController(IAccountManager accountManager, IUserManager userManager)
        {
            _accountManager = accountManager;
            _userManager = userManager;
        }

        public async Task<IActionResult> UserNotificationGet()
        {
            await Task.CompletedTask;
            return PartialView("_NotificationPartial");
        }
    }
}
