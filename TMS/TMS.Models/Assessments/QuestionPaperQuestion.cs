using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Assessments
{
    [Table("QuestionPaperQuestion")]
    public class QuestionPaperQuestion : BaseIdModel
    {
        [Required]
        public int QuestionPaperId { get; set; }

        [ForeignKey(nameof(QuestionPaperId))]
        public virtual QuestionPaper? QuestionPaper { get; set; }

        [Required]
        public int QuestionBankId { get; set; }

        [ForeignKey(nameof(QuestionBankId))]
        public virtual QuestionBank? QuestionBank { get; set; }

        [StringLength(50)]
        public string SectionName { get; set; } = "Section A"; // Section A, Section B, Section C
 
        [StringLength(500)]
        public string? SectionInstructions { get; set; }

        [Required]
        public int DisplayOrder { get; set; } = 1;

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Marks { get; set; } = 1;

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }
}
