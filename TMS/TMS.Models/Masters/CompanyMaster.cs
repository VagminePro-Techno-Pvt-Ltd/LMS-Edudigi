using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Masters
{
    [Table(nameof(CompanyMaster))]
    public class CompanyMaster : BaseMasterModel
    {
        #region removed
        [NotMapped]
        public override string? Description { get; set; }
        #endregion
        [Required, StringLength(50)]
        public string? Initial { get; set; }

        [Required, StringLength(10)]
        public string? Phone { get; set; }
        [Required, StringLength(10)]
        public string? Fax { get; set; }
        [Required, StringLength(100)]
        public string? Email { get; set; }
        [Required, StringLength(100)]
        public string? Address1 { get; set; }
        [StringLength(100)]
        public string? Address2 { get; set; }
        [StringLength(100)]
        public string? Address3 { get; set; }
        [StringLength(100)]
        public string? Address4 { get; set; }
    }
}
