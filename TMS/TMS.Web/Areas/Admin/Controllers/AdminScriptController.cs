using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace TMS.Web.Areas.Admin.Controllers
{
    public class AdminScriptController : Controller
    {
        public ContentResult Role()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append($"var adminRoleUrl = '{Url.Action("Role", "Roles", new {area="Admin"})}';");
            return Content(stringBuilder.ToString(), "application/javascript");
        }
    }
}
