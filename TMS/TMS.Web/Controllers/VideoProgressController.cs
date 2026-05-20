using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using TMS.Repository;
using TMS.Models.Academics;

namespace TMS.Web.Controllers
{
    [Authorize]
    public class VideoProgressController : BaseController
    {
        private readonly ApplicationDBContext _db;

        public VideoProgressController(ApplicationDBContext db)
        {
            _db = db;
        }

        // GET: /VideoProgress/GetProgress?materialId=...
        [HttpGet]
        public async Task<IActionResult> GetProgress(int materialId)
        {
            int studentId = GetUserId();
            if (studentId == 0 || materialId <= 0)
            {
                return Json(new { success = false, message = "Invalid parameters." });
            }

            try
            {
                var progress = await _db.LectureVideoProgresses
                    .FirstOrDefaultAsync(p => p.StudentId == studentId && p.LectureMaterialId == materialId && p.IsActive);

                if (progress == null)
                {
                    return Json(new
                    {
                        success = true,
                        data = new
                        {
                            watchedSeconds = 0,
                            durationSeconds = 0,
                            progressPercentage = 0,
                            isCompleted = false,
                            lastPositionSeconds = 0
                        }
                    });
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        watchedSeconds = progress.WatchedSeconds,
                        durationSeconds = progress.DurationSeconds,
                        progressPercentage = progress.IsCompleted ? 100m : progress.ProgressPercentage,
                        isCompleted = progress.IsCompleted,
                        lastPositionSeconds = progress.LastPositionSeconds
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /VideoProgress/UpdateProgress
        [HttpPost]
        public async Task<IActionResult> UpdateProgress([FromBody] UpdateProgressRequest request)
        {
            int studentId = GetUserId();
            if (studentId == 0 || request == null || request.MaterialId <= 0)
            {
                return Json(new { success = false, message = "Invalid parameters." });
            }

            try
            {
                var progress = await _db.LectureVideoProgresses
                    .FirstOrDefaultAsync(p => p.StudentId == studentId && p.LectureMaterialId == request.MaterialId);

                bool isNew = false;
                if (progress == null)
                {
                    isNew = true;
                    progress = new LectureVideoProgress
                    {
                        StudentId = studentId,
                        LectureMaterialId = request.MaterialId,
                        CreatedBy = studentId,
                        CreatedOn = DateTime.UtcNow,
                        IsActive = true
                    };
                }

                // Ensure duration is valid
                if (request.DurationSeconds > 0)
                {
                    progress.DurationSeconds = request.DurationSeconds;
                }

                // Anti-tamper logic/Progress update
                // Watched seconds can only grow or stay the same
                if (request.WatchedSeconds > progress.WatchedSeconds)
                {
                    // Sanity check: cannot be more than duration
                    int allowedWatched = request.WatchedSeconds;
                    if (progress.DurationSeconds > 0 && allowedWatched > progress.DurationSeconds)
                    {
                        allowedWatched = progress.DurationSeconds;
                    }
                    progress.WatchedSeconds = allowedWatched;
                }

                // Track where the user currently paused/left the video
                if (request.LastPositionSeconds >= 0)
                {
                    progress.LastPositionSeconds = request.LastPositionSeconds;
                }

                // Calculate percentage
                if (progress.DurationSeconds > 0)
                {
                    decimal pct = ((decimal)progress.WatchedSeconds / progress.DurationSeconds) * 100m;
                    if (pct > 100) pct = 100m;
                    progress.ProgressPercentage = Math.Round(pct, 2);

                    // Mark completed if 95%+
                    if (progress.ProgressPercentage >= 95m)
                    {
                        progress.IsCompleted = true;
                    }
                }

                progress.LastWatchedAt = DateTime.UtcNow;
                progress.UpdatedBy = studentId;
                progress.UpdatedOn = DateTime.UtcNow;

                if (isNew)
                {
                    _db.LectureVideoProgresses.Add(progress);
                }
                else
                {
                    _db.Entry(progress).State = EntityState.Modified;
                }

                await _db.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        watchedSeconds = progress.WatchedSeconds,
                        progressPercentage = progress.IsCompleted ? 100m : progress.ProgressPercentage,
                        isCompleted = progress.IsCompleted
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    public class UpdateProgressRequest
    {
        public int MaterialId { get; set; }
        public int WatchedSeconds { get; set; }
        public int DurationSeconds { get; set; }
        public int LastPositionSeconds { get; set; }
    }
}
