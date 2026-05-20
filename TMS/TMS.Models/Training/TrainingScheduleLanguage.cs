using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMS.Models.Masters;

namespace TMS.Models.Training
{
    [Table(nameof(TrainingScheduleLanguage))]
    public class TrainingScheduleLanguage
    {
        public int TraningScheduleId { get; set; }
        public int LanguageId { get; set; }

        [ForeignKey(nameof(TraningScheduleId))]
        public virtual TrainingSchedule? TrainingSchedule { get; set; }
        [ForeignKey(nameof(LanguageId))]
        public virtual LanguageMaster?  Language { get; set; }
    }
}
