using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.ViewModels.Account
{
    public class FormControlViewModel
    {
        public int ControlId { get; set; }
        public int FormId { get; set; }
        public bool IsVisible { get; set; }
        [NotMapped]
        public string? ControlName { get; set; }
    }
}
