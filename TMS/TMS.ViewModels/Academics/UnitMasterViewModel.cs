using TMS;
using System.ComponentModel.DataAnnotations;
using TMS.Common;
using TMS.ViewModels.Masters;

namespace TMS.ViewModels.Academics
{
    public class UnitMasterViewModel : BaseMasterViewModel
    {
        [MapToDTO, Required, Display(Name = "Subject")]
        public int SubjectId { get; set; }

        [MapToDTO, Display(Name = "Sort Order")]
        public int SortOrder { get; set; }

        public SubjectMasterViewModel? Subject { get; set; }

        public override string? NameStatus => $"{Name} ({IsActiveStr})";
    }
}
