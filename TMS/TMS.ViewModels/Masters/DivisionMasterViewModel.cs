using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Common;

namespace TMS.ViewModels.Masters
{
    public class DivisionMasterViewModel : BaseMasterViewModel
    {
        [MapToDTO, Display(Name = "Department")]
        public int DepartmentId { get; set; }
        public virtual DepartmentMasterViewModel? Department { get; set; }
        [MapToDTO]
        
        [RegularExpression(@"^DIV-\d{1,5}$")]
        public string? DivisionCode { get; set; }
        [MapToDTO]
        public string? DepartmentCode { get; set; }

        [NotMapped]
        public override string? NameStatus
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Name)) return null;
                return $"{DivisionCode} ({Name})";
            }
        }
    }
}
