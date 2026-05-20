using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TMS.Models.Masters;
using TMS.Repository;
using TMS.Repository.Managers;
using TMS.ViewModels;
using TMS.ViewModels.Academics;
using TMS.ViewModels.Masters;

namespace TMS.Web.Controllers
{
    [System.Obsolete("Deprecated in favor of StudentCourseController, StudentUnitController, and StudentQuadrantController for unit-first learning structure.")]
    public class StudentCourseContentController : BaseController
    {
        private readonly IMasterManager<CourseMasterViewModel> _courseManager;
        private readonly IMasterBaseManager<CourseEnrollmentViewModel> _enrollmentManager;
        private readonly IMasterBaseManager<LectureMaterialViewModel> _materialManager;
        private readonly IMasterBaseManager<CourseQuadrantViewModel> _quadrantManager;
        private readonly IMasterBaseManager<StudentQuizAttemptViewModel> _attemptManager;
        private readonly IMasterBaseManager<CourseMaterialMappingViewModel> _materialMappingManager;
        private readonly ApplicationDBContext _db;
        private readonly IWebHostEnvironment _env;

        public StudentCourseContentController(
            IMasterManager<CourseMasterViewModel> courseManager,
            IMasterBaseManager<CourseEnrollmentViewModel> enrollmentManager,
            IMasterBaseManager<LectureMaterialViewModel> materialManager,
            IMasterBaseManager<CourseQuadrantViewModel> quadrantManager,
            IMasterBaseManager<StudentQuizAttemptViewModel> attemptManager,
            IMasterBaseManager<CourseMaterialMappingViewModel> materialMappingManager,
            ApplicationDBContext db,
            IWebHostEnvironment env)
        {
            _courseManager = courseManager;
            _enrollmentManager = enrollmentManager;
            _materialManager = materialManager;
            _quadrantManager = quadrantManager;
            _attemptManager = attemptManager;
            _materialMappingManager = materialMappingManager;
            _db = db;
            _env = env;
        }

        // =============================
        // GET: Index - Show enrolled courses & materials
        // =============================
        public async Task<IActionResult> Index()
        {
            int studentId = GetUserId();

            // Fetch enrolled courses with category info
            var enrollments = await _enrollmentManager.GetAsync(
                includes: new[] { "Course", "Course.CourseCategory", "Course.Semester" },
                predicate: e => e.StudentId == studentId && e.IsActive
            );

            var enrolledCourses = enrollments?
                .Select(e => e.Course)
                .Where(c => c != null)
                .ToList() ?? new List<CourseMasterViewModel>();

            if (!enrolledCourses.Any())
            {
                ViewBag.Message = "You are not enrolled in any courses.";
                return View(new StudentCourseCategoryViewModel());
            }

            // Pick first category as “main” (e.g., MBA)
            var firstCategory = enrolledCourses.First().CourseCategory;
            var quadrants = await _quadrantManager.GetAsync(null, q => q.IsActive);

            var viewModel = new StudentCourseCategoryViewModel
            {
                CategoryName = firstCategory?.Name ?? "No Category",
                CategoryDescription = firstCategory?.Description ?? "No description available.",
                Semesters = enrolledCourses
                    .GroupBy(c => c.Semester.Id)
                    .OrderBy(g => g.Key)
                    .Select(g => new SemesterCourseGroupViewModel
                    {
                        Semester = g.Key,
                        Courses = g.Select(c => new CourseSummaryViewModel
                        {
                            CourseId = c.Id,
                            CourseCode = c.CourseCode ?? "N/A",
                            CourseName = c.Name ?? "Untitled Course",
                            //CourseCredit = c.Credit,
                            CategoryId = c.CourseCategoryId
                        }).ToList()
                    }).ToList(),
                Quadrants = quadrants
                    .OrderBy(q => q.QuadrantNumber)
                    .Select(q => new CourseQuadrantViewModel
                    {
                        Id = q.Id,
                        QuadrantNumber = q.QuadrantNumber,
                        Name = q.Name
                    }).ToList()
            };
    
            return View(viewModel);
        }





