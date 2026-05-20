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

namespace TMS.Web.Areas.Admin.Controllers
{
   // [ValidateFormAccess(Common.FormDefination.PhotoGalleryMaster)]
    [Area("Admin")]
    public class PhotoGalleryController : MastersAjaxBaseController<PhotoGalleryMasterViewModel>
    {
        private readonly StorageSettings _storageSettings;
        private readonly IMasterManager<CourseMasterViewModel> _courseManager;

        public PhotoGalleryController(IMasterBaseManager<PhotoGalleryMasterViewModel> manager, IMasterManager<CourseMasterViewModel> courseManager
            , IOptions<StorageSettings> options)
            : base(manager, "Photo Gallery", "Photo Gallery", new[] { "Course" })
        {
            _storageSettings = options.Value;
            _courseManager = courseManager;
        }
        protected override void RemoveFromModelState()
        {
            base.RemoveFromModelState();
            ModelState.Remove("model.Department.Name");
        }

        protected override Expression<Func<PhotoGalleryMasterViewModel, bool>>? GetFilter(DataTableRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Search?.Value))
                return null;
            Expression<Func<PhotoGalleryMasterViewModel, bool>> predicate = t => (t.IsActive ? "Active" : "Inactive").Contains(request.Search.Value)
            || (!string.IsNullOrWhiteSpace(t.Course.Name) && t.Course.Name.Contains(request.Search.Value))
            || (!string.IsNullOrWhiteSpace(t.Course.CourseCode) && t.Course.CourseCode.Contains(request.Search.Value))
            || (!string.IsNullOrWhiteSpace(t.Description) && t.Description.Contains(request.Search.Value));
            return predicate;
        }
        protected override List<DataTableColumnsOrder>? GetOrderColumns(DataTableRequest request)
        {
            var columns = new[] { "", "Course.Name", "Description", "IsActive", "UpdatedByUser.Name,CreatedByUser.Name", "UpdatedOn,CreatedOn" };
            return GetOrderedColumns(columns, request);
        }
        protected override async Task<AppResultViewModel> IsValidModel(PhotoGalleryMasterViewModel model)
        {
            var resultIdCheck = await _manager.CheckExpression(t => (model.Id == 0 || t.Id != model.Id)
                   && t.CourseId == model.CourseId && t.Description == model.Description);
            if (resultIdCheck)
            {
                return GetResultModelFail("Description already in use");
            }
            return GetResultModelSuccess();
        }
        protected override async Task SetDropdownViewBag(PhotoGalleryMasterViewModel model)
        {
            ViewBag.PublicImageBaseUrl = $"{_storageSettings.StorageUrl}/{_storageSettings.PublicContainerName}";
            var list = await _courseManager.GetAsync(null, t => t.IsActive || t.Id == model.CourseId);
            ViewBag.CourseId = list.GetSelectList(model.CourseId, dataTextField: _nameWithStatusField);
        }
        protected override void SetEditProperties(ref PhotoGalleryMasterViewModel model)
        {
            model.Course = null;
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
