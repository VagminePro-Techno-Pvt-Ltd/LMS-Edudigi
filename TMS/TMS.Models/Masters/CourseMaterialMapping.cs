using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Masters
{
    [Table(nameof(CourseMaterialMapping))]
    public class CourseMaterialMapping : BaseModel
    {
        [Required]
        public int LectureMaterialId { get; set; }

        [ForeignKey(nameof(LectureMaterialId))]
        public virtual LectureMaterial? LectureMaterial { get; set; }

        [Required]
        public int CourseId { get; set; }

        [ForeignKey(nameof(CourseId))]
        public virtual CourseMaster? Course { get; set; }

        public int CourseQuadrantId { get; set; }

        [ForeignKey(nameof(CourseQuadrantId))]
        public virtual CourseQuadrantMaster? CourseQuadrant { get; set; }

        public int? UnitId { get; set; }
        public int? SubjectId { get; set; }

        public int SortOrder { get; set; } = 0;

        public int AssignedBy { get; set; }

        public DateTime AssignedOn { get; set; } = DateTime.UtcNow;
    }
}