using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Academics
{
    /// <summary>
    /// Records a student's answer for a LectureQuizQuestion.
    /// Unique constraint on (StudentId, LectureQuizQuestionId) prevents duplicate attempts.
    /// IsCorrect is calculated server-side.
    /// </summary>
    [Table(nameof(LectureQuizAttempt))]
    public class LectureQuizAttempt : BaseModel
    {
        [Required]
        public int StudentId { get; set; }

        [Required]
        public int LectureQuizQuestionId { get; set; }

        [ForeignKey(nameof(LectureQuizQuestionId))]
        public virtual LectureQuizQuestion? Question { get; set; }

        /// <summary>The option the student selected: "A","B","C","D".</summary>
        [Required, StringLength(1)]
        public string SelectedOption { get; set; } = default!;

        /// <summary>Evaluated server-side by comparing SelectedOption to CorrectOption.</summary>
        public bool IsCorrect { get; set; } = false;
    }
}
