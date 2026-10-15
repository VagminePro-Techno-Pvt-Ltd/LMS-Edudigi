using System;
using System.Collections.Generic;
using TMS.Models.Assessments;
using TMS.ViewModels.Masters;

namespace TMS.ViewModels.Academics
{
    public class StudentAssessmentHubViewModel
    {
        public IEnumerable<ExamMaster> Exams { get; set; } = new List<ExamMaster>();
        public List<QuizMasterViewModel> Quizzes { get; set; } = new();
        public List<StudentQuizAttemptViewModel> QuizAttempts { get; set; } = new();
    }
}
