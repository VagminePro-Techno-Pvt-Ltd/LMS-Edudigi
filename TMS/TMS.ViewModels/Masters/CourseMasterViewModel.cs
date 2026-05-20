using TMS;
using System.ComponentModel.DataAnnotations;
using TMS.Common;
using TMS.Models.Academics;
using TMS.Models.Account;

namespace TMS.ViewModels.Masters
{
    public class CourseMasterViewModel : BaseMasterViewModel
    {
        [MapToDTO, MaxLength(20), Required,  Display(Name = "Course Code")]
        public string? CourseCode { get; set; }
        [MapToDTO, Required, Display(Name = "Program Name")]
        public int CourseCategoryId { get; set; }
        [MapToDTO, Required, Display(Name = "Course Duration (Months)")]
        public int? CourseDurationMonths { get; set; }
        [MapToDTO, Required, Display(Name = "Semester")]
        public int? SemesterId { get; set; }

        [Required]
        [MapToDTO]
        [Display(Name = "No.of Credit")]
        public int NoofCredit { get; set; }

        public virtual CourseCategoryMasterViewModel? CourseCategory { get; set; }

        public List<UserViewModel>? MappedFaculties { get; set; }
        public List<UserMaster>? EnrolledStudents { get;  set; } 
        public override string? NameStatus
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Name)) return null;
                return $"{CourseCode} - {Name} ({IsActiveStr})";
            }
        }
        public override string? NameStatusHtml
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Name)) return null;
                return $"{CourseCode} - {Name} ({IsActiveStrHtml})";
            }
        }
        public virtual SemesterMaster? Semester { get; set; }
    }
}
