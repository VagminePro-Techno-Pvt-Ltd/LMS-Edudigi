using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Masters
{


    [Table(nameof(QuizQuestion))]
    public class QuizQuestion : BaseModel
    {
        [Required]
        public int QuizId { get; set; }

        [ForeignKey(nameof(QuizId))]
        public virtual QuizMaster? Quiz { get; set; }

        [Required]
        public string QuestionText { get; set; } = default!;

        [Required]
        public QuestionType QuestionType { get; set; } = QuestionType.Objective;

        public virtual ICollection<QuizOption> Options { get; set; } = new List<QuizOption>();

        // store which option is correct (for Objective)
        public int? CorrectOptionId { get; set; }

        [ForeignKey(nameof(CorrectOptionId))]
        public virtual QuizOption? CorrectOption { get; set; }
        
        [Required]
        public int? MaxMarks { get; set; }
    }



}
