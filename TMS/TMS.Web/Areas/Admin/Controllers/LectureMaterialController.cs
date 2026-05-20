using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using TMS.Models.Academics;
using TMS.Repository.Managers;
using TMS.Utilities;
using TMS.ViewModels;
using TMS.ViewModels.Academics;
using TMS.ViewModels.Masters;
using TMS.Web.Controllers;
using TMS.Web.Models;

namespace TMS.Web.Areas.Admin.Controllers
{
    [ValidateFormAccess(Common.FormDefination.LectureMaterial)]
    [Area("Admin")]
    [Authorize]
    public class LectureMaterialController : BaseController
    {
        private readonly IMasterBaseManager<LectureMaterialViewModel> _lectureMaterialManager;
        private readonly IMasterBaseManager<CourseMeetingViewModel> _meetingManager;
        private readonly IMasterManager<CourseMasterViewModel> _courseManager;
        private readonly IMasterBaseManager<CourseQuadrantViewModel> _quadrantManager;
        private readonly IMasterBaseManager<CourseFacultyMapViewModel> _courseFacultyMapManager;
        private readonly IWebHostEnvironment _env;
        private readonly ZoomSettings _zoomSettings;
        private readonly IMasterBaseManager<CourseMaterialMappingViewModel> _courseMaterialMappingManager;
        private readonly IMasterManager<SemesterMasterViewModel> _semesterManager;
        private readonly TMS.Repository.ApplicationDBContext _db;

        public LectureMaterialController(
            IMasterBaseManager<LectureMaterialViewModel> lectureMaterialManager,
            IMasterManager<CourseMasterViewModel> courseManager,
            IMasterBaseManager<CourseQuadrantViewModel> quadrantManager,
            IMasterBaseManager<CourseFacultyMapViewModel> courseFacultyMapManager,
            IWebHostEnvironment env,
            IOptions<ZoomSettings> options,
            IMasterBaseManager<CourseMeetingViewModel> meetingManager,
            IMasterBaseManager<CourseMaterialMappingViewModel> mappingManager,
            IMasterManager<SemesterMasterViewModel> semesterManager,
            TMS.Repository.ApplicationDBContext db)
        {
            _lectureMaterialManager = lectureMaterialManager;
            _courseManager = courseManager;
            _quadrantManager = quadrantManager;
            _courseFacultyMapManager = courseFacultyMapManager;
            _env = env;
            _zoomSettings = options.Value;
            _meetingManager = meetingManager;
            _courseMaterialMappingManager = mappingManager;
            _semesterManager = semesterManager;
            _db = db;
        }


        // ═══════════════════════════════════════════════════
        //           INDEX — Content Library with Global Library Tab
        // ═══════════════════════════════════════════════════

