using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Common;

namespace TMS.ViewModels.Masters
{
    public class DepartmentMasterViewModel : BaseMasterViewModel
    {
        [Required, MapToDTO, Display(Name = "Location")]
        public int LocationId { get; set; }
        [MapToDTO]
    
        [RegularExpression(@"^DEP-\d{1,5}$")]
        public string? DepartmentCode { get; set; }

        [NotMapped]
        public override string? NameStatus
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Name)) return null;
                return $"{DepartmentCode} ({Name})";
            }
        }
        public virtual LocationMasterViewModel? Location { get; set; }
        [NotMapped]
        public string? NameWithParent
        {
            get
            {
                if (Location == null) return $"{Name} ({IsActiveStr})";
                return $"{Location.Name} ({Location.IsActiveStr})>>{Name} ({IsActiveStr})";
            }
        }
        [NotMapped]
        public string? NameWithParentHtml
        {
            get
            {
                if (Location == null) return $"{Name} ({IsActiveStr})";
                return $"{Location.Name} ({Location.IsActiveStrHtml}) >> {Name} ({IsActiveStrHtml})";
            }
        }
    }
}
