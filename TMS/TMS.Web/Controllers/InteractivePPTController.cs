using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.Models.Academics;
using TMS.Repository;
using TMS.Web.Services;

namespace TMS.Web.Controllers
{
    /// <summary>
    /// Interactive PPT Viewer + Slide Interaction Management.
    /// Completely separate from DocumentViewerController — no shared code.
    /// Routes:
    ///   GET  /InteractivePPT/View/{pptId}              — Student slide viewer
    ///   GET  /InteractivePPT/ManageInteractions/{pptId} — Faculty interaction manager
    ///   POST /InteractivePPT/AddInteraction             — Faculty: save a new mapping
    ///   POST /InteractivePPT/DeleteInteraction/{id}     — Faculty: remove a mapping
    ///   GET  /InteractivePPT/Slide/{pptId}/{num}        — Serves a single slide PNG
    ///   POST /InteractivePPT/ConvertSlides/{pptId}      — Triggers slide conversion
    /// </summary>
    [Authorize]
    public class InteractivePPTController : BaseController
    {
        private readonly ApplicationDBContext _db;
        private readonly IInteractivePPTService _pptService;
        private readonly IWebHostEnvironment _env;

        public InteractivePPTController(
            ApplicationDBContext db,
            IInteractivePPTService pptService,
            IWebHostEnvironment env)
        {
            _db = db;
            _pptService = pptService;
            _env = env;
        }

