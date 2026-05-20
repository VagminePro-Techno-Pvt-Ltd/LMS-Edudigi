using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TMS.Common;

namespace TMS.ViewModels.Masters
{
    public class CourseCategoryMasterViewModel : BaseMasterViewModel
    {
        [Required]
        [MaxLength(100)]
        [MapToDTO]
        [Display(Name = "Program Name")]
        public override string? Name { get; set; }
        [Required]
        [Display(Name= "No Of Semester")]
        [MapToDTO]
        public int? NoOfSemester { get; set; }
    }
}
