using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Academics;
using TMS.Models.Training;

namespace TMS.Models.Masters
{
    [Table(nameof(CourseMaster))]
    public class CourseMaster : BaseMasterModel
    {
        [Required, StringLength(20)]
        public string? CourseCode { get; set; }

        public int CourseCategoryId { get; set; }
        public int CourseDurationMonths { get; set; }
       

        public int? SemesterId { get; set; }

        [ForeignKey(nameof(SemesterId))]
        public virtual SemesterMaster? Semester { get; set; }

        [ForeignKey(nameof(CourseCategoryId))]
        public virtual CourseCategoryMaster? CourseCategory { get; set; }

        

        public virtual List<CourseQuadrantMaster>? CourseQuadrants { get; set; }
        public virtual List<CourseEnrollment>? Enrollments { get; set; }

        
        public int NoofCredit { get; set; }
    }
}

