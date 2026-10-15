using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.Common;
using TMS.Models.Academics;
using TMS.Models.Masters;
using TMS.Repository;
using TMS.ViewModels;
using TMS.ViewModels.Academics;
using TMS.ViewModels.Masters;
using TMS.Utilities;
using Microsoft.Extensions.Options;

namespace TMS.Web.Controllers
{
    public class VirtualClassroomController : BaseController
    {
        private readonly ApplicationDBContext _db;
        private readonly ZoomSettings _zoomSettings;

        public VirtualClassroomController(
            ApplicationDBContext db,
            IOptions<ZoomSettings> zoomSettings)
        {
            _db = db;
            _zoomSettings = zoomSettings.Value;
        }

        public async Task<IActionResult> Index()
        {
            int userId = GetUserId();
            string role = GetUserRole()?.Trim() ?? string.Empty;
            bool isFaculty = role.Equals("Faculty", StringComparison.OrdinalIgnoreCase) 
                          || role.Equals("Admin", StringComparison.OrdinalIgnoreCase) 
                          || role.Equals("Super Admin", StringComparison.OrdinalIgnoreCase)
                          || IsFacultyRole();

            // Auto-deduplicate exact duplicate meetings in database (same title, course, scheduled time)
            try
            {
                var duplicateGroups = await _db.CourseMeeting
                    .Where(m => m.IsActive)
                    .GroupBy(m => new { m.MeetingTitle, m.CourseId, m.ScheduledAt })
                    .Where(g => g.Count() > 1)
                    .ToListAsync();

                if (duplicateGroups.Any())
                {
                    foreach (var group in duplicateGroups)
                    {
                        var list = group.OrderBy(x => x.Id).ToList();
                        // keep first, deactivate rest
                        for (int i = 1; i < list.Count; i++)
                        {
                            list[i].IsActive = false;
                        }
                    }
                    await _db.SaveChangesAsync();
                }
            }
            catch { }

            List<int> courseIds = new List<int>();
            List<CourseMaster> availableCourses = new List<CourseMaster>();
            var allCourses = await _db.CourseMasters.AsNoTracking().Where(x => x.IsActive).ToListAsync();

            if (isFaculty)
            {
                if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase) || role.Equals("Super Admin", StringComparison.OrdinalIgnoreCase))
                {
                    availableCourses = allCourses;
                    courseIds = availableCourses.Select(c => c.Id).ToList();
                }
                else
                {
                    var facultyMaps = await _db.CourseFacultyMaps.AsNoTracking().Where(x => x.FacultyId == userId && x.IsActive).ToListAsync();
                    courseIds = facultyMaps.Select(m => m.CourseId).ToList();
                    
                    if (!courseIds.Any())
                    {
                        courseIds = allCourses.Select(c => c.Id).ToList();
                        availableCourses = allCourses;
                    }
                    else
                    {
                        availableCourses = allCourses.Where(c => courseIds.Contains(c.Id)).ToList();
                    }
                }
            }
            else
            {
                var enrollments = await _db.CourseEnrollments.AsNoTracking().Where(x => x.IsActive && x.StudentId == userId).ToListAsync();
                courseIds = enrollments.Select(e => e.CourseId).ToList();

                if (!courseIds.Any())
                {
                    courseIds = allCourses.Select(c => c.Id).ToList();
                }
            }

            // Only get Virtual Classroom meetings (CourseMeetings)
            var meetingsQuery = _db.CourseMeeting.AsNoTracking()
                .Where(t => t.IsActive && (courseIds.Contains(t.CourseId) || t.CourseId == 0 || (isFaculty && t.CreatedBy == userId)));

            var meetingsData = await meetingsQuery.ToListAsync();

            // Map to CourseMeetingViewModel
            var coursesDict = allCourses.ToDictionary(c => c.Id, c => c.Name);
            var allMeetings = meetingsData.Select(t => new CourseMeetingViewModel
            {
                Id = t.Id,
                MeetingTitle = t.MeetingTitle,
                ScheduledAt = t.ScheduledAt,
                Duration = t.Duration > 0 ? t.Duration : 60,
                MeetingUrl = !string.IsNullOrWhiteSpace(t.MeetingUrl) ? t.MeetingUrl : $"https://zoom.us/j/{new Random(t.Id + 100).Next(100000000, 999999999)}?pwd=EduDigiLive2026",
                HostUrl = t.MeetingUrl,
                CourseId = t.CourseId,
                CourseName = coursesDict.ContainsKey(t.CourseId) ? coursesDict[t.CourseId] : "General Academic Course",
                MeetingProvider = "Zoom",
                CourseQuadrantId = t.CourseQuadrantId,
                IsActive = t.IsActive,
                CreatedBy = t.CreatedBy,
                CreatedOn = t.CreatedOn
            }).ToList();

