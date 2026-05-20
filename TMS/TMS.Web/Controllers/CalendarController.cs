using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using TMS.Repository.Managers;
using TMS.Common;
using TMS.Utilities;
using TMS.ViewModels;
using TMS.ViewModels.Academics;
using TMS.ViewModels.Masters;

namespace TMS.Web.Controllers
{
    [Authorize]
    public class CalendarController : BaseController
    {
        private readonly IMasterBaseManager<ClassSeriesViewModel> _seriesManager;
        private readonly IMasterBaseManager<ClassInstanceViewModel> _instanceManager;
        private readonly IMasterBaseManager<ClassAttendanceViewModel> _attendanceManager;
        private readonly IMasterBaseManager<CourseEnrollmentViewModel> _enrollmentManager;
        private readonly IMasterBaseManager<CourseFacultyMapViewModel> _facultyMapManager;
        private readonly IMasterManager<CourseMasterViewModel> _courseManager;
        private readonly IMasterManager<SubjectMasterViewModel> _subjectManager;
        private readonly ZoomSettings _zoomSettings;

        public CalendarController(
            IMasterBaseManager<ClassSeriesViewModel> seriesManager,
            IMasterBaseManager<ClassInstanceViewModel> instanceManager,
            IMasterBaseManager<ClassAttendanceViewModel> attendanceManager,
            IMasterBaseManager<CourseEnrollmentViewModel> enrollmentManager,
            IMasterBaseManager<CourseFacultyMapViewModel> facultyMapManager,
            IMasterManager<CourseMasterViewModel> courseManager,
            IMasterManager<SubjectMasterViewModel> subjectManager,
            IOptions<ZoomSettings> zoomSettings)
        {
            _seriesManager = seriesManager;
            _instanceManager = instanceManager;
            _attendanceManager = attendanceManager;
            _enrollmentManager = enrollmentManager;
            _facultyMapManager = facultyMapManager;
            _courseManager = courseManager;
            _subjectManager = subjectManager;
            _zoomSettings = zoomSettings.Value;
        }

        // ═══════════════════════════════════════════════════
        //           MAIN INDEX (Role-aware)
        // ═══════════════════════════════════════════════════

        public async Task<IActionResult> Index()
        {
            int userId = GetUserId();
            string userRole = GetUserRole() ?? "Student";

            var model = new ClassroomCalendarViewModel
            {
                UserId = userId,
                UserRole = userRole
            };

            List<int> courseIds = new List<int>();
            var allCoursesData = await _courseManager.GetAsync(new[] { "Semester" });
            var activeCourses = allCoursesData.Where(t => t.IsActive).ToList();

            if (userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) || userRole.Equals("Super Admin", StringComparison.OrdinalIgnoreCase))
            {
                model.Courses = activeCourses;
                courseIds = activeCourses.Select(c => c.Id).ToList();
            }
            else if (userRole.Equals("Faculty", StringComparison.OrdinalIgnoreCase))
            {
                var maps = await _facultyMapManager.GetAsync(null, x => x.FacultyId == userId);
                courseIds = maps.Select(m => m.CourseId).Distinct().ToList();
                model.Courses = activeCourses.Where(c => courseIds.Contains(c.Id)).ToList();
            }
            else
            {
                var enrollments = await _enrollmentManager.GetAsync(null, x => x.IsActive);
                var studentEnrollments = enrollments.Where(e => e.StudentId == userId).ToList();
                courseIds = studentEnrollments.Select(e => e.CourseId).ToList();
                model.Courses = activeCourses.Where(c => courseIds.Contains(c.Id)).ToList();
            }

            // Fetch all relevant instances
            var allInstances = await _instanceManager.GetAsync(
                new[] { "Course", "Subject", "Unit", "Faculty" },
                i => i.IsActive
            );

            List<ClassInstanceViewModel> relevantInstances;

            if (userRole.Equals("Faculty", StringComparison.OrdinalIgnoreCase))
            {
                // Faculty sees classes they teach
                relevantInstances = allInstances.Where(i => i.FacultyId == userId).ToList();
            }
            else
            {
                relevantInstances = allInstances.Where(i => courseIds.Contains(i.CourseId)).ToList();
            }

