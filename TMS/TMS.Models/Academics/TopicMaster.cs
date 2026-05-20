using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Academics
{
    [Table(nameof(TopicMaster))]
    public class TopicMaster : BaseMasterModel
    {
        /// <summary>Parent unit this topic belongs to.</summary>
        [Required]
        public int UnitId { get; set; }

        [ForeignKey(nameof(UnitId))]
        public virtual UnitMaster? Unit { get; set; }

        /// <summary>Display order within the unit.</summary>
        public int SortOrder { get; set; } = 0;
    }
}
