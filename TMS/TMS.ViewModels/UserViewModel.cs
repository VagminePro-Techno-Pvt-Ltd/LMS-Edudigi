using TMS.ViewModels.Account;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TMS.Common;
using TMS.ViewModels.Masters;

namespace TMS.ViewModels
{
    public class UserViewModel : BaseMasterViewModel
    {
        [MapToDTO, Required, DataType(DataType.EmailAddress), Display(Name = "Email")]
        public string? Email { get; set; }
		public int? ContactNo { get; set; }
		 
		 

		[Required, DataType(DataType.Password), Display(Name = "Password")]
        public string? Password { get; set; }
        [MapToDTO, Display(Name = "Role")]
        public int? RoleId { get; set; }

        /// <summary>
        /// When true, user must change password before accessing the system.
        /// </summary>
        public bool ForcePasswordChange { get; set; }

        public virtual RoleMasterViewModel? Role { get; set; }
        public string RoleName { get { return Role == null || string.IsNullOrWhiteSpace(Role.Name) ? string.Empty : Role.Name + (Role.IsActive ? "" : " (Inactive)"); } }
        public virtual List<UserRolesViewModel> UserRoles { get; set; } = new();
        
        public virtual UserViewModel? User { get; set; }
    }
}
