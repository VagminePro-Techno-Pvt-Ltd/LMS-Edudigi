using TMS.Repository.Managers;
using TMS.ViewModels;
using TMS.ViewModels.Masters;
using TMS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq.Expressions;

namespace TMS.Web.Areas.Admin.Controllers
{
  //  [ValidateFormAccess(Common.FormDefination.VenueMaster)]
    [Area("Admin")]
    public class VenueController : MastersAjaxController<VenueMasterViewModel>
    {
        public VenueController(IMasterManager<VenueMasterViewModel> manager)
            : base(manager, "Venue", "Venue")
        {
        }

        protected override Expression<Func<VenueMasterViewModel, bool>>? GetFilter(DataTableRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Search?.Value))
                return null;
            Expression<Func<VenueMasterViewModel, bool>> predicate = t => (t.IsActive ? "Active" : "Inactive").Contains(request.Search.Value)
            || (!string.IsNullOrWhiteSpace(t.Name) && t.Name.Contains(request.Search.Value))
            || (!string.IsNullOrWhiteSpace(t.Description) && t.Description.Contains(request.Search.Value))
            || (!string.IsNullOrWhiteSpace(t.VenueId) && t.VenueId.Contains(request.Search.Value));
            return predicate;
        }
        protected override List<DataTableColumnsOrder>? GetOrderColumns(DataTableRequest request)
        {
            var columns = new[] { "", "VenueId", "Name", "Description", "IsActive", "UpdatedByUser.Name,CreatedByUser.Name", "UpdatedOn,CreatedOn" };
            return GetOrderedColumns(columns, request);
        }
        protected override async Task<AppResultViewModel> IsValidModel(VenueMasterViewModel model)
        {
            var result = await base.IsValidModel(model);
            if (!result.Status)
            {
                return result;
            }
            var resultIdCheck = await _manager.CheckExpression(t => (model.Id == 0 || t.Id != model.Id)
                    && t.VenueId == model.VenueId);
            if (resultIdCheck)
            {
                return GetResultModelFail("Venue Id already in use");
            }
            return GetResultModelSuccess();
        }
    }
}
