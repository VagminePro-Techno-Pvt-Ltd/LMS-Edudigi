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
        private readonly IMasterManager<UserViewModel> _userManager;
        private readonly TMS.Repository.ApplicationDBContext _db;
        private readonly ZoomSettings _zoomSettings;
        private readonly TMS.Utilities.IZoomService _zoomService;

        public CalendarController(
            IMasterBaseManager<ClassSeriesViewModel> seriesManager,
            IMasterBaseManager<ClassInstanceViewModel> instanceManager,
            IMasterBaseManager<ClassAttendanceViewModel> attendanceManager,
            IMasterBaseManager<CourseEnrollmentViewModel> enrollmentManager,
            IMasterBaseManager<CourseFacultyMapViewModel> facultyMapManager,
            IMasterManager<CourseMasterViewModel> courseManager,
            IMasterManager<SubjectMasterViewModel> subjectManager,
            IMasterManager<UserViewModel> userManager,
            TMS.Repository.ApplicationDBContext db,
            IOptions<ZoomSettings> zoomSettings,
            TMS.Utilities.IZoomService? zoomService = null)
        {
            _seriesManager = seriesManager;
            _instanceManager = instanceManager;
            _attendanceManager = attendanceManager;
            _enrollmentManager = enrollmentManager;
            _facultyMapManager = facultyMapManager;
            _courseManager = courseManager;
            _subjectManager = subjectManager;
            _userManager = userManager;
            _db = db;
            _zoomSettings = zoomSettings.Value;
            _zoomService = zoomService ?? new TMS.Utilities.ZoomService(zoomSettings);
        }

        // ═══════════════════════════════════════════════════
        //           MAIN INDEX (Role-aware)
        // ═══════════════════════════════════════════════════

        public async Task<IActionResult> Index()
        {
            int userId = GetUserId();
            string userRole = GetUserRole() ?? "Student";
            bool isFaculty = IsFacultyRole();
            bool isAdmin = userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                           userRole.Equals("Super Admin", StringComparison.OrdinalIgnoreCase) ||
                           userRole.Equals("Management", StringComparison.OrdinalIgnoreCase) ||
                           userRole.Equals("Exam Coordinator", StringComparison.OrdinalIgnoreCase);

            var model = new ClassroomCalendarViewModel
            {
                UserId = userId,
                UserRole = isAdmin ? "Admin" : (isFaculty ? "Faculty" : "Student")
            };

            List<int> courseIds = new List<int>();
            var allCoursesData = await _courseManager.GetAsync(new[] { "Semester" });
            var activeCourses = allCoursesData.Where(t => t.IsActive).ToList();

            // Fetch faculties for assignment dropdown
            var allUsers = await _userManager.GetAsync(new[] { "Role" }, u => u.IsActive);
            var facultyUsers = allUsers
                .Where(u => u.Role != null && (
                    u.Role.Name.Contains("Faculty", StringComparison.OrdinalIgnoreCase) ||
                    u.Role.Name.Contains("Teacher", StringComparison.OrdinalIgnoreCase) ||
                    u.Role.Name.Contains("Instructor", StringComparison.OrdinalIgnoreCase) ||
                    u.Role.Name.Contains("Trainer", StringComparison.OrdinalIgnoreCase) ||
                    u.Role.Name.Contains("Admin", StringComparison.OrdinalIgnoreCase)))
                .OrderBy(u => u.Name)
                .ToList();

            if (!facultyUsers.Any())
            {
                facultyUsers = allUsers.OrderBy(u => u.Name).ToList();
            }
            model.Faculties = facultyUsers;
            ViewBag.FacultyList = facultyUsers;

            if (isAdmin)
            {
                model.Courses = activeCourses;
                courseIds = activeCourses.Select(c => c.Id).ToList();
            }
            else if (isFaculty)
            {
                var maps = await _facultyMapManager.GetAsync(null, x => x.FacultyId == userId && x.IsActive);
                courseIds = maps.Select(m => m.CourseId).Distinct().ToList();
                var mappedCourses = activeCourses.Where(c => courseIds.Contains(c.Id)).ToList();
                model.Courses = mappedCourses.Any() ? mappedCourses : activeCourses;
                if (!courseIds.Any())
                {
                    courseIds = activeCourses.Select(c => c.Id).ToList();
                }
            }
            else
            {
                var enrollments = await _enrollmentManager.GetAsync(null, x => x.IsActive && x.StudentId == userId && (x.Status == 0 || x.Status == (int)EnrollmentStatus.Active));
                courseIds = enrollments.Select(e => e.CourseId).Distinct().ToList();
                var enrolledCourses = activeCourses.Where(c => courseIds.Contains(c.Id)).ToList();
                model.Courses = enrolledCourses.Any() ? enrolledCourses : activeCourses;
            }

            // Fetch unified synced sessions across ClassInstance + CourseMeeting
            var relevantInstances = await GetUnifiedSessionsAsync(userId, userRole, courseIds, isFaculty, isAdmin, activeCourses, allUsers);

            var now = DateTime.Now;

            model.LiveSessions = relevantInstances
                .Where(i => i.Status == 1 || (i.ScheduledStart <= now && i.ScheduledEnd >= now && i.Status == 0))
                .OrderBy(i => i.ScheduledStart)
                .ToList();

            model.UpcomingSessions = relevantInstances
                .Where(i => i.ScheduledStart > now && (i.Status == 0 || i.Status == 4))
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

            model.NextClass = model.LiveSessions.FirstOrDefault() ?? model.UpcomingSessions.FirstOrDefault();

            // Stats
            model.TotalUpcoming = model.UpcomingSessions.Count;
            model.TotalCompleted = relevantInstances.Count(i => i.Status == 2);
            model.TotalCancelled = relevantInstances.Count(i => i.Status == 3);

            // Faculty/Admin: load their series
            if (!isFaculty && !isAdmin)
            {
                // Student: no series
            }
            else
            {
                var allSeries = await _seriesManager.GetAsync(
                    new[] { "Course", "Subject", "Faculty" },
                    s => s.IsActive
                );

                if (isFaculty && !isAdmin)
                {
                    model.MySeries = allSeries.Where(s => s.FacultyId == userId || courseIds.Contains(s.CourseId)).ToList();
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
            bool isFaculty = IsFacultyRole();
            bool isAdmin = userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                           userRole.Equals("Super Admin", StringComparison.OrdinalIgnoreCase) ||
                           userRole.Equals("Management", StringComparison.OrdinalIgnoreCase) ||
                           userRole.Equals("Exam Coordinator", StringComparison.OrdinalIgnoreCase);

            var allCoursesData = await _courseManager.GetAsync(new[] { "Semester" });
            var activeCourses = allCoursesData.Where(t => t.IsActive).ToList();
            var allUsers = await _userManager.GetAsync(new[] { "Role" }, u => u.IsActive);

            List<int> courseIds = new List<int>();
            if (isAdmin)
            {
                courseIds = activeCourses.Select(c => c.Id).ToList();
            }
            else if (isFaculty)
            {
                var maps = await _facultyMapManager.GetAsync(null, x => x.FacultyId == userId && x.IsActive);
                courseIds = maps.Select(m => m.CourseId).Distinct().ToList();
                if (!courseIds.Any())
                {
                    courseIds = activeCourses.Select(c => c.Id).ToList();
                }
            }
            else
            {
                var enrollments = await _enrollmentManager.GetAsync(null, x => x.IsActive && x.StudentId == userId && (x.Status == 0 || x.Status == (int)EnrollmentStatus.Active));
                courseIds = enrollments.Select(e => e.CourseId).Distinct().ToList();
            }

            var instances = await GetUnifiedSessionsAsync(userId, userRole, courseIds, isFaculty, isAdmin, activeCourses, allUsers);

            // Filter by date range
            if (start.HasValue)
                instances = instances.Where(i => i.ScheduledStart >= start.Value || i.ScheduledEnd >= start.Value).ToList();
            if (end.HasValue)
                instances = instances.Where(i => i.ScheduledStart <= end.Value).ToList();
            if (courseId.HasValue && courseId > 0)
                instances = instances.Where(i => i.CourseId == courseId.Value).ToList();

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
                    course = i.CourseName ?? "Academic Course",
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
        //   HELPER: Get Unified Synced Sessions
        // ═══════════════════════════════════════════════════
        private async Task<List<ClassInstanceViewModel>> GetUnifiedSessionsAsync(
            int userId,
            string userRole,
            List<int> courseIds,
            bool isFaculty,
            bool isAdmin,
            List<CourseMasterViewModel> activeCourses,
            List<UserViewModel> allUsers)
        {
            var coursesDict = activeCourses.ToDictionary(c => c.Id, c => c);
            var usersDict = allUsers.ToDictionary(u => u.Id, u => u);

            // 1. Fetch ClassInstances
            var instancesData = await _instanceManager.GetAsync(new[] { "Course", "Subject", "Unit", "Faculty" }, i => i.IsActive);
            var allInstances = instancesData.ToList();

            // 2. Fetch CourseMeetings (Virtual Classroom Zoom sessions)
            try
            {
                var meetings = _db.CourseMeeting.Where(m => m.IsActive).ToList();
                foreach (var m in meetings)
                {
                    int duration = m.Duration > 0 ? m.Duration : 60;
                    var scheduledEnd = m.ScheduledAt.AddMinutes(duration);
                    var now = DateTime.Now;

                    int status = 0;
                    if (m.ScheduledAt <= now && scheduledEnd >= now) status = 1; // Live
                    else if (scheduledEnd < now) status = 2; // Completed

                    var courseObj = coursesDict.ContainsKey(m.CourseId) ? coursesDict[m.CourseId] : null;
                    var facultyObj = usersDict.ContainsKey(m.CreatedBy) ? usersDict[m.CreatedBy] : null;

                    allInstances.Add(new ClassInstanceViewModel
                    {
                        Id = 100000 + m.Id, // unique positive key
                        Title = m.MeetingTitle ?? "Virtual Class",
                        CourseId = m.CourseId,
                        Course = courseObj,
                        FacultyId = m.CreatedBy,
                        Faculty = facultyObj,
                        ScheduledStart = m.ScheduledAt,
                        ScheduledEnd = scheduledEnd,
                        MeetingProvider = "Zoom",
                        JoinUrl = m.MeetingUrl,
                        HostUrl = m.MeetingUrl,
                        Status = status,
                        IsActive = true
                    });
                }
            }
            catch { }

            // 3. Filter by role
            List<ClassInstanceViewModel> result;
            if (isAdmin)
            {
                result = allInstances;
            }
            else if (isFaculty)
            {
                var courseIdSet = courseIds.ToHashSet();
                result = allInstances
                    .Where(i => i.FacultyId == userId || (i.CourseId > 0 && courseIdSet.Contains(i.CourseId)) || i.CreatedBy == userId)
                    .ToList();

                if (!result.Any())
                {
                    result = allInstances;
                }
            }
            else
            {
                // Student
                if (courseIds.Any())
                {
                    var courseIdSet = courseIds.ToHashSet();
                    result = allInstances
                        .Where(i => courseIdSet.Contains(i.CourseId) || i.CourseId == 0)
                        .ToList();
                }
                else
                {
                    result = allInstances;
                }
            }

            return result;
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

                if (string.IsNullOrWhiteSpace(model.Title))
                    return Json(new { success = false, message = "Please enter a class title." });

                if (model.CourseId <= 0)
                    return Json(new { success = false, message = "Please select a valid course." });

                if (model.StartDate == default)
                    return Json(new { success = false, message = "Please select a valid start date." });

                if (userRole.Equals("Faculty", StringComparison.OrdinalIgnoreCase))
                {
                    model.FacultyId = userId;
                }
                else
                {
                    // Admin: Use selected faculty or fallback
                    if (model.FacultyId <= 0)
                    {
                        var maps = await _facultyMapManager.GetAsync(null, m => m.CourseId == model.CourseId && m.IsActive);
                        var firstMap = maps.FirstOrDefault();
                        model.FacultyId = firstMap != null ? firstMap.FacultyId : userId;
                    }
                }

                // Build RRULE from selected days
                if (model.IsRecurring && model.SelectedDays != null && model.SelectedDays.Any())
                {
                    model.RecurrenceRule = $"FREQ=WEEKLY;BYDAY={string.Join(",", model.SelectedDays)}";
                }

                // Generate instances first
                var instances = GenerateInstances(model);
                if (!instances.Any())
                {
                    return Json(new { success = false, message = "No valid class dates generated for this schedule. Please check the start/end dates and selected repeat days." });
                }

                // 1. Validate Time Conflict across all generated instances
                var existingInstances = await _instanceManager.GetAsync(null, i => i.IsActive && i.Status != 3);
                foreach (var inst in instances)
                {
                    var conflict = existingInstances.FirstOrDefault(i =>
                        ((i.FacultyId == model.FacultyId) || (i.CourseId == model.CourseId)) &&
                        (inst.ScheduledStart < i.ScheduledEnd && inst.ScheduledEnd > i.ScheduledStart)
                    );

                    if (conflict != null)
                    {
                        var conflictType = conflict.FacultyId == model.FacultyId ? "Faculty" : "Course/Students";
                        return Json(new {
                            success = false,
                            message = $"Time Conflict on {inst.ScheduledStart:dd MMM yyyy}! {conflictType} is already scheduled with '{conflict.Title}' from {conflict.ScheduledStart:hh:mm tt} to {conflict.ScheduledEnd:hh:mm tt}."
                        });
                    }
                }

                // 2. Save the series
                var result = await _seriesManager.AddUpdateAsync(model, userId);
                if (!result)
                    return Json(new { success = false, message = "Failed to save class series." });

                // Fallback: If ID is still 0, fetch the latest series created
                if (model.Id == 0)
                {
                    var allSeries = await _seriesManager.GetAsync(null, s => s.FacultyId == model.FacultyId && s.Title == model.Title);
                    var latest = allSeries.OrderByDescending(s => s.Id).FirstOrDefault();
                    if (latest != null) model.Id = latest.Id;
                }

                if (model.Id == 0)
                    return Json(new { success = false, message = "Could not retrieve the new Class ID. Please try again." });

                // 3. Save each generated instance
                foreach (var inst in instances)
                {
                    inst.SeriesId = model.Id;
                    inst.FacultyId = model.FacultyId;
                    inst.CourseId = model.CourseId;
                    inst.SubjectId = model.SubjectId;

                    // Create Zoom meeting only if provider is Zoom
                    if (inst.MeetingProvider == "Zoom")
                    {
                        try
                        {
                            var zoomResult = await _zoomService.CreateZoomMeetingAsync(
                                inst.Title,
                                inst.ScheduledStart,
                                inst.DurationMinutes
                            );
                            inst.JoinUrl = zoomResult.JoinUrl;
                            inst.HostUrl = zoomResult.HostUrl;
                        }
                        catch (Exception)
                        {
                            inst.JoinUrl = null;
                            inst.HostUrl = null;
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
                    message = $"Server Error: {ex.Message}{innerMsg}"
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
            if (duration <= 0) duration = 60;

            var newStart = request.NewStart;
            var newEnd = request.NewStart.AddMinutes(duration);

            // Check conflict for the rescheduled time
            var existingInstances = await _instanceManager.GetAsync(null, i => i.IsActive && i.Id != instance.Id && i.Status != 3);
            var conflict = existingInstances.FirstOrDefault(i =>
                ((i.FacultyId == instance.FacultyId) || (i.CourseId == instance.CourseId)) &&
                (newStart < i.ScheduledEnd && newEnd > i.ScheduledStart)
            );

            if (conflict != null)
            {
                var conflictType = conflict.FacultyId == instance.FacultyId ? "Faculty" : "Course/Students";
                return Json(new {
                    success = false,
                    message = $"Conflict with '{conflict.Title}' ({conflict.ScheduledStart:hh:mm tt} - {conflict.ScheduledEnd:hh:mm tt}) for {conflictType}."
                });
            }

            instance.Status = 4; // Rescheduled
            instance.ScheduledStart = newStart;
            instance.ScheduledEnd = newEnd;

            // Re-create Zoom meeting if applicable
            if (instance.MeetingProvider.StartsWith("Zoom", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var joinUrl = await _zoomService.CreateMeetingAsync(instance.Title, newStart, duration);
                    instance.JoinUrl = joinUrl;
                }
                catch { /* keep existing link */ }
            }

            var result = await _instanceManager.AddUpdateAsync(instance, userId);
            return Json(new { success = result, message = result ? "Session rescheduled successfully." : "Failed to reschedule." });
        }

        public class RescheduleRequest { public int InstanceId { get; set; } public DateTime NewStart { get; set; } public int? NewDuration { get; set; } }


        // ═══════════════════════════════════════════════════
        //           GET STUDENTS FOR ATTENDANCE (AJAX)
        // ═══════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> GetStudentsForAttendance(int instanceId)
        {
            var instance = await _instanceManager.GetAsync(instanceId, new[] { "Course" });
            if (instance == null)
                return Json(new { success = false, message = "Session not found." });

            var enrollments = await _enrollmentManager.GetAsync(new[] { "Student" }, e => e.CourseId == instance.CourseId && e.IsActive);
            var existingAttendances = await _attendanceManager.GetAsync(null, a => a.ClassInstanceId == instanceId && a.IsActive);
            var presentStudentIds = existingAttendances.Where(a => a.AttendanceStatus == 1).Select(a => a.StudentId).ToHashSet();

            var students = enrollments
                .Where(e => e.Student != null)
                .Select(e => new
                {
                    studentId = e.StudentId,
                    studentName = e.Student?.Name ?? ("Student #" + e.StudentId),
                    studentEmail = e.Student?.Email ?? "",
                    isPresent = presentStudentIds.Contains(e.StudentId)
                })
                .OrderBy(s => s.studentName)
                .ToList();

            return Json(new {
                success = true,
                students,
                sessionTitle = instance.Title,
                courseName = instance.CourseName ?? "Course Session",
                scheduledDate = instance.ScheduledStart.ToString("dd MMM yyyy (hh:mm tt)")
            });
        }


        // ═══════════════════════════════════════════════════
        //           MARK ATTENDANCE (Faculty/Admin)
        // ═══════════════════════════════════════════════════

        [HttpPost]
        public async Task<IActionResult> MarkAttendance([FromBody] AttendanceRequest request)
        {
            int userId = GetUserId();
            var instance = await _instanceManager.GetAsync(request.InstanceId);
            if (instance == null)
                return Json(new { success = false, message = "Session not found." });

            var enrollments = await _enrollmentManager.GetAsync(null, e => e.CourseId == instance.CourseId && e.IsActive);
            var existingAttendances = await _attendanceManager.GetAsync(null, a => a.ClassInstanceId == request.InstanceId && a.IsActive);
            var presentSet = (request.PresentStudentIds ?? new List<int>()).ToHashSet();

            int duration = (int)(instance.ScheduledEnd - instance.ScheduledStart).TotalMinutes;
            if (duration <= 0) duration = 60;

            foreach (var enrollment in enrollments)
            {
                var existing = existingAttendances.FirstOrDefault(a => a.StudentId == enrollment.StudentId);
                bool isPresent = presentSet.Contains(enrollment.StudentId);

                if (existing != null)
                {
                    existing.AttendanceStatus = isPresent ? 1 : 0;
                    existing.JoinedAt = isPresent ? (existing.JoinedAt ?? instance.ScheduledStart) : null;
                    existing.LeftAt = isPresent ? (existing.LeftAt ?? instance.ScheduledEnd) : null;
                    existing.DurationMinutes = isPresent ? duration : 0;
                    existing.Source = "Manual";
                    await _attendanceManager.AddUpdateAsync(existing, userId);
                }
                else
                {
                    var att = new ClassAttendanceViewModel
                    {
                        ClassInstanceId = request.InstanceId,
                        StudentId = enrollment.StudentId,
                        AttendanceStatus = isPresent ? 1 : 0,
                        JoinedAt = isPresent ? instance.ScheduledStart : null,
                        LeftAt = isPresent ? instance.ScheduledEnd : null,
                        DurationMinutes = isPresent ? duration : 0,
                        Source = "Manual",
                        IsActive = true
                    };
                    await _attendanceManager.AddUpdateAsync(att, userId);
                }
            }

            return Json(new { success = true, message = $"Attendance recorded for {presentSet.Count} present student(s)." });
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
                var allSubjects = await _subjectManager.GetAsync(null, s => s.IsActive);
                if (courseId <= 0)
                {
                    return Json(allSubjects.Select(s => new { id = s.Id, name = s.Name }));
                }

                // Get course to find its semester
                var course = await _courseManager.GetAsync(courseId, new[] { "Semester" });
                List<SubjectMasterViewModel> subjects = new();

                if (course?.SemesterId != null && course.SemesterId > 0)
                {
                    subjects = allSubjects.Where(s => s.SemesterId == course.SemesterId.Value).ToList();
                }

                if (!subjects.Any())
                {
                    subjects = allSubjects.ToList();
                }

                return Json(subjects.Select(s => new { id = s.Id, name = s.Name }));
            }
            catch (Exception)
            {
                return Json(new List<object>());
            }
        }


        // ═══════════════════════════════════════════════════
        //           GET FACULTIES (AJAX)
        // ═══════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> GetFaculties()
        {
            try
            {
                var allUsers = await _userManager.GetAsync(new[] { "Role" }, u => u.IsActive);
                var facultyUsers = allUsers
                    .Where(u => u.Role != null && (
                        u.Role.Name.Contains("Faculty", StringComparison.OrdinalIgnoreCase) ||
                        u.Role.Name.Contains("Teacher", StringComparison.OrdinalIgnoreCase) ||
                        u.Role.Name.Contains("Instructor", StringComparison.OrdinalIgnoreCase) ||
                        u.Role.Name.Contains("Trainer", StringComparison.OrdinalIgnoreCase) ||
                        u.Role.Name.Contains("Admin", StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(u => u.Name)
                    .ToList();

                if (!facultyUsers.Any())
                {
                    facultyUsers = allUsers.OrderBy(u => u.Name).ToList();
                }

                return Json(facultyUsers.Select(f => new { id = f.Id, name = f.Name, role = f.Role?.Name ?? "Instructor" }));
            }
            catch (Exception)
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