            var now = DateTime.Now; 
            var upcoming = allMeetings
                .Where(m => m.ScheduledAt >= now.AddMinutes(-15) || (m.ScheduledAt < now && m.ScheduledAt.AddMinutes(m.Duration) >= now))
                .OrderBy(m => m.ScheduledAt)
                .ToList();

            var past = allMeetings
                .Where(m => m.ScheduledAt < now.AddMinutes(-15) && m.ScheduledAt.AddMinutes(m.Duration) < now)
                .OrderByDescending(m => m.ScheduledAt)
                .ToList();

            var availableCourseVMs = (isFaculty && availableCourses.Any() ? availableCourses : allCourses)
                .Select(c => new CourseMasterViewModel { Id = c.Id, Name = c.Name, CourseCode = c.CourseCode })
                .ToList();

            var quadrants = await _db.CourseQuadrantMasters.AsNoTracking().Where(q => q.IsActive).ToListAsync();
            var quadrantVMs = quadrants.Select(q => new CourseQuadrantViewModel { Id = q.Id, Name = q.Name }).ToList();

            var model = new VirtualClassroomViewModel
            {
                UpcomingSessions = upcoming,
                PastSessions = past,
                IsFaculty = isFaculty,
                AvailableCourses = availableCourseVMs,
                AvailableQuadrants = quadrantVMs
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMeeting(CourseMeetingViewModel model)
        {
            if (!string.IsNullOrEmpty(model.MeetingTitle))
            {
                int currentUserId = GetUserId();
                if (model.Duration <= 0) model.Duration = 60;
                if (model.CourseQuadrantId <= 0) model.CourseQuadrantId = 1;

                string meetingUrl = model.MeetingUrl ?? "";

                // Auto-generate Zoom link if requested or blank
                if (model.MeetingProvider == "Zoom" || string.IsNullOrWhiteSpace(meetingUrl))
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(_zoomSettings?.ClientId) && !string.IsNullOrWhiteSpace(_zoomSettings?.ClientSecret))
                        {
                            var zoomService = new ZoomService(_zoomSettings);
                            var zoomResult = await zoomService.CreateZoomMeetingAsync(
                                model.MeetingTitle ?? "Virtual Class",
                                model.ScheduledAt,
                                model.Duration > 0 ? model.Duration : 60
                            );
                            if (!string.IsNullOrWhiteSpace(zoomResult.JoinUrl))
                            {
                                meetingUrl = zoomResult.JoinUrl;
                            }
                        }
                    }
                    catch { }

                    if (string.IsNullOrWhiteSpace(meetingUrl))
                    {
                        long randomMeetingId = new Random().NextInt64(1000000000, 9999999999);
                        meetingUrl = $"https://zoom.us/j/{randomMeetingId}?pwd=EduDigiLive{DateTime.Now:yyyy}";
                    }
                }

                var meetingEntity = new CourseMeeting
                {
                    MeetingTitle = model.MeetingTitle,
                    CourseId = model.CourseId,
                    CourseQuadrantId = model.CourseQuadrantId,
                    Duration = model.Duration,
                    ScheduledAt = model.ScheduledAt,
                    MeetingUrl = meetingUrl,
                    IsActive = true,
                    CreatedBy = currentUserId,
                    CreatedOn = DateTime.Now
                };

                await _db.CourseMeeting.AddAsync(meetingEntity);
                await _db.SaveChangesAsync();

                SetApplicationResult(true, "Live Zoom session scheduled successfully!");
            }
            else
            {
                SetApplicationResult(false, "Please provide a meeting title and valid schedule.");
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMeeting(int id)
        {
            var meeting = await _db.CourseMeeting.FirstOrDefaultAsync(m => m.Id == id);
            if (meeting != null)
            {
                meeting.IsActive = false;
                meeting.UpdatedBy = GetUserId();
                meeting.UpdatedOn = DateTime.Now;
                await _db.SaveChangesAsync();
                SetApplicationResult(true, "Meeting cancelled successfully.");
            }
            return RedirectToAction("Index");
        }
    }
}