        public async Task<IActionResult> Index(int? courseId, int? categoryId, bool? globalLibrary)
        {
            ViewData["Title"] = "Content Library";

            int userId = GetUserId();
            string? userRole = User.FindFirstValue(ClaimTypes.Role);

            // 1. Fetch all courses (filtered by faculty role if needed)
            List<CourseMasterViewModel> allCourses;
            if (userRole?.Equals("Faculty", StringComparison.OrdinalIgnoreCase) == true)
            {
                var facultyMaps = await _courseFacultyMapManager.GetAsync(null, x => x.FacultyId == userId);
                var facultyCourseIds = facultyMaps.Select(m => m.CourseId).Distinct().ToList();
                var dbCourses = await _courseManager.GetAsync(new[] { "Semester", "CourseCategory" }, t => t.IsActive);
                allCourses = dbCourses.Where(c => facultyCourseIds.Contains(c.Id)).ToList();
            }
            else
            {
                allCourses = await _courseManager.GetAsync(new[] { "Semester", "CourseCategory" }, t => t.IsActive);
            }

            // 2. Fetch all material mappings (for counts)
            var allMappings = await _courseMaterialMappingManager.GetAsync(null, x => x.IsActive);

            // 3. Extract Categories for top chips
            var categories = allCourses
                .Where(c => c.CourseCategory != null)
                .Select(c => c.CourseCategory!)
                .GroupBy(cat => cat.Id)
                .Select(g => g.First())
                .OrderBy(cat => cat.Name)
                .ToList();

            // 4. Build Course groups for sidebar (Group by Name)
            var courseGroups = allCourses
                .GroupBy(c => c.Name)
                .Select(g => new CourseGroupViewModel
                {
                    CourseName = g.Key ?? "Unknown",
                    CourseCode = g.First().CourseCode ?? "",
                    CategoryId = g.First().CourseCategoryId,
                    CourseIds = g.Select(c => c.Id).ToList(),
                    TotalMaterials = allMappings.Count(m => g.Select(c => c.Id).Contains(m.CourseId))
                })
                .OrderBy(c => c.CourseName)
                .ToList();

            // 5. Build Final Model
            int selectedCategoryId = categoryId ?? 0;
            
            var filteredGroups = selectedCategoryId > 0 
                ? courseGroups.Where(g => g.CategoryId == selectedCategoryId).ToList() 
                : courseGroups;

            // 6. Global Library count
            var allLibraryItems = await _lectureMaterialManager.GetAsync(null, m => m.IsActive);
            int globalLibraryCount = allLibraryItems.Count;

            bool showGlobalLibrary = globalLibrary == true;

            int selectedCourseId = 0;
            if (!showGlobalLibrary)
            {
                selectedCourseId = courseId ?? filteredGroups.FirstOrDefault()?.CourseIds.FirstOrDefault() ?? 0;
            }

            // 7. Build selected course board (grouped by Semester)
            var quadrants = await _quadrantManager.GetAsync(null, q => q.IsActive);
            CourseMaterialGroupedViewModel? selectedCourseBoard = null;
            if (!showGlobalLibrary && selectedCourseId > 0)
            {
                var targetCourse = allCourses.FirstOrDefault(c => c.Id == selectedCourseId);
                if (targetCourse != null)
                {
                    var relatedCourses = allCourses
                        .Where(c => c.Name == targetCourse.Name)
                        .ToList();

                    selectedCourseBoard = await BuildMultiSemesterBoardAsync(targetCourse.Name, relatedCourses, quadrants.ToList());
                }
            }

            var model = new ContentLibraryPageViewModel
            {
                CourseGroups = filteredGroups,
                Categories = categories,
                SelectedCourseId = selectedCourseId,
                SelectedCategoryId = selectedCategoryId,
                SelectedCourse = selectedCourseBoard,
                AllQuadrants = quadrants.OrderBy(q => q.QuadrantNumber).ToList(),
                ShowGlobalLibrary = showGlobalLibrary,
                GlobalLibraryCount = globalLibraryCount
            };

            await SetDropdowns(new LectureMaterialViewModel());
            return View(model);
        }

        // ═══════════════════════════════════════════════════
        //           AJAX: Get Course Board
        // ═══════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> GetCourseBoard(int courseId)
        {
            var targetCourse = await _courseManager.GetAsync(courseId);
            if (targetCourse == null) return PartialView("_CourseBoard", null);

            var relatedCourses = await _courseManager.GetAsync(new[] { "Semester" }, t => t.Name == targetCourse.Name && t.IsActive);
            var quadrants = await _quadrantManager.GetAsync(null, q => q.IsActive);

            var board = await BuildMultiSemesterBoardAsync(targetCourse.Name, relatedCourses, quadrants.OrderBy(q => q.QuadrantNumber).ToList());
            return PartialView("_CourseBoard", board);
        }

