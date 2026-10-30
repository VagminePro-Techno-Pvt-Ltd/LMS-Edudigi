using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq.Expressions;
using TMS;
using TMS.Common;
using TMS.Repository.Managers;
using TMS.ViewModels;
using TMS.ViewModels.Masters;
using TMS.Web.Models;

namespace TMS.Web.Areas.Admin.Controllers
{
    [ValidateFormAccess(Common.FormDefination.User)]
    [Area("Admin")]
    public class UserController : MastersAjaxController<UserViewModel>
    {
        private readonly IAccountManager _accountManager;
        private readonly IMasterManager<EmploymentTypeMasterViewModel> _employmentManager;
        private readonly IMasterManager<LocationMasterViewModel> _locationManager;
        private readonly IMasterManager<DepartmentMasterViewModel> _departmentManager;
        private readonly IMasterManager<DivisionMasterViewModel> _divisionManager;
        private readonly IMasterManager<DesignationMasterViewModel> _designationManager;
        private readonly IEnrollmentManager _enrollmentManager;
        private readonly IMasterManager<CourseCategoryMasterViewModel> _programManager;
        private readonly IMasterManager<CourseMasterViewModel> _courseManager;
        private readonly TMS.Web.Services.Email.IEmailOutboxService _outboxService;

        private static readonly string[] AllowedUserRoles =
        {
            "Faculty",
            "Student",
            "Super Admin"
        };

        public UserController(
            IMasterManager<UserViewModel> manager,
            IAccountManager accountManager,
            IMasterManager<EmploymentTypeMasterViewModel> employmentManager,
            IMasterManager<LocationMasterViewModel> locationManager,
            IMasterManager<DepartmentMasterViewModel> departmentManager,
            IMasterManager<DivisionMasterViewModel> divisionManager,
            IMasterManager<DesignationMasterViewModel> designationManager,
            IEnrollmentManager enrollmentManager,
            IMasterManager<CourseCategoryMasterViewModel> programManager,
            IMasterManager<CourseMasterViewModel> courseManager,
            TMS.Web.Services.Email.IEmailOutboxService outboxService)
            : base(manager, "User", "User", new[] { "Role" }, new[] { "UserRoles" })
        {
            _accountManager = accountManager;
            _employmentManager = employmentManager;
            _locationManager = locationManager;
            _departmentManager = departmentManager;
            _divisionManager = divisionManager;
            _designationManager = designationManager;
            _enrollmentManager = enrollmentManager;
            _programManager = programManager;
            _courseManager = courseManager;
            _outboxService = outboxService;
        }

        public override async Task<IActionResult> Index()
        {
            var roleFilter = Request.Query["roleFilter"].FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(roleFilter))
            {
                ViewData["Title"] = roleFilter == "Faculty"
                    ? "Faculty"
                    : roleFilter == "Student"
                        ? "Students"
                        : roleFilter == "Admin"
                            ? "Admins"
                            : "User";

                ViewData["RoleFilter"] = roleFilter;
            }
            else
            {
                ViewData["Title"] = "All Users";
            }

            await Task.CompletedTask;
            return View();
        }

        [HttpPost]
        public override async Task<IActionResult> Get(string iId)
        {
            var roleFilter = Request.Query["roleFilter"].FirstOrDefault();
            ViewData["Title"] = _masterType;

            if (iId == null)
            {
                UserViewModel model = new();

                if (!string.IsNullOrWhiteSpace(roleFilter))
                {
                    var roleList = await _accountManager.GetRolesByCompanyId(0, 0);

                    var matchedRole = roleList.FirstOrDefault(r =>
                        r.Name != null &&
                        r.Name.Equals(roleFilter, StringComparison.OrdinalIgnoreCase));

                    if (matchedRole != null &&
                        int.TryParse(matchedRole.Id, out int roleId))
                    {
                        model.RoleId = roleId;
                    }

                    ViewData["RoleFilter"] = roleFilter;
                }

                await SetDropdownViewBag(model);
                return PartialView("Item", model);
            }

            iId = iId.Decrypt(true);

            if (string.IsNullOrWhiteSpace(iId))
                return Json(GetResultModelFail("Record not found"));

            var idInt = Convert.ToInt32(iId);
            var item = await _manager.GetAsync(idInt, _masterEditInclude);

            SetProperties(ref item!);
            await SetDropdownViewBag(item!);

            return PartialView("Item", item);
        }

