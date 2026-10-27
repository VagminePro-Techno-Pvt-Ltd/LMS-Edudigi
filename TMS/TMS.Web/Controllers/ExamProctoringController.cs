using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TMS.Models.Assessments;
using TMS.Repository;
using TMS.ViewModels.Assessments;

namespace TMS.Web.Controllers
{
    [Authorize]
    public class ExamProctoringController : BaseController
    {
        private readonly ApplicationDBContext _db;

        public ExamProctoringController(ApplicationDBContext db)
        {
            _db = db;
        }

        // =========================================================================
        // --- STUDENT PROCTORING SESSION LIFECYCLE ---
        // =========================================================================

        [HttpPost]
        public async Task<IActionResult> StartSession([FromBody] StartProctoringSessionRequestDto dto)
        {
            if (dto == null || dto.AttemptId <= 0)
                return Json(new { success = false, message = "Invalid attempt ID." });

            var attempt = await _db.StudentExamAttempts
                .Include(a => a.Exam)
                .Include(a => a.Student)
                .FirstOrDefaultAsync(a => a.Id == dto.AttemptId);

            if (attempt == null)
                return Json(new { success = false, message = "Exam attempt not found." });

            if (attempt.StudentId != GetUserId())
                return Json(new { success = false, message = "Unauthorized student access." });

            var existingSession = await _db.ProctoringSessions
                .FirstOrDefaultAsync(s => s.AttemptId == dto.AttemptId && s.Status == "Active");

            if (existingSession != null)
            {
                return Json(new
                {
                    success = true,
                    sessionId = existingSession.Id,
                    studentName = attempt.Student?.Name,
                    examTitle = attempt.Exam?.Title,
                    isProctored = attempt.Exam?.IsProctored ?? false,
                    requireCamera = attempt.Exam?.RequireCamera ?? false,
                    trackTabSwitches = attempt.Exam?.TrackTabSwitches ?? true,
                    requireFullScreen = attempt.Exam?.RequireFullScreen ?? false,
                    maxAllowedViolations = attempt.Exam?.MaxAllowedViolations ?? 5
                });
            }

            var session = new ProctoringSession
            {
                AttemptId = attempt.Id,
                ExamId = attempt.ExamId,
                StudentId = attempt.StudentId,
                StartedAt = DateTime.UtcNow,
                CameraConsentGiven = dto.CameraConsentGiven,
                ConsentTimestamp = dto.CameraConsentGiven ? DateTime.UtcNow : null,
                IdentityVerified = true,
                VerificationMethod = dto.VerificationMethod ?? "SystemProfile",
                Status = "Active",
                TotalViolationsCount = 0
            };

            await _db.ProctoringSessions.AddAsync(session);
            await _db.SaveChangesAsync();

            return Json(new
            {
                success = true,
                sessionId = session.Id,
                studentName = attempt.Student?.Name,
                examTitle = attempt.Exam?.Title,
                isProctored = attempt.Exam?.IsProctored ?? false,
                requireCamera = attempt.Exam?.RequireCamera ?? false,
                trackTabSwitches = attempt.Exam?.TrackTabSwitches ?? true,
                requireFullScreen = attempt.Exam?.RequireFullScreen ?? false,
                maxAllowedViolations = attempt.Exam?.MaxAllowedViolations ?? 5
            });
        }

        [HttpPost]
        public async Task<IActionResult> LogIncident([FromBody] LogProctoringIncidentRequestDto dto)
        {
            if (dto == null || dto.SessionId <= 0 || string.IsNullOrWhiteSpace(dto.EventType))
                return Json(new { success = false, message = "Invalid incident log parameters." });

            var session = await _db.ProctoringSessions
                .Include(s => s.Exam)
                .FirstOrDefaultAsync(s => s.Id == dto.SessionId);

            if (session == null)
                return Json(new { success = false, message = "Proctoring session not found." });

            if (session.StudentId != GetUserId())
                return Json(new { success = false, message = "Unauthorized session access." });

            var incident = new ProctoringIncident
            {
                SessionId = session.Id,
                AttemptId = session.AttemptId,
                EventType = dto.EventType.Trim().ToUpperInvariant(),
                Severity = dto.Severity ?? "Medium",
                ServerTimestamp = DateTime.UtcNow,
                ClientTimestamp = dto.ClientTimestamp ?? DateTime.UtcNow,
                Details = dto.Details,
                RequiresReview = true,
                IsReviewed = false
            };

            await _db.ProctoringIncidents.AddAsync(incident);
            session.TotalViolationsCount++;

            int maxViolations = session.Exam?.MaxAllowedViolations ?? 5;
            if (session.TotalViolationsCount >= maxViolations)
            {
                session.Status = "FlaggedForReview";
            }

            await _db.SaveChangesAsync();

            return Json(new
            {
                success = true,
                violationCount = session.TotalViolationsCount,
                maxAllowed = maxViolations,
                status = session.Status,
                message = $"Incident logged: {incident.EventType}"
            });
        }

