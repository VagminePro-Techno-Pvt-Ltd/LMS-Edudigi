using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Masters
{
    [Table(nameof(DesignationMaster))]
    public class DesignationMaster : BaseMasterModel
    {
        public int DivisionId { get; set; }
        [ForeignKey(nameof(DivisionId))]
        public virtual DivisionMaster? Division { get; set; }
          [Required, StringLength(20)]
        public string? DesignationCode { get; set; }
    }
}
