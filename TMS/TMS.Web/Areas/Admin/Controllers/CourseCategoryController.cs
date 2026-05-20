using TMS.Repository.Managers;
using TMS.ViewModels;
using TMS.ViewModels.Masters;
using TMS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq.Expressions;

namespace TMS.Web.Areas.Admin.Controllers
{
    [ValidateFormAccess(Common.FormDefination.CourseCategoryMaster)]
    [Area("Admin")]
    public class CourseCategoryController : MastersAjaxController<CourseCategoryMasterViewModel>
    {
        public CourseCategoryController(IMasterManager<CourseCategoryMasterViewModel> manager)
            : base(manager, "Programs Offered", "Programs Offered")
        {
        }

        protected override async Task<AppResultViewModel> IsValidModel(CourseCategoryMasterViewModel model)
        {
            var userRole = User.FindFirst("role")?.Value ?? "";
            if (!userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) 
                && !userRole.Equals("Super Admin", StringComparison.OrdinalIgnoreCase))
            {
                return GetResultModelFail("Only Administrators can manage Programs.");
            }
            return await base.IsValidModel(model);
        }

        protected override Expression<Func<CourseCategoryMasterViewModel, bool>>? GetFilter(DataTableRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Search?.Value))
                return null;
            Expression<Func<CourseCategoryMasterViewModel, bool>> predicate = t => (t.IsActive ? "Active" : "Inactive").Contains(request.Search.Value)
            || (!string.IsNullOrWhiteSpace(t.Name) && t.Name.Contains(request.Search.Value))
            || (!string.IsNullOrWhiteSpace(t.Description) && t.Description.Contains(request.Search.Value));
            return predicate;
        }
        protected override List<DataTableColumnsOrder>? GetOrderColumns(DataTableRequest request)
        {
            var columns = new[] { "", "Name", "Description", "IsActive", "UpdatedByUser.Name,CreatedByUser.Name", "UpdatedOn,CreatedOn" };
            return GetOrderedColumns(columns, request);
        }
    }
}
