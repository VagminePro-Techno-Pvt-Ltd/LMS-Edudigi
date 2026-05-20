using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Masters
{
    [Table(nameof(BadgeMaster))]
    public class BadgeMaster : BaseMasterModel
    {
        public int CourseCategoryId { get; set; }
        public int DepartmentId { get; set; }
        public int? NoOfTrainingApplicable { get; set; }
        [StringLength(100)]
        public string? FileName { get; set; }
        [StringLength(50)]
        public string? FilePath { get; set; }

        [ForeignKey(nameof(CourseCategoryId))]
        public virtual CourseCategoryMaster? CourseCategory { get; set; }
        [ForeignKey(nameof(DepartmentId))]
        public virtual DepartmentMaster? Department { get; set; }
    }
}
