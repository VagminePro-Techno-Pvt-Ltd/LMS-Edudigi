using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Account;
using TMS.Models.Masters;

namespace TMS.Models.Assessments
{
    [Table("QuestionBank")]
    public class QuestionBank : BaseModel
    {
        [Required]
        public int CourseId { get; set; }

        [ForeignKey(nameof(CourseId))]
        public virtual CourseMaster? Course { get; set; }

        [Required]
        public int FacultyId { get; set; }

        [ForeignKey(nameof(FacultyId))]
        public virtual UserMaster? Faculty { get; set; }

        [Required]
        [StringLength(50)]
        public string QuestionType { get; set; } = default!; // MCQ, MSQ, Subjective

        [Required]
        [StringLength(50)]
        public string DifficultyLevel { get; set; } = default!; // Easy, Medium, Hard

        [Required]
        public string QuestionText { get; set; } = default!;

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Marks { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal NegativeMarks { get; set; } = 0;

        public string? Explanation { get; set; }

        public virtual ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
        public virtual ICollection<ExamQuestionMap> QuestionMaps { get; set; } = new List<ExamQuestionMap>();
    }
}
