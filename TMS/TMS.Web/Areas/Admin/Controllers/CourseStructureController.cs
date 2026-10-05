using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public async Task<IActionResult> Index(int? programId)
        {
            ViewData["Title"] = "Curriculum Builder";

            int userId = GetUserId();
            bool isFaculty = IsFaculty();

            var programs = await _courseCategoryManager.GetAsync(
                null,
                x => x.IsActive);

            List<CourseMasterViewModel> allCourses;

            if (isFaculty)
            {
                var facultyMappings = await _facultyMapManager.GetAsync(
                    null,
                    x => x.FacultyId == userId);

                var facultyCourseIds = facultyMappings
                    .Select(x => x.CourseId)
                    .Distinct()
                    .ToHashSet();

                var dbCourses = await _courseManager.GetAsync(
                    new[] { "Semester", "CourseCategory" },
                    x => x.IsActive);

                allCourses = dbCourses
                    .Where(x => facultyCourseIds.Contains(x.Id))
                    .ToList();
            }
            else
            {
                allCourses = await _courseManager.GetAsync(
                    new[] { "Semester", "CourseCategory" },
                    x => x.IsActive);
            }

            var orderedPrograms = programs
                .OrderBy(x => x.Name)
                .ToList();

            /*
             * IMPORTANT:
             * The UI must display the actual number of semesters currently
             * represented by courses instead of trusting a stale program-level
             * NoOfSemester value.
             */
            foreach (var program in orderedPrograms)
            {
                var actualSemesterCount = allCourses
                    .Where(x =>
                        x.CourseCategoryId == program.Id &&
                        x.SemesterId.HasValue)
                    .Select(x => x.SemesterId!.Value)
                    .Distinct()
                    .Count();

                program.NoOfSemester = actualSemesterCount;
            }

            int selectedProgramId = 0;

            if (programId.HasValue &&
                orderedPrograms.Any(x => x.Id == programId.Value))
            {
                selectedProgramId = programId.Value;
            }
            else
            {
                selectedProgramId = orderedPrograms.FirstOrDefault()?.Id ?? 0;
            }

            ProgramStructureViewModel? selectedStructure = null;

            if (selectedProgramId > 0)
            {
                var selectedProgram = orderedPrograms
                    .FirstOrDefault(x => x.Id == selectedProgramId);

                if (selectedProgram != null)
                {
                    selectedStructure = await BuildProgramStructureAsync(
                        selectedProgram,
                        allCourses);
                }
            }

            var quadrants = await _quadrantManager.GetAsync(
                null,
                x => x.IsActive);

            var model = new CurriculumBuilderPageViewModel
            {
                Programs = orderedPrograms,
                SelectedProgramId = selectedProgramId,
                SelectedProgramStructure = selectedStructure,
                AllQuadrants = quadrants
                    .OrderBy(x => x.QuadrantNumber)
                    .ToList()
            };

            ViewBag.IsFaculty = isFaculty;

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetProgramStructure(int programId)
        {
            if (programId <= 0)
            {
                return BadRequest("Invalid program.");
            }

            var program = await _courseCategoryManager.GetAsync(programId);

            if (program == null || !program.IsActive)
            {
                return NotFound(
                    "<div class=\"cb-error-state\">" +
                    "<i class=\"fa-solid fa-circle-exclamation\"></i>" +
                    "<div>Program not found.</div>" +
                    "</div>");
            }

            var courses = await GetAccessibleCoursesAsync();

            var structure = await BuildProgramStructureAsync(
                program,
                courses);

            ViewBag.IsFaculty = IsFaculty();

            return PartialView("_ProgramStructureTree", structure);
        }

        private async Task<List<CourseMasterViewModel>> GetAccessibleCoursesAsync()
        {
            var courses = await _courseManager.GetAsync(
                new[] { "Semester", "CourseCategory" },
                x => x.IsActive);

            if (!IsFaculty())
            {
                return courses;
            }

            int userId = GetUserId();

            var mappings = await _facultyMapManager.GetAsync(
                null,
                x => x.FacultyId == userId);

            var allowedCourseIds = mappings
                .Select(x => x.CourseId)
                .Distinct()
                .ToHashSet();

            return courses
                .Where(x => allowedCourseIds.Contains(x.Id))
                .ToList();
        }

        private async Task<bool> CanAccessCourseAsync(int courseId)
        {
            if (courseId <= 0)
            {
                return false;
            }

            var course = await _courseManager.GetAsync(courseId);

            if (course == null || !course.IsActive)
            {
                return false;
            }

            if (!IsFaculty())
            {
                return true;
            }

            int userId = GetUserId();

            var mappings = await _facultyMapManager.GetAsync(
                null,
                x => x.FacultyId == userId && x.CourseId == courseId);

            return mappings.Any();
        }

        private bool IsFaculty()
        {
            return string.Equals(
                GetUserRole(),
                "faculty",
                StringComparison.OrdinalIgnoreCase);
        }

        private async Task<ProgramStructureViewModel> BuildProgramStructureAsync(
            CourseCategoryMasterViewModel program,
            List<CourseMasterViewModel> allCourses)
        {
            var programCourses = allCourses
                .Where(x => x.CourseCategoryId == program.Id)
                .ToList();

            var courseIds = programCourses
                .Select(x => x.Id)
                .Distinct()
                .ToArray();

            List<CourseMaterialMappingViewModel> allMappings;

            if (courseIds.Length == 0)
            {
                allMappings = new List<CourseMaterialMappingViewModel>();
            }
            else
            {
                allMappings = await _materialMappingManager.GetAsync(
                    new[] { "LectureMaterial", "CourseQuadrant" },
                    x => x.IsActive && courseIds.Contains(x.CourseId));
            }

            var semesterGroups = programCourses
                .Where(x => x.SemesterId.HasValue && x.Semester != null)
                .GroupBy(x => new
                {
                    SemesterId = x.SemesterId!.Value,
                    SemesterName = x.Semester!.Name
                })
                .OrderBy(x => x.Key.SemesterId)
                .ThenBy(x => x.Key.SemesterName)
                .Select(group => new SemesterCourseGroupNode
                {
                    SemesterId = group.Key.SemesterId,
                    SemesterName = string.IsNullOrWhiteSpace(group.Key.SemesterName)
                        ? $"Semester {group.Key.SemesterId}"
                        : group.Key.SemesterName,

                    Courses = group
                        .OrderBy(x => x.CourseCode)
                        .ThenBy(x => x.Name)
                        .Select(course => new CourseNode
                        {
                            CourseId = course.Id,
                            CourseName = string.IsNullOrWhiteSpace(course.Name)
                                ? "Untitled Course"
                                : course.Name,

                            CourseCode = course.CourseCode ?? string.Empty,
                            Credits = course.NoofCredit,

                            ContentCount = allMappings.Count(
                                x => x.CourseId == course.Id)
                        })
                        .ToList()
                })
                .ToList();

            var unassignedCourses = programCourses
                .Where(x => !x.SemesterId.HasValue || x.Semester == null)
                .OrderBy(x => x.CourseCode)
                .ThenBy(x => x.Name)
                .ToList();

            if (unassignedCourses.Any())
            {
                semesterGroups.Add(new SemesterCourseGroupNode
                {
                    SemesterId = 0,
                    SemesterName = "General / Unassigned",

                    Courses = unassignedCourses
                        .Select(course => new CourseNode
                        {
                            CourseId = course.Id,
                            CourseName = string.IsNullOrWhiteSpace(course.Name)
                                ? "Untitled Course"
                                : course.Name,

                            CourseCode = course.CourseCode ?? string.Empty,
                            Credits = course.NoofCredit,

                            ContentCount = allMappings.Count(
                                x => x.CourseId == course.Id)
                        })
                        .ToList()
                });
            }

            int actualSemesterCount = semesterGroups
                .Where(x => x.SemesterId > 0)
                .Select(x => x.SemesterId)
                .Distinct()
                .Count();

            return new ProgramStructureViewModel
            {
                ProgramId = program.Id,
                ProgramName = program.Name ?? "Unknown Program",
                ProgramDescription = program.Description,
                NoOfSemester = actualSemesterCount,
                SemesterGroups = semesterGroups
            };
        }

        [HttpGet]
        public async Task<IActionResult> GetCourseContent(int courseId)
        {
            if (!await CanAccessCourseAsync(courseId))
            {
                return Forbid();
            }

            var mappings = await _materialMappingManager.GetAsync(
                new[] { "LectureMaterial", "CourseQuadrant" },
                x => x.IsActive && x.CourseId == courseId);

            var activeQuadrants = await _quadrantManager.GetAsync(
                null,
                x => x.IsActive);

            var q1 = activeQuadrants.FirstOrDefault(x => x.QuadrantNumber == 1);
            var q2 = activeQuadrants.FirstOrDefault(x => x.QuadrantNumber == 2);

            var items = mappings
                .OrderBy(x => x.Id)
                .Select(x =>
                {
                    var materialType = x.LectureMaterial?.MaterialType ?? "Document";
                    var normalizedType = materialType.Trim().ToLowerInvariant();

                    var effectiveQuadrant = x.CourseQuadrant;
                    var effectiveQuadrantId = x.CourseQuadrantId;

                    if ((normalizedType == "video" ||
                         normalizedType == "videourl" ||
                         normalizedType == "url" ||
                         normalizedType == "link") && q2 != null)
                    {
                        effectiveQuadrant = q2;
                        effectiveQuadrantId = q2.Id;
                    }
                    else if (normalizedType == "document" && q1 != null)
                    {
                        effectiveQuadrant = q1;
                        effectiveQuadrantId = q1.Id;
                    }

                    return new
                    {
                        mappingId = x.Id,
                        title = x.LectureMaterial?.Title ?? "Untitled",
                        type = materialType,
                        quadrant = effectiveQuadrant?.Name ?? "General",
                        quadrantId = effectiveQuadrantId
                    };
                })
                .ToList();

            return Json(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignContent(
            int materialId,
            int courseId,
            int? quadrantId)
        {
            if (IsFaculty())
            {
                return Json(new
                {
                    success = false,
                    message = "You do not have permission to modify course content."
                });
            }

            if (materialId <= 0 || courseId <= 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Invalid course or content."
                });
            }

            if (!quadrantId.HasValue || quadrantId.Value <= 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Please select a valid quadrant."
                });
            }

            if (!await CanAccessCourseAsync(courseId))
            {
                return Json(new
                {
                    success = false,
                    message = "Course not found."
                });
            }

            var material = await _lectureMaterialManager.GetAsync(materialId);

            if (material == null || !material.IsActive)
            {
                return Json(new
                {
                    success = false,
                    message = "Content resource not found."
                });
            }

            var activeQuadrants = await _quadrantManager.GetAsync(
                null,
                x => x.IsActive);

            var normalizedType = (material.MaterialType ?? string.Empty)
                .Trim()
                .ToLowerInvariant();

            CourseQuadrantViewModel? quadrant;

            if (normalizedType == "video" ||
                normalizedType == "videourl" ||
                normalizedType == "url" ||
                normalizedType == "link")
            {
                quadrant = activeQuadrants
                    .FirstOrDefault(x => x.QuadrantNumber == 2);

                if (quadrant == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Q2 - Video quadrant is not configured or active."
                    });
                }
            }
            else if (normalizedType == "document")
            {
                quadrant = activeQuadrants
                    .FirstOrDefault(x => x.QuadrantNumber == 1);

                if (quadrant == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Q1 - PDF / Notes quadrant is not configured or active."
                    });
                }
            }
            else
            {
                quadrant = activeQuadrants
                    .FirstOrDefault(x => x.Id == quadrantId.Value);

                if (quadrant == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Selected quadrant is invalid."
                    });
                }
            }

            var existing = await _materialMappingManager.GetAsync(
                null,
                x =>
                    x.LectureMaterialId == materialId &&
                    x.CourseId == courseId &&
                    x.IsActive);

            if (existing.Any())
            {
                return Json(new
                {
                    success = false,
                    message = "Content is already assigned to this course."
                });
            }

            var mapping = new CourseMaterialMappingViewModel
            {
                LectureMaterialId = materialId,
                CourseId = courseId,
                CourseQuadrantId = quadrant.Id,
                AssignedBy = GetUserId(),
                AssignedOn = DateTime.Now,
                IsActive = true
            };

            var result = await _materialMappingManager.AddUpdateAsync(
                mapping,
                GetUserId());

            return Json(new
            {
                success = result,
                message = result
                    ? "Content assigned successfully."
                    : "Unable to assign content."
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnassignContent(int mappingId)
        {
            if (IsFaculty())
            {
                return Json(new
                {
                    success = false,
                    message = "You do not have permission to modify course content."
                });
            }

            if (mappingId <= 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Invalid mapping."
                });
            }

            var mapping = await _materialMappingManager.GetAsync(mappingId);

            if (mapping == null || !mapping.IsActive)
            {
                return Json(new
                {
                    success = false,
                    message = "Mapping not found."
                });
            }

            if (!await CanAccessCourseAsync(mapping.CourseId))
            {
                return Json(new
                {
                    success = false,
                    message = "Course not found."
                });
            }

            mapping.IsActive = false;

            var result = await _materialMappingManager.AddUpdateAsync(
                mapping,
                GetUserId());

            return Json(new
            {
                success = result,
                message = result
                    ? "Content removed successfully."
                    : "Unable to remove content."
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetLibraryItems(string? search)
        {
            var items = await _lectureMaterialManager.GetAsync(
                null,
                x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                items = items
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.Title) &&
                        x.Title.Contains(
                            search,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return Json(
                items
                    .OrderByDescending(x => x.Id)
                    .Select(x => new
                    {
                        id = x.Id,
                        title = x.Title ?? "Untitled",
                        type = x.MaterialType ?? "Document"
                    }));
        }

        // ---------------------------------------------------------
        // LEGACY ENDPOINTS
        // ---------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> GetMappedItems(int unitId)
        {
            var mappings = await _materialMappingManager.GetAsync(
                new[] { "LectureMaterial" },
                x => x.IsActive && x.UnitId == unitId);

            return Json(
                mappings.Select(x => new
                {
                    mappingId = x.Id,
                    title = x.LectureMaterial?.Title,
                    type = x.LectureMaterial?.MaterialType
                }));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCourse(
            string name,
            string code,
            int categoryId)
        {
            if (IsFaculty())
            {
                return Json(new { success = false });
            }

            if (string.IsNullOrWhiteSpace(name) || categoryId <= 0)
            {
                return Json(new
                {
                    success = false,
                    message = "Course name and program are required."
                });
            }

            var course = new CourseMasterViewModel
            {
                Name = name.Trim(),
                CourseCode = code?.Trim(),
                CourseCategoryId = categoryId,
                IsActive = true
            };

            var result = await _courseManager.AddUpdateAsync(
                course,
                GetUserId());

            return Json(new { success = result });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSemester(
            string name,
            int courseId)
        {
            if (IsFaculty())
            {
                return Json(new { success = false });
            }

            if (string.IsNullOrWhiteSpace(name) || courseId <= 0)
            {
                return Json(new { success = false });
            }

            var baseCourse = await _courseManager.GetAsync(courseId);

            if (baseCourse == null)
            {
                return Json(new { success = false });
            }

            string semesterName = name.Trim();

            var semester = (
                await _semesterManager.GetAsync(
                    null,
                    x => x.Name == semesterName))
                .FirstOrDefault();

            if (semester == null)
            {
                semester = new SemesterMasterViewModel
                {
                    Name = semesterName,
                    IsActive = true
                };

                await _semesterManager.AddUpdateAsync(
                    semester,
                    GetUserId());

                semester = (
                    await _semesterManager.GetAsync(
                        null,
                        x => x.Name == semesterName))
                    .FirstOrDefault();
            }

            if (semester == null)
            {
                return Json(new { success = false });
            }

            var newVariant = new CourseMasterViewModel
            {
                Name = baseCourse.Name,
                CourseCode = baseCourse.CourseCode,
                CourseCategoryId = baseCourse.CourseCategoryId,
                SemesterId = semester.Id,
                IsActive = true
            };

            var result = await _courseManager.AddUpdateAsync(
                newVariant,
                GetUserId());

            return Json(new { success = result });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSubject(
            string name,
            string? code,
            int semesterId,
            int courseId)
        {
            if (IsFaculty())
            {
                return Json(new { success = false });
            }

            if (string.IsNullOrWhiteSpace(name) ||
                semesterId <= 0 ||
                courseId <= 0)
            {
                return Json(new { success = false });
            }

            var targetCourse = await _courseManager.GetAsync(courseId);

            if (targetCourse == null)
            {
                return Json(new { success = false });
            }

            var variants = await _courseManager.GetAsync(
                null,
                x =>
                    x.Name == targetCourse.Name &&
                    x.SemesterId == semesterId);

            var variant = variants.FirstOrDefault();

            if (variant == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Semester link not found."
                });
            }

            var existingSubjects = await _subjectManager.GetAsync(
                null,
                x => x.SemesterId == semesterId);

            var subject = new SubjectMasterViewModel
            {
                Name = name.Trim(),
                SubjectCode = code?.Trim(),
                SemesterId = semesterId,
                SortOrder = existingSubjects.Any()
                    ? existingSubjects.Max(x => x.SortOrder) + 1
                    : 1,
                IsActive = true
            };

            var result = await _subjectManager.AddUpdateAsync(
                subject,
                GetUserId());

            return Json(new { success = result });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUnit(
            string name,
            string? desc,
            int subjectId)
        {
            if (IsFaculty())
            {
                return Json(new { success = false });
            }

            if (string.IsNullOrWhiteSpace(name) || subjectId <= 0)
            {
                return Json(new { success = false });
            }

            var existingUnits = await _unitManager.GetAsync(
                null,
                x => x.SubjectId == subjectId);

            var unit = new UnitMasterViewModel
            {
                Name = name.Trim(),
                Description = desc?.Trim(),
                SubjectId = subjectId,
                SortOrder = existingUnits.Any()
                    ? existingUnits.Max(x => x.SortOrder) + 1
                    : 1,
                IsActive = true
            };

            var result = await _unitManager.AddUpdateAsync(
                unit,
                GetUserId());

            return Json(new { success = result });
        }
    }
}

