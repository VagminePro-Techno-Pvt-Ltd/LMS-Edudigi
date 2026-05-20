using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.Models.Masters
{
    public class CourseEmploymentTypeMapping : BaseModel
    {
        public int CourseId { get; set; }
        public int EmploymentTypeId { get; set; }
        [ForeignKey(nameof(CourseId))]
        public CourseMaster? Course { get; set; }
        [ForeignKey(nameof(EmploymentTypeId))]
        public virtual EmploymentTypeMaster? EmploymentType { get; set; }
    }
}
