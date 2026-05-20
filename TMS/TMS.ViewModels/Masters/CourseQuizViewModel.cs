using TMS;
using System.ComponentModel.DataAnnotations;
using TMS.Common;

namespace TMS.ViewModels.Masters
{
    public class CourseQuizViewModel : BaseViewModel
    {
        [MapToDTO, Required, Display(Name = "Quadrant")]
        public int QuadrantId { get; set; }

        [MapToDTO, Required, Display(Name = "Quiz Title"), MaxLength(100)]
        public string? Title { get; set; }

        [MapToDTO, Display(Name = "Total Marks")]
        public int TotalMarks { get; set; }

        public virtual List<CourseQuizQuestionViewModel> Questions { get; set; } = new();
    }
}
