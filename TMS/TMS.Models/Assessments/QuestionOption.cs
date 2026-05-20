using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Assessments
{
    [Table("QuestionOption")]
    public class QuestionOption : BaseIdModel
    {
        [Required]
        public int QuestionId { get; set; }

        [ForeignKey(nameof(QuestionId))]
        public virtual QuestionBank? Question { get; set; }

        [Required]
        [StringLength(1000)]
        public string OptionText { get; set; } = default!;

        [Required]
        public bool IsCorrect { get; set; } = false;
    }
}
