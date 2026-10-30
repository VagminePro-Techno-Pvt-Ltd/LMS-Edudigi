using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.Models.Academics;
using TMS.Repository;
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
        private readonly ApplicationDBContext _db;
        private readonly TMS.Web.Services.ContentSync.IContentFolderProvisioner _provisioner;

        public CourseStructureController(
            IMasterManager<CourseMasterViewModel> courseManager,
            IMasterManager<SemesterMasterViewModel> semesterManager,
            IMasterManager<CourseCategoryMasterViewModel> courseCategoryManager,
            IMasterManager<UnitMasterViewModel> unitManager,
            IMasterManager<SubjectMasterViewModel> subjectManager,
            IMasterBaseManager<CourseFacultyMapViewModel> facultyMapManager,
            IMasterBaseManager<CourseMaterialMappingViewModel> materialMappingManager,
            IMasterBaseManager<LectureMaterialViewModel> lectureMaterialManager,
            IMasterBaseManager<CourseQuadrantViewModel> quadrantManager,
            ApplicationDBContext db,
            TMS.Web.Services.ContentSync.IContentFolderProvisioner provisioner)
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
            _db = db;
            _provisioner = provisioner;
        }

        public async Task<IActionResult> Index(int? programId, string? view = null)
        {
            ViewData["Title"] = "Academic Hub - Programs & Courses";

            int userId = GetUserId();
            bool isFaculty = IsFacultyRole();

            var programs = await _courseCategoryManager.GetAsync(
                null,
                x => x.IsActive);

            List<CourseMasterViewModel> allCourses;

            if (isFaculty)
            {
                var facultyMappings = await _facultyMapManager.GetAsync(
                    null,
                    x => x.FacultyId == userId && x.IsActive);

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

                var mappedProgramIds = allCourses
                    .Where(x => x.CourseCategoryId > 0)
                    .Select(x => x.CourseCategoryId)
                    .Distinct()
                    .ToHashSet();

                programs = programs
                    .Where(x => mappedProgramIds.Contains(x.Id))
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

            foreach (var program in orderedPrograms)
            {
                if (!program.NoOfSemester.HasValue || program.NoOfSemester.Value <= 0)
                {
                    var actualSemesterCount = allCourses
                        .Where(x =>
                            x.CourseCategoryId == program.Id &&
                            x.SemesterId.HasValue)
                        .Select(x => x.SemesterId!.Value)
                        .Distinct()
                        .Count();

                    program.NoOfSemester = actualSemesterCount > 0 ? actualSemesterCount : 2;
                }
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

            var allSemesters = await _semesterManager.GetAsync(
                null,
                x => x.IsActive);

            var model = new CurriculumBuilderPageViewModel
            {
                Programs = orderedPrograms,
                SelectedProgramId = selectedProgramId,
                SelectedProgramStructure = selectedStructure,
                AllQuadrants = quadrants
                    .OrderBy(x => x.QuadrantNumber)
                    .ToList(),
                AllCourses = allCourses
                    .OrderBy(x => x.CourseCategory?.Name ?? "")
                    .ThenBy(x => x.Semester?.Name ?? "")
                    .ThenBy(x => x.Name)
                    .ToList(),
                AllSemesters = allSemesters
                    .OrderBy(x => x.Name)
                    .ToList(),
                ActiveView = string.IsNullOrWhiteSpace(view) ? "tree" : view.ToLower()
            };

            ViewBag.IsFaculty = isFaculty;
            try
            {
                ViewBag.ContentSyncSummary = await _provisioner.GetStatusSummaryAsync();
                ViewBag.LocalTree = await _provisioner.GetFolderHierarchyAsync("LocalDrive");
                ViewBag.GDriveTree = await _provisioner.GetFolderHierarchyAsync("GoogleDrive");
            }
            catch
            {
                // Graceful fallback if background migrator is in progress
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetCourseFolderSyncDetails(int courseId)
        {
            if (courseId <= 0) return BadRequest("Invalid course ID");
            var details = await _provisioner.GetCourseFolderSyncDetailsAsync(courseId);
            return Json(details);
        }

        [HttpGet]
        public async Task<IActionResult> GetProgramCoursesSyncStatus(int programId)
        {
            if (programId <= 0) return BadRequest("Invalid program ID");
            var statuses = await _provisioner.GetProgramCoursesSyncStatusAsync(programId);
            return Json(statuses);
        }

        [HttpPost]
        public async Task<IActionResult> ProvisionCourseNow(int courseId)
        {
            if (courseId <= 0) return BadRequest("Invalid course ID");
            var updated = await _provisioner.ProvisionCourseNowAsync(courseId);
            return Json(updated);
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
            return IsFacultyRole();
        }

        private async Task<List<SemesterMasterViewModel>> EnsureAndGetProgramSemestersAsync(int? configuredSemesters)
        {
            int semCount = configuredSemesters ?? 0;
            if (semCount <= 0) semCount = 2;
            semCount = Math.Min(semCount, 12);

            var allSemesters = await _semesterManager.GetAsync(null, t => t.IsActive);
            var result = new List<SemesterMasterViewModel>();
            int userId = GetUserId();

            for (int i = 1; i <= semCount; i++)
            {
                string semName = $"Semester {i}";
                var existing = allSemesters.FirstOrDefault(s =>
                    s.Name != null &&
                    s.Name.Trim().Equals(semName, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    result.Add(existing);
                    continue;
                }

                var newSem = new SemesterMasterViewModel
                {
                    Name = semName,
                    IsActive = true
                };

                await _semesterManager.AddUpdateAsync(newSem, userId);

                var created = (await _semesterManager.GetAsync(
                    null,
                    s => s.Name == semName && s.IsActive))
                    .FirstOrDefault();

                if (created != null)
                {
                    result.Add(created);
                }
            }

            return result;
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

            var definedSemesters = await EnsureAndGetProgramSemestersAsync(program.NoOfSemester);
            var semesterGroups = new List<SemesterCourseGroupNode>();

            foreach (var sem in definedSemesters)
            {
                var semCourses = programCourses
                    .Where(x => x.SemesterId == sem.Id)
                    .OrderBy(x => x.CourseCode)
                    .ThenBy(x => x.Name)
                    .Select(course => new CourseNode
                    {
                        CourseId = course.Id,
                        CourseName = string.IsNullOrWhiteSpace(course.Name) ? "Untitled Course" : course.Name,
                        CourseCode = course.CourseCode ?? string.Empty,
                        Credits = course.NoofCredit > 0 ? course.NoofCredit : 4,
                        DurationMonths = course.CourseDurationMonths.GetValueOrDefault() > 0 ? course.CourseDurationMonths.Value : 6,
                        Description = course.Description,
                        SemesterId = course.SemesterId,
                        ContentCount = allMappings.Count(x => x.CourseId == course.Id)
                    })
                    .ToList();

                semesterGroups.Add(new SemesterCourseGroupNode
                {
                    SemesterId = sem.Id,
                    SemesterName = sem.Name,
                    Courses = semCourses
                });
            }

            var definedSemIds = definedSemesters.Select(x => x.Id).ToHashSet();
            var extraSemesterCourses = programCourses
                .Where(x => x.SemesterId.HasValue && !definedSemIds.Contains(x.SemesterId.Value) && x.Semester != null)
                .GroupBy(x => new { SemesterId = x.SemesterId!.Value, SemesterName = x.Semester!.Name })
                .OrderBy(x => x.Key.SemesterId);

            foreach (var group in extraSemesterCourses)
            {
                semesterGroups.Add(new SemesterCourseGroupNode
                {
                    SemesterId = group.Key.SemesterId,
                    SemesterName = string.IsNullOrWhiteSpace(group.Key.SemesterName) ? $"Semester {group.Key.SemesterId}" : group.Key.SemesterName,
                    Courses = group.OrderBy(x => x.CourseCode).ThenBy(x => x.Name).Select(course => new CourseNode
                    {
                        CourseId = course.Id,
                        CourseName = string.IsNullOrWhiteSpace(course.Name) ? "Untitled Course" : course.Name,
                        CourseCode = course.CourseCode ?? string.Empty,
                        Credits = course.NoofCredit > 0 ? course.NoofCredit : 4,
                        DurationMonths = course.CourseDurationMonths.GetValueOrDefault() > 0 ? course.CourseDurationMonths.Value : 6,
                        Description = course.Description,
                        SemesterId = course.SemesterId,
                        ContentCount = allMappings.Count(x => x.CourseId == course.Id)
                    }).ToList()
                });
            }

            var unassignedCourses = programCourses
                .Where(x => !x.SemesterId.HasValue || x.SemesterId == 0)
                .OrderBy(x => x.CourseCode)
                .ThenBy(x => x.Name)
                .ToList();

            if (unassignedCourses.Any())
            {
                semesterGroups.Add(new SemesterCourseGroupNode
                {
                    SemesterId = 0,
                    SemesterName = "General / Unassigned",
                    Courses = unassignedCourses.Select(course => new CourseNode
                    {
                        CourseId = course.Id,
                        CourseName = string.IsNullOrWhiteSpace(course.Name) ? "Untitled Course" : course.Name,
                        CourseCode = course.CourseCode ?? string.Empty,
                        Credits = course.NoofCredit > 0 ? course.NoofCredit : 4,
                        DurationMonths = course.CourseDurationMonths.GetValueOrDefault() > 0 ? course.CourseDurationMonths.Value : 6,
                        Description = course.Description,
                        SemesterId = course.SemesterId,
                        ContentCount = allMappings.Count(x => x.CourseId == course.Id)
                    }).ToList()
                });
            }

            return new ProgramStructureViewModel
            {
                ProgramId = program.Id,
                ProgramName = program.Name ?? "Unknown Program",
                ProgramDescription = program.Description,
                NoOfSemester = Math.Max(program.NoOfSemester ?? 0, definedSemesters.Count),
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
        // CURRICULUM BUILDER MANAGEMENT ENDPOINTS
        // ---------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProgram(string name, int? noOfSemester, string? description)
        {
            if (IsFaculty())
            {
                return Json(new { success = false, message = "Only administrators can create programs." });
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return Json(new { success = false, message = "Program Name is required." });
            }

            string cleanName = name.Trim();
            var existing = await _courseCategoryManager.GetAsync(null, x => x.Name.ToLower() == cleanName.ToLower() && x.IsActive);
            if (existing.Any())
            {
                return Json(new { success = false, message = "A program with this name already exists." });
            }

            int semCount = Math.Max(1, Math.Min(noOfSemester ?? 6, 12));

            var program = new CourseCategoryMasterViewModel
            {
                Name = cleanName,
                NoOfSemester = semCount,
                Description = description?.Trim(),
                IsActive = true
            };

            bool saved = await _courseCategoryManager.AddUpdateAsync(program, GetUserId());
            if (!saved)
            {
                return Json(new { success = false, message = "Failed to create program." });
            }

            var created = (await _courseCategoryManager.GetAsync(null, x => x.Name == cleanName && x.IsActive)).FirstOrDefault();
            if (created != null)
            {
                await EnsureAndGetProgramSemestersAsync(semCount);
                await _provisioner.EnqueueProvisioningAsync("Program", created.Id, "CreateOrUpdate");
            }

            return Json(new
            {
                success = true,
                programId = created?.Id ?? 0,
                message = "Program created successfully."
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProgram(int programId, string name, int? noOfSemester, string? description)
        {
            if (IsFaculty())
            {
                return Json(new { success = false, message = "Only administrators can update programs." });
            }

            if (programId <= 0 || string.IsNullOrWhiteSpace(name))
            {
                return Json(new { success = false, message = "Valid Program ID and Name are required." });
            }

            var program = await _courseCategoryManager.GetAsync(programId);
            if (program == null || !program.IsActive)
            {
                return Json(new { success = false, message = "Program not found." });
            }

            string cleanName = name.Trim();
            var duplicate = await _courseCategoryManager.GetAsync(null, x => x.Id != programId && x.Name.ToLower() == cleanName.ToLower() && x.IsActive);
            if (duplicate.Any())
            {
                return Json(new { success = false, message = "Another program with this name already exists." });
            }

            int semCount = Math.Max(1, Math.Min(noOfSemester ?? program.NoOfSemester ?? 6, 12));

            program.Name = cleanName;
            program.NoOfSemester = semCount;
            program.Description = description?.Trim();

            bool saved = await _courseCategoryManager.AddUpdateAsync(program, GetUserId());
            if (!saved)
            {
                return Json(new { success = false, message = "Failed to update program." });
            }

            await EnsureAndGetProgramSemestersAsync(semCount);
            await _provisioner.EnqueueProvisioningAsync("Program", program.Id, "Rename");

            return Json(new
            {
                success = true,
                programId = program.Id,
                message = "Program updated successfully."
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProgram(int programId)
        {
            if (IsFaculty())
            {
                return Json(new { success = false, message = "Only administrators can delete programs." });
            }

            if (programId <= 0)
            {
                return Json(new { success = false, message = "Invalid program ID." });
            }

            var program = await _courseCategoryManager.GetAsync(programId);
            if (program == null || !program.IsActive)
            {
                return Json(new { success = false, message = "Program not found." });
            }

            var hasCourses = await _courseManager.CheckExpression(x => x.CourseCategoryId == programId && x.IsActive);
            if (hasCourses)
            {
                return Json(new { success = false, message = "Cannot delete program because it has active courses assigned. Please remove or move the courses first." });
            }

            program.IsActive = false;
            bool result = await _courseCategoryManager.AddUpdateAsync(program, GetUserId());
            return Json(new
            {
                success = result,
                message = result ? "Program removed successfully." : "Failed to remove program."
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickAddCourse(int courseCategoryId, int semesterId, List<CourseMasterViewModel>? courses)
        {
            if (IsFaculty())
            {
                return Json(new { success = false, message = "Only administrators can add courses." });
            }

            if (courseCategoryId <= 0)
            {
                return Json(new { success = false, message = "Please select a Program." });
            }

            if (semesterId <= 0)
            {
                return Json(new { success = false, message = "Please select a Semester." });
            }

            if (courses == null || courses.Count == 0)
            {
                return Json(new { success = false, message = "Please add at least one course." });
            }

            var cleanCourses = courses
                .Where(x => !string.IsNullOrWhiteSpace(x.CourseCode) || !string.IsNullOrWhiteSpace(x.Name))
                .ToList();

            if (cleanCourses.Count == 0)
            {
                return Json(new { success = false, message = "Please enter Course Code and Course Title." });
            }

            var requestCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var course in cleanCourses)
            {
                course.Id = 0;
                course.CourseCategoryId = courseCategoryId;
                course.SemesterId = semesterId;
                course.CourseCode = course.CourseCode?.Trim();
                course.Name = course.Name?.Trim();
                course.Description = course.Description?.Trim();
                course.NoofCredit = course.NoofCredit > 0 ? course.NoofCredit : 4;
                course.CourseDurationMonths = course.CourseDurationMonths > 0 ? course.CourseDurationMonths : 6;
                course.IsActive = true;

                if (string.IsNullOrWhiteSpace(course.CourseCode))
                    return Json(new { success = false, message = "Course Code is required for every course." });

                if (string.IsNullOrWhiteSpace(course.Name))
                    return Json(new { success = false, message = $"Course Title is required for {course.CourseCode}." });

                if (!requestCodes.Add(course.CourseCode))
                    return Json(new { success = false, message = $"Duplicate Course Code '{course.CourseCode}' in this form." });

                var codeExists = await _courseManager.CheckExpression(t =>
                    t.CourseCode != null &&
                    t.CourseCode == course.CourseCode);

                if (codeExists)
                    return Json(new { success = false, message = $"Course Code '{course.CourseCode}' is already in use." });
            }

            foreach (var course in cleanCourses)
            {
                var saved = await _courseManager.AddUpdateAsync(course, GetUserId());
                if (!saved)
                {
                    return Json(new { success = false, message = $"Failed to save course '{course.CourseCode}'." });
                }

                if (course.Id > 0)
                {
                    await _provisioner.EnqueueProvisioningAsync("Course", course.Id, "CreateOrUpdate");
                }
            }

            return Json(new
            {
                success = true,
                message = $"{cleanCourses.Count} course{(cleanCourses.Count == 1 ? "" : "s")} added successfully."
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCourse(CourseMasterViewModel model)
        {
            if (IsFaculty())
            {
                return Json(new { success = false, message = "Only administrators can edit courses." });
            }

            if (model.Id <= 0)
            {
                return Json(new { success = false, message = "Course not found." });
            }

            var existing = await _courseManager.GetAsync(model.Id);
            if (existing == null || !existing.IsActive)
            {
                return Json(new { success = false, message = "Course not found." });
            }

            model.CourseCode = model.CourseCode?.Trim();
            model.Name = model.Name?.Trim();
            model.Description = model.Description?.Trim();
            model.NoofCredit = model.NoofCredit > 0 ? model.NoofCredit : 4;
            model.CourseDurationMonths = model.CourseDurationMonths > 0 ? model.CourseDurationMonths : 6;
            model.IsActive = true;

            if (string.IsNullOrWhiteSpace(model.CourseCode))
                return Json(new { success = false, message = "Course Code is required." });

            if (string.IsNullOrWhiteSpace(model.Name))
                return Json(new { success = false, message = "Course Title is required." });

            var codeExists = await _courseManager.CheckExpression(t =>
                t.Id != model.Id &&
                t.CourseCode != null &&
                t.CourseCode == model.CourseCode);

            if (codeExists)
                return Json(new { success = false, message = $"Course Code '{model.CourseCode}' is already in use." });

            bool saved = await _courseManager.AddUpdateAsync(model, GetUserId());
            if (saved && model.Id > 0)
            {
                await _provisioner.EnqueueProvisioningAsync("Course", model.Id, "RenameOrMove");
            }

            return Json(new
            {
                success = saved,
                message = saved ? "Course updated successfully." : "Failed to update course."
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourse(int courseId)
        {
            if (IsFaculty())
            {
                return Json(new { success = false, message = "Only administrators can delete courses." });
            }

            if (courseId <= 0)
            {
                return Json(new { success = false, message = "Invalid course." });
            }

            var course = await _courseManager.GetAsync(courseId);
            if (course == null || !course.IsActive)
            {
                return Json(new { success = false, message = "Course not found." });
            }

            course.IsActive = false;
            bool result = await _courseManager.AddUpdateAsync(course, GetUserId());
            return Json(new
            {
                success = result,
                message = result ? "Course removed successfully." : "Failed to remove course."
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetCourseDetails(int courseId)
        {
            if (!await CanAccessCourseAsync(courseId))
            {
                return Forbid();
            }

            var course = await _courseManager.GetAsync(courseId);
            if (course == null)
            {
                return NotFound();
            }

            return Json(new
            {
                id = course.Id,
                name = course.Name,
                courseCode = course.CourseCode,
                courseCategoryId = course.CourseCategoryId,
                semesterId = course.SemesterId,
                noofCredit = course.NoofCredit > 0 ? course.NoofCredit : 4,
                courseDurationMonths = course.CourseDurationMonths > 0 ? course.CourseDurationMonths : 6,
                description = course.Description
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetProgramSemesters(int programId)
        {
            if (programId <= 0) return Json(new List<object>());

            var program = await _courseCategoryManager.GetAsync(programId);
            if (program == null) return Json(new List<object>());

            var semesters = await EnsureAndGetProgramSemestersAsync(program.NoOfSemester);
            return Json(semesters.Select(s => new { id = s.Id, name = s.Name }));
        }

        [HttpGet]
        public async Task<IActionResult> GetCourseFullDetails(int courseId)
        {
            if (!await CanAccessCourseAsync(courseId))
            {
                return Forbid();
            }

            var course = await _courseManager.GetAsync(courseId);
            if (course == null)
            {
                return NotFound();
            }

            var dto = new CourseFullStructureDto
            {
                CourseId = course.Id,
                ProgramId = course.CourseCategoryId,
                SemesterId = course.SemesterId ?? 0,
                CourseCode = course.CourseCode ?? string.Empty,
                CourseTitle = course.Name ?? string.Empty,
                Description = course.Description,
                Credits = course.NoofCredit > 0 ? course.NoofCredit : 4,
                DurationMonths = course.CourseDurationMonths.GetValueOrDefault() > 0 ? course.CourseDurationMonths.Value : 6,
                Modules = new List<ModuleStructureDto>()
            };

            var subject = await _db.SubjectMasters
                .FirstOrDefaultAsync(s => s.SemesterId == course.SemesterId && (s.SubjectCode == course.CourseCode || s.Name == course.Name) && s.IsActive);

            if (subject != null)
            {
                var units = await _db.UnitMasters
                    .Where(u => u.SubjectId == subject.Id && u.IsActive)
                    .OrderBy(u => u.SortOrder)
                    .ToListAsync();

                var unitIds = units.Select(u => u.Id).ToList();
                var topics = await _db.TopicMasters
                    .Where(t => unitIds.Contains(t.UnitId) && t.IsActive)
                    .OrderBy(t => t.SortOrder)
                    .ToListAsync();

                foreach (var unit in units)
                {
                    var moduleDto = new ModuleStructureDto
                    {
                        Id = unit.Id,
                        Name = unit.Name ?? string.Empty,
                        SortOrder = unit.SortOrder,
                        Topics = topics
                            .Where(t => t.UnitId == unit.Id)
                            .Select(t => new TopicStructureDto
                            {
                                Id = t.Id,
                                Name = t.Name ?? string.Empty,
                                SortOrder = t.SortOrder
                            })
                            .ToList()
                    };
                    dto.Modules.Add(moduleDto);
                }
            }

            return Json(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCourseFullStructure([FromBody] CourseFullStructureDto model)
        {
            if (IsFaculty())
            {
                return Json(new { success = false, message = "Only administrators can create or edit courses." });
            }

            if (model.ProgramId <= 0)
                return Json(new { success = false, message = "Please select a Program." });

            if (model.SemesterId <= 0)
                return Json(new { success = false, message = "Please select a Target Semester." });

            if (string.IsNullOrWhiteSpace(model.CourseCode))
                return Json(new { success = false, message = "Course Code is required." });

            if (string.IsNullOrWhiteSpace(model.CourseTitle))
                return Json(new { success = false, message = "Full Course Title is required." });

            model.CourseCode = model.CourseCode.Trim();
            model.CourseTitle = model.CourseTitle.Trim();
            model.Description = model.Description?.Trim();
            model.Credits = model.Credits > 0 ? model.Credits : 4;
            model.DurationMonths = model.DurationMonths > 0 ? model.DurationMonths : 6;

            int userId = GetUserId();

            var codeExists = await _courseManager.CheckExpression(t =>
                t.Id != model.CourseId &&
                t.CourseCode != null &&
                t.CourseCode == model.CourseCode);

            if (codeExists)
            {
                return Json(new { success = false, message = $"Course Code '{model.CourseCode}' is already in use." });
            }

            CourseMasterViewModel course;
            if (model.CourseId > 0)
            {
                course = await _courseManager.GetAsync(model.CourseId);
                if (course == null || !course.IsActive)
                {
                    return Json(new { success = false, message = "Course not found." });
                }
                course.CourseCategoryId = model.ProgramId;
                course.SemesterId = model.SemesterId;
                course.CourseCode = model.CourseCode;
                course.Name = model.CourseTitle;
                course.Description = model.Description;
                course.NoofCredit = model.Credits;
                course.CourseDurationMonths = model.DurationMonths;
                await _courseManager.AddUpdateAsync(course, userId);
            }
            else
            {
                course = new CourseMasterViewModel
                {
                    CourseCategoryId = model.ProgramId,
                    SemesterId = model.SemesterId,
                    CourseCode = model.CourseCode,
                    Name = model.CourseTitle,
                    Description = model.Description,
                    NoofCredit = model.Credits,
                    CourseDurationMonths = model.DurationMonths,
                    IsActive = true
                };
                bool saved = await _courseManager.AddUpdateAsync(course, userId);
                if (!saved)
                {
                    return Json(new { success = false, message = "Failed to save course." });
                }

                var created = (await _courseManager.GetAsync(null, x => x.CourseCode == model.CourseCode && x.IsActive)).FirstOrDefault();
                if (created != null)
                {
                    model.CourseId = created.Id;
                }
            }

            if (model.CourseId > 0)
            {
                await _provisioner.EnqueueProvisioningAsync("Course", model.CourseId, "CreateOrUpdate");
            }

            // Sync SubjectMaster, UnitMasters, TopicMasters
            var subject = await _db.SubjectMasters
                .FirstOrDefaultAsync(s => s.SemesterId == model.SemesterId && (s.SubjectCode == model.CourseCode || s.Name == model.CourseTitle) && s.IsActive);

            if (subject == null)
            {
                subject = new SubjectMaster
                {
                    SemesterId = model.SemesterId,
                    SubjectCode = model.CourseCode,
                    Name = model.CourseTitle,
                    Description = model.Description,
                    SortOrder = 1,
                    IsActive = true,
                    CreatedBy = userId,
                    CreatedOn = DateTime.Now
                };
                _db.SubjectMasters.Add(subject);
                await _db.SaveChangesAsync();
            }
            else
            {
                subject.SubjectCode = model.CourseCode;
                subject.Name = model.CourseTitle;
                subject.Description = model.Description;
                subject.UpdatedBy = userId;
                subject.UpdatedOn = DateTime.Now;
                await _db.SaveChangesAsync();
            }

            if (model.Modules != null)
            {
                var existingUnits = await _db.UnitMasters
                    .Where(u => u.SubjectId == subject.Id && u.IsActive)
                    .ToListAsync();

                int moduleOrder = 1;
                foreach (var mod in model.Modules.Where(m => !string.IsNullOrWhiteSpace(m.Name)))
                {
                    var cleanModName = mod.Name.Trim();
                    var unit = existingUnits.FirstOrDefault(u => (mod.Id > 0 && u.Id == mod.Id) || u.Name.Equals(cleanModName, StringComparison.OrdinalIgnoreCase));

                    if (unit == null)
                    {
                        unit = new UnitMaster
                        {
                            SubjectId = subject.Id,
                            Name = cleanModName,
                            SortOrder = moduleOrder++,
                            IsActive = true,
                            CreatedBy = userId,
                            CreatedOn = DateTime.Now
                        };
                        _db.UnitMasters.Add(unit);
                        await _db.SaveChangesAsync();
                    }
                    else
                    {
                        unit.Name = cleanModName;
                        unit.SortOrder = moduleOrder++;
                        unit.UpdatedBy = userId;
                        unit.UpdatedOn = DateTime.Now;
                        await _db.SaveChangesAsync();
                    }

                    if (mod.Topics != null)
                    {
                        var existingTopics = await _db.TopicMasters
                            .Where(t => t.UnitId == unit.Id && t.IsActive)
                            .ToListAsync();

                        int topicOrder = 1;
                        foreach (var top in mod.Topics.Where(t => !string.IsNullOrWhiteSpace(t.Name)))
                        {
                            var cleanTopName = top.Name.Trim();
                            var topic = existingTopics.FirstOrDefault(t => (top.Id > 0 && t.Id == top.Id) || (t.Name != null && t.Name.Equals(cleanTopName, StringComparison.OrdinalIgnoreCase)));

                            if (topic == null)
                            {
                                topic = new TopicMaster
                                {
                                    UnitId = unit.Id,
                                    Name = cleanTopName,
                                    SortOrder = topicOrder++,
                                    IsActive = true,
                                    CreatedBy = userId,
                                    CreatedOn = DateTime.Now
                                };
                                _db.TopicMasters.Add(topic);
                            }
                            else
                            {
                                topic.Name = cleanTopName;
                                topic.SortOrder = topicOrder++;
                                topic.UpdatedBy = userId;
                                topic.UpdatedOn = DateTime.Now;
                            }
                        }
                        await _db.SaveChangesAsync();
                    }
                }
            }

            return Json(new
            {
                success = true,
                courseId = model.CourseId,
                message = "Course and structure saved successfully."
            });
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

            if (result && course.Id > 0)
            {
                await _provisioner.EnqueueProvisioningAsync("Course", course.Id, "CreateOrUpdate");
            }

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

            await _provisioner.EnqueueProvisioningAsync("Semester", semester.Id, "CreateOrUpdate");

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

            if (result && newVariant.Id > 0)
            {
                await _provisioner.EnqueueProvisioningAsync("Course", newVariant.Id, "CreateOrUpdate");
            }

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

