using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.Models.Masters
{
    public class TrainingScheduleStatusMaster : BaseMasterModel
    {
        [StringLength(30)]
        public string? TextColorCode { get; set; }
        [StringLength(30)]
        public string? BackgroundColorCode { get; set; }
    }
}
