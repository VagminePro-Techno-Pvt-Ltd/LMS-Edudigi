using TMS.Common;
using TMS.Repository.Managers;
using TMS.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TMS.Web
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ValidateFormAccessAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        private readonly int _formId;

        public ValidateFormAccessAttribute(FormDefination Form)
        {
            _formId = (int)Form;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // 🔥 BYPASS: Give full access to Admin and Faculty roles
            var roleClaim = user.Claims.FirstOrDefault(c => c.Type == "role")?.Value?.ToLower()?.Trim() ?? "";
            bool isBypassAdmin = roleClaim == "admin" || roleClaim == "super admin" || roleClaim == "exam coordinator" || roleClaim == "management";
            bool isBypassFaculty = roleClaim == "faculty" || roleClaim == "co faculty" || roleClaim == "co-faculty" || roleClaim == "teacher";
            if (isBypassAdmin || isBypassFaculty)
            {
                if (context.HttpContext != null)
                {
                    context.HttpContext.Items["ViewPermission"] = 1;
                    context.HttpContext.Items["AddPermission"] = 1;
                    context.HttpContext.Items["EditPermission"] = 1;
                }
                return;
            }

            var userPermissions = context.HttpContext?.Session.GetObjectFromJson<UserPermissions>("UserPermissions");
            if (userPermissions == null)
            {
                var accountManager = context.HttpContext?.RequestServices.GetService(typeof(IAccountManager)) as IAccountManager;
                var userId = (user as System.Security.Claims.ClaimsPrincipal)?.Claims?.Where(t => t.Type == "userId")?.Select(t => t.Value)?.FirstOrDefault();
                if (int.TryParse(userId, out int userIdInt))
                    userPermissions = accountManager?.GetUserPermissions(userIdInt).GetAwaiter().GetResult();
                if (userPermissions == null)
                {
                    context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                    return;
                }
                context.HttpContext?.Session.SetObjectAsJson("UserPermissions", userPermissions);
            }

            var permission = userPermissions.Forms.Where(t => t.Id == _formId).FirstOrDefault();
            if (permission == null)
            {
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                return;
            }
            if (!permission.View)
            {
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                return;
            }
            if (context.HttpContext != null)
            {
                context.HttpContext.Items["ViewPermission"] = permission.View ? 1 : 0;
                context.HttpContext.Items["AddPermission"] = permission.Add ? 1 : 0;
                context.HttpContext.Items["EditPermission"] = permission.Edit ? 1 : 0;
            }
            return;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ValidateFormAccessActionAttribute : ValidateFormAccessAttribute
    {
        public ValidateFormAccessActionAttribute(FormDefination Form) : base(Form)
        {

        }
    }
}
