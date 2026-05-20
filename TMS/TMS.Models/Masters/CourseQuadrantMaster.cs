using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Masters
{
    [Table(nameof(CourseQuadrantMaster))]
    public class CourseQuadrantMaster : BaseModel
    {
      

        [Required]
        public int QuadrantNumber { get; set; } // 1=Lecture, 2=Supplementary, 3=Quiz, 4=Forum

        [Required, StringLength(50)]
        public string Name { get; set; } = default!;

        //public virtual List<LectureMaterial>? Materials { get; set; }
        //public virtual List<QuizMaster>? Quizzes { get; set; }
        //public virtual List<DiscussionThread>? DiscussionThreads { get; set; }
    }
}
