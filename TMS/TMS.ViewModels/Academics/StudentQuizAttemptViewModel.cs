using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TMS.Common;
using TMS.Models;
using TMS.ViewModels.Masters;

namespace TMS.ViewModels.Academics
{
    public class StudentQuizAttemptViewModel : BaseViewModel
    {
        [Required]
        [MapToDTO]
        public int StudentId { get; set; }

        [Required]
        [MapToDTO]
        public int QuizId { get; set; }

        [Required]
        [MapToDTO]
        public int CourseId { get; set; }

        [Required]
        [MapToDTO]
        public int CourseQuadrantId { get; set; }

        [Display(Name = "Quiz Title")]
        [MapToDTO]
        public string? QuizTitle { get; set; }

        [Display(Name = "Attempt Date")]
        [MapToDTO]
        public DateTime AttemptDate { get; set; } = DateTime.UtcNow;

        [MapToDTO]
        public bool IsSubmitted { get; set; }

        [MapToDTO]
        public int? Score { get; set; }

        // ✅ Collection of answers for this quiz attempt
        public List<StudentQuizAnswerViewModel> Answers { get; set; } = new();
    }
}
