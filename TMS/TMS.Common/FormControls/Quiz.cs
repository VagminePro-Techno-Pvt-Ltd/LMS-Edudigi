using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.Common.FormControls
{
    public enum Quiz : int
    {
        [Display(Name = "Add Question")]
        AddQuestion = 1
    }
}
