using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TMS.Repository.Managers;
using TMS.ViewModels.Academics;
using TMS.ViewModels.Masters;
using TMS.Web.Controllers;

namespace TMS.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class CourseStructureController : BaseController
    {
        private readonly IMasterManager<CourseMasterViewModel> _courseManager;
        private readonly IMasterManager<SemesterMasterViewModel> _semesterManager;
        private readonly IMasterManager<CourseCategoryMasterViewModel> _courseCategoryManager;
        private readonly IMasterManager<UnitMasterViewModel> _unitManager;
        private readonly IMasterManager<SubjectMasterViewModel> _subjectManager;
        private readonly IMasterBaseManager<CourseFacultyMapViewModel> _facultyMapManager;
        private readonly IMasterBaseManager<CourseMaterialMappingViewModel> _materialMappingManager;
        private readonly IMasterBaseManager<LectureMaterialViewModel> _lectureMaterialManager;
        private readonly IMasterBaseManager<CourseQuadrantViewModel> _quadrantManager;

        public CourseStructureController(
            IMasterManager<CourseMasterViewModel> courseManager,
            IMasterManager<SemesterMasterViewModel> semesterManager,
            IMasterManager<CourseCategoryMasterViewModel> courseCategoryManager,
            IMasterManager<UnitMasterViewModel> unitManager,
            IMasterManager<SubjectMasterViewModel> subjectManager,
            IMasterBaseManager<CourseFacultyMapViewModel> facultyMapManager,
            IMasterBaseManager<CourseMaterialMappingViewModel> materialMappingManager,
            IMasterBaseManager<LectureMaterialViewModel> lectureMaterialManager,
            IMasterBaseManager<CourseQuadrantViewModel> quadrantManager)
        {
            _courseManager = courseManager;
            _semesterManager = semesterManager;
            _courseCategoryManager = courseCategoryManager;
            _unitManager = unitManager;
            _subjectManager = subjectManager;
            _facultyMapManager = facultyMapManager;
            _materialMappingManager = materialMappingManager;
            _lectureMaterialManager = lectureMaterialManager;
            _quadrantManager = quadrantManager;
        }

        // ═══════════════════════════════════════════════════
        //  INDEX: Program → Semester → Course → Content
        // ═══════════════════════════════════════════════════

        public async Task<IActionResult> Index(int? programId)
        {
            ViewData["Title"] = "Curriculum Builder";

            int userId = GetUserId();
            var userRole = GetUserRole()?.ToLower() ?? "";
            bool isFaculty = userRole == "faculty";

            // 1. Fetch all programs (CourseCategories)
            var programs = await _courseCategoryManager.GetAsync(null, t => t.IsActive);

            // 2. All courses (with Program + Semester included)
            List<CourseMasterViewModel> allCourses;
            if (isFaculty)
            {
                var facultyMaps = await _facultyMapManager.GetAsync(null, x => x.FacultyId == userId);
                var facultyCourseIds = facultyMaps.Select(m => m.CourseId).Distinct().ToList();
                var dbCourses = await _courseManager.GetAsync(new[] { "Semester", "CourseCategory" }, t => t.IsActive);
                allCourses = dbCourses.Where(c => facultyCourseIds.Contains(c.Id)).ToList();
            }
            else
            {
                allCourses = await _courseManager.GetAsync(new[] { "Semester", "CourseCategory" }, t => t.IsActive);
            }

            // 3. Determine selected program
            int selectedProgramId = programId ?? programs.FirstOrDefault()?.Id ?? 0;
            var selectedProgram = programs.FirstOrDefault(p => p.Id == selectedProgramId);

            // 4. Build structure for the selected program
            ProgramStructureViewModel? selectedStructure = null;
            if (selectedProgram != null)
            {
                selectedStructure = await BuildProgramStructureAsync(selectedProgram, allCourses);
            }

            // 5. Get quadrants for content display
            var quadrants = await _quadrantManager.GetAsync(null, q => q.IsActive);

            var model = new CurriculumBuilderPageViewModel
            {
                Programs = programs.OrderBy(p => p.Name).ToList(),
                SelectedProgramId = selectedProgramId,
                SelectedProgramStructure = selectedStructure,
                AllQuadrants = quadrants.OrderBy(q => q.QuadrantNumber).ToList()
            };

            ViewBag.IsFaculty = isFaculty;
            return View(model);
        }

        // ═══════════════════════════════════════════════════
        //  AJAX: Get Program Structure (partial view)
        // ═══════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> GetProgramStructure(int programId)
        {
            var program = await _courseCategoryManager.GetAsync(programId);
            if (program == null) return Content("<div class='text-muted text-center p-5'>Program not found.</div>");

            var allCourses = await _courseManager.GetAsync(new[] { "Semester", "CourseCategory" }, t => t.IsActive);
            var structure = await BuildProgramStructureAsync(program, allCourses);

            ViewBag.IsFaculty = (GetUserRole()?.ToLower() ?? "") == "faculty";
            return PartialView("_ProgramStructureTree", structure);
        }

        // ═══════════════════════════════════════════════════
        //  BUILDER: Program → Semesters → Courses → Content
        // ═══════════════════════════════════════════════════

        private async Task<ProgramStructureViewModel> BuildProgramStructureAsync(
            CourseCategoryMasterViewModel program,
            List<CourseMasterViewModel> allCourses)
        {
            var programCourses = allCourses
                .Where(c => c.CourseCategoryId == program.Id)
                .ToList();

            // Fetch all material mappings for these courses
            var courseIds = programCourses.Select(c => c.Id).ToArray();
            var allMappings = await _materialMappingManager.GetAsync(
                new[] { "LectureMaterial", "CourseQuadrant" },
                m => m.IsActive && courseIds.Contains(m.CourseId));

            // Group courses by Semester
            var semesterGroups = programCourses
                .Where(c => c.Semester != null)
                .GroupBy(c => new { c.SemesterId, SemesterName = c.Semester?.Name ?? "General" })
                .OrderBy(g => g.Key.SemesterName)
                .Select(g => new SemesterCourseGroupNode
                {
                    SemesterId = g.Key.SemesterId ?? 0,
                    SemesterName = g.Key.SemesterName,
                    Courses = g.OrderBy(c => c.CourseCode).Select(c => new CourseNode
                    {
                        CourseId = c.Id,
                        CourseName = c.Name ?? "Untitled",
                        CourseCode = c.CourseCode ?? "",
                        Credits = c.NoofCredit,
                        ContentCount = allMappings.Count(m => m.CourseId == c.Id)
                    }).ToList()
                }).ToList();

            // Include courses without a semester (if any)
            var noSemesterCourses = programCourses.Where(c => c.SemesterId == null || c.Semester == null).ToList();
            if (noSemesterCourses.Any())
            {
                semesterGroups.Add(new SemesterCourseGroupNode
                {
                    SemesterId = 0,
                    SemesterName = "General / Unassigned",
                    Courses = noSemesterCourses.Select(c => new CourseNode
                    {
                        CourseId = c.Id,
                        CourseName = c.Name ?? "Untitled",
                        CourseCode = c.CourseCode ?? "",
                        Credits = c.NoofCredit,
                        ContentCount = allMappings.Count(m => m.CourseId == c.Id)
                    }).ToList()
                });
            }

            return new ProgramStructureViewModel
            {
                ProgramId = program.Id,
                ProgramName = program.Name ?? "Unknown",
                ProgramDescription = program.Description,
                NoOfSemester = program.NoOfSemester ?? 0,
                SemesterGroups = semesterGroups
            };
        }

        // ═══════════════════════════════════════════════════
        //  AJAX: Content for a Course (mapped materials)
        // ═══════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> GetCourseContent(int courseId)
        {
            var mappings = await _materialMappingManager.GetAsync(
                new[] { "LectureMaterial", "CourseQuadrant" },
                m => m.IsActive && m.CourseId == courseId);

            var items = mappings.Select(m => new
            {
                mappingId = m.Id,
                title = m.LectureMaterial?.Title ?? "Untitled",
                type = m.LectureMaterial?.MaterialType ?? "Document",
                quadrant = m.CourseQuadrant?.Name ?? "General",
                quadrantId = m.CourseQuadrantId
            }).ToList();

            return Json(items);
        }

        // ═══════════════════════════════════════════════════
        //  AJAX: Assign Content to Course
        // ═══════════════════════════════════════════════════

        [HttpPost]
        public async Task<IActionResult> AssignContent(int materialId, int courseId, int? quadrantId)
        {
            // Check for duplicate
            var existing = await _materialMappingManager.GetAsync(null,
                x => x.LectureMaterialId == materialId && x.CourseId == courseId && x.IsActive);
            if (existing.Any())
                return Json(new { success = false, message = "Content already assigned to this course." });

            var mapping = new CourseMaterialMappingViewModel
            {
                LectureMaterialId = materialId,
                CourseId = courseId,
                CourseQuadrantId = quadrantId ?? 0,
                AssignedBy = GetUserId(),
                AssignedOn = DateTime.Now,
                IsActive = true
            };
            var res = await _materialMappingManager.AddUpdateAsync(mapping, GetUserId());
            return Json(new { success = res });
        }

        [HttpPost]
        public async Task<IActionResult> UnassignContent(int mappingId)
        {
            var mapping = await _materialMappingManager.GetAsync(mappingId);
            if (mapping == null) return Json(new { success = false });
            mapping.IsActive = false;
            var res = await _materialMappingManager.AddUpdateAsync(mapping, GetUserId());
            return Json(new { success = res });
        }

        [HttpGet]
        public async Task<IActionResult> GetLibraryItems(string? search)
        {
            var items = await _lectureMaterialManager.GetAsync(null, m => m.IsActive);
            if (!string.IsNullOrEmpty(search))
                items = items.Where(m => m.Title != null && m.Title.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
            return Json(items.OrderByDescending(m => m.Id).Select(m => new { id = m.Id, title = m.Title, type = m.MaterialType }));
        }

        // ═══════════════════════════════════════════════════
        //  LEGACY SUPPORT: Keep old endpoints working
        // ═══════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> GetMappedItems(int unitId)
        {
            // Legacy: map unitId to courseId for backward compatibility
            var mappings = await _materialMappingManager.GetAsync(new[] { "LectureMaterial" }, m => m.IsActive && m.UnitId == unitId);
            return Json(mappings.Select(m => new { mappingId = m.Id, title = m.LectureMaterial?.Title, type = m.LectureMaterial?.MaterialType }));
        }

        [HttpPost]
        public async Task<IActionResult> AddCourse(string name, string code, int categoryId)
        {
            var course = new CourseMasterViewModel { Name = name, CourseCode = code, CourseCategoryId = categoryId, IsActive = true };
            var res = await _courseManager.AddUpdateAsync(course, GetUserId());
            return Json(new { success = res });
        }

        [HttpPost]
        public async Task<IActionResult> AddSemester(string name, int courseId)
        {
            var baseCourse = await _courseManager.GetAsync(courseId);
            if (baseCourse == null) return Json(new { success = false });
            var semester = (await _semesterManager.GetAsync(null, s => s.Name == name)).FirstOrDefault();
            if (semester == null)
            {
                semester = new SemesterMasterViewModel { Name = name, IsActive = true };
                await _semesterManager.AddUpdateAsync(semester, GetUserId());
                semester = (await _semesterManager.GetAsync(null, s => s.Name == name)).FirstOrDefault();
            }
            var newVariant = new CourseMasterViewModel
            {
                Name = baseCourse.Name,
                CourseCode = baseCourse.CourseCode,
                CourseCategoryId = baseCourse.CourseCategoryId,
                SemesterId = semester?.Id,
                IsActive = true
            };
            var res = await _courseManager.AddUpdateAsync(newVariant, GetUserId());
            return Json(new { success = res });
        }

        [HttpPost]
        public async Task<IActionResult> AddSubject(string name, string? code, int semesterId, int courseId)
        {
            var targetCourse = await _courseManager.GetAsync(courseId);
            var variant = (await _courseManager.GetAsync(null, c => c.Name == targetCourse.Name && c.SemesterId == semesterId)).FirstOrDefault();
            if (variant == null) return Json(new { success = false, message = "Semester link not found." });
            var existing = await _subjectManager.GetAsync(null, s => s.SemesterId == semesterId);
            var subject = new SubjectMasterViewModel
            {
                Name = name, SubjectCode = code, SemesterId = semesterId,
                SortOrder = existing.Any() ? existing.Max(s => s.SortOrder) + 1 : 1,
                IsActive = true
            };
            var res = await _subjectManager.AddUpdateAsync(subject, GetUserId());
            return Json(new { success = res });
        }

        [HttpPost]
        public async Task<IActionResult> AddUnit(string name, string? desc, int subjectId)
        {
            var existing = await _unitManager.GetAsync(null, u => u.SubjectId == subjectId);
            var unit = new UnitMasterViewModel
            {
                Name = name, Description = desc, SubjectId = subjectId,
                SortOrder = existing.Any() ? existing.Max(u => u.SortOrder) + 1 : 1,
                IsActive = true
            };
            var res = await _unitManager.AddUpdateAsync(unit, GetUserId());
            return Json(new { success = res });
        }
    }
}