        // ══════════════════════════════════════════════════════════════
        // STUDENT: GET /InteractivePPT/View/{pptId}
        // ══════════════════════════════════════════════════════════════
        [HttpGet]
        [Route("InteractivePPT/View/{pptId:int}")]
        public async Task<IActionResult> View(int pptId)
        {
            var ppt = await _db.InteractivePPTs
                .Include(p => p.Course)
                .FirstOrDefaultAsync(p => p.Id == pptId && p.IsActive);

            if (ppt == null)
                return NotFound("PPT not found.");

            // Resolve physical path of the PPT file
            var physPath = Path.Combine(_env.WebRootPath,
                ppt.FilePath.Replace("~/", "").TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            // Run conversion (or return cached slides)
            var (slideCount, outputDir, error) = await _pptService.EnsureSlidesConvertedAsync(pptId, physPath);

            // Load all slide interactions for this PPT
            var interactions = await _db.PPTSlideInteractions
                .Where(i => i.PPTId == pptId && i.IsActive)
                .OrderBy(i => i.SlideNumber).ThenBy(i => i.DisplayOrder)
                .ToListAsync();

            ViewBag.PPTId = pptId;
            ViewBag.PPTTitle = ppt.Title;
            ViewBag.CourseName = ppt.Course?.Name ?? "Course";
            ViewBag.SlideCount = slideCount;
            ViewBag.ConversionError = error;
            ViewBag.Interactions = interactions;

            return View("~/Views/InteractivePPT/View.cshtml");
        }

        // ══════════════════════════════════════════════════════════════
        // STUDENT: GET /InteractivePPT/Slide/{pptId}/{num}
        // Streams a single slide PNG inline
        // ══════════════════════════════════════════════════════════════
        [HttpGet]
        [Route("InteractivePPT/Slide/{pptId:int}/{num:int}")]
        public IActionResult Slide(int pptId, int num)
        {
            var dir = _pptService.GetSlideOutputDir(pptId, _env.WebRootPath);
            var path = Path.Combine(dir, $"slide_{num}.png");
            if (!System.IO.File.Exists(path))
                return NotFound();

            return PhysicalFile(path, "image/png");
        }

        // ══════════════════════════════════════════════════════════════
        // AJAX: POST /InteractivePPT/ConvertSlides/{pptId}
        // Called by the viewer page when slides haven't been converted yet.
        // ══════════════════════════════════════════════════════════════
        [HttpPost]
        [Route("InteractivePPT/ConvertSlides/{pptId:int}")]
        public async Task<IActionResult> ConvertSlides(int pptId)
        {
            var ppt = await _db.InteractivePPTs
                .FirstOrDefaultAsync(p => p.Id == pptId && p.IsActive);
            if (ppt == null)
                return Json(new { success = false, message = "PPT not found." });

            var physPath = Path.Combine(_env.WebRootPath,
                ppt.FilePath.Replace("~/", "").TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            var (slideCount, _, error) = await _pptService.EnsureSlidesConvertedAsync(pptId, physPath);

            if (slideCount > 0)
                return Json(new { success = true, slideCount });

            return Json(new { success = false, message = error ?? "Conversion failed." });
        }

        // ══════════════════════════════════════════════════════════════
        // FACULTY: GET /InteractivePPT/ManageInteractions/{pptId}
        // ══════════════════════════════════════════════════════════════
        [HttpGet]
        [Route("InteractivePPT/ManageInteractions/{pptId:int}")]
        public async Task<IActionResult> ManageInteractions(int pptId)
        {
            var facultyId = GetUserId();
            var ppt = await _db.InteractivePPTs
                .Include(p => p.Course)
                .FirstOrDefaultAsync(p => p.Id == pptId && p.UploadedBy == facultyId);

            if (ppt == null)
            {
                SetApplicationResult(false, "PPT not found or access denied.");
                return RedirectToAction("Index", "FacultyPPT");
            }

            var interactions = await _db.PPTSlideInteractions
                .Where(i => i.PPTId == pptId && i.IsActive)
                .OrderBy(i => i.SlideNumber).ThenBy(i => i.DisplayOrder)
                .ToListAsync();

            // Slide count
            var slideCount = _pptService.GetExistingSlideCount(pptId, _env.WebRootPath);

            // Available LMS items for dropdowns
            var quizzes = await _db.QuizMasters
                .Where(q => q.IsActive)
                .OrderBy(q => q.Title)
                .Select(q => new { q.Id, Name = q.Title })
                .ToListAsync();

            var assignments = await _db.AssignmentMasters
                .Where(a => a.IsActive)
                .OrderBy(a => a.Title)
                .Select(a => new { a.Id, Name = a.Title })
                .ToListAsync();

            var documents = await _db.LectureMaterials
                .Where(m => m.IsActive && m.MaterialType == "Document")
                .OrderBy(m => m.Title)
                .Select(m => new { m.Id, Name = m.Title })
                .ToListAsync();

            var videos = await _db.LectureMaterials
                .Where(m => m.IsActive && (m.MaterialType == "Video" || m.MaterialType == "VideoURL"))
                .OrderBy(m => m.Title)
                .Select(m => new { m.Id, Name = m.Title })
                .ToListAsync();

            ViewBag.PPTId = pptId;
            ViewBag.PPTTitle = ppt.Title;
            ViewBag.CourseName = ppt.Course?.Name ?? "";
            ViewBag.SlideCount = slideCount;
            ViewBag.Interactions = interactions;
            ViewBag.Quizzes = quizzes;
            ViewBag.Assignments = assignments;
            ViewBag.Documents = documents;
            ViewBag.Videos = videos;

            return View("~/Views/InteractivePPT/ManageInteractions.cshtml");
        }

        // ══════════════════════════════════════════════════════════════
        // FACULTY: POST /InteractivePPT/AddInteraction
        // ══════════════════════════════════════════════════════════════
        [HttpPost]
        [Route("InteractivePPT/AddInteraction")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddInteraction(
            int pptId, int slideNumber, string itemType, int itemId,
            string buttonText, int displayOrder = 0)
        {
            var facultyId = GetUserId();
            var pptOwned = await _db.InteractivePPTs
                .AnyAsync(p => p.Id == pptId && p.UploadedBy == facultyId);
            if (!pptOwned)
                return Json(new { success = false, message = "Access denied." });

            if (slideNumber < 1 || string.IsNullOrWhiteSpace(itemType) ||
                itemId <= 0 || string.IsNullOrWhiteSpace(buttonText))
                return Json(new { success = false, message = "Invalid input." });

            var interaction = new PPTSlideInteraction
            {
                PPTId = pptId,
                SlideNumber = slideNumber,
                ItemType = itemType.Trim(),
                ItemId = itemId,
                ButtonText = buttonText.Trim(),
                DisplayOrder = displayOrder,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _db.PPTSlideInteractions.Add(interaction);
            await _db.SaveChangesAsync();

            return Json(new { success = true, id = interaction.Id });
        }

        // ══════════════════════════════════════════════════════════════
        // FACULTY: POST /InteractivePPT/DeleteInteraction/{id}
        // ══════════════════════════════════════════════════════════════
        [HttpPost]
        [Route("InteractivePPT/DeleteInteraction/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteInteraction(int id)
        {
            var facultyId = GetUserId();
            var interaction = await _db.PPTSlideInteractions
                .Include(i => i.PPT)
                .FirstOrDefaultAsync(i => i.Id == id && i.IsActive && i.PPT!.UploadedBy == facultyId);

            if (interaction == null)
                return Json(new { success = false, message = "Not found or access denied." });

            interaction.IsActive = false;
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ══════════════════════════════════════════════════════════════
        // AJAX: GET /InteractivePPT/GetInteractionsForSlide
        // Returns interactions for one slide (used by student viewer JS)
        // ══════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> GetInteractionsForSlide(int pptId, int slideNumber)
        {
            var items = await _db.PPTSlideInteractions
                .Where(i => i.PPTId == pptId && i.SlideNumber == slideNumber && i.IsActive)
                .OrderBy(i => i.DisplayOrder)
                .Select(i => new
                {
                    i.Id,
                    i.ItemType,
                    i.ItemId,
                    i.ButtonText,
                    i.DisplayOrder
                })
                .ToListAsync();

            return Json(items);
        }
    }
}