        // ═══════════════════════════════════════════════════
        //           AJAX: Get Global Library Items (partial)
        // ═══════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> GetGlobalLibraryBoard(string? search, string? sourceFilter, string? typeFilter)
        {
            int userId = GetUserId();
            string? userRole = User.FindFirstValue(ClaimTypes.Role);

            var allItems = await _lectureMaterialManager.GetAsync(null, m => m.IsActive);

            // Faculty can see all library items (for discovery), but only assign to their courses
            if (!string.IsNullOrEmpty(search))
                allItems = allItems.Where(m => m.Title != null && m.Title.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

            if (!string.IsNullOrEmpty(sourceFilter) && sourceFilter != "All")
                allItems = allItems.Where(m => m.Source == sourceFilter).ToList();

            if (!string.IsNullOrEmpty(typeFilter) && typeFilter != "All")
                allItems = allItems.Where(m => m.MaterialType == typeFilter).ToList();

            // Enrich with mapping counts and names for summary
            var allMappings = await _courseMaterialMappingManager.GetAsync(new[] { "Course", "Course.Semester" }, x => x.IsActive);

            var libraryItems = allItems
                .OrderByDescending(m => m.Id)
                .Select(m => {
                    var mappings = allMappings.Where(x => x.LectureMaterialId == m.Id).ToList();
                    var summary = string.Empty;
                    if (mappings.Any())
                    {
                        var courses = mappings.Select(x => x.Course?.Name).Where(n => n != null).Distinct().Take(2).ToList();
                        summary = "Assigned to: " + string.Join(", ", courses);
                        if (mappings.Select(x => x.CourseId).Distinct().Count() > 2) summary += "...";
                    }

                    return new GlobalLibraryItemViewModel
                    {
                        Id = m.Id,
                        Title = m.Title ?? "Untitled",
                        MaterialType = m.MaterialType ?? "Document",
                        Source = m.Source ?? "Local",
                        FilePath = m.FilePath,
                        OriginalFileName = m.OriginalFileName,
                        FileSizeBytes = m.FileSizeBytes,
                        UploadedOn = m.UploadedOn,
                        MappedCourseCount = mappings.Count,
                        AssignmentSummary = summary,
                        EncryptedId = EncriptorUtility.Encrypt(m.Id.ToString(), true)
                    };
                })
                .ToList();

            return PartialView("_GlobalLibraryBoard", libraryItems);
        }

        // ═══════════════════════════════════════════════════
        //           AJAX: Quick Assign from Library
        // ═══════════════════════════════════════════════════

        [HttpPost]
        public async Task<IActionResult> QuickAssign(int materialId, int courseId, int? quadrantId)
        {
            int userId = GetUserId();

            // Fetch course to get its SemesterId (subjects are usually linked to course/semester)
            var course = await _courseManager.GetAsync(courseId, new[] { "Semester" });
            if (course == null) return Json(new { success = false, message = "Course not found." });

            // Check duplicate
            var existing = await _courseMaterialMappingManager.GetAsync(null,
                x => x.LectureMaterialId == materialId && x.CourseId == courseId && x.IsActive);
            if (existing.Any())
                return Json(new { success = false, message = "Content already assigned to this course." });

            var mapping = new CourseMaterialMappingViewModel
            {
                LectureMaterialId = materialId,
                CourseId = courseId,
                CourseQuadrantId = quadrantId ?? 0,
                AssignedBy = userId,
                AssignedOn = DateTime.Now,
                CreatedBy = userId,
                CreatedOn = DateTime.Now,
                IsActive = true,
                // Automatically legacy link to course's default semester/subject if possible
                // Or we can leave them null for now as per minimal requirement
            };

            var res = await _courseMaterialMappingManager.AddUpdateAsync(mapping, userId);
            return Json(new { success = res, message = res ? "Assigned successfully!" : "Failed to assign." });
        }

        [HttpGet]
        public async Task<IActionResult> GetAssignments(int materialId)
        {
            var mappings = await _courseMaterialMappingManager.GetAsync(
                new[] { "Course", "Course.Semester", "CourseQuadrant" },
                x => x.LectureMaterialId == materialId && x.IsActive
            );

            // Fetch names for CreatedBy/AssignedBy users if needed, or use Faculty/Admin titles
            var result = mappings.Select(m => new ContentAssignmentDetailViewModel
            {
                MappingId = m.Id,
                CourseName = m.Course?.Name ?? "Unknown",
                CourseCode = m.Course?.CourseCode ?? "N/A",
                SemesterName = m.Course?.Semester?.Name ?? "General",
                SubjectName = "Core Content", // Placeholder or fetch actual Subject link if exists
                AssignedBy = "Admin/Faculty", // Can be extended with user manager
                AssignedDate = m.AssignedOn
            }).ToList();

            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> RemoveAssignment(int mappingId)
        {
            var res = await _courseMaterialMappingManager.DeleteAsync(mappingId);
            return Json(new { success = res, message = res ? "Assignment removed." : "Failed to remove." });
        }

        // ═══════════════════════════════════════════════════
        //           AJAX: Get Courses for Quick-Assign Modal
        // ═══════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> GetAssignableCourses()
        {
            int userId = GetUserId();
            string? userRole = User.FindFirstValue(ClaimTypes.Role);

            List<CourseMasterViewModel> courses;
            if (userRole?.Equals("Faculty", StringComparison.OrdinalIgnoreCase) == true)
            {
                var mappings = await _courseFacultyMapManager.GetAsync(null, x => x.FacultyId == userId);
                var courseIds = mappings.Select(m => m.CourseId).Distinct().ToList();
                var all = await _courseManager.GetAsync(new[] { "Semester" }, t => t.IsActive);
                courses = all.Where(c => courseIds.Contains(c.Id)).ToList();
            }
            else
            {
                courses = await _courseManager.GetAsync(new[] { "Semester" }, t => t.IsActive);
            }

            var quadrants = await _quadrantManager.GetAsync(null, q => q.IsActive);

            return Json(new
            {
                courses = courses.Select(c => new { id = c.Id, name = c.Name, semester = c.Semester?.Name ?? "General" }),
                quadrants = quadrants.OrderBy(q => q.QuadrantNumber).Select(q => new { id = q.Id, name = q.Name })
            });
        }


        // ═══════════════════════════════════════════════════
        //           ITEM — Upload/Edit (GET)
        // ═══════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> Item(string iId)
        {
            LectureMaterialViewModel model = new();

            if (!string.IsNullOrEmpty(iId))
            {
                iId = iId.Decrypt(true);
                if (string.IsNullOrWhiteSpace(iId))
                    return NotFound();
                int materialId = Convert.ToInt32(iId);
                var existing = await _lectureMaterialManager.GetAsync(materialId, new[] { "Course", "CourseQuadrant" });

                if (existing != null)
                    model = existing;
                if (model.MaterialType == "VideoURL" && !string.IsNullOrEmpty(model.FilePath))
                {
                    model.ContentPath = model.FilePath;
                }

                // Populate SelectedCourseIds for the multi-select
                var mappings = await _courseMaterialMappingManager.GetAsync(null, x => x.LectureMaterialId == model.Id && x.IsActive);
                model.SelectedCourseIds = mappings.Select(x => x.CourseId).ToList();
                model.AssignToCourse = model.SelectedCourseIds.Any();

                if (mappings.Any())
                {
                    var first = mappings.First();
                    model.SubjectId = first.SubjectId;
                    model.UnitId = first.UnitId;
                    if (first.SubjectId.HasValue)
                    {
                        var subject = await _db.SubjectMasters.FindAsync(first.SubjectId.Value);
                        model.SemesterId = subject?.SemesterId;
                    }
                }
            }
            model.SelectedCourseIds ??= new List<int>();

            await SetDropdowns(model);
            return View(model);
        }


        // ═══════════════════════════════════════════════════
        //           ITEM — Upload/Edit (POST)
        // ═══════════════════════════════════════════════════

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Item(LectureMaterialViewModel model, IFormFile? uploadFile)
        {
            int userId = GetUserId();

            model.SelectedCourseIds ??= new List<int>();

            // ─── COURSE ASSIGNMENT LOGIC ───
            if (model.AssignToCourse && model.SelectedCourseIds.Any())
            {
                model.CourseId = model.SelectedCourseIds.First();
                ModelState.Remove("CourseId");
            }
            else if (model.AssignToCourse && !model.SelectedCourseIds.Any())
            {
                ModelState.AddModelError("SelectedCourseIds", "Please select at least one course or uncheck 'Assign to Course'.");
                await SetDropdowns(model);
                return View(model);
            }
            else
            {
                // Library-only: no course required
                model.CourseId = null;
                model.CourseQuadrantId = null;
                ModelState.Remove("CourseId");
                ModelState.Remove("CourseQuadrantId");
            }

            // ─── SOURCE-BASED FILE HANDLING ───
            if (model.Source == "GoogleDrive" || model.Source == "OneDrive")
            {
                // Cloud files: FilePath is a URL/link, ExternalFileId stores cloud ID
                if (string.IsNullOrWhiteSpace(model.FilePath) && string.IsNullOrWhiteSpace(model.ExternalFileId))
                {
                    ModelState.AddModelError("FilePath", "Please select a file from cloud drive.");
                    await SetDropdowns(model);
                    return View(model);
                }
                // FilePath is set from the cloud picker JS on the client side
                ModelState.Remove("FilePath");
            }
            else if (model.MaterialType == "Video" || model.MaterialType == "Document")
            {
                // ─── LOCAL FILE UPLOAD ───
                if (uploadFile != null && uploadFile.Length > 0)
                {
                    const long maxFileSize = 100 * 1024 * 1024;
                    if (uploadFile.Length > maxFileSize)
                    {
                        ModelState.AddModelError("FilePath", "File size cannot exceed 100MB.");
                        await SetDropdowns(model);
                        return View(model);
                    }

                    var allowedExtensions = new[] { ".pdf", ".mp4", ".docx", ".pptx", ".jpg", ".png", ".xlsx", ".ppt", ".doc" };
                    string fileExt = Path.GetExtension(uploadFile.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(fileExt))
                    {
                        ModelState.AddModelError("FilePath", "Invalid file type. Allowed: pdf, mp4, docx, pptx, jpg, png, xlsx, ppt, doc");
                        await SetDropdowns(model);
                        return View(model);
                    }

                    string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "lectures");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    // ─── DEDUPLICATION LOGIC ───
                    string fileHash = string.Empty;
                    using (var md5 = MD5.Create())
                    {
                        using (var stream = uploadFile.OpenReadStream())
                        {
                            var hashBytes = md5.ComputeHash(stream);
                            fileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                        }
                    }

                    var existingItem = (await _lectureMaterialManager.GetAsync(null, m => m.FileHash == fileHash && m.IsActive)).FirstOrDefault();

                    if (existingItem != null)
                    {
                        model.FilePath = existingItem.FilePath;
                        model.FileHash = fileHash;
                    }
                    else
                    {
                        string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(uploadFile.FileName)}";
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await uploadFile.CopyToAsync(fileStream);
                        }
                        model.FilePath = $"/uploads/lectures/{uniqueFileName}";
                        model.FileHash = fileHash;
                    }

                    // Track file metadata
                    model.OriginalFileName = uploadFile.FileName;
                    model.FileSizeBytes = uploadFile.Length;
                    model.MimeType = uploadFile.ContentType;
                    model.Source = "Local";
                }
                else if (model.Id == 0)
                {
                    ModelState.AddModelError("FilePath", "Please upload a file.");
                    await SetDropdowns(model);
                    return View(model);
                }
            }
            else if (model.MaterialType == "VideoURL")
            {
                if (string.IsNullOrWhiteSpace(model.ContentPath))
                {
                    ModelState.AddModelError("FilePath", "Please enter a valid video URL.");
                    await SetDropdowns(model);
                    return View(model);
                }
                model.FilePath = model.ContentPath.Trim();
                model.Source = "Local"; // URLs are still "Local" source
            }

