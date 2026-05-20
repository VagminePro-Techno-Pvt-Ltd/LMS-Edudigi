using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Account;
using TMS.Models.Masters;

namespace TMS.Models.Academics
{
    public class StudentQuizResponse:BaseIdModel
    {
      

        [Required]
        public int StudentId { get; set; }

        [Required]
        public int QuizId { get; set; }

        public DateTime AttemptedOn { get; set; } = DateTime.UtcNow;

        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public double ScorePercent { get; set; }

        [ForeignKey(nameof(StudentId))]
        public virtual UserMaster Student { get; set; } = null!;

        [ForeignKey(nameof(QuizId))]
        public virtual QuizMaster Quiz { get; set; } = null!;

        // ✅ Navigation to detailed question-level responses
        public virtual List<StudentQuizAnswer> Answers { get; set; } = new();
    }
}
