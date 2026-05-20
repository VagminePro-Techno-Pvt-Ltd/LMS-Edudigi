using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.Models.Training
{
    public class TrainingScheduleApprovalHistory : BaseModel
    {
        public int TrainingScheduleId { get; set; }
        public int StatusId { get; set; }
        [StringLength(300)]
        public string? Remarks { get; set; }

        [DataType(DataType.Date), Column(TypeName = "Date")]
        public DateTime? OldFromDate { get; set; }
        [DataType(DataType.Date), Column(TypeName = "Date")]
        public DateTime? OldToDate { get; set; }
        [DataType(DataType.Time)]
        public TimeSpan? OldFromTime { get; set; }
        [DataType(DataType.Time)]
        public TimeSpan? OldToTime { get; set; }


        [DataType(DataType.Date), Column(TypeName = "Date")]
        public DateTime? NewFromDate { get; set; }
        [DataType(DataType.Date), Column(TypeName = "Date")]
        public DateTime? NewToDate { get; set; }
        [DataType(DataType.Time)]
        public TimeSpan? NewFromTime { get; set; }
        [DataType(DataType.Time)]
        public TimeSpan? NewToTime { get; set; }


        [ForeignKey(nameof(TrainingScheduleId))]
        public TrainingSchedule? TrainingSchedule { get; set; }



        #region Removed Extra Properties
        [NotMapped]
        public override bool IsActive { get; set; }
        [NotMapped]
        public override int? UpdatedBy { get; set; }
        [NotMapped]
        public override DateTime? UpdatedOn { get; set; }
        #endregion
    }
}
