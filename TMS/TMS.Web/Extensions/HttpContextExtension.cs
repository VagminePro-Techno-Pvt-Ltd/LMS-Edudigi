using TMS.ViewModels.Account;
namespace TMS.Web
{
    public static class HttpContextExtension
    {
        public static bool IsViewAllowed(this HttpContext context)
        {
            if (context == null)
                return false;
            if (!Int32.TryParse(Convert.ToString(context.Items["ViewPermission"]), out int permission))
                return false;
            return permission == 1 ? true : false;
        }

        public static bool IsAddAllowed(this HttpContext context)
        {
            if (context == null)
                return false;
            if (!Int32.TryParse(Convert.ToString(context.Items["AddPermission"]), out int permission))
                return false;
            return permission == 1 ? true : false;
        }
        public static bool IsEditAllowed(this HttpContext context)
        {
            if (context == null)
                return false;
            if (!Int32.TryParse(Convert.ToString(context.Items["EditPermission"]), out int permission))
                return false;
            return permission == 1 ? true : false;
        }

        public static FormPermissions GetFormPermissions(this HttpContext context)
        {
            FormPermissions formPermissions = new();
            formPermissions.View = context.IsViewAllowed();
            formPermissions.Add = context.IsAddAllowed();
            formPermissions.Edit = context.IsEditAllowed();
            return formPermissions;
        }
    }
}
