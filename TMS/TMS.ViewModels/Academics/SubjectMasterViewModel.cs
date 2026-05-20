using TMS;
using System.ComponentModel.DataAnnotations;
using TMS.Common;
using TMS.ViewModels.Masters;

namespace TMS.ViewModels.Academics
{
    public class SubjectMasterViewModel : BaseMasterViewModel
    {
        [MapToDTO, Required, Display(Name = "Semester")]
        public int SemesterId { get; set; }

        [MapToDTO, Display(Name = "Subject Code"), MaxLength(20)]
        public string? SubjectCode { get; set; }

        [MapToDTO, Display(Name = "Sort Order")]
        public int SortOrder { get; set; }

        public SemesterMasterViewModel? Semester { get; set; }

        public override string? NameStatus => $"{SubjectCode} - {Name} ({IsActiveStr})";
    }
}
