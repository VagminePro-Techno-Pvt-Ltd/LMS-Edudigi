using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.Models.Masters
{
    [Table(nameof(DepartmentMaster))]
    public class DepartmentMaster : BaseMasterModel
    {
        public int LocationId { get; set; }
        [ForeignKey(nameof(LocationId))]
        public virtual LocationMaster? Location { get; set; }
        [Required, StringLength(20)]
        public string? DepartmentCode { get; set; }
    }
}
