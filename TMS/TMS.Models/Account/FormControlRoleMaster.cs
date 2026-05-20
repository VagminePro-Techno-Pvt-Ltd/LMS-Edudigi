using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.Models.Account
{
    [Table(nameof(FormControlRoleMaster))]
    public class FormControlRoleMaster : BaseModel
    {
        public int RoleId { get; set; }
        public int FormId { get; set; }
        public int ControlId { get; set; }
        public bool IsVisible { get; set; }


        [ForeignKey(nameof(RoleId))]
        public virtual RoleMaster? Role { get; set; } 
        [ForeignKey(nameof(FormId))]
        public virtual FormMaster? Form { get; set; }


        //Removed
        [NotMapped]
        public override bool IsActive { get; set; }
    }
}
