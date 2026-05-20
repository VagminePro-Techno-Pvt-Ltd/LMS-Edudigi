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
                    var userList = await _userManager.GetAsync(predicate: u => u.Id == userId);
                    userName = userList?.FirstOrDefault()?.Name ?? "User";
                }
                catch { /* Swallow — name isn't critical */ }

                var cleanRole = userRole.Trim().ToLower();
                viewModel.IsFaculty = cleanRole == "faculty" || cleanRole == "co faculty" || cleanRole == "co-faculty" || cleanRole == "teacher";
                viewModel.IsAdmin = cleanRole == "admin" || cleanRole == "super admin" || cleanRole == "exam coordinator" || cleanRole == "management";
                viewModel.FacultyName = userName;
                viewModel.LastLoginString = DateTime.Now.AddHours(-2.5).ToString("dd MMM yyyy, hh:mm tt");

                if (viewModel.IsAdmin)
                {
                    try
                    {
                        var courses = await _courseManager.GetAsync();
                        viewModel.TotalCourses = courses?.Count() ?? 0;
                    }
                    catch { viewModel.TotalCourses = 0; }

                    try
                    {
                        var materials = await _lectureMaterialManager.GetAsync();
                        viewModel.TotalMaterials = materials?.Count() ?? 0;
                    }
                    catch { viewModel.TotalMaterials = 0; }

                    try
                    {
                        var users = await _userManager.GetAsync(new[] { "Role" });
                        viewModel.TotalStudents = users?.Count(u => u.Role != null && u.Role.Name != null && u.Role.Name.ToLower() == "student") ?? 0;
                        viewModel.TotalFaculty = users?.Count(u => u.Role != null && u.Role.Name != null && u.Role.Name.ToLower() == "faculty") ?? 0;
                    }
                    catch
                    {
                        viewModel.TotalStudents = 0;
                        viewModel.TotalFaculty = 0;
                    }

                    try
                    {
                        var enrollments = await _enrollmentManager.GetAsync(
                            includes: new[] { "Course", "Course.CourseCategory" },
                            predicate: e => e.IsActive
                        );

                        viewModel.CourseCategoryStats = enrollments?
                            .Where(e => e.Course != null && e.Course.CourseCategory != null)
                            .GroupBy(e => e.Course!.CourseCategory!.Name ?? "Uncategorized")
                            .Select(g => new CourseCategoryStatViewModel
                            {
                                CategoryName = g.Key,
                                TotalCourses = g.Select(e => e.CourseId).Distinct().Count(),
                                TotalStudents = g.Select(e => e.StudentId).Distinct().Count()
                            })
                            .OrderByDescending(g => g.TotalStudents)
                            .ToList() ?? new List<CourseCategoryStatViewModel>();
                    }
                    catch { viewModel.CourseCategoryStats = new List<CourseCategoryStatViewModel>(); }
                }
                else if (viewModel.IsFaculty)
                {
                    var facultyCoursesMap = await _facultyMapManager.GetAsync(null, f => f.IsActive && f.FacultyId == userId);
                    var assignedCourseIds = facultyCoursesMap.Select(f => f.CourseId).ToList();

                    var enrollments = await _enrollmentManager.GetAsync(predicate: e => e.IsActive);
                    var facultyStudentIds = enrollments?.Where(e => assignedCourseIds.Contains(e.CourseId))
                                                       .Select(e => e.StudentId)
                                                       .Distinct()
                                                       .ToList();
                    
                    int pendingGrading = 0;
                    try
                    {
                        var allAttempts = await _studentQuizManager.GetAsync(null, a => a.IsSubmitted);
                        var attemptsInMyCourses = allAttempts.Where(a => assignedCourseIds.Contains(a.CourseId)).ToList();
                        pendingGrading = attemptsInMyCourses.Count(a => a.Score == null || a.Score == 0);
                    }
                    catch { /* Quiz table might not exist yet */ }

                    viewModel.MyCourses = assignedCourseIds.Count;
                    viewModel.MyStudents = facultyStudentIds?.Count ?? 0;
                    viewModel.PendingAssessments = pendingGrading;

                    viewModel.RecentActivities = new List<string> {
                        "Course Content updated assigned limit reached",
                        "New Students joined your recent course",
                        $"Pending {pendingGrading} manual quiz assessments"
                    };
                }
            }
            catch (Exception ex)
            {
                viewModel.RecentActivities = new List<string> { "Error loading some insights. Please contact admin." };
            }

            return PartialView("_UserDashboardPartial", viewModel);
        }

        [Authorize(Roles = "Student")]
        public async Task<IActionResult> StudentDashboard()
        {
            var userId = GetUserId(); // Assuming BaseController has this helper
            var userList = await _userManager.GetAsync(predicate: u => u.Id == userId);
            var user = userList?.FirstOrDefault();
            var userName = user?.Name ?? "Student";
            int? roleId = user?.RoleId;

            // Fetch course-related data for this student (Only ACTIVE enrollments)
            var enrollments = await _enrollmentManager.GetAsync(
                includes: new[] { "Course", "Course.CourseCategory" },
                predicate: e => e.Status == (int)EnrollmentStatus.Active && e.StudentId == userId
            );

            var totalCourses = enrollments?.Select(e => e.CourseId).Distinct().Count() ?? 0;
            var totalExams = 0; // Replace with real count later if you have exams table
            var totalResults = 0; // Replace with real data later
            var passingRate = 0; // Replace with actual logic

            var todayExams = new List<string>(); // Add logic to fetch today’s exams
            var courseIds = enrollments?.Select(e => e.CourseId).Distinct().ToList();
            var todayVirtualClasses = new List<string>(); 
            if (courseIds != null && courseIds.Any())
            {
                var instancesData = await _instanceManager.GetAsync();
                var instances = instancesData != null 
                    ? instancesData.Where(i => i.IsActive && courseIds.Contains(i.CourseId) && i.ScheduledStart.Date == DateTime.Today && i.Status != 3).ToList()
                    : new List<ClassInstanceViewModel>();

                if (instances != null && instances.Any())
                {
                    todayVirtualClasses = instances
                        .OrderBy(i => i.ScheduledStart)
                        .Select(i => $"{i.Title} ({i.ScheduledStart:hh:mm tt})")
                        .ToList();
                }
            }

            // Fetch visible announcements (limit to top 3 for dashboard)
            var visibleAnnouncements = await _announcementManager.GetVisibleAnnouncementsAsync(userId, roleId, courseIds); 
            var recentAnnouncements = visibleAnnouncements.Take(3).ToList();

            var recentDocs = new List<StudentDashboardDocumentItem>();
            if (courseIds != null && courseIds.Any())
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

            var model = new StudentDashboardViewModel
            {
                StudentName = userName,
                TotalCourses = totalCourses,
                TotalExams = totalExams,
                TotalResults = totalResults,
                PassingRate = passingRate,
                TodayExams = todayExams,
                TodayVirtualClasses = todayVirtualClasses,
                RecentAnnouncements = recentAnnouncements,
                EnrolledCourses = enrollments?.Where(e => e.Course != null).Select(e => e.Course!).ToList() ?? new(),
                LastLoginString = DateTime.Now.AddHours(-1.5).ToString("dd MMM yyyy, hh:mm tt"),
                RecentDocuments = recentDocs
            };

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