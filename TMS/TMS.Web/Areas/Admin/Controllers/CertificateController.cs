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
   // [ValidateFormAccess(Common.FormDefination.CertificateMaster)]
    [Area("Admin")]
    public class CertificateController : MastersAjaxController<CertificateMasterViewModel>
    {
        private readonly StorageSettings _storageSettings;

        public CertificateController(IMasterManager<CertificateMasterViewModel> manager, IOptions<StorageSettings> options)
            : base(manager, "Certificate", "Certificate")
        {
            _storageSettings = options.Value;
        }
        protected override Expression<Func<CertificateMasterViewModel, bool>>? GetFilter(DataTableRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Search?.Value))
                return null;
            Expression<Func<CertificateMasterViewModel, bool>> predicate = t => (t.IsActive ? "Active" : "Inactive").Contains(request.Search.Value)
            || (!string.IsNullOrWhiteSpace(t.Name) && t.Name.Contains(request.Search.Value))
            || (!string.IsNullOrWhiteSpace(t.Description) && t.Description.Contains(request.Search.Value));
            return predicate;
        }
        protected override List<DataTableColumnsOrder>? GetOrderColumns(DataTableRequest request)
        {
            var columns = new[] { "", "Name", "Description", "IsActive", "UpdatedByUser.Name,CreatedByUser.Name", "UpdatedOn,CreatedOn" };
            return GetOrderedColumns(columns, request);
        }
        protected override async Task SetDropdownViewBag(CertificateMasterViewModel model)
        {
            ViewBag.PublicImageBaseUrl = $"{_storageSettings.StorageUrl}/{_storageSettings.PublicContainerName}";
            await Task.CompletedTask;
        }
        protected override void SetEditProperties(ref CertificateMasterViewModel model)
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
