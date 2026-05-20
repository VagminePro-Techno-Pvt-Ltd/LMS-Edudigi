using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.Models.Masters
{
    [Table(nameof(DivisionMaster))]
    public class DivisionMaster : BaseMasterModel
    {
        public int DepartmentId { get; set; }
        [ForeignKey(nameof(DepartmentId))]
        public virtual DepartmentMaster? Department { get; set; }

        [Required, StringLength(20)]
        public string? DivisionCode { get; set; }
    }
}
