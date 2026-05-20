using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Assessments
{
    [Table("ExamQuestionMap")]
    public class ExamQuestionMap : BaseIdModel
    {
        [Required]
        public int ExamId { get; set; }

        [ForeignKey(nameof(ExamId))]
        public virtual ExamMaster? Exam { get; set; }

        [Required]
        public int SectionId { get; set; }

        [ForeignKey(nameof(SectionId))]
        public virtual ExamSection? Section { get; set; }

        [Required]
        public int QuestionId { get; set; }

        [ForeignKey(nameof(QuestionId))]
        public virtual QuestionBank? Question { get; set; }

        [Required]
        public int DisplayOrder { get; set; } = 1;
    }
}
