using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Account;
using TMS.Models.Masters;

namespace TMS.Models.Academics
{
    public class StudentCourseProgress:BaseModel
    {
       

        [Required]
        public int StudentId { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        public int QuadrantId { get; set; }

        public bool IsCompleted { get; set; } = false;
        public DateTime? CompletedOn { get; set; }

        [ForeignKey(nameof(StudentId))]
        public virtual UserMaster Student { get; set; } = null!;

        [ForeignKey(nameof(CourseId))]
        public virtual CourseMaster Course { get; set; } = null!;

        [ForeignKey(nameof(QuadrantId))]
        public virtual CourseQuadrantMaster CourseQuadrant { get; set; } = null!;
    }
}
