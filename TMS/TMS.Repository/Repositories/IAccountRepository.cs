using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMS.Repository.Repositories
{
    internal interface IAccountRepository
    {
        DataSet GetUserMenu(int userId);
        DataSet GetAllRolePermissionMapping();
        DataSet UpdateRoleAccessMapping(DataTable mappings);
        DataSet GetRolePermissionsById(int roleId);
        DataSet UpdateRolePermissions(DataTable permissions, int roleId);
        DataSet GetAllUserTypes();
        DataSet RolesByCompanyId(int companyId, int userId);
        DataSet GetFormControlPermissionByUser(int formId, int userId);
        DataSet GetFormControlPermissionByRole(int formId, int roleId);
        DataSet UpdateFormControlPermissionByRole(DataTable permissions, int roleId, int userId);
        DataSet CompanyLogoGetByUser(int userId);
    }
}
