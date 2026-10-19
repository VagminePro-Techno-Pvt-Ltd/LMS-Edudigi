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

            var userPrincipal = User as System.Security.Claims.ClaimsPrincipal;
            var userId = userPrincipal?.Claims?.Where(t => t.Type == "userId")?.Select(t => t.Value)?.FirstOrDefault();
            int.TryParse(userId, out int userIdInt);

            if (userPermissions == null && userIdInt > 0)
            {
                userPermissions = _accountManager.GetUserPermissions(userIdInt).GetAwaiter().GetResult();
                if (userPermissions != null)
                    _contextAccessor.HttpContext?.Session.SetObjectAsJson("UserPermissions", userPermissions);
            }

            if (userPermissions != null)
            {
                finalPermissions.Menus = userPermissions.Menus.ToList();
                finalPermissions.Forms = userPermissions.Forms.ToList();
            }

            // Guarantee essential Academic menus for staff roles (Admin, Faculty, etc.)
            var roleClaim = userPrincipal?.Claims?.FirstOrDefault(c => c.Type == "role")?.Value?.ToLower()?.Trim() ?? "";
            bool isStaff = roleClaim == "admin" || roleClaim == "super admin" || roleClaim == "faculty" || roleClaim == "co faculty" || roleClaim == "co-faculty" || roleClaim == "teacher" || roleClaim == "exam coordinator" || roleClaim == "management" || string.IsNullOrEmpty(roleClaim);

            if (isStaff || userIdInt > 0)
            {
                // Ensure ACADEMICS menu group exists
                var academicsMenu = finalPermissions.Menus.FirstOrDefault(m => m.Id == 1 || m.Name.Equals("ACADEMICS", StringComparison.OrdinalIgnoreCase));
                if (academicsMenu == null)
                {
                    academicsMenu = new MenuViewModel { Id = 1, Name = "ACADEMICS", Sequence = 1, IsAdmin = true };
                    finalPermissions.Menus.Insert(0, academicsMenu);
                }

                // 1. Ensure Quiz Master
                if (!finalPermissions.Forms.Any(f => f.Controller != null && f.Controller.Equals("Quiz", StringComparison.OrdinalIgnoreCase)))
                {
                    finalPermissions.Forms.Add(new FormViewModel
                    {
                        Id = (int)TMS.Common.FormDefination.Quiz,
                        Name = "Quiz Master",
                        Area = "Admin",
                        Controller = "Quiz",
                        Action = "Index",
                        IconClass = "fa-solid fa-clipboard-question",
                        Sequence = 14,
                        MenuId = academicsMenu.Id,
                        IsAdmin = true,
                        View = true,
                        Add = true,
                        Edit = true
                    });
                }

                // 2. Ensure Quiz Assessment
                if (!finalPermissions.Forms.Any(f => f.Controller != null && f.Controller.Equals("FacultyQuizAssessment", StringComparison.OrdinalIgnoreCase)))
                {
                    finalPermissions.Forms.Add(new FormViewModel
                    {
                        Id = (int)TMS.Common.FormDefination.QuizAssemement,
                        Name = "Quiz Assessment",
                        Area = "",
                        Controller = "FacultyQuizAssessment",
                        Action = "Index",
                        IconClass = "fa-solid fa-spell-check",
                        Sequence = 15,
                        MenuId = academicsMenu.Id,
                        IsAdmin = true,
                        View = true,
                        Add = true,
                        Edit = true
                    });
                }
            }

            return View(finalPermissions);
        }
    }
}
