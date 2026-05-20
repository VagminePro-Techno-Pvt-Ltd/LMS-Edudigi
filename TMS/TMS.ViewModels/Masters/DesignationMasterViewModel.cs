using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Common;

namespace TMS.ViewModels.Masters
{
    public class DesignationMasterViewModel : BaseMasterViewModel
    {
        [MapToDTO, Display(Name = "Division")]
        public int DivisionId { get; set; }
        public virtual DivisionMasterViewModel? Division { get; set; }
        [MapToDTO]
        [RegularExpression(@"^DES-\d{1,5}$")]
        public string? DesignationCode { get; set; }

        [NotMapped]
        public override string? NameStatus
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Name)) return null;
                return $"{DesignationCode} ({Name})";
            }
        }
    }
}
