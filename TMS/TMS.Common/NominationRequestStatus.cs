using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.Common
{
	public enum NominationRequestStatus : int
	{
		Requested = 0,
		Approved = 1,
		Rejected =2,
        WithdrawRequested = 3,
        WithdrawApproved = 4,
        WithdrawRejected = 5
    }
	
}
