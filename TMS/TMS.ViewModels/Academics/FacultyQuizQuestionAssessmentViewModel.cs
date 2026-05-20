
    using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Common;
using TMS.Models;
using TMS.Models.Academics;
using TMS.Models.Masters;
using TMS.ViewModels.Masters;

namespace TMS.ViewModels.Academics
    {
    public class FacultyQuizQuestionAssessmentViewModel:BaseViewModel
    {
        [MapToDTO]
        public int FacultyQuizAssessmentId { get; set; }
        [MapToDTO]
        public int QuestionId { get; set; }
        [MapToDTO]
        public string QuestionText { get; set; }
        [MapToDTO]
        public decimal MaxScore { get; set; }      // editable by faculty
        [MapToDTO]
        public decimal StudentScore { get; set; }  // auto-calculated based on MaxScore

        public string? SelectedOptionText { get; set; }
        public string? WrittenAnswer { get; set; }
        public QuestionType QuestionType { get; set; }
      
    }
}


