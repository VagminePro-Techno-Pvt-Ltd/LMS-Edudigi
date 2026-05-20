using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.ViewModels.Account
{
    public class FormControlListViewModel
    {
        public List<FormControlViewModel> FormControls { get; set; } = new();

        public bool IsVisible(int controlId) 
        {
            if (FormControls == null || FormControls.Count == 0)
                return false;
            var control = FormControls.Where(t => t.ControlId == controlId).FirstOrDefault();
            if (control != null && control.IsVisible)
                return true;
            return false;
        }
        public string GetVisibleCssClass(int controlId)
        {
            if (FormControls == null || FormControls.Count == 0)
                return string.Empty;
            var control = FormControls.Where(t => t.ControlId == controlId).FirstOrDefault();
            if (control != null && control.IsVisible)
                return "d-none";
            return string.Empty;
        }

        public string GetPermissionScript()
        {
            if (FormControls == null || FormControls.Count == 0)
                return string.Empty;
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var item in FormControls)
            {
                stringBuilder.Append($"var visibleControl{item.ControlId}={item.IsVisible.ToString().ToLower()};");
            }
            return stringBuilder.ToString().Encrypt();
        }
    }
}
