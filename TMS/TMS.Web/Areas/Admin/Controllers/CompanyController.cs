using TMS.Repository.Managers;
using TMS.ViewModels;
using TMS.ViewModels.Masters;
using TMS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq.Expressions;

namespace TMS.Web.Areas.Admin.Controllers
{
   // [ValidateFormAccess(Common.FormDefination.CompanyMaster)]
    [Area("Admin")]
    public class CompanyController : MastersAjaxController<CompanyMasterViewModel>
    {
        public CompanyController(IMasterManager<CompanyMasterViewModel> manager)
            : base(manager, "Company", "Company")
        {
        }

        protected override Expression<Func<CompanyMasterViewModel, bool>>? GetFilter(DataTableRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Search?.Value))
                return null;
            Expression<Func<CompanyMasterViewModel, bool>> predicate = t => (t.IsActive ? "Active" : "Inactive").Contains(request.Search.Value)
            || (!string.IsNullOrWhiteSpace(t.Name) && t.Name.Contains(request.Search.Value))
            || (!string.IsNullOrWhiteSpace(t.Initial) && t.Initial.Contains(request.Search.Value))
            || (!string.IsNullOrWhiteSpace(t.Phone) && t.Phone.Contains(request.Search.Value))
            || (!string.IsNullOrWhiteSpace(t.Fax) && t.Fax.Contains(request.Search.Value))
            || (!string.IsNullOrWhiteSpace(t.Email) && t.Email.Contains(request.Search.Value))
            || (!string.IsNullOrWhiteSpace(t.Address1) && t.Address1.Contains(request.Search.Value));
            return predicate;
        }
        protected override List<DataTableColumnsOrder>? GetOrderColumns(DataTableRequest request)
        {
            var columns = new[] { "", "Name", "Initial", "Phone", "Fax", "Email", "Address1", "IsActive", "UpdatedByUser.Name,CreatedByUser.Name", "UpdatedOn,CreatedOn" };
            return GetOrderedColumns(columns, request);
        }
        protected override async Task<AppResultViewModel> IsValidModel(CompanyMasterViewModel model)
        {
            var result = await base.IsValidModel(model);
            if (!result.Status)
            {
                return result;
            }
            return GetResultModelSuccess();
        }
    }
}
