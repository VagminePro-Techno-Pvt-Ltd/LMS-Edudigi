using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Account
{
    [Table(nameof(RoleMaster))]
    public class RoleMaster : BaseMasterModel
    {
        public int? UserTypeId { get; set; }

        [ForeignKey(nameof(UserTypeId))]
        public virtual UserType? UserType { get; set; }
    }
}
