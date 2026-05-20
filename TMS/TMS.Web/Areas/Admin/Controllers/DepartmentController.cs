using TMS.Repository.Managers;
using TMS.ViewModels;
using TMS.ViewModels.Masters;
using TMS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq.Expressions;

namespace TMS.Web.Areas.Admin.Controllers
{
   // [ValidateFormAccess(Common.FormDefination.DepartmentMaster)]
    [Area("Admin")]
    public class DepartmentController : MastersAjaxController<DepartmentMasterViewModel>
    {
        private readonly IMasterManager<LocationMasterViewModel> _locationManager;

        public DepartmentController(IMasterManager<DepartmentMasterViewModel> manager, IMasterManager<LocationMasterViewModel> locationManager)
            : base(manager, "Department", "Department", new[] { "Location" })
        {
            _locationManager = locationManager;
        }

        protected override Expression<Func<DepartmentMasterViewModel, bool>>? GetFilter(DataTableRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Search?.Value))
                return null;
            Expression<Func<DepartmentMasterViewModel, bool>> predicate = t => (t.IsActive ? "Active" : "Inactive").Contains(request.Search.Value)
            || (!string.IsNullOrWhiteSpace(t.Location!.Name) && t.Location.Name.Contains(request.Search.Value))
            || (!string.IsNullOrWhiteSpace(t.Name) && t.Name.Contains(request.Search.Value))
            || (!string.IsNullOrWhiteSpace(t.Description) && t.Description.Contains(request.Search.Value));
            return predicate;
        }
        protected override List<DataTableColumnsOrder>? GetOrderColumns(DataTableRequest request)
        {
            var columns = new[] { "", "Location.Name", "Name", "Description", "IsActive", "UpdatedByUser.Name,CreatedByUser.Name", "UpdatedOn,CreatedOn" };
            return GetOrderedColumns(columns, request);
        }
        protected override async Task<AppResultViewModel> IsValidModel(DepartmentMasterViewModel model)
        {
            var resultIdCheck = await _manager.CheckExpression(t => (model.Id == 0 || t.Id != model.Id)
                   && t.LocationId == model.LocationId && t.Name == model.Name);
            if (resultIdCheck)
            {
                return GetResultModelFail("Name already in use");
            }
            return GetResultModelSuccess();
        }
        protected override async Task SetDropdownViewBag(DepartmentMasterViewModel model)
        {
            var locations = await _locationManager.GetAsync(null, t => t.IsActive || t.Id == model.LocationId);
            ViewBag.LocationId = locations.GetSelectList(model.LocationId, dataTextField: "NameStatus");
        }
    }
}
