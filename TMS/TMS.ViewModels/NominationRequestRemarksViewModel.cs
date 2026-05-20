using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Http;



using System.Reflection.Metadata.Ecma335;

using TMS.Common;

using UoN.ExpressiveAnnotations.NetCore.Attributes;
using TMS.ViewModels.Training;

namespace TMS.ViewModels.Masters

{

	public class NominationRequestRemarksViewModel : BaseViewModel

	{

		public int NominationRequestId { get; set; }

		[MapToDTO]
		public string? Remarks { get; set; }
		[MapToDTO]
		public int NominationRequestStatus { get; set; }
		public NominationRequestViewModel? NominationRequest { get; set; }

	}

}
