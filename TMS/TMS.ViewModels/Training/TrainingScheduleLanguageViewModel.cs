using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using TMS.Common;
using TMS.ViewModels.Masters;

namespace TMS.ViewModels.Training
{
    public class TrainingScheduleLanguageViewModel
    {
        [MapToDTO]
        public int TraningScheduleId { get; set; }
        [MapToDTO]
        public int LanguageId { get; set; }
     
        public virtual LanguageMasterViewModel? Language { get; set; }
      
		public virtual TrainingScheduleViewModel? TrainingSchedule { get; set; }



		[NotMapped]
        public bool IsSelected { get; set; }
    }
}
