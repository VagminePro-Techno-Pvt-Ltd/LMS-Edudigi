using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMS.Models.Training;

namespace TMS.Models
{
	public class NominationRequestRemarks:BaseModel
	{
		public int  NominationRequestId { get; set; }
		[ForeignKey(nameof(NominationRequestId))]
		public NominationRequest? NominationRequest { get; set; }
		public string? Remarks { get; set; }

		public int NominationRequestStatus {  get; set; }  
	}
}
