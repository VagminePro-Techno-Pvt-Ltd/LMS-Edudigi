using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.Models.Account
{
    public class RolePermissions
    {
        public int RoleId { get; set; }
        public int FormId { get; set; }
        public bool View { get; set; }
        public bool Add { get; set; }
        public bool Edit { get; set; }

        [ForeignKey("RoleId")]
        public virtual RoleMaster? Role { get; set; }
        [ForeignKey("FormId")]
        public virtual FormMaster? Form { get; set; }
    }
}
