using System.ComponentModel.DataAnnotations;
using TMS.Common;

namespace TMS.ViewModels.Masters
{
    public class CourseEmploymentTypeMappingViewModel : BaseViewModel
    {
        [MapToDTO, Display(Name = "Course")]
        public int CourseId { get; set; }
        [MapToDTO, Display(Name = "Employment Type")]
        public int EmploymentTypeId { get; set; }
        public virtual EmploymentTypeMasterViewModel? EmploymentType { get; set; }
    }
}
