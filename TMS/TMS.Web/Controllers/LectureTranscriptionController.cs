using System;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.Common;
using TMS.Repository;
using TMS.Models.Academics;
using TMS.ViewModels.Academics;
using TMS.Web.Services;

namespace TMS.Web.Controllers
{
    [Route("api/transcription")]
    [ApiController]
    [Authorize]
    public class LectureTranscriptionController : BaseController
    {
        private readonly ILectureTranscriptionService _transcriptionService;
        private readonly ILectureTranscriptionQueue _transcriptionQueue;
        private readonly ITranscriptExportService _exportService;
        private readonly IStudentLearningService _learningService;
        private readonly ApplicationDBContext _db;

        public LectureTranscriptionController(
            ILectureTranscriptionService transcriptionService,
            ILectureTranscriptionQueue transcriptionQueue,
            ITranscriptExportService exportService,
            IStudentLearningService learningService,
            ApplicationDBContext db)
        {
            _transcriptionService = transcriptionService;
            _transcriptionQueue = transcriptionQueue;
            _exportService = exportService;
            _learningService = learningService;
            _db = db;
        }

        private bool IsStaffUser()
        {
            return User.IsInRole("Admin") ||
                   User.IsInRole("Super Admin") ||
                   User.IsInRole("Faculty") ||
                   User.IsInRole("Exam Coordinator");
        }

