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
  //  [ValidateFormAccess(Common.FormDefination.DesignationMaster)]
    [Area("Admin")]
    public class DesignationController : MastersAjaxController<DesignationMasterViewModel>
    {
        private readonly IMasterManager<LocationMasterViewModel> _locationManager;
        private readonly IMasterManager<DepartmentMasterViewModel> _departmentManager;
        private readonly IMasterManager<DivisionMasterViewModel> _divisionManager;

        public DesignationController(IMasterManager<DesignationMasterViewModel> manager, IMasterManager<LocationMasterViewModel> locationManager
            , IMasterManager<DepartmentMasterViewModel> departmentManager, IMasterManager<DivisionMasterViewModel> divisionManager)
            : base(manager, "Designation", "Designation", new[] { "Division", "Division.Department", "Division.Department.Location" }, new[] { "Division", "Division.Department", "Division.Department.Location" })
        {
            _locationManager = locationManager;
            _departmentManager = departmentManager;
            _divisionManager = divisionManager;
        }
        protected override void RemoveFromModelState()
        {
            base.RemoveFromModelState();
            ModelState.Remove("Division.Name");
            ModelState.Remove("Division.Department.Name");
        }

        protected override Expression<Func<DesignationMasterViewModel, bool>>? GetFilter(DataTableRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Search?.Value))
                return null;
            Expression<Func<DesignationMasterViewModel, bool>> predicate = t => (t.IsActive ? "Active" : "Inactive").Contains(request.Search.Value)
            || (!string.IsNullOrWhiteSpace(t.Division.Department.Location.Name) && t.Division.Department.Location.Name.Contains(request.Search.Value))
            || (!string.IsNullOrWhiteSpace(t.Division.Department.Name) && t.Division.Department.Name.Contains(request.Search.Value))
            || (!string.IsNullOrWhiteSpace(t.Division.Name) && t.Division.Name.Contains(request.Search.Value))
            || (!string.IsNullOrWhiteSpace(t.Name) && t.Name.Contains(request.Search.Value))
            || (!string.IsNullOrWhiteSpace(t.Description) && t.Description.Contains(request.Search.Value));
            return predicate;
        }
        protected override List<DataTableColumnsOrder>? GetOrderColumns(DataTableRequest request)
        {
            var columns = new[] { "", "Division.Name", "Division.Department.Name", "Division.Department.Location.Name", "Name", "Description", "IsActive", "UpdatedByUser.Name,CreatedByUser.Name", "UpdatedOn,CreatedOn" };
            return GetOrderedColumns(columns, request);
        }
        protected override async Task<AppResultViewModel> IsValidModel(DesignationMasterViewModel model)
        {
            var resultIdCheck = await _manager.CheckExpression(t => (model.Id == 0 || t.Id != model.Id)
                   && t.DivisionId == model.DivisionId && t.Name == model.Name);
            if (resultIdCheck)
            {
                return GetResultModelFail("Name already in use");
            }
            return GetResultModelSuccess();
        }
        protected override async Task SetDropdownViewBag(DesignationMasterViewModel model)
        {
            int locationId = model.Division?.Department?.LocationId ?? 0;
            var locations = await _locationManager.GetAsync(null, t => t.IsActive || t.Id == locationId);
            ViewBag.LocationId = locations.GetSelectList(model.Division?.Department?.LocationId, dataTextField: "NameStatus");

            if (model.Division != null)
            {
                var departments = await _departmentManager.GetAsync(null, t => t.LocationId == model.Division!.Department!.LocationId && (t.IsActive || t.Id == model.Division.DepartmentId));
                ViewBag.DepartmentId = departments.GetSelectList(model.Division.DepartmentId, dataTextField: "NameStatus");
                var divisions = await _divisionManager.GetAsync(null, t => t.DepartmentId == model.Division!.DepartmentId && (t.IsActive || t.Id == model.DivisionId));
                ViewBag.DivisionId = divisions.GetSelectList(model.DivisionId, dataTextField: "NameStatus");
            }
        }
        protected override void SetEditProperties(ref DesignationMasterViewModel model)
        {
            model.Division = null;
        }
        public async Task<IActionResult> GetDepartments(int id)
        {
            var list = await _departmentManager.GetAsync(null, t => t.LocationId == id && t.IsActive);
            return base.GetDropdownList(list.GetSelectList(dataTextField: "NameStatus"), true);
        }
        public async Task<IActionResult> GetDivisions(int id)
        {
            var list = await _divisionManager.GetAsync(null, t => t.DepartmentId == id && t.IsActive);
            return base.GetDropdownList(list.GetSelectList(dataTextField: "NameStatus"), true);
        }
    }
}
