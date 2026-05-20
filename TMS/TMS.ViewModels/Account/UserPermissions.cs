using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.ViewModels.Account
{
    [Serializable]
    public class UserPermissions
    {
        public List<MenuViewModel> Menus { get; set; }=new();
        public List<FormViewModel> Forms { get; set; } =new();

    }
}
