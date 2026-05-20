using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.Models.Training
{
    public class TrainingScheduleAttachment : BaseIdModel
    {
        public int TraningScheduleId { get; set; }
        [StringLength(100)]
        public string? FileName { get; set; }
        [StringLength(50)]
        public string? FilePath { get; set; }
        [ForeignKey(nameof(TraningScheduleId))]
        public virtual TrainingSchedule? TrainingSchedule { get; set; }
    }
}
