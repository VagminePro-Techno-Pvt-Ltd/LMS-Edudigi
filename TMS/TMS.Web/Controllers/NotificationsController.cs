using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.Repository.Managers;
using TMS.ViewModels;
using TMS.ViewModels.Academics;
using TMS.ViewModels.Masters;

namespace TMS.Web.Controllers
{
    [Authorize]
    public class NotificationsController : BaseController
    {
        private readonly IMasterBaseManager<CourseEnrollmentViewModel> _enrollmentManager;
        private readonly IMasterBaseManager<LectureMaterialViewModel> _materialManager;
        private readonly IMasterBaseManager<ClassInstanceViewModel> _classManager;
        private readonly IMasterBaseManager<CourseMeetingViewModel> _meetingManager;
        private readonly IAnnouncementManager _announcementManager;

        public NotificationsController(
            IMasterBaseManager<CourseEnrollmentViewModel> enrollmentManager,
            IMasterBaseManager<LectureMaterialViewModel> materialManager,
            IMasterBaseManager<ClassInstanceViewModel> classManager,
            IMasterBaseManager<CourseMeetingViewModel> meetingManager,
            IAnnouncementManager announcementManager)
        {
            _enrollmentManager = enrollmentManager;
            _materialManager = materialManager;
            _classManager = classManager;
            _meetingManager = meetingManager;
            _announcementManager = announcementManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            var role = GetUserRole()?.ToLower();
            var roleIdStr = User.Claims.FirstOrDefault(c => c.Type == "roleId")?.Value;
            int? roleId = string.IsNullOrEmpty(roleIdStr) ? null : Convert.ToInt32(roleIdStr);

            var notifications = new List<NotificationDto>();
            var threshold = DateTime.Now.AddDays(-7);

            // 1. Fetch Visibility-Filtered Announcements
            List<int>? courseIds = null;
            if (role == "student")
            {
                var enrollments = await _enrollmentManager.GetAsync(
                    predicate: e => e.IsActive && e.StudentId == userId && e.Status == 1
                );
                courseIds = enrollments?.Select(e => e.CourseId).Distinct().ToList();
            }

            var announcements = await _announcementManager.GetVisibleAnnouncementsAsync(userId, roleId, courseIds);
            if (announcements != null)
            {
                foreach (var ann in announcements.Where(a => a.PublishDate >= threshold))
                {
                    notifications.Add(new NotificationDto
                    {
                        Title = ann.Title,
                        Message = ann.Description,
                        TimeAgo = GetTimeAgo(ann.PublishDate),
                        Type = ann.AnnouncementType switch
                        {
                            1 => "warning", // Event
                            2 => "success", // Urgent
                            _ => "info"     // General
                        }
                    });
                }
            }

            // 2. System updates for Students
            if (role == "student" && courseIds != null && courseIds.Any())
            {
                // Fetch Course Content Updates (LectureMaterial)
                var allMaterials = await _materialManager.GetAsync(
                    includes: new[] { "Course" },
                    predicate: m => m.IsActive
                );

                var materials = allMaterials?
                    .Where(m => m.CourseId.HasValue && courseIds.Contains(m.CourseId.Value) && m.CreatedOn >= threshold)
                    .OrderByDescending(m => m.CreatedOn)
                    .Take(10)
                    .ToList();

                if (materials != null)
                {
                    foreach (var m in materials)
                    {
                        notifications.Add(new NotificationDto
                        {
                            Title = "Course Material Uploaded",
                            Message = $"New resources for '{m.CourseName}' have been uploaded: {m.Title}.",
                            TimeAgo = GetTimeAgo(m.CreatedOn),
                            Type = "success"
                        });
                    }
                }

                // Fetch Class Schedules (ClassInstance)
                var allClassInstances = await _classManager.GetAsync(
                    includes: new[] { "Course" },
                    predicate: c => c.IsActive
                );

                var classes = allClassInstances?
                    .Where(c => courseIds.Contains(c.CourseId) && c.ScheduledStart >= threshold)
                    .OrderByDescending(c => c.ScheduledStart)
                    .Take(10)
                    .ToList();

                if (classes != null)
                {
                    foreach (var c in classes)
                    {
                        notifications.Add(new NotificationDto
                        {
                            Title = "New Class Scheduled",
                            Message = $"'{c.Title}' is scheduled on {c.ScheduledStart:f}.",
                            TimeAgo = GetTimeAgo(c.ScheduledStart),
                            Type = "info"
                        });
                    }
                }

                // Fetch Virtual Classroom Meetings (CourseMeeting)
                var allMeetings = await _meetingManager.GetAsync(
                    includes: null,
                    predicate: m => m.IsActive
                );

                var meetings = allMeetings?
                    .Where(m => courseIds.Contains(m.CourseId) && m.ScheduledAt >= threshold)
                    .OrderByDescending(m => m.ScheduledAt)
                    .Take(10)
                    .ToList();

                if (meetings != null)
                {
                    foreach (var m in meetings)
                    {
                        notifications.Add(new NotificationDto
                        {
                            Title = "Virtual Meeting Live link",
                            Message = $"Access '{m.MeetingTitle}' session scheduled for {m.ScheduledAt:f}.",
                            TimeAgo = GetTimeAgo(m.ScheduledAt),
                            Type = "warning"
                        });
                    }
                }
            }

            return View(notifications.OrderByDescending(n => n.TimeAgo).ToList());
        }

        private string GetTimeAgo(DateTime date)
        {
            var span = DateTime.UtcNow - date;
            if (span.TotalDays > 365) return $"{(int)(span.TotalDays / 365)}y ago";
            if (span.TotalDays > 30) return $"{(int)(span.TotalDays / 30)}mo ago";
            if (span.TotalDays > 1) return $"{(int)span.TotalDays}d ago";
            if (span.TotalHours > 1) return $"{(int)span.TotalHours}h ago";
            if (span.TotalMinutes > 1) return $"{(int)span.TotalMinutes}m ago";
            return "Just Now";
        }
    }
}
