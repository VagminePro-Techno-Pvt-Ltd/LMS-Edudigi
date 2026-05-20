using TMS;
using System.ComponentModel.DataAnnotations;
using TMS.Common;

namespace TMS.ViewModels.Masters
{
    public class CourseQuadrantViewModel : BaseViewModel
    {


        [MapToDTO, Required, Display(Name = "Quadrant Number")]
        [Range(1, 4, ErrorMessage = "Quadrant number must be between 1 and 4.")]
        public int QuadrantNumber { get; set; }

        [MapToDTO, Display(Name = "Title"), MaxLength(100)]
        public string? Name { get; set; }

        [MapToDTO, Display(Name = "Description"), MaxLength(500)]
        public string? Description { get; set; }

        [MapToDTO, Display(Name = "Sequence")]
        public int Sequence { get; set; }

        //public virtual List<LectureMaterialViewModel> Materials { get; set; } = new();
        //public virtual List<CourseQuizViewModel> Quizzes { get; set; } = new();
        //public virtual List<DiscussionThreadViewModel> Discussions { get; set; } = new();
    }
}