        //[HttpGet]
        //public async Task<IActionResult> Details(int semesterId, int? courseId = null)
        //{
        //    // ✅ Fetch all active courses for the semester (and optionally for a specific course)
        //    var courses = await _courseManager.GetAsync(
        //        includes: new[] { "CourseCategory", "Semester" },
        //        predicate: c => c.SemesterId == semesterId && c.IsActive &&
        //                        (!courseId.HasValue || c.Id == courseId.Value)
        //    );

        //    if (courses == null || !courses.Any())
        //        return NotFound();

        //    // ✅ Fetch all lecture materials for these courses
        //    var courseIds = courses.Select(c => c.Id).ToList();

        //    // Fetch all materials for the semester, then filter locally
        //    var allMaterials = await _materialManager.GetAsync(
        //        includes: new[] { "CourseQuadrant" },
        //        predicate: m => m.IsActive
        //    );

        //    // Filter locally for matching course IDs
        //    var materials = allMaterials.Where(m => courseIds.Contains(m.CourseId)).ToList();


        //    // ✅ Group materials by course, then by unit (quadrant)
        //    var groupedCourses = courses.Select(c => new CourseMaterialGroupedViewModel
        //    {
        //        Course = c,
        //        GroupedMaterials = materials
        //            .Where(m => m.CourseId == c.Id)
        //            .GroupBy(m => m.CourseQuadrant?.Name ?? "Unassigned")
        //            .ToDictionary(g => g.Key, g => g.ToList())
        //    }).ToList();

        //    return View(groupedCourses);
        //}
        [HttpGet]
        public async Task<IActionResult> Details(int semesterId, int? courseId = null)
        {
            int studentId = GetUserId();

            var courses = await _courseManager.GetAsync(
                includes: new[] { "CourseCategory", "Semester" },
                predicate: c => c.SemesterId == semesterId && c.IsActive &&
                                (!courseId.HasValue || c.Id == courseId.Value)
            );

            if (courses == null || !courses.Any())
                return NotFound();

            var courseIds = courses.Select(c => c.Id).ToList();

            var allMappings = await _materialMappingManager.GetAsync(
                includes: new[] { "LectureMaterial", "CourseQuadrant" },
                predicate: m => m.IsActive
            );
            var mappings = allMappings.Where(m => courseIds.Contains(m.CourseId)).ToList();

            var attempts = await _attemptManager.GetAsync(null, t => t.IsActive);
            var studentAttempts = attempts
                .Where(a => a.StudentId == studentId && courseIds.Contains(a.CourseId))
                .ToList();

            var groupedCourses = courses.Select(c =>
            {
                var courseMappings = mappings.Where(m => m.CourseId == c.Id).ToList();
                var groupedMaterials = courseMappings
                    .GroupBy(m => new { QuadrantId = m.CourseQuadrantId, QuadrantName = m.CourseQuadrant != null ? m.CourseQuadrant.Name : "General" })
                    .Select(g => new QuadrantMaterialGroupViewModel
                    {
                        QuadrantId = g.Key.QuadrantId,
                        QuadrantName = g.Key.QuadrantName,
                        Materials = g.Where(m => m.LectureMaterial != null).Select(m => m.LectureMaterial!).ToList(),
                        HasAttemptedQuiz = studentAttempts.Any(a => a.CourseId == c.Id && a.CourseQuadrantId == g.Key.QuadrantId)
                    }).ToList();

                return new CourseMaterialGroupedViewModel { Course = c, GroupedMaterials = groupedMaterials };
            }).ToList();

            // Interactive PPTs (student view)
            var selectedCourseIds = groupedCourses.Select(gc => gc.Course.Id).Distinct().ToList();
            var pptRows = await _db.InteractivePPTs
                .Where(p => p.IsActive && selectedCourseIds.Contains(p.CourseId))
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    p.Id,
                    p.CourseId,
                    p.Title,
                    p.Description,
                    p.CreatedAt,
                    p.FilePath
                })
                .ToListAsync();

