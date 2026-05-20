using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Assessments
{
    [Table("ExamSection")]
    public class ExamSection : BaseIdModel
    {
        [Required]
        public int ExamId { get; set; }

        [ForeignKey(nameof(ExamId))]
        public virtual ExamMaster? Exam { get; set; }

        [Required]
        [StringLength(100)]
        public string SectionName { get; set; } = default!;

        [Required]
        public int TotalQuestions { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalMarks { get; set; }

        [Required]
        public int DisplayOrder { get; set; } = 1;

        public virtual ICollection<ExamQuestionMap> QuestionMaps { get; set; } = new List<ExamQuestionMap>();
    }
}
