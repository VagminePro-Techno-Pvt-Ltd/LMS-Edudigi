using System;
using System.Collections.Generic;
using TMS.Models.Assessments;
using TMS.ViewModels.Academics;
using TMS.ViewModels.Masters;

namespace TMS.Web.Models
{
    public class StudentAssessmentHubViewModel
    {
        public IEnumerable<ExamMaster> Exams { get; set; } = new List<ExamMaster>();
        public List<QuizMasterViewModel> Quizzes { get; set; } = new();
        public List<StudentQuizAttemptViewModel> QuizAttempts { get; set; } = new();
    }
}
