using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public CourseController(
            IMasterManager<CourseMasterViewModel> manager,
            IMasterManager<CourseCategoryMasterViewModel> courseCategoryManager,
            IMasterManager<SemesterMasterViewModel> semesterManager)
            : base(manager, "Course", "Course",
                  new[] { "CourseCategory", "Semester" },
                  new[] { "CourseCategory", "Semester" })
        {
            _courseCategoryManager = courseCategoryManager;
            _semesterManager = semesterManager;
        }

        protected override Expression<Func<CourseMasterViewModel, bool>>? GetFilter(DataTableRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Search?.Value))
                return null;

            Expression<Func<CourseMasterViewModel, bool>> predicate =
                t => (t.IsActive ? "Active" : "Inactive").Contains(request.Search.Value)
                || (!string.IsNullOrWhiteSpace(t.CourseCode) && t.CourseCode.Contains(request.Search.Value))
                || (!string.IsNullOrWhiteSpace(t.CourseCategory != null ? t.CourseCategory.Name : null)
                    && t.CourseCategory!.Name!.Contains(request.Search.Value))
                || (!string.IsNullOrWhiteSpace(t.Semester != null ? t.Semester.Name : null)
                    && t.Semester!.Name!.Contains(request.Search.Value))
                || (!string.IsNullOrWhiteSpace(t.Name) && t.Name.Contains(request.Search.Value));

            return predicate;
        }

        protected override List<DataTableColumnsOrder>? GetOrderColumns(DataTableRequest request)
        {
            var columns = new[] { "", "CourseCode", "CourseCategory.Name", "Semester.Name", "Name", "NoofCredit" };
            return GetOrderedColumns(columns, request);
        }

        protected override async Task SetDropdownViewBag(CourseMasterViewModel model)
        {
            var programs = await _courseCategoryManager.GetAsync(
                null,
                t => t.IsActive || t.Id == model.CourseCategoryId);

            ViewBag.CourseCategoryId =
                programs.GetSelectList(model.CourseCategoryId, dataTextField: "NameStatus");

            if (model.CourseCategoryId > 0)
            {
                var program = await _courseCategoryManager.GetAsync(model.CourseCategoryId);
                int semCount = program?.NoOfSemester ?? 0;

                var allSemesters = await _semesterManager.GetAsync(
                    null,
                    t => t.IsActive || t.Id == model.SemesterId);

                var allowedNames = Enumerable.Range(1, semCount)
                    .Select(i => $"Semester {i}")
                    .ToList();

                var filteredSemesters = allSemesters
                    .Where(s =>
                        (s.Name != null &&
                         allowedNames.Any(n =>
                             n.Equals(s.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                        || s.Id == model.SemesterId)
                    .OrderBy(s => s.Name)
                    .ToList();

                ViewBag.SemesterId =
                    filteredSemesters.GetSelectList(model.SemesterId, dataTextField: "NameStatus");
            }
            else
            {
                ViewBag.SemesterId =
                    new List<SemesterMasterViewModel>()
                        .GetSelectList(0, dataTextField: "NameStatus");
            }
        }

        protected override async Task<AppResultViewModel> IsValidModel(CourseMasterViewModel model)
        {
            // Intentionally do NOT call base.IsValidModel(model).
            // The base master validation enforces global Name uniqueness,
            // but different courses may legitimately have the same title.
            var code = model.CourseCode?.Trim();

            if (string.IsNullOrWhiteSpace(code))
                return GetResultModelFail("Course Code is required");

            var codeExists = await _manager.CheckExpression(t =>
                (model.Id == 0 || t.Id != model.Id) &&
                t.CourseCode != null &&
                t.CourseCode == code);

            if (codeExists)
                return GetResultModelFail("Course Code is already in use");

            return GetResultModelSuccess();
        }

        [HttpPost]
        public async Task<IActionResult> CreateMultiple(
            int courseCategoryId,
            int semesterId,
            List<CourseMasterViewModel>? courses)
        {
            if (courseCategoryId <= 0)
                return Json(GetResultModelFail("Please select a Program."));

            if (semesterId <= 0)
                return Json(GetResultModelFail("Please select a Semester."));

            if (courses == null || courses.Count == 0)
                return Json(GetResultModelFail("Please add at least one course."));

            var cleanCourses = courses
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.CourseCode) ||
                    !string.IsNullOrWhiteSpace(x.Name))
                .ToList();

            if (cleanCourses.Count == 0)
                return Json(GetResultModelFail("Please add at least one course."));

            var requestCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var course in cleanCourses)
            {
                course.Id = 0;
                course.CourseCategoryId = courseCategoryId;
                course.SemesterId = semesterId;
                course.CourseCode = course.CourseCode?.Trim();
                course.Name = course.Name?.Trim();
                course.Description = course.Description?.Trim();
                course.IsActive = true;

                if (string.IsNullOrWhiteSpace(course.CourseCode))
                    return Json(GetResultModelFail("Course Code is required for every course."));

                if (string.IsNullOrWhiteSpace(course.Name))
                    return Json(GetResultModelFail($"Course Title is required for {course.CourseCode}."));

                if (course.NoofCredit <= 0)
                    return Json(GetResultModelFail($"Credits must be greater than 0 for {course.CourseCode}."));

                if (course.CourseDurationMonths <= 0)
                    return Json(GetResultModelFail($"Duration must be greater than 0 for {course.CourseCode}."));

                if (!requestCodes.Add(course.CourseCode))
                    return Json(GetResultModelFail($"Duplicate Course Code '{course.CourseCode}' in this form."));

                var codeExists = await _manager.CheckExpression(t =>
                    t.CourseCode != null &&
                    t.CourseCode == course.CourseCode);

                if (codeExists)
                    return Json(GetResultModelFail($"Course Code '{course.CourseCode}' is already in use."));
            }

            foreach (var course in cleanCourses)
            {
                var saved = await _manager.AddUpdateAsync(course, GetUserId());

                if (!saved)
                {
                    return Json(GetResultModelFail(
                        $"Unable to create course '{course.CourseCode}'. No further courses were created."));
                }
            }

            return Json(GetResultModelSuccess(
                $"{cleanCourses.Count} course{(cleanCourses.Count == 1 ? "" : "s")} added successfully"));
        }


        [HttpPost]
        public async Task<IActionResult> UpdateWithAdditional(
            CourseMasterViewModel model,
            List<CourseMasterViewModel>? additionalCourses)
        {
            if (model.Id <= 0)
                return Json(GetResultModelFail("Course not found."));

            if (model.CourseCategoryId <= 0)
                return Json(GetResultModelFail("Please select a Program."));

            if (model.SemesterId <= 0)
                return Json(GetResultModelFail("Please select a Semester."));

            model.CourseCode = model.CourseCode?.Trim();
            model.Name = model.Name?.Trim();
            model.Description = model.Description?.Trim();

            if (string.IsNullOrWhiteSpace(model.CourseCode))
                return Json(GetResultModelFail("Course Code is required."));

            if (string.IsNullOrWhiteSpace(model.Name))
                return Json(GetResultModelFail("Course Title is required."));

            if (model.NoofCredit <= 0)
                return Json(GetResultModelFail("Credits must be greater than 0."));

            if (model.CourseDurationMonths <= 0)
                return Json(GetResultModelFail("Duration must be greater than 0."));

            var modelValidation = await IsValidModel(model);
            if (!modelValidation.Status)
                return Json(modelValidation);

            var cleanAdditionalCourses = (additionalCourses ?? new List<CourseMasterViewModel>())
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.CourseCode) ||
                    !string.IsNullOrWhiteSpace(x.Name))
                .ToList();

            var requestCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                model.CourseCode
            };

            foreach (var course in cleanAdditionalCourses)
            {
                course.Id = 0;
                course.CourseCategoryId = model.CourseCategoryId;
                course.SemesterId = model.SemesterId;
                course.CourseCode = course.CourseCode?.Trim();
                course.Name = course.Name?.Trim();
                course.Description = course.Description?.Trim();
                course.IsActive = true;

                if (string.IsNullOrWhiteSpace(course.CourseCode))
                    return Json(GetResultModelFail("Course Code is required for every additional course."));

                if (string.IsNullOrWhiteSpace(course.Name))
                    return Json(GetResultModelFail($"Course Title is required for {course.CourseCode}."));

                if (course.NoofCredit <= 0)
                    return Json(GetResultModelFail($"Credits must be greater than 0 for {course.CourseCode}."));

                if (course.CourseDurationMonths <= 0)
                    return Json(GetResultModelFail($"Duration must be greater than 0 for {course.CourseCode}."));

                if (!requestCodes.Add(course.CourseCode))
                    return Json(GetResultModelFail($"Duplicate Course Code '{course.CourseCode}' in this form."));

                var codeExists = await _manager.CheckExpression(t =>
                    t.CourseCode != null &&
                    t.CourseCode == course.CourseCode);

                if (codeExists)
                    return Json(GetResultModelFail($"Course Code '{course.CourseCode}' is already in use."));
            }

            var updated = await _manager.AddUpdateAsync(model, GetUserId());
            if (!updated)
                return Json(GetResultModelFail("Unable to update the course."));

            foreach (var course in cleanAdditionalCourses)
            {
                var saved = await _manager.AddUpdateAsync(course, GetUserId());
                if (!saved)
                    return Json(GetResultModelFail(
                        $"Course was updated, but additional course '{course.CourseCode}' could not be created."));
            }

            return Json(GetResultModelSuccess(
                cleanAdditionalCourses.Count == 0
                    ? "Course updated successfully"
                    : $"Course updated and {cleanAdditionalCourses.Count} additional course{(cleanAdditionalCourses.Count == 1 ? "" : "s")} added successfully"));
        }

        [HttpGet]
        public async Task<IActionResult> GetSemestersByProgram(int programId)
        {
            var program = await _courseCategoryManager.GetAsync(programId);

            if (program == null)
                return Json(new List<object>());

            int semCount = program.NoOfSemester ?? 0;

            if (semCount == 0)
                return Json(new List<object>());

            var allSemesters = await _semesterManager.GetAsync(null, t => t.IsActive);
            var result = new List<object>();

            for (int i = 1; i <= semCount; i++)
            {
                string semName = $"Semester {i}";

                var existing = allSemesters.FirstOrDefault(s =>
                    s.Name != null &&
                    s.Name.Trim().Equals(
                        semName,
                        StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    result.Add(new { id = existing.Id, name = existing.Name });
                    continue;
                }

                var newSem = new SemesterMasterViewModel
                {
                    Name = semName,
                    IsActive = true
                };

                await _semesterManager.AddUpdateAsync(newSem, GetUserId());

                var created = (await _semesterManager.GetAsync(
                    null,
                    s => s.Name == semName && s.IsActive))
                    .FirstOrDefault();

                if (created != null)
                    result.Add(new { id = created.Id, name = created.Name });
            }

            return Json(result);
        }
    }
}
