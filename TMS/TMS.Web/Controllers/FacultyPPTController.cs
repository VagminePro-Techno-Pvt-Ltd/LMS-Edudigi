using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TMS.Repository;
using TMS.ViewModels.Academics;

namespace TMS.Web.Controllers
{
    /// <summary>
    /// Faculty Interactive PPT Management controller.
    ///
    /// Cascade: Program → Course → Unit → Topic
    ///   Program  (CourseCategoryMaster)
    ///     → Course  (CourseMaster by CourseCategoryId)
    ///       → Unit   (UnitMaster via Course.SemesterId → SubjectMasters → Units)
    ///         → Topic (TopicMaster by UnitId)
    /// </summary>
    [Authorize]
    public class FacultyPPTController : BaseController
    {
        private readonly ApplicationDBContext _db;
        private const string AgentDebugLogPath = @"C:\Users\shiva\Downloads\LMS-New 3\debug-62920a.log";

        public FacultyPPTController(ApplicationDBContext db)
        {
            _db = db;
        }

        private static void AgentLog(string runId, string hypothesisId, string location, string message, object data)
        {
            try
            {
                var payload = new
                {
                    sessionId = "62920a",
                    runId,
                    hypothesisId,
                    location,
                    message,
                    data,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
                System.IO.File.AppendAllText(AgentDebugLogPath, JsonSerializer.Serialize(payload) + Environment.NewLine);
            }
            catch
            {
                // no-op for debug logging failures
            }
        }

        // ── GET: /FacultyPPT/Index ───────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var facultyUserId = GetUserId();
            #region agent log
            AgentLog("pre-fix", "H3", "FacultyPPTController.Index", "Index entered for faculty list retrieval", new { facultyUserId });
            #endregion
            if (facultyUserId <= 0)
            {
                SetApplicationResult(false, "User not authenticated.");
                return RedirectToAction("Login", "Auth");
            }

            // Fetch all PPTs uploaded by this faculty
            var ppts = await _db.InteractivePPTs
                .Include(p => p.Course)
                    .ThenInclude(c => c.CourseCategory)
                .Include(p => p.Unit)
                .Where(p => p.UploadedBy == facultyUserId)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new InteractivePPTListViewModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description,
                    FilePath = p.FilePath,
                    OriginalFileName = p.OriginalFileName,
                    FileSizeBytes = null,
                    ProgramName = p.Course.CourseCategory != null ? p.Course.CourseCategory.Name : "N/A",
                    CourseName = p.Course.Name,
                    UnitName = p.Unit.Name,
                    TopicName = string.Empty,
                    ProgramId = p.Course.CourseCategoryId,
                    CourseId = p.CourseId,
                    UnitId = p.UnitId,
                    TopicId = p.TopicId ?? 0,
                    CreatedAt = p.CreatedAt,
                    UploadedBy = p.UploadedBy ?? 0,
                    IsActive = p.IsActive
                })
                .ToListAsync();

            var topicIds = ppts.Where(p => p.TopicId > 0).Select(p => p.TopicId).Distinct().ToList();
            var topicLookup = topicIds.Any()
                ? await _db.TopicLookups.Where(t => topicIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Name)
                : new Dictionary<int, string>();
            foreach (var row in ppts)
            {
                row.TopicName = row.TopicId > 0 && topicLookup.TryGetValue(row.TopicId, out var name) ? name : "N/A";
            }

