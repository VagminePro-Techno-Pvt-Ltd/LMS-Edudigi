using TMS;
using TMS.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.ViewModels.Masters
{
    public class VenueMasterViewModel : BaseMasterViewModel
    {
        [MapToDTO, Display(Name = "Venue Code"), Required, MaxLength(20), AlphaNumericWithoutSpace]
        public string? VenueId { get; set; }

        [MapToDTO,Display(Name ="Seats"),Required]
         
        public int? VenueSeat {  get; set; }
    }
}
