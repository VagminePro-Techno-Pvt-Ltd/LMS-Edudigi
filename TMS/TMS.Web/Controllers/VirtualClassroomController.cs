using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using TMS.Common;
using TMS.Models.Academics;
using TMS.Models.Masters;
using TMS.Repository.Managers;
using TMS.ViewModels.Academics;
using TMS.ViewModels.Masters;
using TMS.ViewModels;
using TMS.Utilities;
using Microsoft.Extensions.Options;

namespace TMS.Web.Controllers
{
    public class VirtualClassroomController : BaseController
    {
        private readonly IMasterBaseManager<CourseEnrollmentViewModel> _enrollmentManager;
        private readonly IMasterBaseManager<CourseMeetingViewModel> _meetingManager;
        private readonly IMasterBaseManager<CourseFacultyMapViewModel> _facultyMapManager;
        private readonly IMasterManager<CourseMasterViewModel> _courseManager;
        private readonly IMasterBaseManager<CourseQuadrantViewModel> _quadrantManager;
        private readonly IMasterBaseManager<ClassInstanceViewModel> _instanceManager;
        private readonly ZoomSettings _zoomSettings;

        public VirtualClassroomController(
            IMasterBaseManager<CourseEnrollmentViewModel> enrollmentManager,
            IMasterBaseManager<CourseMeetingViewModel> meetingManager,
            IMasterBaseManager<CourseFacultyMapViewModel> facultyMapManager,
            IMasterManager<CourseMasterViewModel> courseManager,
            IMasterBaseManager<CourseQuadrantViewModel> quadrantManager,
            IMasterBaseManager<ClassInstanceViewModel> instanceManager,
            Microsoft.Extensions.Options.IOptions<ZoomSettings> zoomSettings)
        {
            _enrollmentManager = enrollmentManager;
            _meetingManager = meetingManager;
            _facultyMapManager = facultyMapManager;
            _courseManager = courseManager;
            _quadrantManager = quadrantManager;
            _instanceManager = instanceManager;
            _zoomSettings = zoomSettings.Value;
        }

        public async Task<IActionResult> Index()
        {
            int userId = GetUserId();
            string role = GetUserRole();
            bool isFaculty = role == "Faculty" || role == "Admin";

            List<int> courseIds = new List<int>();
            List<CourseMasterViewModel> availableCourses = new List<CourseMasterViewModel>();

            if (isFaculty)
            {
                if (role == "Admin")
                {
                    var allCourses = await _courseManager.GetAsync(null, x => x.IsActive);
                    availableCourses = allCourses.ToList();
                    courseIds = availableCourses.Select(c => c.Id).ToList();
                }
                else
                {
                    var facultyMaps = await _facultyMapManager.GetAsync(null, x => x.FacultyId == userId && x.IsActive);
                    courseIds = facultyMaps.Select(m => m.CourseId).ToList();
                    
                    var allCourses = await _courseManager.GetAsync(null, x => x.IsActive);
                    availableCourses = allCourses.Where(c => courseIds.Contains(c.Id)).ToList();
                }
            }
            else
            {
                var enrollments = await _enrollmentManager.GetAsync(null, x => x.IsActive && x.StudentId == userId);
                courseIds = enrollments.Select(e => e.CourseId).ToList();
            }

            // 1. Get meetings from Virtual Classroom table
            var meetingsData = await _meetingManager.GetAsync();
            var vcMeetings = meetingsData.Where(t => t.IsActive && courseIds.Contains(t.CourseId)).ToList();

            // 2. Get meetings from Classroom Calendar table (ClassInstance)
            // Using in-memory filtering to avoid SerializationException with courseIds List
            var allInstances = await _instanceManager.GetAsync(new[] { "Course", "Faculty" }, i => i.IsActive);
            var calendarInstances = allInstances.Where(i => courseIds.Contains(i.CourseId)).ToList();
            
            // Map Calendar instances to CourseMeetingViewModel for unified display
            var calendarMeetings = calendarInstances.Select(i => new CourseMeetingViewModel
            {
                Id = i.Id,
                MeetingTitle = i.Title,
                ScheduledAt = i.ScheduledStart,
                Duration = i.DurationMinutes,
                MeetingUrl = i.JoinUrl,
                CourseId = i.CourseId,
                CourseName = i.CourseName ?? "Calendar Session",
                IsActive = true,
                // We can use this to distinguish source in UI if needed
                Description = "Calendar" 
            }).ToList();

            // 3. Merge both sources
            var allMeetings = vcMeetings.Concat(calendarMeetings).ToList();
            
            var now = DateTime.Now; 
            var upcoming = allMeetings
                .Where(m => m.ScheduledAt >= now.AddMinutes(-10)) // include sessions starting soon
                .OrderBy(m => m.ScheduledAt)
                .ToList();

            var past = allMeetings
                .Where(m => m.ScheduledAt < now.AddMinutes(-10))
                .OrderByDescending(m => m.ScheduledAt)
                .ToList();

            // Populate CourseNames for VC meetings if missing
            var courses = await _courseManager.GetAsync(null, x => x.IsActive);
            foreach (var m in upcoming.Concat(past))
            {
                if (string.IsNullOrEmpty(m.CourseName) || m.CourseName == "Unknown Course")
                {
                    m.CourseName = courses.FirstOrDefault(c => c.Id == m.CourseId)?.Name ?? "Unknown Course";
                }
            }

            var model = new VirtualClassroomViewModel
            {
                UpcomingSessions = upcoming,
                PastSessions = past,
                IsFaculty = isFaculty,
                AvailableCourses = availableCourses,
                AvailableQuadrants = (await _quadrantManager.GetAsync(null, x => x.IsActive)).ToList()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMeeting(CourseMeetingViewModel model)
        {
            if (ModelState.IsValid || (model.MeetingProvider == "Zoom" && !string.IsNullOrEmpty(model.MeetingTitle)))
            {
                model.IsActive = true;
                model.CreatedBy = GetUserId();
                model.CreatedOn = DateTime.Now;

                // Auto-generate Zoom link if requested
                if (model.MeetingProvider == "Zoom")
                {
                    try
                    {
                        var zoomService = new ZoomService(_zoomSettings);
                        var joinUrl = await zoomService.CreateMeetingAsync(
                            model.MeetingTitle ?? "Virtual Class",
                            model.ScheduledAt,
                            model.Duration > 0 ? model.Duration : 60
                        );
                        model.MeetingUrl = joinUrl;
                    }
                    catch (Exception ex)
                    {
                        SetApplicationResult(false, "Zoom Error: " + ex.Message);
                        return RedirectToAction("Index");
                    }
                }
                
                var result = await _meetingManager.AddUpdateAsync(model, GetUserId());
                if (result)
                {
                    SetApplicationResult(true, "Meeting scheduled successfully" + (model.MeetingProvider == "Zoom" ? " with Zoom integration." : "."));
                }
                else
                {
                    SetApplicationResult(false, "Failed to schedule meeting.");
                }
            }
            else
            {
                SetApplicationResult(false, "Please fill all required fields.");
            }
            return RedirectToAction("Index");
        }
    }
}