            foreach (var gc in groupedCourses)
            {
                gc.InteractivePPTs = pptRows
                    .Where(p => p.CourseId == gc.Course.Id)
                    .Select(p => new InteractivePPTStudentViewModel
                    {
                        Id = p.Id,
                        Title = p.Title,
                        Description = p.Description,
                        CreatedAt = p.CreatedAt,
                        FilePath = p.FilePath
                    })
                    .ToList();
            }

            // Pass quadrant data for the Learning Quadrants section (only first 4)
            var quadrants = await _quadrantManager.GetAsync(null, q => q.IsActive);
            ViewBag.AllQuadrants = quadrants.OrderBy(q => q.QuadrantNumber).Take(4).ToList();
            ViewBag.CourseId = courseId;
            ViewBag.SemesterId = semesterId;

            return View(groupedCourses);
        }

        // =============================================
        // Helper: Check if material matches quadrant type
        // =============================================
        private bool IsMatchingQuadrant(LectureMaterialViewModel material, int quadrantNumber)
        {
            var materialType = (material.MaterialType ?? "").ToLower();
            var filePath = (material.FilePath ?? "").ToLower();
            var fileExt = System.IO.Path.GetExtension(filePath).ToLower();

            switch (quadrantNumber)
            {
                case 1: // PDF / Notes - Documents only
                    return materialType == "document" 
                        || new[] { ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx", ".txt" }.Contains(fileExt);

                case 2: // Videos - Video content only
                    return materialType == "video" 
                        || materialType == "videourl"
                        || new[] { ".mp4", ".webm", ".ogg", ".avi", ".mov" }.Contains(fileExt)
                        || filePath.Contains("youtu") 
                        || filePath.Contains("vimeo");

                case 3: // Quizzes / Assignments
                    // For now, show materials marked as quiz/assignment type
                    // You can customize this based on your MaterialType values
                    return materialType.Contains("quiz") 
                        || materialType.Contains("assignment")
                        || materialType.Contains("test");

                case 4: // Discussion
                    // For now, show materials marked as discussion type
                    return materialType.Contains("discussion") 
                        || materialType.Contains("forum");

                default:
                    return true; // Show all if quadrant number is unknown
            }
        }

        // =============================================
        // GET: QuadrantContent - Shows content for a subject filtered by quadrant
        // =============================================
        [HttpGet]
        public async Task<IActionResult> QuadrantContent(int courseId, int semesterId, int quadrantId)
        {
            int studentId = GetUserId();

            var course = await _courseManager.GetAsync(courseId, new[] { "Semester", "CourseCategory" });
            if (course == null) return RedirectToAction("Index");

            var quadrant = await _quadrantManager.GetAsync(quadrantId);
            if (quadrant == null) return RedirectToAction("Details", new { semesterId, courseId });

            // Fetch ALL mappings for this course (not filtered by quadrantId)
            var allMappings = await _materialMappingManager.GetAsync(
                new[] { "LectureMaterial" },
                m => m.CourseId == courseId && m.IsActive
            );

            // Filter materials based on quadrant type (by MaterialType, not by CourseQuadrantId)
            var activeMappings = allMappings
                .Where(m => m.LectureMaterial != null && m.LectureMaterial.IsActive)
                .Where(m => IsMatchingQuadrant(m.LectureMaterial!, quadrant.QuadrantNumber))
                .OrderBy(m => m.SortOrder)
                .ToList();

            // Fetch unit names for grouping
            var unitIds = activeMappings.Where(m => m.UnitId.HasValue).Select(m => m.UnitId!.Value).Distinct().ToList();
            var unitNames = new Dictionary<int, string>();
            if (unitIds.Any())
            {
                var units = await _db.UnitMasters
                    .Where(u => unitIds.Contains(u.Id) && u.IsActive)
                    .Select(u => new { u.Id, Name = u.Name ?? "Unit" })
                    .ToListAsync();
                foreach (var u in units) unitNames[u.Id] = u.Name;
            }

            // Group by unit
            var grouped = new List<QuadrantMaterialGroupViewModel>();
            foreach (var g in activeMappings.Where(m => m.UnitId.HasValue).GroupBy(m => m.UnitId!.Value).OrderBy(g => g.Key))
            {
                grouped.Add(new QuadrantMaterialGroupViewModel
                {
                    QuadrantId = g.Key,
                    QuadrantName = unitNames.ContainsKey(g.Key) ? unitNames[g.Key] : $"Unit {g.Key}",
                    Materials = g.Select(m => m.LectureMaterial!).ToList()
                });
            }
            var noUnit = activeMappings.Where(m => !m.UnitId.HasValue).ToList();
            if (noUnit.Any())
                grouped.Add(new QuadrantMaterialGroupViewModel { QuadrantId = 0, QuadrantName = "General Content", Materials = noUnit.Select(m => m.LectureMaterial!).ToList() });

            // Quadrant display metadata
            // icon, color, bg, desc — positional tuples (Item1..Item4) for compatibility
            var qIcons  = new[] { "fa-file-pdf",       "fa-video",          "fa-circle-question",  "fa-comments"                   };
            var qColors = new[] { "#ef4444",            "#3b82f6",           "#16a34a",             "#8b5cf6"                       };
            var qBgs    = new[] { "#fef2f2",            "#eff6ff",           "#f0fdf4",             "#f5f3ff"                       };
            var qDescs  = new[] { "PDF notes & docs.",  "Video lectures.",   "Quizzes & assignments.", "Discussion forums."        };

            int qi        = Math.Max(0, Math.Min(quadrant.QuadrantNumber - 1, 3));
            ViewBag.CourseId       = courseId;
            ViewBag.SemesterId     = semesterId;
            ViewBag.CourseName     = course.Name ?? "Subject";
            ViewBag.QuadrantId     = quadrantId;
            ViewBag.QuadrantName   = quadrant.Name ?? "Quadrant";
            ViewBag.QuadrantNumber = quadrant.QuadrantNumber;
            ViewBag.QuadrantIcon   = qIcons[qi];
            ViewBag.QuadrantColor  = qColors[qi];
            ViewBag.QuadrantBg     = qBgs[qi];
            ViewBag.QuadrantDesc   = qDescs[qi];
            ViewBag.TotalItems     = activeMappings?.Count ?? 0;

            return View(grouped);
        }

        [HttpGet]
        [Route("Quadrant/FileViewer/{id}")]
        public async Task<IActionResult> FileViewer(int id)
        {
            var material = await _materialManager.GetAsync(id);
            if (material == null || !material.IsActive)
            {
                return NotFound();
            }

            // Resolve back link (could be details page, or generic quadrant page)
            var mapping = (await _materialMappingManager.GetAsync(
                includes: new[] { "Course" },
                predicate: m => m.LectureMaterialId == id && m.IsActive
            )).FirstOrDefault();

            ViewBag.CourseId = mapping?.CourseId ?? 0;
            ViewBag.SemesterId = mapping?.Course?.SemesterId ?? 0;
            ViewBag.CourseName = mapping?.Course?.Name ?? "Course";
            ViewBag.QuadrantId = mapping?.CourseQuadrantId ?? 0;

            // Determine File Type
            var filePath = material.FilePath ?? "";
            var ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
            var isCloud = material.Source == "GoogleDrive" || material.Source == "OneDrive";
            
            string viewerType = "Unsupported";
            if (isCloud)
            {
                viewerType = "Cloud";
            }
            else if (ext == ".pdf")
            {
                viewerType = "PDF";
            }
            else if (new[] { ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx" }.Contains(ext))
            {
                viewerType = "Office";
            }
            else if (new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp" }.Contains(ext))
            {
                viewerType = "Image";
            }

            ViewBag.ViewerType = viewerType;
            ViewBag.FileExtension = ext;
            
            // Absolute URL to inline file endpoint (Office Online Viewer must fetch this over HTTPS from the internet)
            var request = HttpContext.Request;
            var pathBase = request.PathBase.Value?.TrimEnd('/') ?? "";
            var previewFileUrl = $"{request.Scheme}://{request.Host}{(string.IsNullOrEmpty(pathBase) ? "" : pathBase)}/Quadrant/PreviewFile/{id}";
            ViewBag.PreviewFileUrl = previewFileUrl;
            
            // For local/intranet testing warning
            ViewBag.IsLocalhost = IsLocalOrPrivateAddress(request.Host.Host);

            return View(material);
        }

        private bool IsLocalOrPrivateAddress(string host)
        {
            if (string.IsNullOrEmpty(host) || host == "localhost" || host == "127.0.0.1" || host == "::1")
                return true;

            // Check for standard private IP ranges
            if (System.Net.IPAddress.TryParse(host, out var ip))
            {
                byte[] bytes = ip.GetAddressBytes();
                if (bytes.Length == 4) // IPv4
                {
                    if (bytes[0] == 10) return true;
                    if (bytes[0] == 192 && bytes[1] == 168) return true;
                    if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
                }
            }
            
            // Any hostname without dot might be a local machine name in intranet
            if (!host.Contains('.'))
                return true;

            return false;
        }

        [HttpGet]
        [Route("Quadrant/PreviewFile/{id}")]
        public async Task<IActionResult> PreviewFile(int id)
        {
            var material = await _materialManager.GetAsync(id);
            if (material == null || !material.IsActive || string.IsNullOrEmpty(material.FilePath))
            {
                return NotFound();
            }

            var filePath = material.FilePath;
            if (material.Source == "GoogleDrive" || material.Source == "OneDrive")
            {
                return Redirect(filePath);
            }

            var webRootPath = _env.WebRootPath;
            var relativePath = filePath.Replace("~/", "").TrimStart('/');
            var absolutePath = System.IO.Path.Combine(webRootPath, relativePath);

            if (!System.IO.File.Exists(absolutePath))
            {
                return NotFound("File not found on server.");
            }

            var contentType = GetContentType(absolutePath);
            var fileBytes = await System.IO.File.ReadAllBytesAsync(absolutePath);
            
            var cd = new System.Net.Mime.ContentDisposition
            {
                FileName = material.OriginalFileName ?? System.IO.Path.GetFileName(absolutePath),
                Inline = true
            };
            Response.Headers.Add("Content-Disposition", cd.ToString());

            return File(fileBytes, contentType);
        }

        [HttpGet]
        [Route("Quadrant/DownloadFile/{id}")]
        public async Task<IActionResult> DownloadFile(int id)
        {
            var material = await _materialManager.GetAsync(id);
            if (material == null || !material.IsActive || string.IsNullOrEmpty(material.FilePath))
            {
                return NotFound();
            }

            var filePath = material.FilePath;
            if (material.Source == "GoogleDrive" || material.Source == "OneDrive")
            {
                return Redirect(filePath);
            }

            var webRootPath = _env.WebRootPath;
            var relativePath = filePath.Replace("~/", "").TrimStart('/');
            var absolutePath = System.IO.Path.Combine(webRootPath, relativePath);

            if (!System.IO.File.Exists(absolutePath))
            {
                return NotFound("File not found on server.");
            }

            var contentType = GetContentType(absolutePath);
            var fileBytes = await System.IO.File.ReadAllBytesAsync(absolutePath);
            var fileName = material.OriginalFileName ?? System.IO.Path.GetFileName(absolutePath);

            return File(fileBytes, contentType, fileName);
        }

        private string GetContentType(string path)
        {
            var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(path, out var contentType))
            {
                contentType = "application/octet-stream";
            }
            return contentType;
        }
    }




}
