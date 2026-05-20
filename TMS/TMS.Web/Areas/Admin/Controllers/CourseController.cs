using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Linq.Expressions;
using TMS.Repository.Managers;
using TMS.ViewModels.Masters;
using TMS.Web.Models;

namespace TMS.Web.Areas.Admin.Controllers
{
    [ValidateFormAccess(Common.FormDefination.CourseMaster)]
    [Area("Admin")]
    [Authorize]
    public class CourseController : MastersAjaxController<CourseMasterViewModel>
    {
        private readonly IMasterManager<CourseCategoryMasterViewModel> _courseCategoryManager;
        private readonly IMasterManager<SemesterMasterViewModel> _semesterManager;

        public CourseController(IMasterManager<CourseMasterViewModel> manager, IMasterManager<CourseCategoryMasterViewModel> courseCategoryManager
            ,IMasterManager<SemesterMasterViewModel> semesterManager)
            : base(manager, "Course", "Course", new[] { "CourseCategory", "Semester" }, new[] { "CourseCategory", "Semester" })
        {
            _courseCategoryManager = courseCategoryManager;
            _semesterManager = semesterManager;
        }

        protected override Expression<Func<CourseMasterViewModel, bool>>? GetFilter(DataTableRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Search?.Value))
                return null;
            Expression<Func<CourseMasterViewModel, bool>> predicate = t => (t.IsActive ? "Active" : "Inactive").Contains(request.Search.Value)
            || (!string.IsNullOrWhiteSpace(t.CourseCode) && t.CourseCode.Contains(request.Search.Value))
            || (!string.IsNullOrWhiteSpace(t.CourseCategory != null ? t.CourseCategory.Name : null) && t.CourseCategory!.Name!.Contains(request.Search.Value))
            || (!string.IsNullOrWhiteSpace(t.Semester != null ? t.Semester.Name : null) && t.Semester!.Name!.Contains(request.Search.Value))
            || (!string.IsNullOrWhiteSpace(t.Name) && t.Name.Contains(request.Search.Value));

            return predicate;
        }

        protected override List<DataTableColumnsOrder>? GetOrderColumns(DataTableRequest request)
        {
            var columns = new[] { "", "CourseCode", "CourseCategory.Name", "Semester.Name", "Name", "NoofCredit" };
            return GetOrderedColumns(columns, request);
        }

        protected async override Task SetDropdownViewBag(CourseMasterViewModel model)
        {
            // Programs dropdown (CourseCategoryMasters)
            var programs = await _courseCategoryManager.GetAsync(null,
              t => t.IsActive || t.Id == model.CourseCategoryId);
            ViewBag.CourseCategoryId = programs.GetSelectList(model.CourseCategoryId, dataTextField: "NameStatus");

            // Semesters dropdown — if editing, load semesters filtered by the selected Program's NoOfSemester
            if (model.CourseCategoryId > 0)
            {
                var program = await _courseCategoryManager.GetAsync(model.CourseCategoryId);
                int semCount = program?.NoOfSemester ?? 0;
                var allSemesters = await _semesterManager.GetAsync(null, t => t.IsActive || t.Id == model.SemesterId);

                // Only show semesters matching "Semester 1" .. "Semester N"
                var allowedNames = Enumerable.Range(1, semCount).Select(i => $"Semester {i}").ToList();
                var filteredSemesters = allSemesters
                    .Where(s => (s.Name != null && allowedNames.Any(n => n.Equals(s.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                                || s.Id == model.SemesterId) // Always include the currently selected one
                    .OrderBy(s => s.Name)
                    .ToList();

                ViewBag.SemesterId = filteredSemesters.GetSelectList(model.SemesterId, dataTextField: "NameStatus");
            }
            else
            {
                // No program selected yet — show empty placeholder
                ViewBag.SemesterId = new List<SemesterMasterViewModel>().GetSelectList(0, dataTextField: "NameStatus");
            }
        }

        protected override async Task<AppResultViewModel> IsValidModel(CourseMasterViewModel model)
        {
            var result = await base.IsValidModel(model);
            if (!result.Status)
            {
                return result;
            }
            // Check CourseCode uniqueness
            var codeExists = await _manager.CheckExpression(t => (model.Id == 0 || t.Id != model.Id)
                    && t.CourseCode == model.CourseCode);
            if (codeExists)
            {
                return GetResultModelFail("Course Code is already in use");
            }
            // Check duplicate: same Name + same Program + same Semester
            var dupExists = await _manager.CheckExpression(t => (model.Id == 0 || t.Id != model.Id)
                    && t.Name == model.Name
                    && t.CourseCategoryId == model.CourseCategoryId
                    && t.SemesterId == model.SemesterId);
            if (dupExists)
            {
                return GetResultModelFail("A course with this name already exists in the selected Program + Semester");
            }
            return GetResultModelSuccess();
        }

        // ═══════════════════════════════════════════════════
        //  AJAX: Load Semesters by Program (for dependent dropdown)
        // ═══════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> GetSemestersByProgram(int programId)
        {
            var program = await _courseCategoryManager.GetAsync(programId);
            if (program == null)
                return Json(new List<object>());

            int semCount = program.NoOfSemester ?? 0;
            if (semCount == 0)
                return Json(new List<object>());

            // Fetch all existing semesters
            var allSemesters = await _semesterManager.GetAsync(null, t => t.IsActive);

            // Build result: only "Semester 1" .. "Semester N"
            var result = new List<object>();
            for (int i = 1; i <= semCount; i++)
            {
                string semName = $"Semester {i}";

                // Find existing semester with this name (case-insensitive)
                var existing = allSemesters.FirstOrDefault(s =>
                    s.Name != null && s.Name.Trim().Equals(semName, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    result.Add(new { id = existing.Id, name = existing.Name });
                }
                else
                {
                    // Auto-create the missing semester
                    var newSem = new SemesterMasterViewModel
                    {
                        Name = semName,
                        IsActive = true
                    };
                    await _semesterManager.AddUpdateAsync(newSem, GetUserId());

                    // Re-fetch to get the generated Id
                    var created = (await _semesterManager.GetAsync(null, s => s.Name == semName && s.IsActive)).FirstOrDefault();
                    if (created != null)
                    {
                        result.Add(new { id = created.Id, name = created.Name });
                    }
                }
            }

            return Json(result);
        }
    }
}
