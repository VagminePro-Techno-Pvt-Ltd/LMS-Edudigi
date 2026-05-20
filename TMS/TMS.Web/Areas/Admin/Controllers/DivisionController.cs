using TMS.Repository.Managers;
using TMS.ViewModels;
using TMS.ViewModels.Masters;
using TMS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq.Expressions;
using TMS.Models.Masters;

namespace TMS.Web.Areas.Admin.Controllers
{
    //[ValidateFormAccess(Common.FormDefination.DivisionMaster)]
    [Area("Admin")]
    public class DivisionController : MastersAjaxController<DivisionMasterViewModel>
    {
        private readonly IMasterManager<LocationMasterViewModel> _locationManager;
        private readonly IMasterManager<DepartmentMasterViewModel> _departmentManager;

        public DivisionController(IMasterManager<DivisionMasterViewModel> manager, IMasterManager<LocationMasterViewModel> locationManager
            , IMasterManager<DepartmentMasterViewModel> departmentManager)
            : base(manager, "Division", "Division", new[] { "Department", "Department.Location" }, new[] { "Department", "Department.Location" })
        {
            _locationManager = locationManager;
            _departmentManager = departmentManager;
        }
        protected override void RemoveFromModelState()
        {
            base.RemoveFromModelState();
            ModelState.Remove("model.Department.Name");
            ModelState.Remove("Department.Name");
        }

        protected override Expression<Func<DivisionMasterViewModel, bool>>? GetFilter(DataTableRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Search?.Value))
                return null;
            Expression<Func<DivisionMasterViewModel, bool>> predicate = t => (t.IsActive ? "Active" : "Inactive").Contains(request.Search.Value)
            || (!string.IsNullOrWhiteSpace(t.Department.Location.Name) && t.Department.Location.Name.Contains(request.Search.Value))
            || (!string.IsNullOrWhiteSpace(t.Department.Name) && t.Department.Name.Contains(request.Search.Value))
            || (!string.IsNullOrWhiteSpace(t.Name) && t.Name.Contains(request.Search.Value))
            || (!string.IsNullOrWhiteSpace(t.Description) && t.Description.Contains(request.Search.Value));
            return predicate;
        }
        protected override List<DataTableColumnsOrder>? GetOrderColumns(DataTableRequest request)
        {
            var columns = new[] { "", "Department.Name", "Department.Location.Name", "Name", "Description", "IsActive", "UpdatedByUser.Name,CreatedByUser.Name", "UpdatedOn,CreatedOn" };
            return GetOrderedColumns(columns, request);
        }
        protected override async Task<AppResultViewModel> IsValidModel(DivisionMasterViewModel model)
        {
            var resultIdCheck = await _manager.CheckExpression(t => (model.Id == 0 || t.Id != model.Id)
                   && t.DepartmentId == model.DepartmentId && t.Name == model.Name);
            if (resultIdCheck)
            {
                return GetResultModelFail("Name already in use");
            }
            return GetResultModelSuccess();
        }
        protected override async Task SetDropdownViewBag(DivisionMasterViewModel model)
        {
            int locationId = model.Department?.LocationId ?? 0;
            var locations = await _locationManager.GetAsync(null, t => t.IsActive || t.Id == locationId);
            ViewBag.LocationId = locations.GetSelectList(model.Department?.LocationId, dataTextField: "NameStatus");

            if (model.Department != null)
            {
                var departments = await _departmentManager.GetAsync(null, t => t.LocationId == model.Department.LocationId && (t.IsActive || t.Id == model.DepartmentId));
                ViewBag.DepartmentId = departments.GetSelectList(model.DepartmentId, dataTextField: "NameStatus");
            }
        }
        protected override void SetEditProperties(ref DivisionMasterViewModel model)
        {
            model.Department = null;
        }
        public async Task<IActionResult> GetDepartments(int id)
        {
            var departments = await _departmentManager.GetAsync(null, t => t.LocationId == id && t.IsActive);
            return base.GetDropdownList(departments.GetSelectList(dataTextField: "NameStatus"), true);
        }
    }
}
