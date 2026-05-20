using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Academics
{
    [Table(nameof(SubjectMaster))]
    public class SubjectMaster : BaseMasterModel
    {
        [Required]
        public int SemesterId { get; set; }

        [ForeignKey(nameof(SemesterId))]
        public virtual SemesterMaster? Semester { get; set; }

        [StringLength(20)]
        public string? SubjectCode { get; set; }

        public int SortOrder { get; set; } = 0;
    }
}
