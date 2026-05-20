using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Assessments
{
    [Table("ExamResult")]
    public class ExamResult : BaseIdModel
    {
        [Required]
        public int AttemptId { get; set; }

        [ForeignKey(nameof(AttemptId))]
        public virtual StudentExamAttempt? Attempt { get; set; }

        [Required]
        public int TotalQuestions { get; set; }

        [Required]
        public int AttemptedQuestions { get; set; }

        [Required]
        public int CorrectAnswers { get; set; }

        [Required]
        public int WrongAnswers { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal ObtainedMarks { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Percentage { get; set; }

        [Required]
        [StringLength(10)]
        public string Grade { get; set; } = "F";

        [Required]
        [StringLength(50)]
        public string ResultStatus { get; set; } = "Fail"; // Pass, Fail

        [Required]
        public DateTime PublishedOn { get; set; } = DateTime.UtcNow;
    }
}
