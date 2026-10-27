using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Account;
using TMS.Models.Masters;

namespace TMS.Models.Assessments
{
    [Table("ExamMaster")]
    public class ExamMaster : BaseModel
    {
        [Required]
        [StringLength(250)]
        public string Title { get; set; } = default!;

        public string? Description { get; set; }

        [Required]
        public int CourseId { get; set; }

        [ForeignKey(nameof(CourseId))]
        public virtual CourseMaster? Course { get; set; }

        [Required]
        public int FacultyId { get; set; }

        [ForeignKey(nameof(FacultyId))]
        public virtual UserMaster? Faculty { get; set; }

        [Required]
        public int DurationMinutes { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalMarks { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal PassingMarks { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public int AllowedAttempts { get; set; } = 1;

        [Required]
        public bool ShuffleQuestions { get; set; } = false;

        [Required]
        public bool ShowResultImmediately { get; set; } = true;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Draft"; // Draft, Active, Archived

        // ─── Phase 3 Proctoring Configuration (Default OFF) ───
        public bool IsProctored { get; set; } = false;
        public bool RequireCamera { get; set; } = false;
        public bool TrackTabSwitches { get; set; } = true;
        public bool RequireFullScreen { get; set; } = false;
        public int MaxAllowedViolations { get; set; } = 5;
        public int RetentionDays { get; set; } = 90;
        public bool EnableAiFaceCheck { get; set; } = false;

        public virtual ICollection<ExamSection> Sections { get; set; } = new List<ExamSection>();
        public virtual ICollection<ExamQuestionMap> QuestionMaps { get; set; } = new List<ExamQuestionMap>();
        public virtual ICollection<ExamAssignment> Assignments { get; set; } = new List<ExamAssignment>();
        public virtual ICollection<StudentExamAttempt> Attempts { get; set; } = new List<StudentExamAttempt>();
    }
}
