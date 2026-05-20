using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Common;

namespace TMS.Models.Masters
{
    [Table(nameof(CourseCategoryMaster))]
    public class CourseCategoryMaster : BaseMasterModel
    {
        
        public int? NoOfSemester { get; set; }

        public virtual List<CourseMaster>? Courses { get; set; }
    }
}
