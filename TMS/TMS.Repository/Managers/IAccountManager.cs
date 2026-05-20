using TMS.ViewModels;
using TMS.ViewModels.Account;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.Repository.Managers
{
    public interface IAccountManager
    {
        Task<UserViewModel?> GetUserByEmail(string email);
        Task<UserViewModel?> Login(string email, string pasword);
        Task<UserPermissions?> GetUserPermissions(int userId);
        Task<List<RolePermissionsViewModel>> GetRolePermissionsById(int roleId);
        Task<int> UpdateRolePermissions(List<RolePermissionsViewModel> permissions, int roleId);
        Task<List<UserTypeViewModel>> GetAllUserTypes();
        Task<List<SelectViewModel>> GetRolesByCompanyId(int companyId, int userId);
        Task<FormControlListViewModel> GetFormControlPermissionByUser(int formId, int userId);
        Task<List<FormControlViewModel>> GetFormControlPermissionByRole(int formId, int roleId);
        Task<int> UpdateFormControlPermissionByRole(List<FormControlViewModel> permissions, int roleId, int userId);
        Task<bool> UpdatePassword(UserViewModel model);

        /// <summary>
        /// Verifies a user's current password against the stored encrypted value.
        /// </summary>
        Task<bool> VerifyCurrentPassword(int userId, string currentPassword);

        /// <summary>
        /// Changes the user's password, updates PasswordLastChanged, and clears ForcePasswordChange flag.
        /// </summary>
        Task<bool> ChangePassword(int userId, string newPassword);
    }
}