        protected override Expression<Func<UserViewModel, bool>>? GetFilter(
            DataTableRequest request)
        {
            string? roleFilter = null;

            if (HttpContext?.Request?.Form != null &&
                HttpContext.Request.Form.ContainsKey("roleFilter"))
            {
                roleFilter = HttpContext.Request.Form["roleFilter"].FirstOrDefault();
            }

            var searchValue = request.Search?.Value;

            bool hasSearch = !string.IsNullOrWhiteSpace(searchValue);
            bool hasRoleFilter = !string.IsNullOrWhiteSpace(roleFilter);

            if (!hasSearch && !hasRoleFilter)
                return null;

            Expression<Func<UserViewModel, bool>> predicate = t =>
                (!hasRoleFilter ||
                    (t.Role != null &&
                     !string.IsNullOrWhiteSpace(t.Role.Name) &&
                     t.Role.Name == roleFilter))
                &&
                (!hasSearch
                    || (t.IsActive ? "Active" : "Inactive").Contains(searchValue!)
                    || (!string.IsNullOrWhiteSpace(t.Role.Name) &&
                        t.Role.Name.Contains(searchValue!))
                    || (!string.IsNullOrWhiteSpace(t.Name) &&
                        t.Name.Contains(searchValue!))
                    || (!string.IsNullOrWhiteSpace(t.Email) &&
                        t.Email.Contains(searchValue!)));

            return predicate;
        }

        protected override List<DataTableColumnsOrder>? GetOrderColumns(
            DataTableRequest request)
        {
            var columns = new[]
            {
                "",
                "Role.Name",
                "Name",
                "Email",
                "IsActive",
                "UpdatedByUser.Name,CreatedByUser.Name",
                "UpdatedOn,CreatedOn"
            };

            return GetOrderedColumns(columns, request);
        }

        public async Task<IActionResult> GetRolesOptions(int companyId)
        {
            var list = await _accountManager.GetRolesByCompanyId(companyId, 0);

            var allowedRoles = list
                .Where(r =>
                    r.Name != null &&
                    AllowedUserRoles.Contains(
                        r.Name,
                        StringComparer.OrdinalIgnoreCase))
                .ToList();

            List<SelectListItem> selectList =
                new SelectList(allowedRoles, "Id", "Name").ToList();

            return PartialView(
                "~/Views/Shared/_OptionsPartial.cshtml",
                selectList);
        }

        public async Task<IActionResult> GetDepartments(int id)
        {
            var departments = await _departmentManager.GetAsync(
                null,
                t => t.LocationId == id);

            return GetDropdownList(
                departments.GetSelectList(dataTextField: "NameStatus"),
                true);
        }

        public async Task<IActionResult> GetDivision(int id)
        {
            var departments = await _divisionManager.GetAsync(
                null,
                t => t.DepartmentId == id);

            return GetDropdownList(
                departments.GetSelectList(dataTextField: "NameStatus"),
                true);
        }

        public async Task<IActionResult> GetDesignation(int id)
        {
            var departments = await _designationManager.GetAsync(
                null,
                t => t.DivisionId == id);

            return GetDropdownList(
                departments.GetSelectList(dataTextField: "NameStatus"),
                true);
        }

        protected override void RemoveFromModelState()
        {
            base.RemoveFromModelState();
            ModelState.Remove("Role");
            ModelState.Remove("UserId");
            ModelState.Remove("ProgramId");
            ModelState.Remove("CourseIds");
        }

        protected override void SetEditProperties(ref UserViewModel model)
        {
            if (model.UserRoles != null && !model.UserRoles.Any())
                model.UserRoles = null;
        }

        protected override async Task<AppResultViewModel> IsValidModel(
            UserViewModel model)
        {
            if (model.Id == 0 && string.IsNullOrWhiteSpace(model.Password))
            {
                return GetResultModelFail("Password is required");
            }

            var resultEmail = await _manager.CheckExpression(
                t => (model.Id == 0 || t.Id != model.Id)
                     && t.Email == model.Email);

            if (resultEmail)
            {
                return GetResultModelFail("Email already in use");
            }

            return GetResultModelSuccess();
        }

