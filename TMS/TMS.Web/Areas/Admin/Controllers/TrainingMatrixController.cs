using TMS.Repository.Managers;
using TMS.ViewModels;
using TMS.Web.Controllers;
using TMS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc.Rendering;
using TMS.ViewModels.Masters;

namespace TMS.Web.Areas.Admin.Controllers
{

    //[ValidateFormAccess(Common.FormDefination.TrainingMatrix)]
    [Area("Admin")]
    public class TrainingMatrixController : BaseController
    {
        private readonly IMasterBaseManager<TrainingMatrixViewModel> _manager;
        private readonly IMasterManager<LocationMasterViewModel> _locationManager;
        private readonly IMasterManager<DepartmentMasterViewModel> _departmentManager;
        private readonly IMasterManager<DivisionMasterViewModel> _divisionManager;
        private readonly IMasterManager<DesignationMasterViewModel> _designationManager;
        private readonly IMasterManager<CourseMasterViewModel> _courseManager;
        private readonly IMasterManager<CourseCategoryMasterViewModel> _courseCategoryManager;

        public TrainingMatrixController(IMasterBaseManager<TrainingMatrixViewModel> manager, IMasterManager<LocationMasterViewModel> locationManager
            , IMasterManager<DepartmentMasterViewModel> departmentManager, IMasterManager<DivisionMasterViewModel> divisionManager
            , IMasterManager<DesignationMasterViewModel> designationManager, IMasterManager<CourseMasterViewModel> courseManager
            , IMasterManager<CourseCategoryMasterViewModel> courseCategoryManager)
        {
            _manager = manager;
            _locationManager = locationManager;
            _departmentManager = departmentManager;
            _divisionManager = divisionManager;
            _designationManager = designationManager;
            _courseManager = courseManager;
            _courseCategoryManager = courseCategoryManager;
        }
        public async Task<IActionResult> Index()
        {
            var locations = await _locationManager.GetAsync();
            ViewBag.LocationId = locations.GetSelectList(dataTextField: "NameStatus");
            var courceCategories = await _courseCategoryManager.GetAsync();
            ViewBag.CourseCategoryId = courceCategories.GetSelectList(dataTextField: "NameStatus");
            return View();
        }
        public async Task<IActionResult> GetDepartments(int id)
        {
            var departments = await _departmentManager.GetAsync(null, t => t.LocationId == id && t.IsActive);
            return GetDropdownList(departments.GetSelectList(dataTextField: "NameStatus"), true);
        }
        public async Task<IActionResult> GetDivision(int id)
        {
            var departments = await _divisionManager.GetAsync(null, t => t.DepartmentId == id && t.IsActive);
            return GetDropdownList(departments.GetSelectList(dataTextField: "NameStatus"), true);
        }
        public async Task<IActionResult> GetDesignation(int id)
        {
            var departments = await _designationManager.GetAsync(null, t => t.DivisionId == id && t.IsActive);
            return GetDropdownList(departments.GetSelectList(dataTextField: "NameStatus"), true);
        }
        public async Task<IActionResult> GetTrainingMatrix(TrainingMatrixViewModel model)
        {
            List<TrainingMatrixViewModel> returnModel = new();
            var trainingMatrices = await _manager.GetAsync(new[] { "Course", "Course.CourseCategory", "Course.CourseEmploymentTypeMappings", "Course.CourseEmploymentTypeMappings.EmploymentType" }
                , t => model.CourseCategoryId == 0 || t.Course!.CourseCategoryId == model.CourseCategoryId);
            var courseList = await _courseManager.GetAsync(new[] { "CourseCategory", "CourseEmploymentTypeMappings", "CourseEmploymentTypeMappings.EmploymentType" }, t => t.IsActive
                & (model.CourseCategoryId == 0 || t.CourseCategoryId == model.CourseCategoryId));
            foreach (var item in courseList)
            {
                if (!trainingMatrices.Any(t => t.CourseId == item.Id) && item.IsActive)
                {
                    trainingMatrices.Add(new() { Course = item });
                }
            }
            returnModel = trainingMatrices.OrderBy(t => t.Course?.Name).ToList();
            return PartialView("_TrainingCourseList", returnModel);
        }
        protected virtual IActionResult GetDropdownList(List<SelectListItem> selectList, bool excludeDefault = false)
        {
            ViewBag.ExcludeDefault = excludeDefault;
            return PartialView("~/Views/Shared/_OptionsPartial.cshtml", selectList);
        }
        [HttpPost]
        public async Task<IActionResult> SaveTrainingMatrix(List<TrainingMatrixViewModel> model)
        {
            bool result = await _manager.AddUpdateAsync(model, this.GetUserId());
            if (result)
            {
                return Json(GetResultModelSuccess("Submitted successfully"));
            }
            return Json(GetResultModelFail("Something went worng, please contact support"));

        }

    }
}