        private async Task<bool> HasAccessToMaterialAsync(int materialId)
        {
            if (IsStaffUser()) return true;

            int studentId = GetUserId();
            if (studentId <= 0) return false;

            return await _learningService.IsStudentEnrolledInMaterialAsync(studentId, materialId);
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  GET /api/transcription/video/{materialId}
        // ═══════════════════════════════════════════════════════════════════════
        [HttpGet("video/{materialId}")]
        public async Task<IActionResult> GetTranscript(int materialId)
        {
            if (materialId <= 0) return BadRequest(new { success = false, message = "Invalid material ID." });

            if (!await HasAccessToMaterialAsync(materialId))
                return Forbid();

            var transcript = await _transcriptionService.GetTranscriptAsync(materialId);
            if (transcript == null)
                return NotFound(new { success = false, message = "Lecture video not found." });

            // If no transcript exists yet or status is Pending, auto-transcribe on-demand
            if (transcript.Status == "None" || transcript.Status == TranscriptStatus.Pending)
            {
                var transResult = await _transcriptionService.TranscribeVideoAsync(materialId);
                if (transResult.Success)
                {
                    transcript = await _transcriptionService.GetTranscriptAsync(materialId) ?? transcript;
                }
                else
                {
                    _transcriptionQueue.Queue(materialId);
                    transcript.Status = TranscriptStatus.Processing;
                }
            }

            return Ok(new
            {
                success = true,
                data = transcript
            });
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  GET /api/transcription/video/{materialId}/status
        // ═══════════════════════════════════════════════════════════════════════
        [HttpGet("video/{materialId}/status")]
        public async Task<IActionResult> GetStatus(int materialId)
        {
            if (materialId <= 0) return BadRequest(new { success = false, message = "Invalid material ID." });

            if (!await HasAccessToMaterialAsync(materialId))
                return Forbid();

            var status = await _transcriptionService.GetStatusAsync(materialId);
            return Ok(new
            {
                success = true,
                data = status
            });
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  POST /api/transcription/video/{materialId}/queue (Faculty/Admin only)
        // ═══════════════════════════════════════════════════════════════════════
        [HttpPost("video/{materialId}/queue")]
        public async Task<IActionResult> QueueTranscription(int materialId)
        {
            if (!IsStaffUser()) return Forbid();

            var material = await _db.LectureMaterials
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == materialId && m.IsActive);

            if (material == null)
                return NotFound(new { success = false, message = "Material not found." });

            _transcriptionQueue.Queue(materialId);

            return Ok(new
            {
                success = true,
                message = "Transcription task successfully queued for processing."
            });
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  POST /api/transcription/video/{materialId}/regenerate (Faculty/Admin only)
        // ═══════════════════════════════════════════════════════════════════════
        [HttpPost("video/{materialId}/regenerate")]
        public async Task<IActionResult> RegenerateTranscript(int materialId)
        {
            if (!IsStaffUser()) return Forbid();

            var material = await _db.LectureMaterials
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == materialId && m.IsActive);

            if (material == null)
                return NotFound(new { success = false, message = "Material not found." });

            // Trigger regeneration
            var result = await _transcriptionService.RegenerateTranscriptAsync(materialId);

            return Ok(new
            {
                success = result.Success,
                status = result.Status,
                message = result.Message,
                segmentCount = result.SegmentCount
            });
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  POST /api/transcription/save (Faculty/Admin only)
        // ═══════════════════════════════════════════════════════════════════════
        [HttpPost("save")]
        public async Task<IActionResult> SaveEdits([FromBody] SaveTranscriptEditDto dto)
        {
            if (!IsStaffUser()) return Forbid();

            if (dto == null || dto.LectureMaterialId <= 0)
                return BadRequest(new { success = false, message = "Invalid transcript data provided." });

            int userId = GetUserId();
            bool saved = await _transcriptionService.SaveTranscriptEditsAsync(dto, userId);

            if (!saved)
                return BadRequest(new { success = false, message = "Could not save transcript edits." });

            return Ok(new
            {
                success = true,
                message = "Transcript updated and saved successfully."
            });
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  GET /api/transcription/video/{materialId}/download/txt
        // ═══════════════════════════════════════════════════════════════════════
        [HttpGet("video/{materialId}/download/txt")]
        public async Task<IActionResult> DownloadText(int materialId)
        {
            if (!await HasAccessToMaterialAsync(materialId))
                return Forbid();

            var transcript = await _transcriptionService.GetTranscriptAsync(materialId);
            if (transcript == null || transcript.Status == "None" || transcript.Status == TranscriptStatus.Pending)
            {
                await _transcriptionService.TranscribeVideoAsync(materialId);
                transcript = await _transcriptionService.GetTranscriptAsync(materialId);
            }

            if (transcript == null || transcript.Segments.Count == 0 || transcript.Status != TranscriptStatus.Ready)
                return NotFound("No transcript available for download.");

            var mapping = await _db.CourseMaterialMappings
                .AsNoTracking()
                .Include(m => m.Course)
                .FirstOrDefaultAsync(m => m.LectureMaterialId == materialId && m.IsActive);

            string courseName = mapping?.Course?.Name ?? "Course";
            byte[] fileBytes = _exportService.ExportToText(transcript, transcript.VideoTitle, courseName);

            string slug = SanitizeFileName(transcript.VideoTitle);
            return File(fileBytes, "text/plain; charset=utf-8", $"{slug}_Transcript.txt");
        }

        // ═══════════════════════════════════════════════════════════════════════
        //  GET /api/transcription/video/{materialId}/download/pdf
        // ═══════════════════════════════════════════════════════════════════════
        [HttpGet("video/{materialId}/download/pdf")]
        public async Task<IActionResult> DownloadPdf(int materialId)
        {
            if (!await HasAccessToMaterialAsync(materialId))
                return Forbid();

            var transcript = await _transcriptionService.GetTranscriptAsync(materialId);
            if (transcript == null || transcript.Status == "None" || transcript.Status == TranscriptStatus.Pending)
            {
                await _transcriptionService.TranscribeVideoAsync(materialId);
                transcript = await _transcriptionService.GetTranscriptAsync(materialId);
            }

            if (transcript == null || transcript.Segments.Count == 0 || transcript.Status != TranscriptStatus.Ready)
                return NotFound("No transcript available for download.");

            var mapping = await _db.CourseMaterialMappings
                .AsNoTracking()
                .Include(m => m.Course)
                .FirstOrDefaultAsync(m => m.LectureMaterialId == materialId && m.IsActive);

            string courseName = mapping?.Course?.Name ?? "Course";
            byte[] fileBytes = _exportService.ExportToPdf(transcript, transcript.VideoTitle, courseName);

            string slug = SanitizeFileName(transcript.VideoTitle);
            return File(fileBytes, "application/pdf", $"{slug}_Transcript.pdf");
        }

        private static string SanitizeFileName(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "Lecture";
            string clean = Regex.Replace(input, @"[^\w\d\-]+", "_").Trim('_');
            return string.IsNullOrEmpty(clean) ? "Lecture" : clean;
        }
    }
}
