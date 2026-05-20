using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Masters
{
    [Table(nameof(CertificateMaster))]
    public class CertificateMaster : BaseMasterModel
    {
        [StringLength(100)]
        public string? FileName { get; set; }
        [StringLength(50)]
        public string? FilePath { get; set; }
    }
}
