using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Masters
{
    [Table(nameof(LocationMaster))]
    public class LocationMaster : BaseMasterModel
    {
        [Required,StringLength(20)]
        public string? LocationCode { get; set; }
    }
}
