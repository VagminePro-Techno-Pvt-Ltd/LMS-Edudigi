using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq.Expressions;
using TMS;
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

        public UserController(IMasterManager<UserViewModel> manager, IAccountManager accountManager
            , IMasterManager<EmploymentTypeMasterViewModel> employmentManager, IMasterManager<LocationMasterViewModel> locationManager
            , IMasterManager<DepartmentMasterViewModel> departmentManager, IMasterManager<DivisionMasterViewModel> divisionManager
            , IMasterManager<DesignationMasterViewModel> designationManager
            )
            : base(manager, "User", "User", new[] { "Role" }, new[] { "UserRoles" })
        {
            _accountManager = accountManager;
            _employmentManager = employmentManager;
            _locationManager = locationManager;
            _departmentManager = departmentManager;
            _divisionManager = divisionManager;
            _designationManager = designationManager;
        }

        /// <summary>
        /// Supports role-specific views: /Admin/User/Index?roleFilter=Student
        /// When roleFilter is set, the page title and DataTable filter are scoped to that role.
        /// </summary>
        public override async Task<IActionResult> Index()
        {
            var roleFilter = Request.Query["roleFilter"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(roleFilter))
            {
                ViewData["Title"] = roleFilter == "Faculty" ? "Faculty" : 
                                    roleFilter == "Student" ? "Students" :
                                    roleFilter == "Admin" ? "Admins" : "User";
                ViewData["RoleFilter"] = roleFilter;
            }
            else
            {
                ViewData["Title"] = "All Users";
            }
            await Task.CompletedTask;
            return View();
        }

        /// <summary>
        /// When adding from a role-specific tab, pre-assign the role.
        /// </summary>
        [HttpPost]
        public override async Task<IActionResult> Get(string iId)
        {
            var roleFilter = Request.Query["roleFilter"].FirstOrDefault();
            ViewData["Title"] = _masterType;

            if (iId == null)
            {
                UserViewModel model = new();
                // Pre-assign role if coming from role-specific tab
                if (!string.IsNullOrWhiteSpace(roleFilter))
                {
                    var roleList = await _accountManager.GetRolesByCompanyId(0, 0);
                    var matchedRole = roleList.FirstOrDefault(r => 
                        r.Name != null && r.Name.Equals(roleFilter, StringComparison.OrdinalIgnoreCase));
                    if (matchedRole != null && int.TryParse(matchedRole.Id, out int roleId))
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



        /// <summary>
        /// Server-side filter: supports both search text AND roleFilter query parameter.
        /// roleFilter is passed from the DataTable AJAX call to scope results by role.
        /// </summary>
        protected override Expression<Func<UserViewModel, bool>>? GetFilter(DataTableRequest request)
        {
            // Check for role filter in the raw request (passed via DataTable ajax data)
            string? roleFilter = null;
            if (HttpContext?.Request?.Form != null && HttpContext.Request.Form.ContainsKey("roleFilter"))
            {
                roleFilter = HttpContext.Request.Form["roleFilter"].FirstOrDefault();
            }

            var searchValue = request.Search?.Value;
            bool hasSearch = !string.IsNullOrWhiteSpace(searchValue);
            bool hasRoleFilter = !string.IsNullOrWhiteSpace(roleFilter);

            if (!hasSearch && !hasRoleFilter)
                return null;

            Expression<Func<UserViewModel, bool>> predicate = t =>
                // Role filter (exact match)
                (!hasRoleFilter || (t.Role != null && !string.IsNullOrWhiteSpace(t.Role.Name) && t.Role.Name == roleFilter))
                &&
                // Search filter (partial match across fields)
                (!hasSearch 
                    || (t.IsActive ? "Active" : "Inactive").Contains(searchValue!)
                    || (!string.IsNullOrWhiteSpace(t.Role.Name) && t.Role.Name.Contains(searchValue!))
                    || (!string.IsNullOrWhiteSpace(t.Name) && t.Name.Contains(searchValue!))
                    || (!string.IsNullOrWhiteSpace(t.Email) && t.Email.Contains(searchValue!))
                );
            return predicate;
        }
        protected override List<DataTableColumnsOrder>? GetOrderColumns(DataTableRequest request)
        {
            var columns = new[] { "", "Role.Name", "Name", "Email", "IsActive", "UpdatedByUser.Name,CreatedByUser.Name", "UpdatedOn,CreatedOn" };
            return GetOrderedColumns(columns, request);
        }
        public async Task<IActionResult> GetRolesOptions(int companyId)
        {
            var list = await _accountManager.GetRolesByCompanyId(companyId, 0);
            List<SelectListItem> selectList = new SelectList(list, "Id", "Name").ToList();
            return PartialView("~/Views/Shared/_OptionsPartial.cshtml", selectList);
        }
        public async Task<IActionResult> GetDepartments(int id)
        {
            var departments = await _departmentManager.GetAsync(null, t => t.LocationId == id);
            return GetDropdownList(departments.GetSelectList(dataTextField: "NameStatus"), true);
        }
        public async Task<IActionResult> GetDivision(int id)
        {
            var departments = await _divisionManager.GetAsync(null, t => t.DepartmentId == id);
            return GetDropdownList(departments.GetSelectList(dataTextField: "NameStatus"), true);
        }
        public async Task<IActionResult> GetDesignation(int id)
        {
            var departments = await _designationManager.GetAsync(null, t => t.DivisionId == id);
            return GetDropdownList(departments.GetSelectList(dataTextField: "NameStatus"), true);
        }

        protected override void RemoveFromModelState()
        {
            base.RemoveFromModelState();
            ModelState.Remove("Role");
            //ModelState.Remove("Password");
            ModelState.Remove("UserId");
        }

        protected override void SetEditProperties(ref UserViewModel model)
        {
            if (model.Id == 0)
                model.Password = model.Password?.Encrypt(false);
            if (model.UserRoles != null && !model.UserRoles.Any())
                model.UserRoles = null;
        }

        protected override async Task<AppResultViewModel> IsValidModel(UserViewModel model)
        {
            if (model.Id == 0 && string.IsNullOrWhiteSpace(model.Password))
            {
                return GetResultModelFail("Password is required");
            }
            var resultEmail = await _manager.CheckExpression(t => (model.Id == 0 || t.Id != model.Id)
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
            ViewBag.RoleId = roleList.GetSelectList();
         
          
            
        }
        [HttpPost]
        public override async Task<IActionResult> Item(UserViewModel model)
        {
            RemoveFromModelState();

            ViewData["Title"] = _masterType;
            if (!ModelState.IsValid)
            {
                return Json(GetFirstModelError(ModelState));
            }

            // ── BACKEND ROLE ENFORCEMENT ──
            // When saving from a role-specific tab, FORCE the correct role regardless of what UI sent
            var roleFilter = Request.Query["roleFilter"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(roleFilter) && model.Id == 0)
            {
                var roleList = await _accountManager.GetRolesByCompanyId(0, 0);
                var matchedRole = roleList.FirstOrDefault(r =>
                    r.Name != null && r.Name.Equals(roleFilter, StringComparison.OrdinalIgnoreCase));
                if (matchedRole != null && int.TryParse(matchedRole.Id, out int enforcedRoleId))
                {
                    model.RoleId = enforcedRoleId; // Override whatever UI sent
                }
            }

            var validationResult = await IsValidModel(model);
            if (!validationResult.Status)
            {
                return Json(validationResult);
            }
            SetEditProperties(ref model);
            model.Password = EncriptorUtility.Encrypt(model.Password);
            var result = await _manager.AddUpdateAsync(model, this.GetUserId());
            var r = await _accountManager.UpdatePassword(model);
            if (result)
            {
                return Json(GetResultModelSuccess($"{_masterType} {(model.Id == 0 ? "added" : "updated")} successfully"));
            }
            return Json(GetResultModelFail("Something went worng, please contact support"));
        }
    }
}
