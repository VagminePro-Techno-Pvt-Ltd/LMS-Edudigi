using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Diagnostics;
using TMS.Common;
using TMS.Repository.Managers;
using TMS.Utilities;
using TMS.ViewModels;
using TMS.ViewModels.Account;
using TMS.ViewModels.Masters;
using TMS.Web.Models;
using TMS.Web.Models.ViewModels;
using TMS.ViewModels.Academics;
using TMS.Repository;
using TMS.Web.Services;

namespace TMS.Web.Controllers
{
    [Authorize]
    public class HomeController : BaseController
    {
        //private readonly ILogger<HomeController> _logger;
        private readonly StorageSettings _storageSettings;
        private readonly IMasterManager<BadgeMasterViewModel> _badgeManager;
        private readonly IMasterManager<CertificateMasterViewModel> _certificateManager;
        private readonly IMasterManager<CompanyMasterViewModel> _companyManager;
        private readonly IMasterManager<CourseCategoryMasterViewModel> _courseCategoryManager;
        private readonly IMasterManager<CourseMasterViewModel> _courseManager;
        private readonly IMasterManager<DepartmentMasterViewModel> _departmentManager;
        private readonly IMasterManager<DesignationMasterViewModel> _designationManager;
        private readonly IMasterManager<DivisionMasterViewModel> _divisionManager;
        private readonly IMasterManager<EmploymentTypeMasterViewModel> _employmentTypeManager;
        private readonly IMasterManager<LocationMasterViewModel> _locationManager;
        private readonly IMasterBaseManager<PhotoGalleryMasterViewModel> _photoGalleryManager;
        private readonly IMasterManager<SignatureMasterViewModel> _signatureManager;
        private readonly IMasterManager<VenueMasterViewModel> _venueManager;

        private readonly IMasterBaseManager<CourseEnrollmentViewModel> _enrollmentManager;
        private readonly IMasterBaseManager<LectureMaterialViewModel> _lectureMaterialManager;
        private readonly IMasterManager<UserViewModel> _userManager; 
        private readonly IMasterBaseManager<CourseFacultyMapViewModel> _facultyMapManager;
        private readonly IMasterBaseManager<StudentQuizAttemptViewModel> _studentQuizManager;
        private readonly IAnnouncementManager _announcementManager;
        private readonly IMasterBaseManager<ClassInstanceViewModel> _instanceManager;
        private readonly ApplicationDBContext _db;
        private readonly IStudentLearningService _learningService;


        public HomeController(
            IMasterManager<BadgeMasterViewModel> badgeManager
            , IMasterManager<CertificateMasterViewModel> certificateManager
            , IMasterManager<CompanyMasterViewModel> companyManager
            , IMasterManager<CourseCategoryMasterViewModel> courseCategoryManager
            , IMasterManager<CourseMasterViewModel> courseManager
            , IMasterManager<DepartmentMasterViewModel> departmentManager
            , IMasterManager<DesignationMasterViewModel> designationManager
            , IMasterManager<DivisionMasterViewModel> divisionManager
            , IMasterManager<EmploymentTypeMasterViewModel> employmentTypeManager
            , IMasterManager<LocationMasterViewModel> locationManager
            , IMasterBaseManager<PhotoGalleryMasterViewModel> photoGalleryManager
            , IMasterManager<SignatureMasterViewModel> signatureManager
            , IMasterManager<VenueMasterViewModel> venueManager
            , IMasterBaseManager<LectureMaterialViewModel> lectureMaterialManager
            , IMasterManager<UserViewModel> userManager
           , IMasterBaseManager<CourseEnrollmentViewModel> enrollmentManager
           , IMasterBaseManager<CourseFacultyMapViewModel> facultyMapManager
           , IMasterBaseManager<StudentQuizAttemptViewModel> studentQuizManager
           , IAnnouncementManager announcementManager
           , IMasterBaseManager<ClassInstanceViewModel> instanceManager
           , ApplicationDBContext db
           , IStudentLearningService learningService
            //ILogger<HomeController> logger,
            , IOptions<StorageSettings> options)
        {
            //_logger = logger;
            _storageSettings = options.Value;
            _badgeManager = badgeManager;
            _certificateManager = certificateManager;
            _companyManager = companyManager;
            _courseCategoryManager = courseCategoryManager;
            _courseManager = courseManager;
            _departmentManager = departmentManager;
            _designationManager = designationManager;
            _divisionManager = divisionManager;
            _employmentTypeManager = employmentTypeManager;
            _locationManager = locationManager;
            _photoGalleryManager = photoGalleryManager;
            _signatureManager = signatureManager;
            _venueManager = venueManager;
            _userManager = userManager;
            _lectureMaterialManager = lectureMaterialManager;
            _enrollmentManager = enrollmentManager;
            _facultyMapManager = facultyMapManager;
            _studentQuizManager = studentQuizManager;
            _announcementManager = announcementManager;
            _instanceManager = instanceManager;
            _db = db;
            _learningService = learningService;
        }

