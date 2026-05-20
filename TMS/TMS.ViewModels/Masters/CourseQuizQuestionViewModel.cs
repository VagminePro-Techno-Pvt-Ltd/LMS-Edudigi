using TMS;
using System.ComponentModel.DataAnnotations;
using TMS.Common;

namespace TMS.ViewModels.Masters
{
    public class CourseQuizQuestionViewModel : BaseViewModel
    {
        [MapToDTO, Required, Display(Name = "Quiz")]
        public int QuizId { get; set; }

        [MapToDTO, Required, Display(Name = "Question Text"), MaxLength(500)]
        public string? QuestionText { get; set; }

        [MapToDTO, Display(Name = "Option A"), MaxLength(200)]
        public string? OptionA { get; set; }

        [MapToDTO, Display(Name = "Option B"), MaxLength(200)]
        public string? OptionB { get; set; }

        [MapToDTO, Display(Name = "Option C"), MaxLength(200)]
        public string? OptionC { get; set; }

        [MapToDTO, Display(Name = "Option D"), MaxLength(200)]
        public string? OptionD { get; set; }

        [MapToDTO, Required, Display(Name = "Correct Option")]
        public string? CorrectOption { get; set; }

        [MapToDTO, Display(Name = "Marks")]
        public int Marks { get; set; }
    }
}
