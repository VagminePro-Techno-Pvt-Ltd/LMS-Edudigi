using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMS.Common;

namespace TMS.ViewModels.Training
{
    public class TrainingScheduleApprovalHistoryViewModel : BaseViewModel
    {
        [MapToDTO]
        public int TrainingScheduleId { get; set; }
        [MapToDTO, Display(Name = "Status")]
        public int StatusId { get; set; }
        [MapToDTO, MaxLength(300), Display(Name = "Remarks")]
        public string? Remarks { get; set; }
        [MapToDTO]
        public DateTime? OldFromDateTime { get; set; }
        [MapToDTO]
        public DateTime? OldToDateTime { get; set; }
        [MapToDTO]
        public DateTime? NewFromDateTime { get; set; }
        [MapToDTO]
        public DateTime? NewToDateTime { get; set; }
        public TrainingScheduleViewModel? TrainingSchedule { get; set; }

    }
}
