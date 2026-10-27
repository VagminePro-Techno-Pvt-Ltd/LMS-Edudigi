using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Account;
using TMS.Models.Academics;
using TMS.Models.Masters;

namespace TMS.Models.Assessments
{
    [Table("QuestionPaper")]
    public class QuestionPaper : BaseModel
    {
        [Required]
        public int CourseId { get; set; }

        [ForeignKey(nameof(CourseId))]
        public virtual CourseMaster? Course { get; set; }

        public int? SemesterId { get; set; }

        [ForeignKey(nameof(SemesterId))]
        public virtual SemesterMaster? Semester { get; set; }

        public int? SubjectId { get; set; }

        [ForeignKey(nameof(SubjectId))]
        public virtual SubjectMaster? Subject { get; set; }

        [Required]
        [StringLength(250)]
        public string Title { get; set; } = default!;

        [Required]
        [StringLength(100)]
        public string AssessmentType { get; set; } = "Internal Assessment"; // Quiz, Internal Assessment, Mid Term, Final Exam, Practice

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalMarks { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Draft"; // Draft, Published

        [Required]
        public int FacultyId { get; set; }

        [ForeignKey(nameof(FacultyId))]
        public virtual UserMaster? Faculty { get; set; }

        public DateTime? PublishedOn { get; set; }

        [StringLength(1000)]
        public string? Instructions { get; set; }
 
        public int? DurationMinutes { get; set; } = 180;

        public virtual ICollection<QuestionPaperQuestion> Questions { get; set; } = new List<QuestionPaperQuestion>();
    }
}
