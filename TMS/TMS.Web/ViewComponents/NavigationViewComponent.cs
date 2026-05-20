using TMS.Repository.Managers;
using TMS.ViewModels.Account;
using Microsoft.AspNetCore.Mvc;

namespace TMS.Web
{
    public class NavigationViewComponent : ViewComponent
    {
        private readonly IAccountManager _accountManager;
        private readonly IHttpContextAccessor _contextAccessor;

        public NavigationViewComponent(IAccountManager accountManager, IHttpContextAccessor contextAccessor)
        {
            _accountManager = accountManager;
            _contextAccessor = contextAccessor;
        }
        public IViewComponentResult Invoke(string type)
        {
            var finalPermissions = new UserPermissions();
            var userPermissions = _contextAccessor.HttpContext?.Session.GetObjectFromJson<UserPermissions>("UserPermissions");
            ViewBag.HasAdmin = false;
            if (userPermissions == null)
            {
                var userId = (User as System.Security.Claims.ClaimsPrincipal)?.Claims?.Where(t => t.Type == "userId")?.Select(t => t.Value)?.FirstOrDefault();
                if (int.TryParse(userId, out int userIdInt))
                    userPermissions = _accountManager.GetUserPermissions(userIdInt).GetAwaiter().GetResult();
                if (userPermissions != null)
                    _contextAccessor.HttpContext?.Session.SetObjectAsJson("UserPermissions", userPermissions);
            }

            //if (userPermissions != null && !string.IsNullOrWhiteSpace(type) && type == "a")
            //{
            //    finalPermissions.Menus = userPermissions.Menus.Where(t => t.IsAdmin).ToList();
            //    finalPermissions.Forms = userPermissions.Forms.Where(t => t.IsAdmin).ToList();
            //}
            //else if (userPermissions != null)
            //{
            //    finalPermissions.Menus = userPermissions.Menus.Where(t => !t.IsAdmin).ToList();
            //    finalPermissions.Forms = userPermissions.Forms.Where(t => !t.IsAdmin).ToList();
            //    ViewBag.HasAdmin = userPermissions.Forms.Where(t => t.IsAdmin).Any();
            //}
            if (userPermissions != null)
            {
                finalPermissions.Menus = userPermissions.Menus.ToList();
                finalPermissions.Forms = userPermissions.Forms.ToList();
            }
            return View(finalPermissions);
        }
    }
}
