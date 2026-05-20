using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.Models.Account
{
    [Table(nameof(FormControlMaster))]
    public class FormControlMaster
    {
        public int FormId { get; set; }
        public int ControlId { get; set; }
        [Required]
        [StringLength(100)]
        public string? ControlName { get; set; }
        public bool IsVisible { get; set; }
        [ForeignKey(nameof(FormId))]
        public virtual FormMaster? Form { get; set; }
    }
}
