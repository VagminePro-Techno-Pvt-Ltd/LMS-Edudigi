using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMS.Common;
using TMS.Models.Account;
using TMS.Models.Masters;
using TMS.Models.Training;

namespace TMS.Models
{
    public class NominationRequest : BaseModel
    {
        public int TrainingId { get; set; }
        public int CandidateId { get; set; }

        [ForeignKey(nameof(TrainingId))]
        public virtual TrainingSchedule? Training { get; set; }
        [ForeignKey(nameof(CandidateId))]
        public virtual UserMaster? Candidate { get; set; }

        public NominationRequestStatus Status { get; set; }
        public virtual List<NominationRequestRemarks>? NominationRemarks { get; set; }
    }
}