            // Save SelectedCourseIds BEFORE model gets replaced
            var selectedCourseIds = model.SelectedCourseIds?.ToList() ?? new List<int>();
            int? savedQuadrantId = model.CourseQuadrantId;
            bool assignToCourse = model.AssignToCourse;

            // ─── SAVE LectureMaterial to Central Library ───
            var saveResult = await _lectureMaterialManager.AddUpdateResultAsync(model, userId);

            if (saveResult.Result)
            {
                model = saveResult.Model;
                model.SelectedCourseIds = selectedCourseIds;
                model.CourseQuadrantId = savedQuadrantId;

                // ─── SYNC MULTI-MAPPINGS (only if assigning) ───
                // 1. Delete all existing mappings for this material
                var oldMappings = await _courseMaterialMappingManager.GetAsync(null, x => x.LectureMaterialId == model.Id);
                if (oldMappings != null && oldMappings.Any())
                {
                    foreach (var old in oldMappings)
                    {
                        await _courseMaterialMappingManager.DeleteAsync(old.Id);
                    }
                }

                // 2. Insert new mappings only if user chose to assign
                if (assignToCourse && selectedCourseIds.Any())
                {
                    foreach (var cId in selectedCourseIds.Distinct())
                    {
                        var mapping = new CourseMaterialMappingViewModel
                        {
                            LectureMaterialId = model.Id,
                            CourseId = cId,
                            CourseQuadrantId = model.CourseQuadrantId ?? 0,
                            SubjectId = model.SubjectId,
                            UnitId = model.UnitId,
                            AssignedBy = userId,
                            AssignedOn = DateTime.Now,
                            CreatedBy = userId,
                            CreatedOn = DateTime.Now,
                            IsActive = true
                        };

                        await _courseMaterialMappingManager.AddUpdateAsync(mapping, userId);
                    }
                }

                TempData["Success"] = assignToCourse
                    ? "Content saved and assigned to course(s) successfully."
                    : "Content saved to Central Library successfully.";
                SetApplicationResult(true, TempData["Success"]?.ToString() ?? "Saved");
                return RedirectToAction(nameof(Index));
            }

