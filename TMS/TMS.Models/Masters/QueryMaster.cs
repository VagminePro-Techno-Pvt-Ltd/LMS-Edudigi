using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.Models.Masters
{
    [Table(nameof(QueryMaster))]
    public class QueryMaster : BaseModel
    {
        public int CourseId { get; set; }
        [Required, StringLength(200)]
        public virtual string? Description { get; set; }
        [StringLength(100)]
        public string? FileName { get; set; }
        [StringLength(50)]
        public string? FilePath { get; set; }
        [ForeignKey(nameof(CourseId))]
        public virtual CourseMaster? Course { get; set; }
    }
}
