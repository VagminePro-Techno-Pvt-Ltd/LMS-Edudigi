using TMS;
using System;
using System.ComponentModel.DataAnnotations;
using TMS.Common;

namespace TMS.ViewModels.Masters
{
    public class CourseMaterialMappingViewModel : BaseViewModel
    {
        [MapToDTO, Required]
        public int LectureMaterialId { get; set; }

        [MapToDTO, Required]
        public int CourseId { get; set; }

        [MapToDTO]
        public int CourseQuadrantId { get; set; }

        [MapToDTO]
        public int SortOrder { get; set; }

        [MapToDTO]
        public int AssignedBy { get; set; }

        [MapToDTO]
        public DateTime AssignedOn { get; set; }

        [MapToDTO]
        public int? UnitId { get; set; }

        [MapToDTO]
        public int? SubjectId { get; set; }

        public LectureMaterialViewModel? LectureMaterial { get; set; }
        public CourseMasterViewModel? Course { get; set; }
        public CourseQuadrantViewModel? CourseQuadrant { get; set; }
    }
}
