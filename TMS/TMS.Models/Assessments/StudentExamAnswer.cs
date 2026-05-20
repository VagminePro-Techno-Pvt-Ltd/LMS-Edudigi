using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Assessments
{
    [Table("StudentExamAnswer")]
    public class StudentExamAnswer : BaseIdModel
    {
        [Required]
        public int AttemptId { get; set; }

        [ForeignKey(nameof(AttemptId))]
        public virtual StudentExamAttempt? Attempt { get; set; }

        [Required]
        public int QuestionId { get; set; }

        [ForeignKey(nameof(QuestionId))]
        public virtual QuestionBank? Question { get; set; }

        public int? SelectedOptionId { get; set; }

        [ForeignKey(nameof(SelectedOptionId))]
        public virtual QuestionOption? SelectedOption { get; set; }

        public string? SubjectiveAnswer { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? ObtainedMarks { get; set; }

        public bool? IsCorrect { get; set; }
    }
}
