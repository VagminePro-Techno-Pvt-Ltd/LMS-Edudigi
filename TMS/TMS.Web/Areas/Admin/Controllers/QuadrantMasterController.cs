using TMS.Repository.Managers;
using TMS.ViewModels;
using TMS.ViewModels.Masters;
using TMS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace TMS.Web.Areas.Admin.Controllers
{
    [ValidateFormAccess(Common.FormDefination.QuadrantMaster)]
    [Area("Admin")]
    public class QuadrantMasterController : MastersAjaxBaseController<CourseQuadrantViewModel>
    {
        public QuadrantMasterController(IMasterBaseManager<CourseQuadrantViewModel> manager)
            : base(manager, "Quadrant", "Quadrant")
        {
        }

        /// <summary>
        /// Loads the main Quadrant list view
        /// </summary>
        public override async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Quadrant Master";
            await Task.CompletedTask;
            return View();
        }

        /// <summary>
        /// Filter logic for Quadrant datatable search
        /// </summary>
        protected override Expression<Func<CourseQuadrantViewModel, bool>>? GetFilter(DataTableRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Search?.Value))
                return null;

            string searchValue = request.Search.Value.Trim();

            Expression<Func<CourseQuadrantViewModel, bool>> predicate = t =>
                (t.IsActive ? "Active" : "Inactive").Contains(searchValue) ||
                (!string.IsNullOrWhiteSpace(t.Name) && t.Name.Contains(searchValue)) ||
                (!string.IsNullOrWhiteSpace(t.Description) && t.Description.Contains(searchValue)) ||
                t.QuadrantNumber.ToString().Contains(searchValue);

            return predicate;
        }

        /// <summary>
        /// Column sorting for Quadrant datatable
        /// </summary>
        protected override List<DataTableColumnsOrder>? GetOrderColumns(DataTableRequest request)
        {
            var columns = new[]
            {
                "", // Checkbox/Actions
                "QuadrantNumber",
                "Name",
                "Description",
                "Sequence",
                "IsActive",
                "UpdatedByUser.Name,CreatedByUser.Name",
                "UpdatedOn,CreatedOn"
            };
            return GetOrderedColumns(columns, request);
        }

        /// <summary>
        /// Set dropdown data before rendering the form (if any)
        /// </summary>
        protected override async Task SetDropdownViewBag(CourseQuadrantViewModel model)
        {
            // Example: If you later want to assign Quadrants to Courses, you can load the dropdown here.
            // var courseList = await _courseManager.GetAllAsync();
            // ViewBag.CourseList = new SelectList(courseList, "Id", "Title");

            await Task.CompletedTask;
        }

        /// <summary>
        /// Apply custom validation before saving Quadrant
        /// </summary>
        protected override async Task<AppResultViewModel> IsValidModel(CourseQuadrantViewModel model)
        {
            if (model.QuadrantNumber < 1 || model.QuadrantNumber > 4)
            {
                return new AppResultViewModel
                {
                    Status = false,
                    Message = "Quadrant number must be between 1 and 4."
                };
            }

            // Example: Prevent duplicate quadrant numbers
            var existing = await _manager.GetAsync(null,x =>x.QuadrantNumber == model.QuadrantNumber && x.Id != model.Id);

            if (existing.Count>0)
            {
                return new AppResultViewModel
                {
                    Status = false,
                    Message = $"Quadrant {model.QuadrantNumber} already exists."
                };
            }

            return new AppResultViewModel { Status = true };
        }
    }
}
