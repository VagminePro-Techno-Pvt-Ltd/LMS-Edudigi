using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.Models
{
    public class BaseMasterModel : BaseModel
    {
        [Required]
        [StringLength(100)]
        [Column(Order = 1)]
        public virtual string Name { get; set; } = default!;

        [StringLength(200)]
        [Column(Order = 2)]
        public virtual string? Description { get; set; }

    }
}
