using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Linq.Expressions;
using TMS.Repository.Managers;
using TMS.Utilities;
using TMS.ViewModels;
using TMS.ViewModels.Masters;

namespace TMS.Web.Areas.Admin.Controllers
{
    //[ValidateFormAccess(Common.FormDefination.SignatureMaster)]
    [Area("Admin")]
    public class SignatureController : MastersAjaxController<SignatureMasterViewModel>
    {
        private readonly StorageSettings _storageSettings;

        public SignatureController(IMasterManager<SignatureMasterViewModel> manager, IOptions<StorageSettings> options)
            : base(manager, "Signature", "Signature")
        {
            _storageSettings = options.Value;
        }
        protected override Expression<Func<SignatureMasterViewModel, bool>>? GetFilter(DataTableRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Search?.Value))
                return null;
            Expression<Func<SignatureMasterViewModel, bool>> predicate = t => (t.IsActive ? "Active" : "Inactive").Contains(request.Search.Value)
            || (!string.IsNullOrWhiteSpace(t.Name) && t.Name.Contains(request.Search.Value))
            || (!string.IsNullOrWhiteSpace(t.Title) && t.Title.Contains(request.Search.Value))
            || (!string.IsNullOrWhiteSpace(t.Description) && t.Description.Contains(request.Search.Value));
            return predicate;
        }
        protected override List<DataTableColumnsOrder>? GetOrderColumns(DataTableRequest request)
        {
            var columns = new[] { "", "Name", "Title", "Description", "IsActive", "UpdatedByUser.Name,CreatedByUser.Name", "UpdatedOn,CreatedOn" };
            return GetOrderedColumns(columns, request);
        }

        protected override async Task SetDropdownViewBag(SignatureMasterViewModel model)
        {
            ViewBag.PublicImageBaseUrl = $"{_storageSettings.StorageUrl}/{_storageSettings.PublicContainerName}";
            await Task.CompletedTask;
        }
        protected override void SetEditProperties(ref SignatureMasterViewModel model)
        {
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
