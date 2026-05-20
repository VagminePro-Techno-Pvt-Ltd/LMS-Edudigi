using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Academics
{
    /// <summary>
    /// Records a student's answer attempt for a VideoQuizQuestion.
    /// Unique constraint on (StudentId, VideoQuizQuestionId) prevents duplicates.
    /// IsCorrect is calculated server-side — never trusted from frontend.
    /// </summary>
    [Table(nameof(VideoQuizAttempt))]
    public class VideoQuizAttempt : BaseModel
    {
        [Required]
        public int StudentId { get; set; }

        [Required]
        public int VideoQuizQuestionId { get; set; }

        [ForeignKey(nameof(VideoQuizQuestionId))]
        public virtual VideoQuizQuestion? Question { get; set; }

        /// <summary>The option the student selected: "A", "B", "C", or "D".</summary>
        [Required, StringLength(1)]
        public string SelectedOption { get; set; } = default!;

        /// <summary>Evaluated server-side by comparing SelectedOption to CorrectOption.</summary>
        public bool IsCorrect { get; set; } = false;

        public DateTime AttemptedOn { get; set; } = DateTime.UtcNow;
    }
}
