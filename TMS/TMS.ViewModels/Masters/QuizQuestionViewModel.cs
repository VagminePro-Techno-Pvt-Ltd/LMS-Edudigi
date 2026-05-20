using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TMS.Common;
using TMS.Models.Masters;

namespace TMS.ViewModels.Masters
{
    public class QuizQuestionViewModel : BaseViewModel
    {
        [MapToDTO, Required]
        public int QuizId { get; set; }

        [MapToDTO, Required]
        [Display(Name = "Question Text")]
        public string QuestionText { get; set; } = default!;

        [MapToDTO, Required]
        [Display(Name = "Question Type")]
        public QuestionType QuestionType { get; set; } = QuestionType.Objective;

        // For objective questions
        public virtual List<QuizOptionViewModel>? Options { get; set; }

        [Display(Name = "Correct Option")]
        public int? CorrectOptionId { get; set; }
        [Required]
        [Display(Name = "Max Score")]
        public int? MaxMarks { get; set; }
        
    }
}
