using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Security.Claims;
using TMS.Web.Models;

namespace TMS.Web.Controllers
{
    public class BaseController : Controller
    {
        public BaseController()
        {

        }
        public int GetUserId()
        {
            var userId = User?.Claims?.Where(t => t.Type == "userId")?.Select(t => t.Value)?.FirstOrDefault();
            return string.IsNullOrWhiteSpace(userId) ? 0 : Convert.ToInt32(userId);
        }
        public string GetUserRole()
        {
           return User.FindFirstValue("role");
        }
        
        public void SetApplicationResult(bool status, string message)
        {
            TempData.Put<AppResultViewModel>("AppResult", new AppResultViewModel()
            {
                Status = status,
                Message = message
            });
        }
        public AppResultViewModel GetResultModelSuccess(string? message = null)
        {
            return new AppResultViewModel()
            {
                Status = true,
                Message = message
            };
        }
        public AppResultViewModel GetResultModelFail(string? message)
        {
            return new AppResultViewModel()
            {
                Status = false,
                Message = message
            };
        }

        public AppResultViewModel GetFirstModelError(IReadOnlyDictionary<string, ModelStateEntry?> ModelState)
        {
            string message = string.Empty;
            foreach (var item in ModelState)
            {
                if (item.Value != null && item.Value.ValidationState == ModelValidationState.Invalid)
                {
                    message = $"Field {item.Key} is not valid!";
                    break;
                }
            }
            return GetResultModelFail(message);
        }
    }
}
