
    using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TMS.Common;
using TMS.Models;
using TMS.ViewModels.Masters;

namespace TMS.ViewModels.Academics
    {
    public class FacultyQuizAssessmentViewModel:BaseViewModel
    {
        [MapToDTO]
        public int AttemptId { get; set; }       // StudentQuizAttempt Id
        [MapToDTO]
        public int QuizId { get; set; }       // StudentQuizAttempt Id
        [MapToDTO]
        public int StudentId { get; set; }
        [MapToDTO]
        public string StudentName { get; set; }
        [MapToDTO]
        public int CourseId { get; set; }
        [MapToDTO]
        public string CourseName { get; set; }
        [MapToDTO]
        public int QuadrantId { get; set; }
        [MapToDTO]
        public string QuadrantName { get; set; }
        [MapToDTO]
        public string QuizName { get; set; }
        [MapToDTO]
        public DateTime AttemptedOn { get; set; }
        [MapToDTO]
        public decimal? TotalScore { get; set; }
        public bool IsAssessed { get; set; }
        public List<FacultyQuizQuestionAssessmentViewModel> Questions { get; set; } = new();
    }
}


