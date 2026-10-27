using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Account;
using TMS.Models.Academics;
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

        public int? SemesterId { get; set; }

        [ForeignKey(nameof(SemesterId))]
        public virtual SemesterMaster? Semester { get; set; }

        public int? SubjectId { get; set; }

        [ForeignKey(nameof(SubjectId))]
        public virtual SubjectMaster? Subject { get; set; }

        public int? UnitId { get; set; }

        [ForeignKey(nameof(UnitId))]
        public virtual UnitMaster? Unit { get; set; }

        public int? TopicId { get; set; }

        [ForeignKey(nameof(TopicId))]
        public virtual TopicMaster? Topic { get; set; }

        [StringLength(250)]
        public string? LearningOutcome { get; set; }

        public string? NormalizedText { get; set; }

        [Required]
        [StringLength(50)]
        public string QuestionType { get; set; } = default!; // MCQ, Short Answer, Long Answer, Subjective

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

        public string? CorrectAnswer { get; set; }

        public string? Explanation { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Active";

        public virtual ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
        public virtual ICollection<ExamQuestionMap> QuestionMaps { get; set; } = new List<ExamQuestionMap>();
    }
}
