using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using TMS.Common;

namespace TMS.ViewModels.Masters
{
    public class QuizOptionViewModel : BaseViewModel
    {
        [MapToDTO, Required]
        public int QuizQuestionId { get; set; }

        [MapToDTO, Required, Display(Name = "Option Text")]
        public string OptionText { get; set; } = default!;
        [MapToDTO,  Display(Name = "Correct Option"),DefaultValue(false)]
        public bool? IsCorrectOption { get; set; }
    }
}
