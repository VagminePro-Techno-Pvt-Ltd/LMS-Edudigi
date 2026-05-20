using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Masters
{
    [Table(nameof(TrainingMatrix))]
    public class TrainingMatrix : BaseModel
    {
        public int LocationId { get; set; }
        public int DepartmentId { get; set; }
        public int DivisionId { get; set; }
        public int DesignationId { get; set; }
        public int CourseId { get; set; }
        public int CoursePriority { get; set; }


        [ForeignKey(nameof(LocationId))]
        public virtual LocationMaster? Location { get; set; }
        [ForeignKey(nameof(DepartmentId))]
        public virtual DepartmentMaster? Department { get; set; }
        [ForeignKey(nameof(DivisionId))]
        public virtual DivisionMaster? Division { get; set; }
        [ForeignKey(nameof(DesignationId))]
        public virtual DesignationMaster? Designation { get; set; }
        [ForeignKey(nameof(CourseId))]
        public virtual CourseMaster? Course { get; set; }


    }
}
