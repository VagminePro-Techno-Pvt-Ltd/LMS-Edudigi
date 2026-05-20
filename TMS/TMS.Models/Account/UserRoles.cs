using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.Models.Account
{
    public class UserRoles
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
        [ForeignKey(nameof(UserId))]
        public virtual UserMaster? User { get; set; }
        [ForeignKey(nameof(RoleId))]
        public virtual RoleMaster? Role { get; set; }
    }
}
