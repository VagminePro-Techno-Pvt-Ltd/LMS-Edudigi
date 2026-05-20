using TMS.Repository.Managers;
using TMS.ViewModels;
using TMS.ViewModels.Masters;
using TMS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq.Expressions;
using TMS.Models.Masters;
using Microsoft.Extensions.Options;
using TMS.Utilities;
using System.Security.Policy;

namespace TMS.Web.Areas.Admin.Controllers
{
   
    [Area("Admin")]
    public class BadgeController : MastersAjaxController<BadgeMasterViewModel>
    {
        private readonly IMasterManager<DepartmentMasterViewModel> _departmentManager;
        private readonly IMasterManager<CourseCategoryMasterViewModel> _courseCategoryManager;
        private readonly StorageSettings _storageSettings;

        public BadgeController(IMasterManager<BadgeMasterViewModel> manager, IMasterManager<DepartmentMasterViewModel> departmentManager
            , IMasterManager<CourseCategoryMasterViewModel> courseCategoryManager, IOptions<StorageSettings> options)
            : base(manager, "Badge", "Badge", new[] { "CourseCategory", "Department", "Department.Location" }, new[] { "CourseCategory", "Department", "Department.Location" })
        {
            _departmentManager = departmentManager;
            _courseCategoryManager = courseCategoryManager;
            _storageSettings = options.Value;
        }
        protected override void RemoveFromModelState()
        {
            base.RemoveFromModelState();
            ModelState.Remove("model.Department.Name");
        }

        protected override Expression<Func<BadgeMasterViewModel, bool>>? GetFilter(DataTableRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Search?.Value))
                return null;
            Expression<Func<BadgeMasterViewModel, bool>> predicate = t => (t.IsActive ? "Active" : "Inactive").Contains(request.Search.Value)
            || (!string.IsNullOrWhiteSpace(t.CourseCategory.Name) && t.CourseCategory.Name.Contains(request.Search.Value))
            || (!string.IsNullOrWhiteSpace(t.Department.Location.Name) && t.Department.Location.Name.Contains(request.Search.Value))
            || (!string.IsNullOrWhiteSpace(t.Department.Name) && t.Department.Name.Contains(request.Search.Value))
            || (!string.IsNullOrWhiteSpace(t.Name) && t.Name.Contains(request.Search.Value))
            || (!string.IsNullOrWhiteSpace(t.Description) && t.Description.Contains(request.Search.Value));
            return predicate;
        }
        protected override List<DataTableColumnsOrder>? GetOrderColumns(DataTableRequest request)
        {
            var columns = new[] { "", "CourseCategory", "Department.Name", "Name", "Description", "IsActive", "UpdatedByUser.Name,CreatedByUser.Name", "UpdatedOn,CreatedOn" };
            return GetOrderedColumns(columns, request);
        }
        protected override async Task<AppResultViewModel> IsValidModel(BadgeMasterViewModel model)
        {
            var resultIdCheck = await _manager.CheckExpression(t => (model.Id == 0 || t.Id != model.Id)
&& t.DepartmentId == model.DepartmentId && t.CourseCategoryId == model.CourseCategoryId && t.Name == model.Name);
            if (resultIdCheck)
            {
                return GetResultModelFail("Name already in use");
            }
            return GetResultModelSuccess();
        }
        protected override async Task SetDropdownViewBag(BadgeMasterViewModel model)
        {
            ViewBag.PublicImageBaseUrl = $"{_storageSettings.StorageUrl}/{_storageSettings.PublicContainerName}";
            var list = await _departmentManager.GetAsync(new[] { "Location" }, t => t.IsActive || t.Id == model.DepartmentId);
            ViewBag.DepartmentId = list.GetSelectList(model.DepartmentId, dataTextField: _nameWithParentField);
            var courseCategoryList = await _courseCategoryManager.GetAsync(null, t => t.IsActive || t.Id == model.CourseCategoryId);
            ViewBag.CourseCategoryId = courseCategoryList.GetSelectList(model.CourseCategoryId, dataTextField: _nameWithStatusField);
        }
        protected override void SetEditProperties(ref BadgeMasterViewModel model)
        {
            model.Department = null;
            if (model.File != null)
            {
                FileUtility fileUtility = new(_storageSettings);
                var attachmentModel = fileUtility.UploadFile(model.File, true, model.FilePath);
                model.FilePath = attachmentModel.FilePath;
                model.FileName = attachmentModel.FileName;
            }
        }
    }
}

