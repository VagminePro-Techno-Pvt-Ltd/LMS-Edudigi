using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TMS.Common;
 

namespace TMS.ViewModels.Masters
{
    [RequireOptionTextIfObjective]
    public class QuizMasterViewModel : BaseViewModel
    {
        [MapToDTO, Display(Name = "Course")]
        [Required(ErrorMessage = "Course is required")]
        public int CourseId { get; set; }

        [MapToDTO, Display(Name = "Course Quadrant")]
        [Required(ErrorMessage = "Course Quadrant is required")]
        public int CourseQuadrantId { get; set; }

        [MapToDTO, Required, StringLength(100)]
        [Display(Name = "Quiz Title")]
        public string Title { get; set; } = default!;

        public virtual CourseMasterViewModel? Course { get; set; }
        public virtual CourseQuadrantViewModel? CourseQuadrant { get; set; }
        [MapToDTO]
        // List of questions under this quiz
        public virtual List<QuizQuestionViewModel>? Questions { get; set; }

        [MapToDTO]
        [Display(Name = "Total Score")]
        public int? TotalMarks { get; set; }
        [Display(Name = "Passing Score")]
        [MapToDTO]
        [Required]
        public int? PassingMarks { get; set; }
    }
}
