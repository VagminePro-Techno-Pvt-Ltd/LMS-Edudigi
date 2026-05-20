using System.ComponentModel.DataAnnotations;

namespace TMS.ViewModels.Masters
{
    public class TrainingScheduleStatusMasterViewModel : BaseMasterViewModel
    {
        [MaxLength(30)]
        public string? TextColorCode { get; set; }
        [MaxLength(30)]
        public string? BackgroundColorCode { get; set; }
        public override string? NameStatusHtml
        {
            get
            {
                return $"<span class=\"rounded p-1\" style=\"{(string.IsNullOrWhiteSpace(TextColorCode) ? $"color:{TextColorCode};" : "")}{(string.IsNullOrWhiteSpace(BackgroundColorCode) ? $"background-color:{BackgroundColorCode};" : "")}\">{Name}</span>";
            }
        }
    }
}