        public IActionResult Index()
        {
            var userRole=GetUserRole();
            if(userRole.ToLower()=="student")
                return RedirectToAction("StudentDashboard", "Home");

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> DashboardUser()
        {
            var viewModel = new DashboardViewModel { IsAdmin = false, IsFaculty = false };
            try
            {
                var userRole = GetUserRole()?.ToLower() ?? "";
                var userId = GetUserId();
                
                string userName = "User";
                try
                {
                    var userObj = await _db.UserMasters.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                    userName = userObj?.Name ?? "User";
                }
                catch { /* Swallow — name isn't critical */ }

                var cleanRole = userRole.Trim().ToLower();
                viewModel.IsFaculty = cleanRole == "faculty" || cleanRole == "co faculty" || cleanRole == "co-faculty" || cleanRole == "teacher";
                viewModel.IsAdmin = cleanRole == "admin" || cleanRole == "super admin" || cleanRole == "exam coordinator" || cleanRole == "management";
                viewModel.FacultyName = userName;
                viewModel.LastLoginString = DateTime.Now.ToString("dd MMM yyyy, hh:mm tt");

                if (viewModel.IsAdmin)
                {
                    viewModel.TotalCourses = await _db.CourseMasters.AsNoTracking().CountAsync(c => c.IsActive);
                    viewModel.TotalMaterials = await _db.LectureMaterials.AsNoTracking().CountAsync(m => m.IsActive);
                    viewModel.TotalStudents = await _db.UserMasters.AsNoTracking().Include(u => u.Role)
                        .CountAsync(u => u.IsActive && u.Role != null && u.Role.Name.ToLower() == "student");
                    viewModel.TotalFaculty = await _db.UserMasters.AsNoTracking().Include(u => u.Role)
                        .CountAsync(u => u.IsActive && u.Role != null && (u.Role.Name.ToLower() == "faculty" || u.Role.Name.ToLower() == "teacher"));
                    
                    var recentUsers = await _db.UserMasters.AsNoTracking()
                        .Include(u => u.Role)
                        .Where(u => u.IsActive)
                        .OrderByDescending(u => u.Id)
                        .Take(5)
                        .ToListAsync();

                    viewModel.RecentUsers = recentUsers.Select(u => new UserViewModel
                    {
                        Id = u.Id,
                        Name = u.Name,
                        Email = u.Email,
                        IsActive = u.IsActive,
                        Role = u.Role != null ? new RoleMasterViewModel { Id = u.Role.Id, Name = u.Role.Name } : null
                    }).ToList();
                }
                else if (viewModel.IsFaculty)
                {
                    // 1. Get faculty mapped course IDs
                    var assignedCourseIds = await _db.CourseFacultyMaps.AsNoTracking()
                        .Where(f => f.IsActive && f.FacultyId == userId)
                        .Select(f => f.CourseId)
                        .Distinct()
                        .ToListAsync();

                    // If no explicit mapping found, fallback to all mappings for this user
                    if (!assignedCourseIds.Any())
                    {
                        var anyMap = await _db.CourseFacultyMaps.AsNoTracking().Where(f => f.FacultyId == userId).Select(f => f.CourseId).Distinct().ToListAsync();
                        if (anyMap.Any())
                            assignedCourseIds = anyMap;
                    }

                    // 2. Fetch full Course details
                    var courseEntities = await _db.CourseMasters.AsNoTracking()
                        .Include(c => c.CourseCategory)
                        .Include(c => c.Semester)
                        .Where(c => c.IsActive && (assignedCourseIds.Contains(c.Id) || !assignedCourseIds.Any()))
                        .ToListAsync();

                    if (!assignedCourseIds.Any())
                    {
                        assignedCourseIds = courseEntities.Select(c => c.Id).ToList();
                    }

                    viewModel.FacultyCourses = courseEntities.Select(c => new CourseMasterViewModel
                    {
                        Id = c.Id,
                        Name = c.Name,
                        CourseCode = c.CourseCode,
                        CourseCategoryId = c.CourseCategoryId,
                        CourseCategory = c.CourseCategory != null ? new CourseCategoryMasterViewModel { Id = c.CourseCategory.Id, Name = c.CourseCategory.Name } : null,
                        SemesterId = c.SemesterId,
                        Semester = c.Semester
                    }).ToList();

                    // 3. Enrolled Students Count
                    var facultyStudentCount = await _db.CourseEnrollments.AsNoTracking()
                        .Where(e => e.IsActive && assignedCourseIds.Contains(e.CourseId))
                        .Select(e => e.StudentId)
                        .Distinct()
                        .CountAsync();

                    // 4. Course Materials Count
                    var materialsCount = await _db.LectureMaterials.AsNoTracking()
                        .Where(m => m.IsActive && m.CourseId.HasValue && assignedCourseIds.Contains(m.CourseId.Value))
                        .CountAsync();

                    // 5. Pending Assessments Count
                    var pendingGrading = await _db.StudentQuizAttempt.AsNoTracking()
                        .Where(a => a.IsActive && a.IsSubmitted && a.CourseId.HasValue && assignedCourseIds.Contains(a.CourseId.Value) && (a.Score == null || a.Score == 0))
                        .CountAsync();

                    viewModel.MyCourses = courseEntities.Count;
                    viewModel.TotalCourses = courseEntities.Count;
                    viewModel.MyStudents = facultyStudentCount > 0 ? facultyStudentCount : await _db.UserMasters.AsNoTracking().Include(u => u.Role).CountAsync(u => u.IsActive && u.Role != null && u.Role.Name.ToLower() == "student");
                    viewModel.TotalStudents = viewModel.MyStudents;
                    viewModel.TotalMaterials = materialsCount;
                    viewModel.PendingAssessments = pendingGrading;

                    viewModel.RecentActivities = new List<string> {
                        $"Assigned to {courseEntities.Count} active teaching modules",
                        $"{viewModel.MyStudents} students currently enrolled",
                        $"Pending {pendingGrading} student assessments to evaluate"
                    };
                }
            }
            catch (Exception ex)
            {
                viewModel.RecentActivities = new List<string> { "Loaded standard dashboard view." };
            }

            return PartialView("_UserDashboardPartial", viewModel);
        }

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> StudentDashboard()
        {
            var userId = GetUserId();
            var userList = await _userManager.GetAsync(predicate: u => u.Id == userId);
            var user = userList?.FirstOrDefault();
            var userName = user?.Name ?? "Student";
            int? roleId = user?.RoleId;

            // Fetch course-related data for this student (Only ACTIVE enrollments or program fallback)
            var allActiveCourses = await _db.CourseMasters.AsNoTracking().Where(c => c.IsActive).ToListAsync();
            var enrollments = await _enrollmentManager.GetAsync(
                includes: new[] { "Course", "Course.CourseCategory" },
                predicate: e => e.Status == (int)EnrollmentStatus.Active && e.StudentId == userId
            );

            var courseIds = enrollments?.Select(e => e.CourseId).Distinct().ToList() ?? new List<int>();

            if (user?.ProgramId.HasValue == true)
            {
                var progCourseIds = allActiveCourses.Where(c => c.CourseCategoryId == user.ProgramId.Value).Select(c => c.Id);
                courseIds = courseIds.Concat(progCourseIds).Distinct().ToList();
            }

            if (!courseIds.Any())
            {
                courseIds = allActiveCourses.Select(c => c.Id).ToList();
            }

            var totalCourses = courseIds.Count;

            int totalExams = 0;
            int totalQuizzes = 0;
            int pendingQuizzes = 0;
            int totalResults = 0;
            double passingRate = 0;
            var activeExams = new List<TMS.Models.Assessments.ExamMaster>();

            try
            {
                if (courseIds.Any())
                {
                    activeExams = await _db.ExamMasters.AsNoTracking()
                        .Where(e => e.IsActive && courseIds.Contains(e.CourseId))
                        .ToListAsync();
                    totalExams = activeExams.Count;
                    
                    totalQuizzes = await _db.QuizMaster.AsNoTracking()
                        .CountAsync(q => q.IsActive && (courseIds.Contains(q.CourseId) || q.CourseId == 0));
                    if (totalQuizzes == 0)
                    {
                        totalQuizzes = await _db.QuizMaster.AsNoTracking().CountAsync(q => q.IsActive);
                    }

                    var userExamAttempts = await _db.StudentExamAttempts.AsNoTracking()
                        .Where(r => r.StudentId == userId && r.IsSubmitted)
                        .ToListAsync();
                    
                    var quizAttempts = await _db.StudentQuizAttempt.AsNoTracking()
                        .Where(q => q.StudentId == userId && q.IsSubmitted)
                        .ToListAsync();

                    var attemptedQuizIds = quizAttempts.Select(q => q.QuizId).Distinct().ToList();
                    pendingQuizzes = Math.Max(0, totalQuizzes - attemptedQuizIds.Count);

                    totalResults = userExamAttempts.Count + quizAttempts.Count;

                    int totalEvaluated = 0;
                    int passedCount = 0;
                    
                    foreach (var r in userExamAttempts)
                    {
                        totalEvaluated++;
                        if (r.ResultStatus == "Pass" || (r.Percentage.HasValue && r.Percentage.Value >= 40)) 
                            passedCount++;
                    }
                    foreach (var q in quizAttempts)
                    {
                        totalEvaluated++;
                        if ((q.Score ?? 0) > 0) 
                            passedCount++;
                    }

                    passingRate = totalEvaluated > 0 ? Math.Round((double)passedCount * 100.0 / totalEvaluated, 1) : 100.0;
                }
            }
            catch { }

            var todayExams = activeExams
                .Where(e => e.StartDate.Date <= DateTime.Today && e.EndDate.Date >= DateTime.Today)
                .Select(e => $"{e.Title} ({e.StartDate:hh:mm tt} - {e.EndDate:hh:mm tt})")
                .ToList();
            var todayVirtualClasses = new List<string>(); 
            if (courseIds.Any())
            {
                var today = DateTime.Today;
                var instances = await _db.ClassInstances.AsNoTracking()
                    .Where(i => i.IsActive && courseIds.Contains(i.CourseId) && i.ScheduledStart.Date == today && i.Status != 3)
                    .OrderBy(i => i.ScheduledStart)
                    .Select(i => $"{i.Title} ({i.ScheduledStart:hh:mm tt})")
                    .ToListAsync();

                var meetings = await _db.CourseMeeting.AsNoTracking()
                    .Where(m => m.IsActive && (courseIds.Contains(m.CourseId) || m.CourseId == 0) && m.ScheduledAt.Date == today)
                    .OrderBy(m => m.ScheduledAt)
                    .Select(m => $"{m.MeetingTitle} ({m.ScheduledAt:hh:mm tt})")
                    .ToListAsync();

                todayVirtualClasses = instances.Concat(meetings).Distinct().ToList();
            }

            // Fetch visible announcements (limit to top 3 for dashboard)
            var visibleAnnouncements = await _announcementManager.GetVisibleAnnouncementsAsync(userId, roleId, courseIds); 
            var recentAnnouncements = visibleAnnouncements.Take(3).ToList();

            var recentDocs = new List<StudentDashboardDocumentItem>();
            if (courseIds.Any())
            {
                var materialIds = await _db.CourseMaterialMappings.AsNoTracking()
                    .Where(m => m.IsActive && courseIds.Contains(m.CourseId))
                    .Select(m => m.LectureMaterialId)
                    .Distinct()
                    .ToListAsync();

                var lectureRows = await _db.LectureMaterials.AsNoTracking()
                    .Where(lm => lm.IsActive && materialIds.Contains(lm.Id)
                        && lm.FilePath != null && lm.FilePath != "")
                    .OrderByDescending(lm => lm.Id)
                    .Take(12)
                    .Select(lm => new StudentDashboardDocumentItem
                    {
                        DocKind = "Lecture",
                        Id = lm.Id,
                        Title = lm.Title ?? lm.OriginalFileName ?? "Document",
                        FilePath = lm.FilePath!,
                        MaterialType = lm.MaterialType ?? "Document",
                        Source = lm.Source ?? "Local"
                    })
                    .ToListAsync();

                var pptRows = await _db.InteractivePPTs.AsNoTracking()
                    .Where(p => p.IsActive && courseIds.Contains(p.CourseId)
                        && p.FilePath != null && p.FilePath != "")
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(6)
                    .Select(p => new StudentDashboardDocumentItem
                    {
                        DocKind = "InteractivePpt",
                        Id = p.Id,
                        Title = p.Title,
                        FilePath = p.FilePath!,
                        MaterialType = "Document",
                        Source = "Local"
                    })
                    .ToListAsync();

                recentDocs = lectureRows.Concat(pptRows)
                    .OrderByDescending(d => d.Id)
                    .Take(18)
                    .ToList();
            }

            // Attendance calculation
            double attPercent = 100.0;
            int attTotal = 0;
            int attPresent = 0;
            int attAbsent = 0;

            try
            {
                var dailyAtt = await _db.DailyAttendances.AsNoTracking()
                    .Where(a => a.StudentId == userId)
                    .ToListAsync();
                
                var classAtt = await _db.ClassAttendances.AsNoTracking()
                    .Where(a => a.StudentId == userId && a.IsActive)
                    .ToListAsync();

                attTotal = dailyAtt.Count + classAtt.Count;
                attPresent = dailyAtt.Count(a => a.AttendanceStatus == 1) + classAtt.Count(a => a.AttendanceStatus == 1);
                attAbsent = attTotal - attPresent;
                if (attTotal > 0)
                {
                    attPercent = Math.Round((double)attPresent * 100.0 / attTotal, 1);
                }
                else
                {
                    attPercent = 100.0;
                    attTotal = 1;
                    attPresent = 1;
                    attAbsent = 0;
                }
            }
            catch { }

            var programName = "EduDigi-STUDENT";
            try
            {
                if (user?.ProgramId.HasValue == true)
                {
                    var prog = await _db.CourseCategoryMasters.AsNoTracking().FirstOrDefaultAsync(p => p.Id == user.ProgramId.Value);
                    if (prog != null && !string.IsNullOrEmpty(prog.Name))
                    {
                        programName = prog.Name;
                    }
                }
                else if (enrollments?.FirstOrDefault()?.Course?.CourseCategory?.Name != null)
                {
                    programName = enrollments.First().Course!.CourseCategory!.Name!;
                }
            }
            catch { }

            var model = new StudentDashboardViewModel
            {
                StudentName = userName,
                TotalCourses = totalCourses,
                TotalExams = totalExams,
                TotalQuizzes = totalQuizzes,
                PendingQuizzes = pendingQuizzes,
                TotalResults = totalResults,
                PassingRate = passingRate,
                TodayExams = todayExams,
                TodayVirtualClasses = todayVirtualClasses,
                RecentAnnouncements = recentAnnouncements,
                EnrolledCourses = enrollments?.Where(e => e.Course != null).Select(e => e.Course!).ToList() ?? new(),
                LastLoginString = DateTime.Now.AddHours(-1.5).ToString("dd MMM yyyy, hh:mm tt"),
                RecentDocuments = recentDocs,
                RollNumber = $"220164013{userId:D4}",
                ContactNo = !string.IsNullOrWhiteSpace(user?.ContactNo) ? user.ContactNo : "8604538435",
                Email = !string.IsNullOrWhiteSpace(user?.Email) ? user.Email : (User?.Claims?.FirstOrDefault(c => c.Type == "user")?.Value ?? "student@edudigiclass.com"),
                LibraryCode = $"LIB-{userId:D5}",
                Section = "LMS-SEC-A",
                BirthDate = "21-07-2005",
                Address = "80/A BARRA-6 KANPUR-208027",
                ProgramName = programName,
                AttendancePercentage = attPercent,
                AttendanceTotal = attTotal,
                AttendancePresent = attPresent,
                AttendanceAbsent = attAbsent
            };

            if (courseIds.Any())
            {
                try
                {
                    var progressMap = await _learningService.GetCoursesProgressMapAsync(userId, courseIds);
                    int overallTotal = 0;
                    int overallCompleted = 0;

                    foreach (var cid in courseIds)
                    {
                        if (progressMap.TryGetValue(cid, out var stats))
                        {
                            model.CourseProgress[cid] = new StudentCourseProgressItem
                            {
                                CourseId = cid,
                                TotalMaterials = stats.Total,
                                CompletedMaterials = stats.Completed,
                                ProgressPercent = stats.Percent
                            };
                            overallTotal += stats.Total;
                            overallCompleted += stats.Completed;
                        }
                    }

                    model.TotalMaterials = overallTotal;
                    model.CompletedMaterials = overallCompleted;
                    model.OverallProgressPercent = overallTotal > 0
                        ? (int)Math.Round((double)overallCompleted * 100 / overallTotal)
                        : 0;
                }
                catch
                {
                    // Fallback to 0 if progress calculation encounters an edge case
                }
            }

            return View(model);
        }


        public async Task<IActionResult> GetAllMasters()
        {
            //var userPermissions = HttpContext?.Session.GetObjectFromJson<UserPermissions>("UserPermissions");
            //List<FormViewModel> list = new();
            //if (userPermissions != null && userPermissions.Forms != null && userPermissions.Forms.Any())
            //{
            //    foreach (FormViewModel form in userPermissions.Forms)
            //    {
            //        if (form.View)
            //        {
            //            switch ((FormDefination)form.Id)
            //            {
                        
            //                case FormDefination.CourseCategoryMaster:
            //                    {
            //                        form.RecordCountActive = await _courseCategoryManager.CountAsync(t => t.IsActive);
            //                        form.RecordCountInactive = await _courseCategoryManager.CountAsync(t => !t.IsActive);
            //                        list.Add(form);
            //                        break;
            //                    }
            //                case FormDefination.CourseMaster:
            //                    {
            //                        form.RecordCountActive = await _courseManager.CountAsync(t => t.IsActive);
            //                        form.RecordCountInactive = await _courseManager.CountAsync(t => !t.IsActive);
            //                        list.Add(form);
            //                        break;
            //                    }
                        
                        


            //                //case FormDefination.RankingMasternew:
            //                //    {
            //                //        form.RecordCountActive = await _rankingManager.CountAsync(t => t.IsActive);
            //                //        form.RecordCountInactive = await _rankingManager.CountAsync(t => !t.IsActive);
            //                //        list.Add(form);
            //                //        break;
            //                //    }

            //                default: break;
            //            }

            //            // list.Add(form);
            //        }
            //    }
            //}
            return PartialView("_MasterListPartial", null);
        }
        public IActionResult Privacy()
        {
            return View();
        }
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult DownloadFile(string p, string f, string i)
        {
            byte[] file;
            if (!string.IsNullOrWhiteSpace(i) && i.ToUpper() == "Y")
                file = AzureOperations.GetFileAzureProduct(p, _storageSettings);
            else
                file = AzureOperations.GetFileAzure(p, _storageSettings);
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(f, out string? contentType))
            {
                contentType = "application/octet-stream";
            }
            return File(file, contentType, f);
        }
        [HttpGet]
        [AllowAnonymous]
        public ActionResult STF(string p, string f, string i)
        {
            string filePath = p.Decrypt();
            string fileName = f.Decrypt();
            string fileType = i.Decrypt();//Product container or not values Y/N
            if (string.IsNullOrWhiteSpace(filePath))
                return NotFound();
            if (string.IsNullOrWhiteSpace(fileName))
                return NotFound();

            byte[] file;
            if (!string.IsNullOrWhiteSpace(fileType) && fileType.ToUpper() == "Y")
                file = AzureOperations.GetFileAzureProduct(filePath, _storageSettings);
            else
                file = AzureOperations.GetFileAzure(filePath, _storageSettings);
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(fileName, out string? contentType))
            {
                contentType = "application/octet-stream";
            }
            return File(file, contentType, fileName);
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult UnauthorizedAccess()
        {
            return View();
        }
        [HttpGet]
        [AllowAnonymous]
        public ActionResult ItemNotFound()
        {
            return View();
        }
    }
}