using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.Repository;

namespace TMS.Web.Controllers
{
    [Authorize]
    public class StudentPPTController : BaseController
    {
        private readonly ApplicationDBContext _db;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;

        public StudentPPTController(ApplicationDBContext db, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int studentId = GetUserId();
            if (studentId <= 0)
            {
                SetApplicationResult(false, "User not authenticated.");
                return RedirectToAction("Login", "Auth");
            }

            // Minimal safe listing: show active PPTs uploaded for student's enrolled courses.
            var enrolledCourseIds = await _db.CourseEnrollments
                .Where(e => e.StudentId == studentId && e.IsActive)
                .Select(e => e.CourseId)
                .Distinct()
                .ToListAsync();

            var ppts = await _db.InteractivePPTs
                .Where(p => p.IsActive && enrolledCourseIds.Contains(p.CourseId))
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new StudentPPTViewModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    Description = p.Description,
                    CreatedAt = p.CreatedAt,
                    FilePath = p.FilePath
                })
                .ToListAsync();

            return View(ppts);
        }

        [HttpGet]
        public async Task<IActionResult> View(int id)
        {
            var ppt = await _db.InteractivePPTs
                .Where(p => p.Id == id && p.IsActive)
                .FirstOrDefaultAsync();

            if (ppt == null)
                return NotFound();

            var request = HttpContext.Request;
            var domain = $"{request.Scheme}://{request.Host}{request.PathBase.Value?.TrimEnd('/')}";
            
            // Clean file path
            var relativePath = (ppt.FilePath ?? "").Replace("~/", "").TrimStart('/');
            var publicPptUrl = $"{domain}/{relativePath}";
            
            // Construct Microsoft Office Online viewer embed link
            var officeViewerUrl = $"https://view.officeapps.live.com/op/embed.aspx?src={Uri.EscapeDataString(publicPptUrl)}";

            // [TELEMETRY LOGGING] - Log details to debug console
            System.Diagnostics.Debug.WriteLine("===============================================");
            System.Diagnostics.Debug.WriteLine($"[PPT DIAGNOSTIC] View Request for ID: {id}");
            System.Diagnostics.Debug.WriteLine($"[PPT DIAGNOSTIC] File Title: {ppt.Title}");
            System.Diagnostics.Debug.WriteLine($"[PPT DIAGNOSTIC] Saved FilePath: {ppt.FilePath}");
            System.Diagnostics.Debug.WriteLine($"[PPT DIAGNOSTIC] Public PPT URL: {publicPptUrl}");
            System.Diagnostics.Debug.WriteLine($"[PPT DIAGNOSTIC] Office Viewer URL: {officeViewerUrl}");

            // Validate file on physical storage
            var physPath = Path.Combine(_env.WebRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(physPath))
            {
                var fi = new FileInfo(physPath);
                System.Diagnostics.Debug.WriteLine($"[PPT DIAGNOSTIC] PHYSICAL FILE EXISTS!");
                System.Diagnostics.Debug.WriteLine($"[PPT DIAGNOSTIC] File Size: {fi.Length} bytes ({(fi.Length / 1024.0):F2} KB)");
                
                // Display stats dynamically in console
                Console.WriteLine($"[PPT DIAGNOSTIC] Loaded {ppt.Title} - Size: {fi.Length} bytes");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[PPT DIAGNOSTIC] WARNING: Physical file NOT found at path: {physPath}");
                Console.WriteLine($"[PPT DIAGNOSTIC] ERROR: File missing at: {physPath}");
            }
            System.Diagnostics.Debug.WriteLine("===============================================");

            var viewModel = new StudentPPTViewModel
            {
                Id = ppt.Id,
                Title = ppt.Title,
                Description = ppt.Description,
                CreatedAt = ppt.CreatedAt,
                FilePath = ppt.FilePath,
                PublicPptUrl = publicPptUrl,
                OfficeViewerUrl = officeViewerUrl,
                IsLocalhost = request.Host.Host.Contains("localhost") || request.Host.Host.Contains("127.0.0.1")
            };

            return View(viewModel);
        }
    }

    public class StudentPPTViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string FilePath { get; set; } = string.Empty;
        /// <summary>Absolute URL to the PPT file (same URL encoded into Office Viewer <c>src</c>).</summary>
        public string PublicPptUrl { get; set; } = string.Empty;
        public string OfficeViewerUrl { get; set; } = string.Empty;
        public bool IsLocalhost { get; set; }
    }
}

