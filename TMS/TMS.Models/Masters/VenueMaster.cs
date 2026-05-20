using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Masters
{
    [Table(nameof(VenueMaster))]
    public class VenueMaster : BaseMasterModel
    {
        [StringLength(20)]
        public string? VenueId { get; set; }
     
        public int? VenueSeat { get; set; }
    }
}