        protected override async Task SetDropdownViewBag(UserViewModel model)
        {
            var roleList = await _accountManager.GetRolesByCompanyId(0, 0);

            var allowedRoles = roleList
                .Where(r =>
                    r.Name != null &&
                    AllowedUserRoles.Contains(
                        r.Name,
                        StringComparer.OrdinalIgnoreCase))
                .ToList();

            ViewBag.RoleId = allowedRoles.GetSelectList();

            // Populate Programs and Courses for Student creation/editing
            var programs = await _programManager.GetAsync(null, p => p.IsActive);
            ViewBag.ProgramList = programs.OrderBy(p => p.Name).ToList();

            var courses = await _courseManager.GetAsync(new[] { "Semester", "CourseCategory" }, c => c.IsActive);
            ViewBag.CourseList = courses.OrderBy(c => c.Name).ToList();

            if (model != null && model.Id > 0)
            {
                var enrolled = await _enrollmentManager.GetAsync(null,
                    e => e.StudentId == model.Id && e.Status == (int)EnrollmentStatus.Active);
                ViewBag.EnrolledCourseIds = enrolled.Select(e => e.CourseId).ToList();
            }
            else
            {
                ViewBag.EnrolledCourseIds = new List<int>();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCoursesByProgram(int programId)
        {
            var courses = await _courseManager.GetAsync(new[] { "Semester" },
                c => c.IsActive && (programId == 0 || c.CourseCategoryId == programId));

            var result = courses.Select(c => new
            {
                id = c.Id,
                name = c.Name,
                code = c.CourseCode,
                semester = c.Semester?.Name ?? ""
            }).OrderBy(c => c.name);

            return Json(result);
        }

        [HttpPost]
        public override async Task<IActionResult> Item(UserViewModel model)
        {
            RemoveFromModelState();

            if (model.Id > 0)
            {
                ModelState.Remove(nameof(UserViewModel.Password));
            }

            model.Email = model.Email?.Trim();

            ViewData["Title"] = _masterType;

            if (!ModelState.IsValid)
            {
                return Json(GetFirstModelError(ModelState));
            }

            var roleFilter = Request.Query["roleFilter"].FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(roleFilter) && model.Id == 0)
            {
                var roleList = await _accountManager.GetRolesByCompanyId(0, 0);

                var matchedRole = roleList.FirstOrDefault(r =>
                    r.Name != null &&
                    r.Name.Equals(
                        roleFilter,
                        StringComparison.OrdinalIgnoreCase));

                if (matchedRole != null &&
                    int.TryParse(matchedRole.Id, out int enforcedRoleId))
                {
                    model.RoleId = enforcedRoleId;
                }
            }

            var validationResult = await IsValidModel(model);

            if (!validationResult.Status)
            {
                return Json(validationResult);
            }

            bool isNew = (model.Id == 0);
            SetEditProperties(ref model);

            var result = await _manager.AddUpdateAsync(
                model,
                this.GetUserId());

            if (result)
            {
                // Auto-Enroll Student in Selected Program/Courses
                try
                {
                    int targetStudentId = model.Id;
                    if (targetStudentId == 0)
                    {
                        var createdUser = (await _manager.GetAsync(null, u => u.Email == model.Email)).FirstOrDefault();
                        if (createdUser != null) targetStudentId = createdUser.Id;
                    }

                    if (targetStudentId > 0 && (model.ProgramId.HasValue || (model.CourseIds != null && model.CourseIds.Any())))
                    {
                        var coursesToEnroll = new HashSet<int>();
                        if (model.ProgramId.HasValue && model.ProgramId.Value > 0)
                        {
                            var progCourses = await _courseManager.GetAsync(null, c => c.IsActive && c.CourseCategoryId == model.ProgramId.Value);
                            foreach (var pc in progCourses) coursesToEnroll.Add(pc.Id);
                        }
                        if (model.CourseIds != null)
                        {
                            foreach (var cid in model.CourseIds) coursesToEnroll.Add(cid);
                        }
                        foreach (var cId in coursesToEnroll)
                        {
                            await _enrollmentManager.EnrollStudentAsync(cId, targetStudentId, this.GetUserId(), "Student Creation");
                        }
                    }

                    if (isNew && targetStudentId > 0 && !string.IsNullOrWhiteSpace(model.Email))
                    {
                        string roleName = "Student";
                        if (model.RoleId.HasValue)
                        {
                            var roles = await _accountManager.GetRolesByCompanyId(0, 0);
                            var roleObj = roles.FirstOrDefault(r => r.Id == model.RoleId.Value.ToString());
                            if (roleObj != null) roleName = roleObj.Name ?? "Student";
                        }

                        await _outboxService.EnqueueAsync(new TMS.ViewModels.Email.EmailEnqueueRequest
                        {
                            RecipientEmail = model.Email.Trim(),
                            RecipientName = model.Name,
                            Subject = "Welcome to EduDigiClass LMS — Account Activated",
                            TemplateName = "AccountWelcome",
                            TemplateData = new Dictionary<string, string>
                            {
                                ["RecipientName"] = model.Name ?? "User",
                                ["Email"] = model.Email.Trim(),
                                ["Role"] = roleName
                            },
                            EventType = "UserWelcome",
                            IdempotencyKey = $"welcome-user-{targetStudentId}"
                        });
                    }
                }
                catch
                {
                    // Non-fatal: keep user creation successful
                }

                return Json(
                    GetResultModelSuccess(
                        $"{_masterType} {(model.Id == 0 ? "added" : "updated")} successfully"));
            }

            return Json(
                GetResultModelFail(
                    "Something went wrong, please contact support"));
        }
    }
}