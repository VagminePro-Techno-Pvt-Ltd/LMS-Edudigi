using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Masters
{
    public enum QuestionType
    {
        Objective = 1,
        Subjective = 2
    }

    [Table(nameof(QuizMaster))]
    public class QuizMaster : BaseModel
    {
        [Required]
        public int CourseId { get; set; }

        [ForeignKey(nameof(CourseId))]
        public virtual CourseMaster? Course { get; set; }

        [Required]
        public int CourseQuadrantId { get; set; }

        [ForeignKey(nameof(CourseQuadrantId))]
        public virtual CourseQuadrantMaster? CourseQuadrant { get; set; }

        [Required, StringLength(100)]
        public string Title { get; set; } = default!;
        [Required]
        public int? TotalMarks { get; set; }
        [Required]
        public int? PassingMarks { get; set; }

        public virtual List<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
    }


}
