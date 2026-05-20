using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Common;
using TMS.ViewModels.Masters;

namespace TMS.ViewModels.Academics
{
    public class StudentQuizAnswerViewModel:BaseViewModel
    {
        [Required]
        [MapToDTO]
        public int QuizQuestionId { get; set; }
        [Required]
        [MapToDTO]
        public int StudentQuizAttemptId { get; set; } // ✅ FIXED
        [Display(Name = "Question")]
        [MapToDTO]
        public string QuestionText { get; set; } = string.Empty;

        [Display(Name = "Selected Option")]
        [MapToDTO]
        public int? SelectedOptionId { get; set; }
        [MapToDTO]
        public string? WrittenAnswer { get; set; }
        [NotMapped]
        public string? QuestionType { get; set; }
        // ✅ Options for this question
        public List<QuizOptionViewModel> Options { get; set; } = new();
    }
}
