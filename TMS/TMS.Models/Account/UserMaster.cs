using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Models.Masters;

namespace TMS.Models.Account
{
    [Table(nameof(UserMaster))]
    public class UserMaster : BaseMasterModel
    {
        [NotMapped]
        public override string? Description { get; set; }
        [Required]
        [StringLength(100)]
        public string? Email { get; set; }
        [Required]
        [StringLength(100)]
        public string? Password { get; set; }
        public int? RoleId { get; set; }

       
        [StringLength(20)]
        public string? ContactNo { get; set; }

        /// <summary>
        /// When true, user is forced to change password on next login.
        /// Defaults to false for backward compatibility.
        /// </summary>
        public bool ForcePasswordChange { get; set; } = false;

        /// <summary>
        /// Tracks when the password was last changed for security auditing.
        /// </summary>
        public DateTime? PasswordLastChanged { get; set; }

		[ForeignKey(nameof(RoleId))]
        public virtual RoleMaster? Role { get; set; }
        public virtual List<UserRoles>? UserRoles { get; set; }

       
	}
}
