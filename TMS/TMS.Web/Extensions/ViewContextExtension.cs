using TMS.ViewModels.Account;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TMS.Web
{
    public static class ViewContextExtension
    {
        public static FormControlListViewModel GetControlPermissions(this ViewContext viewContext)
        {
            FormControlListViewModel model = new();
            if (viewContext.ViewBag.ControlPermissions != null)
                model = (FormControlListViewModel)viewContext.ViewBag.ControlPermissions;
            return model;
        }
    }
}
