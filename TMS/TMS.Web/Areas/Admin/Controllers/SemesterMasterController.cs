using TMS.Repository.Managers;
using TMS.ViewModels;
using TMS.ViewModels.Masters;
using TMS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq.Expressions;

namespace TMS.Web.Areas.Admin.Controllers
{
    [ValidateFormAccess(Common.FormDefination.SemesterMaster)]
    [Area("Admin")]
    public class SemesterMasterController : MastersAjaxController<SemesterMasterViewModel>
    {
        private readonly TMS.Web.Services.ContentSync.IContentFolderProvisioner _provisioner;

        public SemesterMasterController(
            IMasterManager<SemesterMasterViewModel> manager,
            TMS.Web.Services.ContentSync.IContentFolderProvisioner provisioner)
            : base(manager, "Semester Master", "Semester Master")
        {
            _provisioner = provisioner;
        }

        [HttpPost]
        public override async Task<IActionResult> Item(SemesterMasterViewModel model)
        {
            var res = await base.Item(model);
            if (model.Id > 0)
            {
                await _provisioner.EnqueueProvisioningAsync("Semester", model.Id, "CreateOrUpdate");
            }
            return res;
        }

        protected override Expression<Func<SemesterMasterViewModel, bool>>? GetFilter(DataTableRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Search?.Value))
                return null;
            Expression<Func<SemesterMasterViewModel, bool>> predicate = t => (t.IsActive ? "Active" : "Inactive").Contains(request.Search.Value)
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
