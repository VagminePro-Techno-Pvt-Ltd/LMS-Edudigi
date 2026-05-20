using TMS.Repository.Helpers;
using Microsoft.Data.SqlClient;
using System.Data;

namespace TMS.Repository.Repositories.Implementations
{
    internal class AccountRepository : IAccountRepository
    {
        private readonly IDBHelper _dBHelper;
        public AccountRepository(IDBHelper dBHelper)
        {
            _dBHelper = dBHelper;
        }

        public DataSet GetUserMenu(int userId)
        {
            return _dBHelper.ExecuteProc("GetUserMenu",
                 new SqlParameter("UserId", userId));
        }

        public DataSet GetAllRolePermissionMapping()
        {
            return _dBHelper.ExecuteProc("GetAllRolePermissionMapping");
        }

        public DataSet UpdateRoleAccessMapping(DataTable mappings)
        {
            return _dBHelper.ExecuteProc("UpdateRoleAccessMapping",
                new SqlParameter("@Mappings", mappings));
        }

        public DataSet GetRolePermissionsById(int roleId)
        {
            return _dBHelper.ExecuteProc("GetRolePermissionsById",
                new SqlParameter("@RoleId", roleId));
        }

        public DataSet UpdateRolePermissions(DataTable permissions, int roleId)
        {
            return _dBHelper.ExecuteProc("UpdateRolePermissions",
                new SqlParameter("@RoleId", roleId),
                new SqlParameter("@Permissions", permissions));
        }

        public DataSet GetAllUserTypes()
        {
            return _dBHelper.ExecuteProc("GetAllUserTypes");
        }

        public DataSet RolesByCompanyId(int companyId, int userId)
        {
            return _dBHelper.ExecuteProc("RolesByCompanyId",
                new SqlParameter("@CompanyId", companyId),
                new SqlParameter("@UserId", userId));
        }

        public DataSet GetFormControlPermissionByUser(int formId, int userId)
        {
            return _dBHelper.ExecuteProc("FormControlPermissionByUser",
                new SqlParameter("@FormId", formId),
                new SqlParameter("@UserId", userId));
        }

        public DataSet GetFormControlPermissionByRole(int formId, int roleId)
        {
            return _dBHelper.ExecuteProc("FormControlPermissionByRole",
               new SqlParameter("@FormId", formId),
               new SqlParameter("@RoleId", roleId));
        }

        public DataSet UpdateFormControlPermissionByRole(DataTable permissions, int roleId, int userId)
        {
            return _dBHelper.ExecuteProc("FormControlPermissionUpdate",
               new SqlParameter("@Permissions", permissions),
               new SqlParameter("@RoleId", roleId),
               new SqlParameter("@UserId", userId));
        }

        public DataSet CompanyLogoGetByUser(int userId)
        {
            return _dBHelper.ExecuteProc("CompanyLogoGetByUser",
                new SqlParameter("@UserId", userId));
        }
    }
}