            var now = DateTime.Now;

            model.LiveSessions = relevantInstances
                .Where(i => i.Status == 1 || (i.ScheduledStart <= now && i.ScheduledEnd >= now && i.Status == 0))
                .OrderBy(i => i.ScheduledStart)
                .ToList();

            model.UpcomingSessions = relevantInstances
                .Where(i => i.ScheduledStart > now && i.Status == 0)
                .OrderBy(i => i.ScheduledStart)
                .ToList();

            model.PastSessions = relevantInstances
                .Where(i => i.ScheduledEnd < now || i.Status == 2)
                .OrderByDescending(i => i.ScheduledStart)
                .Take(50)
                .ToList();

            model.AllSessions = relevantInstances
                .OrderBy(i => i.ScheduledStart)
                .ToList();

            model.NextClass = model.UpcomingSessions.FirstOrDefault() ?? model.LiveSessions.FirstOrDefault();

            // Stats
            model.TotalUpcoming = model.UpcomingSessions.Count;
            model.TotalCompleted = relevantInstances.Count(i => i.Status == 2);
            model.TotalCancelled = relevantInstances.Count(i => i.Status == 3);

            // Faculty/Admin: load their series
            if (!userRole.Equals("Student", StringComparison.OrdinalIgnoreCase))
            {
                var allSeries = await _seriesManager.GetAsync(
                    new[] { "Course", "Subject", "Faculty" },
                    s => s.IsActive
                );

                if (userRole.Equals("Faculty", StringComparison.OrdinalIgnoreCase))
                {
                    model.MySeries = allSeries.Where(s => s.FacultyId == userId).ToList();
                }
                else
                {
                    model.MySeries = allSeries.ToList();
                }
            }

            return View(model);
        }


        // ═══════════════════════════════════════════════════
        //           AJAX: Get Calendar Events (JSON)
        // ═══════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> GetEvents(DateTime? start, DateTime? end, int? courseId)
        {
            int userId = GetUserId();
            string userRole = GetUserRole() ?? "Student";

            var instancesData = await _instanceManager.GetAsync(new[] { "Course", "Subject", "Faculty" });
            var allInstances = instancesData.Where(i => i.IsActive).ToList();

            // Filter by role
            List<ClassInstanceViewModel> instances;
            if (userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) || userRole.Equals("Super Admin", StringComparison.OrdinalIgnoreCase))
            {
                instances = allInstances.ToList();
            }
            else if (userRole.Equals("Faculty", StringComparison.OrdinalIgnoreCase))
            {
                instances = allInstances.Where(i => i.FacultyId == userId).ToList();
            }
            else
            {
                var enrollments = await _enrollmentManager.GetAsync(null, x => x.IsActive);
                var courseIds = enrollments.Where(e => e.StudentId == userId)
                    .Select(e => e.CourseId).ToList();
                instances = allInstances.Where(i => courseIds.Contains(i.CourseId)).ToList();
            }

            // Filter by date range
            if (start.HasValue)
                instances = instances.Where(i => i.ScheduledStart >= start.Value).ToList();
            if (end.HasValue)
                instances = instances.Where(i => i.ScheduledStart <= end.Value).ToList();
            if (courseId.HasValue && courseId > 0)
                instances = instances.Where(i => i.CourseId == courseId).ToList();

            var events = instances.Select(i => new
            {
                id = i.Id,
                title = i.Title,
                start = i.ScheduledStart.ToString("yyyy-MM-ddTHH:mm:ss"),
                end = i.ScheduledEnd.ToString("yyyy-MM-ddTHH:mm:ss"),
                color = i.StatusColor,
                url = i.JoinUrl ?? "",
                extendedProps = new
                {
                    course = i.CourseName ?? "N/A",
                    subject = i.SubjectName,
                    faculty = i.FacultyName,
                    status = i.StatusText,
                    statusCode = i.Status,
                    joinUrl = i.JoinUrl ?? "",
                    recordingUrl = i.RecordingUrl ?? "",
                    instanceId = i.Id,
                    duration = i.DurationMinutes,
                    canJoin = i.CanJoin
                }
            });

