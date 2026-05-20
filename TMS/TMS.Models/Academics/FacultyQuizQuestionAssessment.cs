using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Account;
using TMS.Models.Masters;

namespace TMS.Models.Academics
{
    public class FacultyQuizQuestionAssessment:BaseModel
    {
        [ForeignKey(nameof(FacultyQuizAssessment))]
        public int FacultyQuizAssessmentId { get; set; }
        public int QuestionId { get; set; }
        public string QuestionText { get; set; }
        public decimal MaxScore { get; set; }      // editable by faculty
        public decimal StudentScore { get; set; }  // auto-calculated based on MaxScore
        public virtual FacultyQuizAssessment FacultyQuizAssessment { get; set; }
    }
}
