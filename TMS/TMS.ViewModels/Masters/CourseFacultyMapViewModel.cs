using TMS;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TMS.Common;

namespace TMS.ViewModels.Masters
{
    public class CourseFacultyMapViewModel : BaseViewModel
    {
        [MapToDTO, Required(ErrorMessage = "Course is required")]
        [Display(Name = "Course")]
        public int CourseId { get; set; }

        [MapToDTO]
        [Display(Name = "Faculty")]
        public int FacultyId { get; set; } // for legacy single mapping support

        // for multi-select
        public List<int>? FacultyIds { get; set; }

        [MapToDTO]
        public bool IsActive { get; set; } = true;

        public CourseMasterViewModel? Course { get; set; }
        public UserViewModel? Faculty { get; set; }
       
    }
}
