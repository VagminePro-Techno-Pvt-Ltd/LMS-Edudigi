using TMS.Repository.Managers;
using TMS.ViewModels;
using TMS.ViewModels.Masters;
using TMS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq.Expressions;

namespace TMS.Web.Areas.Admin.Controllers
{
    //[ValidateFormAccess(Common.FormDefination.LocationMaster)]
    [Area("Admin")]
    public class LocationController : MastersAjaxController<LocationMasterViewModel>
    {
        public LocationController(IMasterManager<LocationMasterViewModel> manager)
            : base(manager, "Location", "Location")
        {
        }

        protected override Expression<Func<LocationMasterViewModel, bool>>? GetFilter(DataTableRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Search?.Value))
                return null;
            Expression<Func<LocationMasterViewModel, bool>> predicate = t => (t.IsActive ? "Active" : "Inactive").Contains(request.Search.Value)
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