        [HttpPost]
        public async Task<IActionResult> EndSession([FromBody] EndProctoringSessionRequestDto dto)
        {
            if (dto == null || dto.SessionId <= 0)
                return Json(new { success = false, message = "Invalid session ID." });

            var session = await _db.ProctoringSessions.FindAsync(dto.SessionId);
            if (session == null)
                return Json(new { success = false, message = "Session not found." });

            if (session.StudentId != GetUserId() && !IsStaffRole())
                return Json(new { success = false, message = "Unauthorized access." });

            session.EndedAt = DateTime.UtcNow;
            if (session.Status == "Active")
            {
                session.Status = session.TotalViolationsCount > 0 ? "CompletedWithFlags" : "Completed";
            }

            await _db.SaveChangesAsync();
            return Json(new { success = true, status = session.Status });
        }

        // =========================================================================
        // --- SUPERVISOR / PROCTOR DASHBOARD API ---
        // =========================================================================

        [HttpGet]
        public async Task<IActionResult> GetSessionAuditTrail(int sessionId)
        {
            if (!IsStaffRole()) return Forbid();

            var session = await _db.ProctoringSessions
                .Include(s => s.Student)
                .Include(s => s.Exam)
                .Include(s => s.Incidents)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null) return NotFound();

            var result = new ProctoringSessionSummaryDto
            {
                SessionId = session.Id,
                AttemptId = session.AttemptId,
                ExamId = session.ExamId,
                ExamTitle = session.Exam?.Title ?? "Exam",
                StudentId = session.StudentId,
                StudentName = session.Student?.Name ?? "Student",
                StudentEmail = session.Student?.Email ?? "",
                StartedAt = session.StartedAt,
                EndedAt = session.EndedAt,
                CameraConsentGiven = session.CameraConsentGiven,
                IdentityVerified = session.IdentityVerified,
                VerificationMethod = session.VerificationMethod,
                TotalViolationsCount = session.TotalViolationsCount,
                Status = session.Status,
                ReviewerRemarks = session.ReviewerRemarks,
                Incidents = session.Incidents
                    .OrderByDescending(i => i.ServerTimestamp)
                    .Select(i => new ProctoringIncidentItemDto
                    {
                        IncidentId = i.Id,
                        EventType = i.EventType,
                        Severity = i.Severity,
                        ServerTimestamp = i.ServerTimestamp,
                        Details = i.Details,
                        IsReviewed = i.IsReviewed,
                        ReviewNotes = i.ReviewNotes
                    }).ToList()
            };

            return Json(new { success = true, session = result });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewIncident([FromBody] ReviewIncidentDto dto)
        {
            if (!IsStaffRole()) return Forbid();
            if (dto == null || dto.IncidentId <= 0)
                return Json(new { success = false, message = "Invalid incident ID." });

            var incident = await _db.ProctoringIncidents.FindAsync(dto.IncidentId);
            if (incident == null) return Json(new { success = false, message = "Incident not found." });

            incident.IsReviewed = true;
            incident.ReviewedBy = GetUserId();
            incident.ReviewNotes = dto.ReviewNotes?.Trim();
            await _db.SaveChangesAsync();

            return Json(new { success = true, message = "Incident reviewed successfully." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReviewSession([FromBody] ReviewSessionDto dto)
        {
            if (!IsStaffRole()) return Forbid();
            if (dto == null || dto.SessionId <= 0)
                return Json(new { success = false, message = "Invalid session ID." });

            var session = await _db.ProctoringSessions.FindAsync(dto.SessionId);
            if (session == null) return Json(new { success = false, message = "Session not found." });

            session.Status = dto.Decision == "Dismiss" ? "Reviewed" : "FlaggedForReview";
            session.ReviewerRemarks = dto.Remarks?.Trim();
            session.ReviewedBy = GetUserId();
            session.ReviewedOn = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Json(new { success = true, status = session.Status, message = "Proctoring session review recorded." });
        }

        private bool IsStaffRole()
        {
            return User.IsInRole("Admin") ||
                   User.IsInRole("Super Admin") ||
                   User.IsInRole("Management") ||
                   User.IsInRole("Faculty") ||
                   User.IsInRole("Exam Coordinator");
        }
    }
}
