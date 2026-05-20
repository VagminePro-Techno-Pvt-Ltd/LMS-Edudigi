using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Common;
using TMS.Models.Masters;

namespace TMS.Models.Academics
{
    public class StudentQuizAnswer : BaseModel
    {
        [Required]
        public int StudentQuizAttemptId { get; set; }  // ✅ FIXED

        [Required]
        public int QuizQuestionId { get; set; }

        public int? SelectedOptionId { get; set; }
        public string? WrittenAnswer { get; set; }

        public bool IsCorrect { get; set; }

        [ForeignKey(nameof(StudentQuizAttemptId))]
        public virtual StudentQuizAttempt StudentQuizAttempt { get; set; } = null!; // ✅ FIXED NAVIGATION

        [ForeignKey(nameof(QuizQuestionId))]
        public virtual QuizQuestion QuizQuestion { get; set; } = null!;

        [ForeignKey(nameof(SelectedOptionId))]
        public virtual QuizOption? SelectedOption { get; set; }
    }
}