            SetApplicationResult(false, "Error while saving data.");
            await SetDropdowns(model);
            return View(model);
        }


        // ═══════════════════════════════════════════════════
        //           HELPER: Build Multi-Semester Board
        // ═══════════════════════════════════════════════════

        private async Task<CourseMaterialGroupedViewModel?> BuildMultiSemesterBoardAsync(string? courseName, List<CourseMasterViewModel> courses, List<CourseQuadrantViewModel> quadrants)
        {
            if (!courses.Any()) return null;

            var courseIds = courses.Select(c => c.Id).ToList();
            var mappings = await _courseMaterialMappingManager.GetAsync(
                new[] { "LectureMaterial", "CourseQuadrant", "Course" },
                x => x.IsActive
            );

            var filteredMappings = mappings.Where(x => courseIds.Contains(x.CourseId)).ToList();

            var board = new CourseMaterialGroupedViewModel
            {
                Course = courses.First(),
                GroupedMaterials = new List<QuadrantMaterialGroupViewModel>()
            };

            foreach (var q in quadrants)
            {
                var group = new QuadrantMaterialGroupViewModel
                {
                    QuadrantId = q.Id,
                    QuadrantName = q.Name ?? $"Quadrant {q.QuadrantNumber}",
                    Materials = filteredMappings
                        .Where(m => m.CourseQuadrantId == q.Id && m.LectureMaterial != null)
                        .Select(m => {
                            var lm = m.LectureMaterial!;
                            lm.CourseQuadrant = m.CourseQuadrant;
                            lm.Course = m.Course;
                            return lm;
                        }).ToList()
                };
                board.GroupedMaterials.Add(group);
            }

            return board;
        }


