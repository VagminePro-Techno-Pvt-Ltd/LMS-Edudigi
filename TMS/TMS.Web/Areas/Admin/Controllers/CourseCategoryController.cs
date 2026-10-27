using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using TMS.Repository.Managers;
using TMS.ViewModels;
using TMS.ViewModels.Masters;
using TMS.Web.Models;

namespace TMS.Web.Areas.Admin.Controllers
{
    [ValidateFormAccess(Common.FormDefination.CourseCategoryMaster)]
    [Area("Admin")]
    public class CourseCategoryController
        : MastersAjaxController<CourseCategoryMasterViewModel>
    {
        private readonly IMasterManager<SemesterMasterViewModel> _semesterManager;
        private readonly TMS.Web.Services.ContentSync.IContentFolderProvisioner _provisioner;

        public CourseCategoryController(
            IMasterManager<CourseCategoryMasterViewModel> manager,
            IMasterManager<SemesterMasterViewModel> semesterManager,
            TMS.Web.Services.ContentSync.IContentFolderProvisioner provisioner)
            : base(manager, "Programs Offered", "Programs Offered")
        {
            _semesterManager = semesterManager;
            _provisioner = provisioner;
        }

        [HttpPost]
        public override async Task<IActionResult> Item(CourseCategoryMasterViewModel model)
        {
            var res = await base.Item(model);

            if (model.NoOfSemester.HasValue && model.NoOfSemester.Value > 0)
            {
                var allSemesters = await _semesterManager.GetAsync(null, t => t.IsActive);
                int count = Math.Min(model.NoOfSemester.Value, 12);
                for (int i = 1; i <= count; i++)
                {
                    string semName = $"Semester {i}";
                    if (!allSemesters.Any(s => s.Name != null && s.Name.Trim().Equals(semName, StringComparison.OrdinalIgnoreCase)))
                    {
                        var newSem = new SemesterMasterViewModel
                        {
                            Name = semName,
                            IsActive = true
                        };
                        await _semesterManager.AddUpdateAsync(newSem, GetUserId());
                    }
                }
            }

            if (model.Id > 0)
            {
                await _provisioner.EnqueueProvisioningAsync("Program", model.Id, "CreateOrUpdate");
            }

            return res;
        }

        protected override async Task<AppResultViewModel> IsValidModel(
            CourseCategoryMasterViewModel model)
        {
            var userRole = User.FindFirst("role")?.Value ?? "";

            if (!userRole.Equals(
                    "Admin",
                    StringComparison.OrdinalIgnoreCase) &&
                !userRole.Equals(
                    "Super Admin",
                    StringComparison.OrdinalIgnoreCase))
            {
                return GetResultModelFail(
                    "Only Administrators can manage Programs.");
            }

            return await base.IsValidModel(model);
        }

        protected override Expression<
            Func<CourseCategoryMasterViewModel, bool>>?
            GetFilter(DataTableRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Search?.Value))
            {
                return null;
            }

            var searchValue = request.Search.Value;

            Expression<Func<CourseCategoryMasterViewModel, bool>> predicate =
                t =>
                    (t.IsActive ? "Active" : "Inactive")
                        .Contains(searchValue)
                    ||
                    (!string.IsNullOrWhiteSpace(t.Name) &&
                     t.Name.Contains(searchValue))
                    ||
                    (!string.IsNullOrWhiteSpace(t.Description) &&
                     t.Description.Contains(searchValue));

            return predicate;
        }

        protected override List<DataTableColumnsOrder>?
            GetOrderColumns(DataTableRequest request)
        {
            /*
             * IMPORTANT:
             *
             * This array MUST match CourseCategory-Index.js exactly.
             *
             * DataTable columns:
             *
             * 0 = Sl. No.
             * 1 = Program Name
             * 2 = Semesters
             * 3 = Description
             * 4 = Status
             * 5 = Last Action By
             * 6 = Last Action On
             * 7 = Action
             */
            var columns = new[]
            {
                "",
                "Name",
                "NoOfSemester",
                "Description",
                "IsActive",
                "UpdatedByUser.Name,CreatedByUser.Name",
                "UpdatedOn,CreatedOn",
                ""
            };

            return GetOrderedColumns(columns, request);
        }
    }
}