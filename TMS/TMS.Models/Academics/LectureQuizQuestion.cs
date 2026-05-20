using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Masters;

namespace TMS.Models.Academics
{
    /// <summary>
    /// MCQ question tied to a LectureMaterial video.
    /// ShowAtSeconds: the video timestamp at which the question pops up.
    /// CorrectOption stores "A","B","C","D" — NEVER exposed to the student.
    /// </summary>
    [Table(nameof(LectureQuizQuestion))]
    public class LectureQuizQuestion : BaseModel
    {
        /// <summary>The LectureMaterial (video) this question belongs to.</summary>
        [Required]
        public int LectureMaterialId { get; set; }

        [ForeignKey(nameof(LectureMaterialId))]
        public virtual LectureMaterial? LectureMaterial { get; set; }

        [Required]
        public string QuestionText { get; set; } = default!;

        [Required, StringLength(500)]
        public string OptionA { get; set; } = default!;

        [StringLength(500)]
        public string? OptionB { get; set; }

        [StringLength(500)]
        public string? OptionC { get; set; }

        [StringLength(500)]
        public string? OptionD { get; set; }

        /// <summary>Correct answer letter: "A","B","C","D". Never sent to frontend.</summary>
        [Required, StringLength(1)]
        public string CorrectOption { get; set; } = default!;

        /// <summary>Video playback position (seconds) at which this question appears.</summary>
        [Required]
        public int ShowAtSeconds { get; set; }

        /// <summary>Display order when multiple questions share the same timestamp.</summary>
        public int SortOrder { get; set; } = 0;
    }
}
