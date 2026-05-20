using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.ViewModels.Account
{
    public class RolePermissionsViewModel
    {
        public int RoleId { get; set; }
        public int FormId { get; set; }
        public string? FormName { get; set; }
        public bool View { get; set; }
        public bool Add { get; set; }
        public bool Edit { get; set; }
    }
}
