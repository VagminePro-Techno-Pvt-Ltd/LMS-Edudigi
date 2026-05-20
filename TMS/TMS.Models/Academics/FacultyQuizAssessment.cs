using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Account;
using TMS.Models.Masters;

namespace TMS.Models.Academics
{
    public class FacultyQuizAssessment:BaseModel
    {
        public int AttemptId { get; set; }       // StudentQuizAttempt Id
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public int CourseId { get; set; }
        public string CourseName { get; set; }
        public int QuadrantId { get; set; }
        public string QuadrantName { get; set; }
        public string QuizName { get; set; }
        public DateTime AttemptedOn { get; set; }
        public decimal? TotalScore { get; set; }
        public int QuizId { get; set; }       // StudentQuizAttempt Id
       


        public List<FacultyQuizQuestionAssessment> Questions { get; set; } = new();
    }

   
}