            return View(ppts);
        }

        // ── GET: /FacultyPPT/View/{id} ───────────────────────────────
        [HttpGet]
        public async Task<IActionResult> View(int id)
        {
            var facultyUserId = GetUserId();
            var ppt = await _db.InteractivePPTs
                .Include(p => p.Course)
                    .ThenInclude(c => c.CourseCategory)
                .Include(p => p.Unit)
                .FirstOrDefaultAsync(p => p.Id == id && p.UploadedBy == facultyUserId);

            if (ppt == null)
            {
                SetApplicationResult(false, "PPT not found or access denied.");
                return RedirectToAction(nameof(Index));
            }

            // Return file for viewing/download
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", ppt.FilePath.TrimStart('/'));
            
            if (!System.IO.File.Exists(filePath))
            {
                SetApplicationResult(false, "File not found on server.");
                return RedirectToAction(nameof(Index));
            }

            var contentType = ppt.FilePath.EndsWith(".pptx") 
                ? "application/vnd.openxmlformats-officedocument.presentationml.presentation"
                : "application/vnd.ms-powerpoint";

            return PhysicalFile(filePath, contentType, ppt.OriginalFileName ?? "presentation.pptx");
        }

        // ── GET: /FacultyPPT/Edit/{id} ───────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var facultyUserId = GetUserId();
            var ppt = await _db.InteractivePPTs
                .Include(p => p.Course)
                    .ThenInclude(c => c.CourseCategory)
                .FirstOrDefaultAsync(p => p.Id == id && p.UploadedBy == facultyUserId);

            if (ppt == null)
            {
                SetApplicationResult(false, "PPT not found or access denied.");
                return RedirectToAction(nameof(Index));
            }

            var model = new InteractivePPTEditViewModel
            {
                Id = ppt.Id,
                ProgramId = ppt.Course.CourseCategoryId,
                CourseId = ppt.CourseId,
                UnitId = ppt.UnitId,
                TopicId = ppt.TopicId ?? 0,
                Title = ppt.Title,
                Description = ppt.Description,
                ExistingFilePath = ppt.FilePath,
                ExistingFileName = ppt.OriginalFileName
            };

            // Load dropdowns
            await LoadEditDropdowns(model);

            return View(model);
        }

        // ── POST: /FacultyPPT/Edit/{id} ──────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(InteractivePPTEditViewModel model)
        {
            try
            {
                #region agent log
                AgentLog("pre-fix", "H2", "FacultyPPTController.Edit", "Edit entered with inbound model", new
                {
                    model.Id,
                    model.ProgramId,
                    model.CourseId,
                    model.UnitId,
                    model.TopicId,
                    hasFile = model.PPTFile != null,
                    model.ExistingFilePath
                });
                #endregion
                if (!ModelState.IsValid)
                {
                    await LoadEditDropdowns(model);
                    return View(model);
                }

                var facultyUserId = GetUserId();
                var ppt = await _db.InteractivePPTs
                    .FirstOrDefaultAsync(p => p.Id == model.Id && p.UploadedBy == facultyUserId);

                if (ppt == null)
                {
                    SetApplicationResult(false, "PPT not found or access denied.");
                    return RedirectToAction(nameof(Index));
                }

                // Update basic fields
                ppt.CourseId = model.CourseId;
                
                // Get correct SubjectId from Unit
                var unit = await _db.UnitMasters
                    .Where(u => u.Id == model.UnitId)
                    .Select(u => new { u.SubjectId })
                    .FirstOrDefaultAsync();
                
                if (unit == null)
                {
                    SetApplicationResult(false, $"Unit (ID: {model.UnitId}) not found.");
                    return RedirectToAction(nameof(Index));
                }
                
                ppt.SubjectId = unit.SubjectId;  // Use SubjectId from Unit
                ppt.UnitId = model.UnitId;
                ppt.TopicId = model.TopicId > 0 ? model.TopicId : null;
                ppt.Title = model.Title;
                ppt.Description = model.Description;
                ppt.UpdatedAt = DateTime.UtcNow;
                #region agent log
                AgentLog("pre-fix", "H2", "FacultyPPTController.Edit", "Edit entity prepared before save", new
                {
                    ppt.Id,
                    ppt.CourseId,
                    ppt.SubjectId,
                    ppt.UnitId,
                    ppt.TopicId,
                    ppt.UpdatedAt
                });
                #endregion

                // Handle file replacement if new file uploaded
                if (model.PPTFile != null && model.PPTFile.Length > 0)
                {
                    var ext = Path.GetExtension(model.PPTFile.FileName).ToLowerInvariant();
                    if (ext != ".ppt" && ext != ".pptx")
                    {
                        ModelState.AddModelError("PPTFile", "Only .ppt and .pptx files are accepted.");
                        await LoadEditDropdowns(model);
                        return View(model);
                    }

                    const long MaxBytes = 50L * 1024 * 1024;
                    if (model.PPTFile.Length > MaxBytes)
                    {
                        ModelState.AddModelError("PPTFile", "File exceeds the 50 MB limit.");
                        await LoadEditDropdowns(model);
                        return View(model);
                    }

                    // Save new file
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "ppts");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = $"{Guid.NewGuid()}{ext}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.PPTFile.CopyToAsync(stream);
                    }

                    // Update file info
                    ppt.FilePath = $"/uploads/ppts/{uniqueFileName}";
                    ppt.OriginalFileName = model.PPTFile.FileName;

                    // Optional: Delete old file (commented out for safety)
                    // var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", model.ExistingFilePath.TrimStart('/'));
                    // if (System.IO.File.Exists(oldFilePath)) System.IO.File.Delete(oldFilePath);
                }

                await _db.SaveChangesAsync();
                #region agent log
                AgentLog("pre-fix", "H2", "FacultyPPTController.Edit", "Edit save completed", new { ppt.Id, ppt.UpdatedAt });
                #endregion

                SetApplicationResult(true, "PPT updated successfully!");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                #region agent log
                AgentLog("pre-fix", "H2", "FacultyPPTController.Edit", "Edit failed with exception", new
                {
                    ex.Message,
                    exceptionType = ex.GetType().FullName
                });
                #endregion
                SetApplicationResult(false, $"Error updating PPT: {ex.Message}");
                await LoadEditDropdowns(model);
                return View(model);
            }
        }

        // ── POST: /FacultyPPT/Delete/{id} ────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                #region agent log
                AgentLog("pre-fix", "H4", "FacultyPPTController.Delete", "Delete entered", new { id });
                #endregion
                var facultyUserId = GetUserId();
                var ppt = await _db.InteractivePPTs
                    .FirstOrDefaultAsync(p => p.Id == id && p.UploadedBy == facultyUserId);

                if (ppt == null)
                {
                    return Json(new { success = false, message = "PPT not found or access denied." });
                }

                // Soft delete - set IsActive to false
                ppt.IsActive = false;
                ppt.UpdatedAt = DateTime.UtcNow;
                #region agent log
                AgentLog("pre-fix", "H4", "FacultyPPTController.Delete", "Delete soft-update prepared", new
                {
                    ppt.Id,
                    ppt.IsActive,
                    ppt.UpdatedAt
                });
                #endregion

                await _db.SaveChangesAsync();

                return Json(new { success = true, message = "PPT deleted successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error deleting PPT: {ex.Message}" });
            }
        }

        // ── Helper: Load Edit Dropdowns ──────────────────────────────
        private async Task LoadEditDropdowns(InteractivePPTEditViewModel model)
        {
            // Programs
            ViewBag.Programs = await _db.CourseCategoryMasters
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Name,
                    Selected = p.Id == model.ProgramId
                })
                .ToListAsync();

            // Courses
            if (model.ProgramId > 0)
            {
                ViewBag.Courses = await _db.CourseMasters
                    .Where(c => c.CourseCategoryId == model.ProgramId && c.IsActive)
                    .OrderBy(c => c.Name)
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name,
                        Selected = c.Id == model.CourseId
                    })
                    .ToListAsync();
            }
            else
            {
                ViewBag.Courses = new List<SelectListItem>();
            }

            // Units
            if (model.CourseId > 0)
            {
                ViewBag.Units = await _db.UnitMasters
                    .Where(u => u.SubjectId == model.CourseId && u.IsActive)
                    .OrderBy(u => u.SortOrder)
                    .ThenBy(u => u.Name)
                    .Select(u => new SelectListItem
                    {
                        Value = u.Id.ToString(),
                        Text = u.Name,
                        Selected = u.Id == model.UnitId
                    })
                    .ToListAsync();
            }
            else
            {
                ViewBag.Units = new List<SelectListItem>();
            }

            // Topics
            if (model.UnitId > 0)
            {
                ViewBag.Topics = await _db.TopicLookups
                    .Where(t => t.UnitId == model.UnitId)
                    .OrderBy(t => t.Name)
                    .Select(t => new SelectListItem
                    {
                        Value = t.Id.ToString(),
                        Text = t.Name,
                        Selected = t.Id == model.TopicId
                    })
                    .ToListAsync();
            }
            else
            {
                ViewBag.Topics = new List<SelectListItem>();
            }
        }

        // ── GET: /FacultyPPT/Upload ──────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Upload()
        {
            var programs = await _db.CourseCategoryMasters
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text  = p.Name
                })
                .ToListAsync();

            ViewBag.Programs = programs;
            return View(new InteractivePPTViewModel());
        }

        // ── POST: /FacultyPPT/Upload ─────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(InteractivePPTViewModel model)
        {
            try
            {
                #region agent log
                AgentLog("upload-fix", "H1", "FacultyPPTController.Upload", "Upload entered with inbound model", new
                {
                    model.ProgramId,
                    model.CourseId,
                    model.UnitId,
                    model.TopicId,
                    hasTitle = !string.IsNullOrWhiteSpace(model.Title),
                    hasFile = model.PPTFile != null,
                    fileName = model.PPTFile?.FileName,
                    fileSize = model.PPTFile?.Length
                });
                #endregion

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .FirstOrDefault();
                    #region agent log
                    AgentLog("upload-fix", "H1", "FacultyPPTController.Upload", "ModelState validation failed", new { errors });
                    #endregion
                    return Json(new { success = false, message = errors ?? "Validation failed." });
                }

                var file = model.PPTFile;
                if (file == null || file.Length == 0)
                    return Json(new { success = false, message = "No file received." });

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (ext != ".ppt" && ext != ".pptx")
                    return Json(new { success = false, message = "Only .ppt and .pptx files are accepted." });

                const long MaxBytes = 50L * 1024 * 1024;
                if (file.Length > MaxBytes)
                    return Json(new { success = false, message = "File exceeds the 50 MB limit." });

                // ── Get logged-in faculty user ID ───
                var facultyUserId = GetUserId();
                if (facultyUserId <= 0)
                    return Json(new { success = false, message = "User not authenticated." });

                #region agent log
                AgentLog("upload-fix", "H1", "FacultyPPTController.Upload", "Faculty user authenticated", new { facultyUserId });
                #endregion

                // ── Verify foreign key relationships exist ───
                var courseExists = await _db.CourseMasters.AnyAsync(c => c.Id == model.CourseId && c.IsActive);
                var unitExists = await _db.UnitMasters.AnyAsync(u => u.Id == model.UnitId && u.IsActive);
                var topicExists = model.TopicId > 0 
                    ? await _db.TopicLookups.AnyAsync(t => t.Id == model.TopicId)
                    : true;

                #region agent log
                AgentLog("upload-fix", "H1", "FacultyPPTController.Upload", "Foreign key validation", new
                {
                    model.CourseId,
                    courseExists,
                    model.UnitId,
                    unitExists,
                    model.TopicId,
                    topicExists
                });
                #endregion

                if (!courseExists)
                    return Json(new { success = false, message = $"Selected course (ID: {model.CourseId}) does not exist or is inactive." });

                if (!unitExists)
                    return Json(new { success = false, message = $"Selected unit (ID: {model.UnitId}) does not exist or is inactive." });

                if (!topicExists)
                    return Json(new { success = false, message = $"Selected topic (ID: {model.TopicId}) does not exist." });

                // ── Get the correct SubjectId from the selected Unit ───
                // Unit is already linked to the correct Subject, so use Unit's SubjectId
                var unit = await _db.UnitMasters
                    .Where(u => u.Id == model.UnitId)
                    .Select(u => new { u.SubjectId })
                    .FirstOrDefaultAsync();

                if (unit == null)
                    return Json(new { success = false, message = $"Unit (ID: {model.UnitId}) not found." });

                var subjectId = unit.SubjectId;

                #region agent log
                AgentLog("upload-fix", "H1", "FacultyPPTController.Upload", "SubjectId resolved from Unit", new
                {
                    model.UnitId,
                    subjectId,
                    model.CourseId
                });
                #endregion

                // ── Create upload directory if not exists ───
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "ppts");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // ── Generate unique filename ───
                var uniqueFileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // ── Save file to disk ───
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                #region agent log
                AgentLog("upload-fix", "H1", "FacultyPPTController.Upload", "File saved to disk", new
                {
                    uniqueFileName,
                    filePath,
                    fileSize = file.Length
                });
                #endregion

                // ── Create database entity ───
                var interactivePPT = new TMS.Models.Academics.InteractivePPT
                {
                    CourseId = model.CourseId,
                    SubjectId = subjectId,  // Use SubjectId from Unit (not CourseId)
                    UnitId = model.UnitId,
                    TopicId = model.TopicId > 0 ? model.TopicId : null,
                    Title = model.Title?.Trim() ?? string.Empty,
                    Description = model.Description?.Trim(),
                    FilePath = $"/uploads/ppts/{uniqueFileName}",
                    OriginalFileName = file.FileName,
                    UploadedBy = facultyUserId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    UpdatedAt = null
                };

                #region agent log
                AgentLog("upload-fix", "H1", "FacultyPPTController.Upload", "Entity prepared before DB insert", new
                {
                    interactivePPT.CourseId,
                    interactivePPT.SubjectId,
                    interactivePPT.UnitId,
                    interactivePPT.TopicId,
                    interactivePPT.Title,
                    titleLength = interactivePPT.Title?.Length ?? 0,
                    interactivePPT.Description,
                    descriptionLength = interactivePPT.Description?.Length ?? 0,
                    interactivePPT.FilePath,
                    filePathLength = interactivePPT.FilePath?.Length ?? 0,
                    interactivePPT.OriginalFileName,
                    originalFileNameLength = interactivePPT.OriginalFileName?.Length ?? 0,
                    interactivePPT.UploadedBy,
                    interactivePPT.CreatedAt,
                    interactivePPT.UpdatedAt,
                    interactivePPT.IsActive
                });
                #endregion

                // ── Save to database with detailed error handling ───
                try
                {
                    _db.InteractivePPTs.Add(interactivePPT);
                    await _db.SaveChangesAsync();

                    #region agent log
                    AgentLog("upload-fix", "H1", "FacultyPPTController.Upload", "Upload save completed successfully", new
                    {
                        interactivePPT.Id,
                        interactivePPT.CourseId,
                        interactivePPT.UnitId,
                        interactivePPT.TopicId
                    });
                    #endregion

                    return Json(new
                    {
                        success = true,
                        message = "PPT uploaded successfully!",
                        pptId = interactivePPT.Id
                    });
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
                {
                    // Get the most detailed error message
                    var innerException = dbEx.InnerException;
                    var detailedMessage = dbEx.Message;
                    
                    while (innerException != null)
                    {
                        detailedMessage = innerException.Message;
                        innerException = innerException.InnerException;
                    }

                    #region agent log
                    AgentLog("upload-fix", "H1", "FacultyPPTController.Upload", "Database update exception", new
                    {
                        exceptionType = dbEx.GetType().FullName,
                        message = dbEx.Message,
                        innerMessage = dbEx.InnerException?.Message,
                        deepestMessage = detailedMessage,
                        stackTrace = dbEx.StackTrace
                    });
                    #endregion

                    // Delete the uploaded file since DB insert failed
                    try
                    {
                        if (System.IO.File.Exists(filePath))
                            System.IO.File.Delete(filePath);
                    }
                    catch { /* Ignore file deletion errors */ }

                    return Json(new
                    {
                        success = false,
                        message = $"Database error: {detailedMessage}"
                    });
                }
            }
            catch (Exception ex)
            {
                #region agent log
                AgentLog("upload-fix", "H1", "FacultyPPTController.Upload", "Upload failed with general exception", new
                {
                    exceptionType = ex.GetType().FullName,
                    message = ex.Message,
                    innerMessage = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
                #endregion

                return Json(new
                {
                    success = false,
                    message = $"An error occurred while uploading: {ex.Message}"
                });
            }
        }

        // ── AJAX 1: Courses by Program ───────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetCoursesByProgram(int programId)
        {
            if (programId <= 0) return Json(new List<object>());

            var courses = await _db.CourseMasters
                .Where(c => c.CourseCategoryId == programId && c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new { id = c.Id, name = c.Name })
                .ToListAsync();

            return Json(courses);
        }

        // ── AJAX 2: Units by Course ──────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetUnitsByCourse(int courseId)
        {
            if (courseId <= 0) return Json(new List<object>());

            #region agent log
            AgentLog("unit-dropdown-fix", "H2", "FacultyPPTController.GetUnitsByCourse", "GetUnitsByCourse called", new { courseId });
            #endregion

            // Try direct mapping first: SubjectId = CourseId
            var units = await _db.UnitMasters
                .Where(u => u.SubjectId == courseId && u.IsActive)
                .OrderBy(u => u.SortOrder)
                .ThenBy(u => u.Name)
                .Select(u => new { id = u.Id, name = u.Name })
                .ToListAsync();

            #region agent log
            AgentLog("unit-dropdown-fix", "H2", "FacultyPPTController.GetUnitsByCourse", "Direct mapping result", new { courseId, unitCount = units.Count });
            #endregion

            // If no units found with direct mapping, try semester-based mapping
            if (!units.Any())
            {
                #region agent log
                AgentLog("unit-dropdown-fix", "H2", "FacultyPPTController.GetUnitsByCourse", "Trying semester-based mapping", new { courseId });
                #endregion

                // Get course's semester
                var course = await _db.CourseMasters
                    .Where(c => c.Id == courseId)
                    .Select(c => new { c.SemesterId })
                    .FirstOrDefaultAsync();

                if (course?.SemesterId != null)
                {
                    // Get subjects for this semester
                    var subjectIds = await _db.SubjectMasters
                        .Where(s => s.SemesterId == course.SemesterId && s.IsActive)
                        .Select(s => s.Id)
                        .ToListAsync();

                    #region agent log
                    AgentLog("unit-dropdown-fix", "H2", "FacultyPPTController.GetUnitsByCourse", "Semester subjects found", new { courseId, semesterId = course.SemesterId, subjectCount = subjectIds.Count, subjectIds });
                    #endregion

                    if (subjectIds.Any())
                    {
                        // Get units for these subjects
                        units = await _db.UnitMasters
                            .Where(u => subjectIds.Contains(u.SubjectId) && u.IsActive)
                            .OrderBy(u => u.SortOrder)
                            .ThenBy(u => u.Name)
                            .Select(u => new { id = u.Id, name = u.Name })
                            .ToListAsync();

                        #region agent log
                        AgentLog("unit-dropdown-fix", "H2", "FacultyPPTController.GetUnitsByCourse", "Semester-based mapping result", new { courseId, unitCount = units.Count, units });
                        #endregion
                    }
                }
            }

            #region agent log
            AgentLog("unit-dropdown-fix", "H2", "FacultyPPTController.GetUnitsByCourse", "Final units retrieved", new { courseId, unitCount = units.Count, units });
            #endregion

            return Json(units);
        }

        // ── Debug: data chain inspector ──────────────────────────────
        [HttpGet]
        public async Task<IActionResult> DebugChain(int courseId)
        {
            var course = await _db.CourseMasters
                .Where(c => c.Id == courseId)
                .Select(c => new { c.Id, c.Name, c.SemesterId, c.IsActive, c.CourseCategoryId })
                .FirstOrDefaultAsync();

            if (course == null) return Json(new { error = "Course not found", courseId });

            // Check semester
            var semester = course.SemesterId.HasValue
                ? await _db.SemesterMasters
                    .Where(s => s.Id == course.SemesterId.Value)
                    .Select(s => new { s.Id, s.Name, s.IsActive })
                    .FirstOrDefaultAsync()
                : null;

            // Check subjects for this semester
            var subjects = course.SemesterId.HasValue
                ? await _db.SubjectMasters
                    .Where(s => s.SemesterId == course.SemesterId.Value)
                    .Select(s => new { s.Id, s.Name, s.SemesterId, s.IsActive })
                    .ToListAsync<object>()
                : new List<object>();

            // Check units for these subjects
            var subjectIds = course.SemesterId.HasValue
                ? await _db.SubjectMasters
                    .Where(s => s.SemesterId == course.SemesterId.Value && s.IsActive)
                    .Select(s => s.Id)
                    .ToListAsync()
                : new List<int>();

            var units = subjectIds.Any()
                ? await _db.UnitMasters
                    .Where(u => subjectIds.Contains(u.SubjectId))
                    .Select(u => new { u.Id, u.Name, u.SubjectId, u.IsActive })
                    .ToListAsync<object>()
                : new List<object>();

            // Also check old mapping approach
            var mappings = await _db.CourseMaterialMappings
                .Where(m => m.CourseId == courseId)
                .Select(m => new { m.Id, m.UnitId, m.CourseId, m.IsActive })
                .ToListAsync();

            return Json(new
            {
                course,
                semester,
                subjectCount = subjects.Count,
                subjects,
                unitCount = units.Count,
                units,
                oldMappingApproach = new
                {
                    mappingCount = mappings.Count,
                    unitIdCount = mappings.Where(m => m.UnitId.HasValue).Select(m => m.UnitId).Distinct().Count()
                }
            });
        }


        // ── AJAX 3: Topics by Unit ───────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetTopicsByUnit(int unitId)
        {
            if (unitId <= 0) return Json(new List<object>());

            var topics = await _db.TopicLookups
                .Where(t => t.UnitId == unitId)
                .OrderBy(t => t.Name)
                .Select(t => new { id = t.Id, name = t.Name })
                .ToListAsync();

            return Json(topics);
        }
    }
}