        // ═══════════════════════════════════════════════════
        //           MEETING SCHEDULING
        // ═══════════════════════════════════════════════════

        [HttpPost]
        public async Task<IActionResult> ScheduleMeeting(int courseId, int quadrantId, DateTime startTime, string MeetingTitle, int duration)
        {
            var zoomService = new TMS.Utilities.ZoomService(_zoomSettings);

            var meetingUrl = await zoomService.CreateMeetingAsync(
                $"Course {MeetingTitle} - Virtual Session",
                startTime,
                duration
            );

            var meeting = new CourseMeetingViewModel
            {
                MeetingTitle = MeetingTitle,
                CourseId = courseId,
                CourseQuadrantId = quadrantId,
                ScheduledAt = startTime,
                MeetingUrl = meetingUrl,
                CreatedBy = GetUserId()
            };

            var res = await _meetingManager.AddUpdateAsync(meeting, GetUserId());

            SetApplicationResult(true, "Virtual Class scheduled successfully.");
            return RedirectToAction("Index");
        }

        // ═══════════════════════════════════════════════════
        //           HELPER: Set Dropdowns
        // ═══════════════════════════════════════════════════

        private async Task SetDropdowns(LectureMaterialViewModel model)
        {
            var allCourses = await _courseManager.GetAsync(null, t => t.IsActive);
            ViewBag.CourseId = allCourses;


            var quadrants = await _quadrantManager.GetAsync(
                null, 
                t => t.IsActive || t.Id == (model.CourseQuadrantId ?? 0)
            );
            ViewBag.QuadrantId = quadrants.OrderBy(q => q.QuadrantNumber).ToList();

            var semesters = await _semesterManager.GetAsync(null, t => t.IsActive);
            ViewBag.SemesterId = semesters;
        }


        // ═══════════════════════════════════════════════════
        //           MEETING DETAILS
        // ═══════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> GetMeetingDetails(int courseId, int quadrantId)
        {
            var meetings = await _meetingManager.GetAsync(
                null,
                m => m.CourseId == courseId && m.CourseQuadrantId == quadrantId && m.IsActive
            );

            if (meetings != null && meetings.Any())
            {
                var result = meetings
                    .OrderByDescending(m => m.ScheduledAt)
                    .Select(m => new
                    {
                        id = m.Id,
                        title = m.MeetingTitle,
                        startTime = m.ScheduledAt.ToString("yyyy-MM-ddTHH:mm"),
                        duration = m.Duration,
                        joinUrl = m.MeetingUrl
                    })
                    .ToList();

                return Json(new
                {
                    hasMeeting = true,
                    meetings = result
                });
            }

            return Json(new { hasMeeting = false });
        }
    }
}