            return Json(events);
        }


        // ═══════════════════════════════════════════════════
        //           CREATE CLASS (Faculty/Admin)
        // ═══════════════════════════════════════════════════

        [HttpPost]
        public async Task<IActionResult> CreateClass([FromBody] ClassSeriesViewModel model)
        {
            try
            {
                int userId = GetUserId();
                string userRole = GetUserRole() ?? "Student";

                if (userRole.Equals("Student", StringComparison.OrdinalIgnoreCase))
                    return Json(new { success = false, message = "Students cannot create classes." });

                if (model == null)
                    return Json(new { success = false, message = "Invalid data received." });

                if (model.CourseId <= 0)
                    return Json(new { success = false, message = "Please select a valid course." });

                model.FacultyId = userId;

                // Build RRULE from selected days
                if (model.IsRecurring && model.SelectedDays != null && model.SelectedDays.Any())
                {
                    model.RecurrenceRule = $"FREQ=WEEKLY;BYDAY={string.Join(",", model.SelectedDays)}";
                }

                // 1. Validate Time Conflict
                var startTime = model.StartDate.Date + model.StartTime;
                var endTime = startTime.AddMinutes(model.DurationMinutes);

                var existingInstances = await _instanceManager.GetAsync(null, i => i.IsActive && i.Status != 3);
                var conflict = existingInstances.FirstOrDefault(i => 
                    ((i.FacultyId == model.FacultyId) || (i.CourseId == model.CourseId)) &&
                    (startTime < i.ScheduledEnd && endTime > i.ScheduledStart)
                );

                if (conflict != null)
                {
                    var conflictType = conflict.FacultyId == model.FacultyId ? "Faculty" : "Course/Students";
                    return Json(new { 
                        success = false, 
                        message = $"Time Conflict! {conflictType} is already busy with '{conflict.Title}' from {conflict.ScheduledStart:hh:mm tt} to {conflict.ScheduledEnd:hh:mm tt}." 
                    });
                }

                // 2. Save the series
                var result = await _seriesManager.AddUpdateAsync(model, userId);
                if (!result)
                    return Json(new { success = false, message = "Failed to save class series." });

                // Fallback: If ID is still 0, fetch the latest series created by this user
                if (model.Id == 0)
                {
                    var allSeries = await _seriesManager.GetAsync(null, s => s.FacultyId == userId && s.Title == model.Title);
                    var latest = allSeries.OrderByDescending(s => s.Id).FirstOrDefault();
                    if (latest != null) model.Id = latest.Id;
                }

                if (model.Id == 0)
                    return Json(new { success = false, message = "Could not retrieve the new Class ID. Please try again." });

                // Generate instances
                var instances = GenerateInstances(model);
                foreach (var inst in instances)
                {
                    inst.FacultyId = userId;
                    inst.CourseId = model.CourseId;
                    inst.SubjectId = model.SubjectId;
                    
                    // Create Zoom meeting only if provider is Zoom
                    if (inst.MeetingProvider == "Zoom")
                    {
                        try
                        {
                            var zoomService = new ZoomService(_zoomSettings);
                            var joinUrl = await zoomService.CreateMeetingAsync(
                                inst.Title,
                                inst.ScheduledStart,
                                inst.DurationMinutes
                            );
                            inst.JoinUrl = joinUrl;
                        }
                        catch (Exception ex)
                        {
                            // Log Zoom error but continue with pending status
                            inst.JoinUrl = null; 
                            inst.MeetingProvider = "Zoom (Pending)";
                        }
                    }

                    await _instanceManager.AddUpdateAsync(inst, userId);
                }

                return Json(new { success = true, message = $"Successfully created {instances.Count} session(s)." });
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException != null ? " -> " + ex.InnerException.Message : "";
                return Json(new { 
                    success = false, 
                    message = $"Server Error: {ex.Message}{innerMsg}",
                    debugInfo = $"ModelId: {model?.Id}, CourseId: {model?.CourseId}"
                });
            }
        }


        // ═══════════════════════════════════════════════════
        //           CANCEL INSTANCE (Faculty/Admin)
        // ═══════════════════════════════════════════════════

        [HttpPost]
        public async Task<IActionResult> CancelInstance([FromBody] CancelInstanceRequest request)
        {
            int userId = GetUserId();
            var instance = await _instanceManager.GetAsync(request.InstanceId);
            if (instance == null)
                return Json(new { success = false, message = "Session not found." });

            instance.Status = 3; // Cancelled
            instance.CancellationReason = request.Reason ?? "Cancelled by faculty.";

            var result = await _instanceManager.AddUpdateAsync(instance, userId);
            return Json(new { success = result, message = result ? "Session cancelled." : "Failed to cancel." });
        }

        public class CancelInstanceRequest { public int InstanceId { get; set; } public string? Reason { get; set; } }


        // ═══════════════════════════════════════════════════
        //           RESCHEDULE INSTANCE (Faculty/Admin)
        // ═══════════════════════════════════════════════════

        [HttpPost]
        public async Task<IActionResult> RescheduleInstance([FromBody] RescheduleRequest request)
        {
            int userId = GetUserId();
            var instance = await _instanceManager.GetAsync(request.InstanceId);
            if (instance == null)
                return Json(new { success = false, message = "Session not found." });

            int duration = request.NewDuration ?? (int)(instance.ScheduledEnd - instance.ScheduledStart).TotalMinutes;

            instance.Status = 4; // Rescheduled
            instance.ScheduledStart = request.NewStart;
            instance.ScheduledEnd = request.NewStart.AddMinutes(duration);

            // Re-create Zoom meeting
            try
            {
                var zoomService = new ZoomService(_zoomSettings);
                var joinUrl = await zoomService.CreateMeetingAsync(instance.Title, request.NewStart, duration);
                instance.JoinUrl = joinUrl;
            }
            catch { /* keep old link */ }

            var result = await _instanceManager.AddUpdateAsync(instance, userId);
            return Json(new { success = result, message = result ? "Session rescheduled." : "Failed to reschedule." });
        }

        public class RescheduleRequest { public int InstanceId { get; set; } public DateTime NewStart { get; set; } public int? NewDuration { get; set; } }


        // ═══════════════════════════════════════════════════
        //           MARK ATTENDANCE (Faculty)
        // ═══════════════════════════════════════════════════

        [HttpPost]
        public async Task<IActionResult> MarkAttendance([FromBody] AttendanceRequest request)
        {
            int userId = GetUserId();
            var instance = await _instanceManager.GetAsync(request.InstanceId);
            if (instance == null)
                return Json(new { success = false, message = "Session not found." });

            foreach (var studentId in request.PresentStudentIds)
            {
                var attendance = new ClassAttendanceViewModel
                {
                    ClassInstanceId = request.InstanceId,
                    StudentId = studentId,
                    AttendanceStatus = 1, // Present
                    JoinedAt = instance.ScheduledStart,
                    LeftAt = instance.ScheduledEnd,
                    DurationMinutes = (int)(instance.ScheduledEnd - instance.ScheduledStart).TotalMinutes,
                    Source = "Manual",
                    IsActive = true
                };
                await _attendanceManager.AddUpdateAsync(attendance, userId);
            }

            return Json(new { success = true, message = $"Attendance marked for {request.PresentStudentIds.Count} student(s)." });
        }

        public class AttendanceRequest { public int InstanceId { get; set; } public List<int> PresentStudentIds { get; set; } = new(); }


        // ═══════════════════════════════════════════════════
        //           CONFLICT CHECK (Faculty/Admin)
        // ═══════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> CheckConflict(int facultyId, DateTime start, DateTime end)
        {
            var allInstances = await _instanceManager.GetAsync(null,
                i => i.FacultyId == facultyId && i.IsActive && i.Status != 3
            );

            var conflicts = allInstances
                .Where(i => i.ScheduledStart < end && i.ScheduledEnd > start)
                .Select(i => new { i.Id, i.Title, i.ScheduledStart, i.ScheduledEnd })
                .ToList();

            return Json(new { hasConflict = conflicts.Any(), conflicts });
        }


        // ═══════════════════════════════════════════════════
        //           GET SUBJECTS BY COURSE (AJAX)
        // ═══════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> GetSubjects(int courseId)
        {
            try
            {
                // Get course to find its semester
                var course = await _courseManager.GetAsync(courseId, new[] { "Semester" });
                
                var allSubjects = await _subjectManager.GetAsync(null, s => s.IsActive);
                
                List<SubjectMasterViewModel> subjects;
                if (course?.SemesterId != null && course.SemesterId > 0)
                {
                    // Filter subjects by the course's semester
                    subjects = allSubjects.Where(s => s.SemesterId == course.SemesterId.Value).ToList();
                }
                else
                {
                    // If course has no semester, return all active subjects
                    subjects = allSubjects.ToList();
                }

                return Json(subjects.Select(s => new { id = s.Id, name = s.Name }));
            }
            catch (Exception ex)
            {
                return Json(new List<object>());
            }
        }


        // ═══════════════════════════════════════════════════
        //           HELPER: Generate Instances
        // ═══════════════════════════════════════════════════

        private List<ClassInstanceViewModel> GenerateInstances(ClassSeriesViewModel series)
        {
            var instances = new List<ClassInstanceViewModel>();

            if (!series.IsRecurring)
            {
                // One-off class
                var scheduledStart = series.StartDate.Date + series.StartTime;
                instances.Add(new ClassInstanceViewModel
                {
                    SeriesId = series.Id,
                    Title = series.Title,
                    CourseId = series.CourseId,
                    SubjectId = series.SubjectId,
                    UnitId = series.UnitId,
                    FacultyId = series.FacultyId,
                    ScheduledStart = scheduledStart,
                    ScheduledEnd = scheduledStart.AddMinutes(series.DurationMinutes),
                    MeetingProvider = series.MeetingProvider,
                    JoinUrl = series.MeetingProvider == "External" ? series.ExternalMeetingLink : null,
                    MaxCapacity = series.MaxCapacity,
                    Status = 0,
                    IsActive = true
                });
            }
            else
            {
                // Parse selected days
                var dayMap = new Dictionary<string, DayOfWeek>
                {
                    ["MO"] = DayOfWeek.Monday,
                    ["TU"] = DayOfWeek.Tuesday,
                    ["WE"] = DayOfWeek.Wednesday,
                    ["TH"] = DayOfWeek.Thursday,
                    ["FR"] = DayOfWeek.Friday,
                    ["SA"] = DayOfWeek.Saturday,
                    ["SU"] = DayOfWeek.Sunday
                };

                var selectedDaysOfWeek = series.SelectedDays
                    .Where(d => dayMap.ContainsKey(d))
                    .Select(d => dayMap[d])
                    .ToList();

                if (!selectedDaysOfWeek.Any())
                    selectedDaysOfWeek.Add(series.StartDate.DayOfWeek);

                var endDate = series.EndDate ?? series.StartDate.AddMonths(3);
                var current = series.StartDate.Date;

                while (current <= endDate)
                {
                    if (selectedDaysOfWeek.Contains(current.DayOfWeek))
                    {
                        var scheduledStart = current + series.StartTime;
                        instances.Add(new ClassInstanceViewModel
                        {
                            SeriesId = series.Id,
                            Title = series.Title,
                            CourseId = series.CourseId,
                            SubjectId = series.SubjectId,
                            UnitId = series.UnitId,
                            FacultyId = series.FacultyId,
                            ScheduledStart = scheduledStart,
                            ScheduledEnd = scheduledStart.AddMinutes(series.DurationMinutes),
                            MeetingProvider = series.MeetingProvider,
                            JoinUrl = series.MeetingProvider == "External" ? series.ExternalMeetingLink : null,
                            MaxCapacity = series.MaxCapacity,
                            Status = 0,
                            IsActive = true
                        });
                    }
                    current = current.AddDays(1);
                }
            }

            return instances;
        }
    }
}
