using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Masters;

namespace TMS.Models.Academics
{
    [Table(nameof(UnitMaster))]
    public class UnitMaster : BaseMasterModel
    {
        [Required]
        public int SubjectId { get; set; }

        [ForeignKey(nameof(SubjectId))]
        public virtual SubjectMaster? Subject { get; set; }

        public int SortOrder { get; set; } = 0;
    }
}
