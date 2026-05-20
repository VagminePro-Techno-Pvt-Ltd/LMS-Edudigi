using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Common;
using TMS.Models.Masters;

namespace TMS.Models.Academics
{
    [Table(nameof(StudentQuizAttempt))]
    public class StudentQuizAttempt : BaseModel
    {
        [Required]
        public int StudentId { get; set; }

        [Required]
        public int QuizId { get; set; }

        [ForeignKey(nameof(QuizId))]
        public virtual QuizMaster Quiz { get; set; } = default!;

        [Required]
        public DateTime AttemptDate { get; set; } = DateTime.UtcNow;

        public bool IsSubmitted { get; set; } = false;

        public int? Score { get; set; }
       
        public int? CourseId { get; set; }
        public int? CourseQuadrantId { get; set; }
        // ✅ Navigation - store per-question answers
        public virtual ICollection<StudentQuizAnswer> Answers { get; set; } = new List<StudentQuizAnswer>();
    }

    
}
