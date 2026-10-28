using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TMS.Models.Academics;
using TMS.Models.Masters;
using TMS.Repository;
using TMS.ViewModels.Academics;

namespace TMS.Web.Services
{
    public class StudentAtRiskService : IStudentAtRiskService
    {
        private readonly ApplicationDBContext _db;
        private readonly IContentModerationService _moderationService;
        private readonly ILogger<StudentAtRiskService>? _logger;
        private static bool _schemaEnsured = false;
        private static readonly object SchemaLock = new();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, DateTime> CourseSyncThrottle = new();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, DateTime> StudentSyncThrottle = new();

        public StudentAtRiskService(
            ApplicationDBContext db,
            IContentModerationService moderationService,
            ILogger<StudentAtRiskService>? logger = null)
        {
            _db = db;
            _moderationService = moderationService;
            _logger = logger;
        }

        public async Task EnsureSchemaAsync(CancellationToken ct = default)
        {
            if (_schemaEnsured) return;

            lock (SchemaLock)
            {
                if (_schemaEnsured) return;
            }

            try
            {
                if (_db.Database.IsRelational())
                {
                    const string sql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CourseActivityDeadline')
BEGIN
    CREATE TABLE [dbo].[CourseActivityDeadline](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [CourseId] [int] NOT NULL,
        [ActivityType] [nvarchar](50) NOT NULL,
        [ActivityId] [int] NOT NULL,
        [ActivityTitle] [nvarchar](255) NOT NULL,
        [DeadlineDate] [datetime2](7) NOT NULL,
        [ReminderDaysBefore] [int] NOT NULL DEFAULT 2,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [CreatedBy] [int] NOT NULL DEFAULT 0,
        [CreatedOn] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedOn] [datetime2](7) NULL,
        CONSTRAINT [PK_CourseActivityDeadline] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE UNIQUE NONCLUSTERED INDEX [IX_CourseActivityDeadline_Course_Type_Activity]
    ON [dbo].[CourseActivityDeadline]([CourseId], [ActivityType], [ActivityId]);
    CREATE NONCLUSTERED INDEX [IX_CourseActivityDeadline_CourseId]
    ON [dbo].[CourseActivityDeadline]([CourseId]);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StudentRiskAlert')
BEGIN
    CREATE TABLE [dbo].[StudentRiskAlert](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [StudentId] [int] NOT NULL,
        [CourseId] [int] NOT NULL,
        [ActivityType] [nvarchar](50) NOT NULL,
        [ActivityId] [int] NOT NULL DEFAULT 0,
        [Title] [nvarchar](255) NOT NULL,
        [Message] [nvarchar](max) NOT NULL,
        [RiskLevel] [nvarchar](50) NOT NULL,
        [DeadlineDate] [datetime2](7) NULL,
        [IsResolved] [bit] NOT NULL DEFAULT 0,
        [ResolvedOn] [datetime2](7) NULL,
        [IsRead] [bit] NOT NULL DEFAULT 0,
        [ReadOn] [datetime2](7) NULL,
        [ActionUrl] [nvarchar](500) NULL,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [CreatedBy] [int] NOT NULL DEFAULT 0,
        [CreatedOn] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedOn] [datetime2](7) NULL,
        CONSTRAINT [PK_StudentRiskAlert] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE NONCLUSTERED INDEX [IX_StudentRiskAlert_Student_Resolved]
    ON [dbo].[StudentRiskAlert]([StudentId], [IsResolved]);
    CREATE NONCLUSTERED INDEX [IX_StudentRiskAlert_Course_Student]
    ON [dbo].[StudentRiskAlert]([CourseId], [StudentId]);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ContentModerationFlag')
BEGIN
    CREATE TABLE [dbo].[ContentModerationFlag](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [ContentType] [nvarchar](50) NOT NULL,
        [ContentId] [int] NOT NULL,
        [AuthorUserId] [int] NOT NULL,
        [AuthorName] [nvarchar](255) NULL,
        [ProgramId] [int] NULL,
        [CourseId] [int] NULL,
        [FlaggedText] [nvarchar](max) NOT NULL,
        [DetectedKeywords] [nvarchar](500) NOT NULL,
        [Status] [nvarchar](50) NOT NULL DEFAULT 'PendingReview',
        [ReviewedByUserId] [int] NULL,
        [ReviewedOn] [datetime2](7) NULL,
        [ReviewRemarks] [nvarchar](max) NULL,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [CreatedBy] [int] NOT NULL DEFAULT 0,
        [CreatedOn] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedOn] [datetime2](7) NULL,
        CONSTRAINT [PK_ContentModerationFlag] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE NONCLUSTERED INDEX [IX_ContentModerationFlag_Status]
    ON [dbo].[ContentModerationFlag]([Status]);
    CREATE NONCLUSTERED INDEX [IX_ContentModerationFlag_AuthorUserId]
    ON [dbo].[ContentModerationFlag]([AuthorUserId]);
END;
";
                    await _db.Database.ExecuteSqlRawAsync(sql, ct);
                }
                lock (SchemaLock)
                {
                    _schemaEnsured = true;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "EnsureSchema note: {Message}", ex.Message);
            }
        }

        public async Task<List<CourseMaster>> GetFacultyAssignedCoursesAsync(int facultyUserId, bool isStaff)
        {
            await EnsureSchemaAsync();

            if (isStaff)
            {
                return await _db.CourseMasters
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync();
            }

            var assignedCourseIds = await _db.CourseFacultyMaps
                .AsNoTracking()
                .Where(m => m.FacultyId == facultyUserId && m.IsActive)
                .Select(m => m.CourseId)
                .Distinct()
                .ToListAsync();

            return await _db.CourseMasters
                .AsNoTracking()
                .Where(c => assignedCourseIds.Contains(c.Id) && c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<FacultyCourseActivitiesViewModel> GetCourseActivitiesAsync(
            int courseId,
            int requestingUserId,
            bool isStaff,
            string activityTypeFilter = "All")
        {
            await EnsureSchemaAsync();

            var course = await _db.CourseMasters
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == courseId && c.IsActive);

            if (course == null)
            {
                return new FacultyCourseActivitiesViewModel { CourseId = courseId, CourseName = "Unknown Course" };
            }

            // Authorization check for non-staff
            if (!isStaff)
            {
                bool isMapped = await _db.CourseFacultyMaps
                    .AnyAsync(m => m.CourseId == courseId && m.FacultyId == requestingUserId && m.IsActive);
                if (!isMapped)
                {
                    throw new UnauthorizedAccessException("You are not assigned to this course.");
                }
            }

            var enrolledStudentIds = await _db.CourseEnrollments
                .AsNoTracking()
                .Where(e => e.CourseId == courseId && e.IsActive && e.Status == 1)
                .Select(e => e.StudentId)
                .Distinct()
                .ToListAsync();

            int totalEnrolled = enrolledStudentIds.Count;

            // Fetch custom deadlines configured for this course
            var savedDeadlines = await _db.CourseActivityDeadlines
                .AsNoTracking()
                .Where(d => d.CourseId == courseId && d.IsActive)
                .ToListAsync();

            var deadlineMap = savedDeadlines
                .ToDictionary(d => $"{d.ActivityType}_{d.ActivityId}", d => d);

            var activities = new List<CourseActivityItemViewModel>();

            // 1. VIDEOS & PDFS / DOCUMENTS
            var mappedMaterials = await _db.CourseMaterialMappings
                .AsNoTracking()
                .Include(m => m.LectureMaterial)
                .Where(m => m.CourseId == courseId && m.IsActive && m.LectureMaterial != null && m.LectureMaterial.IsActive)
                .ToListAsync();

            var directMaterials = await _db.LectureMaterials
                .AsNoTracking()
                .Where(m => m.CourseId == courseId && m.IsActive)
                .ToListAsync();

            var allMaterials = mappedMaterials.Select(m => m.LectureMaterial!)
                .Concat(directMaterials)
                .GroupBy(m => m.Id)
                .Select(g => g.First())
                .ToList();

            // Progress records for completion stats
            var matIds = allMaterials.Select(m => m.Id).ToList();
            var materialProgress = await _db.LectureVideoProgresses
                .AsNoTracking()
                .Where(p => matIds.Contains(p.LectureMaterialId) && enrolledStudentIds.Contains(p.StudentId) && p.IsActive)
                .ToListAsync();

            foreach (var mat in allMaterials)
            {
                string mType = (mat.MaterialType ?? "").ToLowerInvariant();
                string fPath = (mat.FilePath ?? "").ToLowerInvariant();
                string ext = Path.GetExtension(fPath).ToLowerInvariant();

                bool isVideo = mType == "video" || mType == "videourl" ||
                               new[] { ".mp4", ".webm", ".ogg", ".avi", ".mov" }.Contains(ext) ||
                               fPath.Contains("youtu") || fPath.Contains("vimeo");

                string actType = isVideo ? "Video" : "Pdf";

                string key = $"{actType}_{mat.Id}";
                deadlineMap.TryGetValue(key, out var savedDl);

                DateTime? dlDate = savedDl?.DeadlineDate;
                int remDays = savedDl?.ReminderDaysBefore ?? 2;

                // Completion count
                int completed = materialProgress
                    .Where(p => p.LectureMaterialId == mat.Id && (p.IsCompleted || (isVideo && p.ProgressPercentage >= 75)))
                    .Select(p => p.StudentId)
                    .Distinct()
                    .Count();

                int pending = Math.Max(0, totalEnrolled - completed);
                bool isOverdue = dlDate.HasValue && DateTime.UtcNow > dlDate.Value;
                int overdueCount = isOverdue ? pending : 0;

                activities.Add(new CourseActivityItemViewModel
                {
                    ActivityType = actType,
                    ActivityId = mat.Id,
                    Title = mat.Title ?? "Untitled Document",
                    UnitOrQuadrant = isVideo ? "Video Lecture" : "E-Text / Document",
                    DeadlineDate = dlDate,
                    ReminderDaysBefore = remDays,
                    HasExistingDeadline = dlDate.HasValue,
                    RemainingText = FormatRemainingTime(dlDate),
                    IsOverdue = isOverdue,
                    TotalEnrolled = totalEnrolled,
                    CompletedCount = completed,
                    PendingCount = pending,
                    OverdueCount = overdueCount
                });
            }

            // 2. ASSIGNMENTS
            var assignments = await _db.AssignmentMasters
                .AsNoTracking()
                .Where(a => (a.CourseId == courseId || (course.CourseCategoryId > 0 && a.ProgramId == course.CourseCategoryId)) && !a.IsDeleted)
                .ToListAsync();

            var assignIds = assignments.Select(a => a.Id).ToList();
            var assignmentSubmissions = await _db.AssignmentSubmissions
                .AsNoTracking()
                .Where(s => assignIds.Contains(s.AssignmentId) && enrolledStudentIds.Contains(s.StudentId) && !s.IsDeleted)
                .ToListAsync();

            foreach (var a in assignments)
            {
                string key = $"Assignment_{a.Id}";
                deadlineMap.TryGetValue(key, out var savedDl);

                // Reuse assignment's existing DueDate if no custom deadline set
                DateTime dlDate = savedDl?.DeadlineDate ?? a.DueDate;
                int remDays = savedDl?.ReminderDaysBefore ?? 2;

                int completed = assignmentSubmissions
                    .Where(s => s.AssignmentId == a.Id)
                    .Select(s => s.StudentId)
                    .Distinct()
                    .Count();

                int pending = Math.Max(0, totalEnrolled - completed);
                bool isOverdue = DateTime.UtcNow > dlDate;
                int overdueCount = isOverdue ? pending : 0;

                activities.Add(new CourseActivityItemViewModel
                {
                    ActivityType = "Assignment",
                    ActivityId = a.Id,
                    Title = a.Title,
                    UnitOrQuadrant = "Assessment & Evaluation",
                    DeadlineDate = dlDate,
                    ReminderDaysBefore = remDays,
                    HasExistingDeadline = true,
                    RemainingText = FormatRemainingTime(dlDate),
                    IsOverdue = isOverdue,
                    TotalEnrolled = totalEnrolled,
                    CompletedCount = completed,
                    PendingCount = pending,
                    OverdueCount = overdueCount
                });
            }

            // 3. QUIZZES
            var quizzes = await _db.QuizMasters
                .AsNoTracking()
                .Where(q => q.CourseId == courseId && q.IsActive)
                .ToListAsync();

            var quizIds = quizzes.Select(q => q.Id).ToList();
            var quizAttempts = await _db.StudentQuizAttempt
                .AsNoTracking()
                .Where(qa => quizIds.Contains(qa.QuizId) && enrolledStudentIds.Contains(qa.StudentId) && qa.IsSubmitted)
                .ToListAsync();

            foreach (var q in quizzes)
            {
                string key = $"Quiz_{q.Id}";
                deadlineMap.TryGetValue(key, out var savedDl);

                DateTime? dlDate = savedDl?.DeadlineDate;
                int remDays = savedDl?.ReminderDaysBefore ?? 2;

                int completed = quizAttempts
                    .Where(qa => qa.QuizId == q.Id)
                    .Select(qa => qa.StudentId)
                    .Distinct()
                    .Count();

                int pending = Math.Max(0, totalEnrolled - completed);
                bool isOverdue = dlDate.HasValue && DateTime.UtcNow > dlDate.Value;
                int overdueCount = isOverdue ? pending : 0;

                activities.Add(new CourseActivityItemViewModel
                {
                    ActivityType = "Quiz",
                    ActivityId = q.Id,
                    Title = q.Title,
                    UnitOrQuadrant = "Self-Assessment Quiz",
                    DeadlineDate = dlDate,
                    ReminderDaysBefore = remDays,
                    HasExistingDeadline = dlDate.HasValue,
                    RemainingText = FormatRemainingTime(dlDate),
                    IsOverdue = isOverdue,
                    TotalEnrolled = totalEnrolled,
                    CompletedCount = completed,
                    PendingCount = pending,
                    OverdueCount = overdueCount
                });
            }

            // 4. DISCUSSIONS
            var discussions = await _db.DiscussionThreads
                .AsNoTracking()
                .Where(t => course.CourseCategoryId > 0 && t.ProgramId == course.CourseCategoryId && t.IsActive)
                .ToListAsync();

            var threadIds = discussions.Select(t => t.Id).ToList();
            var discussionReplies = await _db.DiscussionReplies
                .AsNoTracking()
                .Where(r => threadIds.Contains(r.ThreadId) && enrolledStudentIds.Contains(r.UserId) && r.IsActive)
                .ToListAsync();

            foreach (var d in discussions)
            {
                string key = $"Discussion_{d.Id}";
                deadlineMap.TryGetValue(key, out var savedDl);

                DateTime? dlDate = savedDl?.DeadlineDate;
                int remDays = savedDl?.ReminderDaysBefore ?? 2;

                int completed = discussionReplies
                    .Where(r => r.ThreadId == d.Id)
                    .Select(r => r.UserId)
                    .Distinct()
                    .Count();

                int pending = Math.Max(0, totalEnrolled - completed);
                bool isOverdue = dlDate.HasValue && DateTime.UtcNow > dlDate.Value;
                int overdueCount = isOverdue ? pending : 0;

                activities.Add(new CourseActivityItemViewModel
                {
                    ActivityType = "Discussion",
                    ActivityId = d.Id,
                    Title = d.Title,
                    UnitOrQuadrant = "Collaborative Discussion",
                    DeadlineDate = dlDate,
                    ReminderDaysBefore = remDays,
                    HasExistingDeadline = dlDate.HasValue,
                    RemainingText = FormatRemainingTime(dlDate),
                    IsOverdue = isOverdue,
                    TotalEnrolled = totalEnrolled,
                    CompletedCount = completed,
                    PendingCount = pending,
                    OverdueCount = overdueCount
                });
            }

            // Apply filter
            if (!string.IsNullOrWhiteSpace(activityTypeFilter) && !activityTypeFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                activities = activities.Where(a => a.ActivityType.Equals(activityTypeFilter, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return new FacultyCourseActivitiesViewModel
            {
                CourseId = course.Id,
                CourseName = course.Name ?? "Course",
                CourseCode = course.CourseCode,
                Activities = activities,
                SelectedActivityType = activityTypeFilter
            };
        }

        public async Task<bool> SaveActivityDeadlineAsync(SaveDeadlineRequest request, int userId, bool isStaff)
        {
            await EnsureSchemaAsync();

            if (!isStaff)
            {
                bool isMapped = await _db.CourseFacultyMaps
                    .AnyAsync(m => m.CourseId == request.CourseId && m.FacultyId == userId && m.IsActive);
                if (!isMapped) return false;
            }

            var existing = await _db.CourseActivityDeadlines
                .FirstOrDefaultAsync(d => d.CourseId == request.CourseId &&
                                          d.ActivityType == request.ActivityType &&
                                          d.ActivityId == request.ActivityId);

            if (existing != null)
            {
                existing.DeadlineDate = request.DeadlineDate;
                existing.ReminderDaysBefore = request.ReminderDaysBefore > 0 ? request.ReminderDaysBefore : 2;
                existing.ActivityTitle = request.ActivityTitle;
                existing.UpdatedBy = userId;
                existing.UpdatedOn = DateTime.UtcNow;
                existing.IsActive = true;
            }
            else
            {
                existing = new CourseActivityDeadline
                {
                    CourseId = request.CourseId,
                    ActivityType = request.ActivityType,
                    ActivityId = request.ActivityId,
                    ActivityTitle = request.ActivityTitle,
                    DeadlineDate = request.DeadlineDate,
                    ReminderDaysBefore = request.ReminderDaysBefore > 0 ? request.ReminderDaysBefore : 2,
                    CreatedBy = userId,
                    CreatedOn = DateTime.UtcNow,
                    IsActive = true
                };
                _db.CourseActivityDeadlines.Add(existing);
            }

            // If it's an assignment, keep AssignmentMaster.DueDate aligned
            if (request.ActivityType == "Assignment")
            {
                var assign = await _db.AssignmentMasters.FirstOrDefaultAsync(a => a.Id == request.ActivityId);
                if (assign != null)
                {
                    assign.DueDate = request.DeadlineDate;
                    assign.UpdatedBy = userId;
                    assign.UpdatedOn = DateTime.UtcNow;
                }
            }

            await _db.SaveChangesAsync();

            // Invalidate throttles and refresh student alerts for this course immediately
            CourseSyncThrottle.TryRemove(request.CourseId, out _);
            StudentSyncThrottle.Clear();
            await SyncStudentAlertsForCourseAsync(request.CourseId);

            return true;
        }

        public async Task<FacultyStudentRiskRosterViewModel> GetFacultyStudentRiskRosterAsync(
            int courseId,
            int requestingUserId,
            bool isStaff,
            string riskFilter = "All",
            string? search = null)
        {
            await EnsureSchemaAsync();

            var facultyCourses = await GetFacultyAssignedCoursesAsync(requestingUserId, isStaff);
            if (!facultyCourses.Any())
            {
                return new FacultyStudentRiskRosterViewModel();
            }

            if (courseId <= 0 || !facultyCourses.Any(c => c.Id == courseId))
            {
                courseId = facultyCourses.First().Id;
            }

            var course = facultyCourses.First(c => c.Id == courseId);

            // Synchronize alerts for this course first to guarantee accurate data
            await SyncStudentAlertsForCourseAsync(courseId);

            // Fetch enrolled students
            var enrollments = await _db.CourseEnrollments
                .AsNoTracking()
                .Include(e => e.Student)
                .Where(e => e.CourseId == courseId && e.IsActive && e.Status == 1 && e.Student != null)
                .ToListAsync();

            var studentIds = enrollments.Select(e => e.StudentId).Distinct().ToList();

            // Fetch activities with deadlines for this course
            var courseActivities = await GetCourseActivitiesAsync(courseId, requestingUserId, isStaff, "All");
            var deadlineActivities = courseActivities.Activities.Where(a => a.DeadlineDate.HasValue).ToList();

            // Load completion sets for all students in one batch
            var matIds = deadlineActivities.Where(a => a.ActivityType == "Video" || a.ActivityType == "Pdf").Select(a => a.ActivityId).ToList();
            var assignIds = deadlineActivities.Where(a => a.ActivityType == "Assignment").Select(a => a.ActivityId).ToList();
            var quizIds = deadlineActivities.Where(a => a.ActivityType == "Quiz").Select(a => a.ActivityId).ToList();
            var threadIds = deadlineActivities.Where(a => a.ActivityType == "Discussion").Select(a => a.ActivityId).ToList();

            var videoProgressList = await _db.LectureVideoProgresses
                .AsNoTracking()
                .Where(p => matIds.Contains(p.LectureMaterialId) && studentIds.Contains(p.StudentId) && p.IsActive)
                .ToListAsync();

            var submissionsList = await _db.AssignmentSubmissions
                .AsNoTracking()
                .Where(s => assignIds.Contains(s.AssignmentId) && studentIds.Contains(s.StudentId) && !s.IsDeleted)
                .ToListAsync();

            var quizAttemptsList = await _db.StudentQuizAttempt
                .AsNoTracking()
                .Where(qa => quizIds.Contains(qa.QuizId) && studentIds.Contains(qa.StudentId) && qa.IsSubmitted)
                .ToListAsync();

            var discussionRepliesList = await _db.DiscussionReplies
                .AsNoTracking()
                .Where(r => threadIds.Contains(r.ThreadId) && studentIds.Contains(r.UserId) && r.IsActive)
                .ToListAsync();

            var rosterList = new List<FacultyStudentRiskItemViewModel>();

            foreach (var enr in enrollments)
            {
                var s = enr.Student!;
                int sId = s.Id;

                int completedCount = 0;
                int overdueCount = 0;
                int pendingCount = 0;
                int approachingCount = 0;

                foreach (var act in deadlineActivities)
                {
                    bool isCompleted = false;
                    if (act.ActivityType == "Video")
                    {
                        isCompleted = videoProgressList.Any(p => p.StudentId == sId && p.LectureMaterialId == act.ActivityId && (p.IsCompleted || p.ProgressPercentage >= 75));
                    }
                    else if (act.ActivityType == "Pdf")
                    {
                        isCompleted = videoProgressList.Any(p => p.StudentId == sId && p.LectureMaterialId == act.ActivityId && p.IsCompleted);
                    }
                    else if (act.ActivityType == "Assignment")
                    {
                        isCompleted = submissionsList.Any(sub => sub.StudentId == sId && sub.AssignmentId == act.ActivityId);
                    }
                    else if (act.ActivityType == "Quiz")
                    {
                        isCompleted = quizAttemptsList.Any(qa => qa.StudentId == sId && qa.QuizId == act.ActivityId);
                    }
                    else if (act.ActivityType == "Discussion")
                    {
                        isCompleted = discussionRepliesList.Any(r => r.UserId == sId && r.ThreadId == act.ActivityId);
                    }

                    if (isCompleted)
                    {
                        completedCount++;
                    }
                    else
                    {
                        pendingCount++;
                        if (act.DeadlineDate.HasValue)
                        {
                            if (DateTime.UtcNow > act.DeadlineDate.Value)
                            {
                                overdueCount++;
                            }
                            else if ((act.DeadlineDate.Value - DateTime.UtcNow).TotalDays <= act.ReminderDaysBefore || (act.DeadlineDate.Value - DateTime.UtcNow).TotalHours <= 48)
                            {
                                approachingCount++;
                            }
                        }
                    }
                }

                // Configurable risk levels:
                // CRITICAL: Multiple missed deadlines (>= 2)
                // HIGH: Deadline missed (1)
                // MEDIUM: Deadline approaching (0 missed, >= 1 approaching)
                // LOW: On track
                string riskLevel = "LOW";
                if (overdueCount >= 2)
                {
                    riskLevel = "CRITICAL";
                }
                else if (overdueCount == 1)
                {
                    riskLevel = "HIGH";
                }
                else if (approachingCount >= 1)
                {
                    riskLevel = "MEDIUM";
                }

                rosterList.Add(new FacultyStudentRiskItemViewModel
                {
                    StudentId = sId,
                    StudentName = s.Name,
                    StudentEmail = s.Email ?? "N/A",
                    RiskLevel = riskLevel,
                    PendingCount = pendingCount,
                    OverdueCount = overdueCount,
                    CompletedCount = completedCount,
                    TotalActivities = deadlineActivities.Count
                });
            }

            int critical = rosterList.Count(r => r.RiskLevel == "CRITICAL");
            int high = rosterList.Count(r => r.RiskLevel == "HIGH");
            int medium = rosterList.Count(r => r.RiskLevel == "MEDIUM");
            int low = rosterList.Count(r => r.RiskLevel == "LOW");

            // Filter by search
            if (!string.IsNullOrWhiteSpace(search))
            {
                string sLower = search.Trim().ToLowerInvariant();
                rosterList = rosterList.Where(r => r.StudentName.ToLowerInvariant().Contains(sLower) || r.StudentEmail.ToLowerInvariant().Contains(sLower)).ToList();
            }

            // Filter by risk
            if (!string.IsNullOrWhiteSpace(riskFilter) && !riskFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                rosterList = rosterList.Where(r => r.RiskLevel.Equals(riskFilter, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return new FacultyStudentRiskRosterViewModel
            {
                CourseId = course.Id,
                CourseName = course.Name ?? "Course",
                CourseCode = course.CourseCode,
                FacultyCourses = facultyCourses.Select(c => (c.Id, c.Name ?? "Course", c.CourseCode ?? "")).ToList(),
                Students = rosterList.OrderByDescending(r => r.OverdueCount).ThenBy(r => r.StudentName).ToList(),
                Activities = courseActivities.Activities,
                RiskFilter = riskFilter,
                SearchTerm = search,
                CriticalCount = critical,
                HighCount = high,
                MediumCount = medium,
                LowCount = low
            };
        }

        public async Task<StudentRiskDetailViewModel?> GetStudentRiskDetailAsync(
            int courseId,
            int studentId,
            int requestingUserId,
            bool isStaff)
        {
            await EnsureSchemaAsync();

            var student = await _db.UserMasters.FirstOrDefaultAsync(u => u.Id == studentId);
            var course = await _db.CourseMasters.FirstOrDefaultAsync(c => c.Id == courseId);
            if (student == null || course == null) return null;

            if (!isStaff)
            {
                bool isMapped = await _db.CourseFacultyMaps
                    .AnyAsync(m => m.CourseId == courseId && m.FacultyId == requestingUserId && m.IsActive);
                if (!isMapped && requestingUserId != studentId) return null;
            }

            var courseActivities = await GetCourseActivitiesAsync(courseId, requestingUserId, true, "All");
            var itemizedList = new List<StudentRiskActivityDetailViewModel>();

            int overdueCount = 0;
            int approachingCount = 0;

            foreach (var act in courseActivities.Activities)
            {
                bool isCompleted = false;
                string actionUrl = GetActionUrl(act.ActivityType, act.ActivityId, courseId);

                if (act.ActivityType == "Video")
                {
                    isCompleted = await _db.LectureVideoProgresses
                        .AnyAsync(p => p.StudentId == studentId && p.LectureMaterialId == act.ActivityId && (p.IsCompleted || p.ProgressPercentage >= 75) && p.IsActive);
                }
                else if (act.ActivityType == "Pdf")
                {
                    isCompleted = await _db.LectureVideoProgresses
                        .AnyAsync(p => p.StudentId == studentId && p.LectureMaterialId == act.ActivityId && p.IsCompleted && p.IsActive);
                }
                else if (act.ActivityType == "Assignment")
                {
                    isCompleted = await _db.AssignmentSubmissions
                        .AnyAsync(s => s.StudentId == studentId && s.AssignmentId == act.ActivityId && !s.IsDeleted);
                }
                else if (act.ActivityType == "Quiz")
                {
                    isCompleted = await _db.StudentQuizAttempt
                        .AnyAsync(qa => qa.StudentId == studentId && qa.QuizId == act.ActivityId && qa.IsSubmitted);
                }
                else if (act.ActivityType == "Discussion")
                {
                    isCompleted = await _db.DiscussionReplies
                        .AnyAsync(r => r.UserId == studentId && r.ThreadId == act.ActivityId && r.IsActive);
                }

                string status = "OnTrack";
                if (isCompleted)
                {
                    status = "Completed";
                }
                else if (act.DeadlineDate.HasValue)
                {
                    if (DateTime.UtcNow > act.DeadlineDate.Value)
                    {
                        status = "Overdue";
                        overdueCount++;
                    }
                    else if ((act.DeadlineDate.Value - DateTime.UtcNow).TotalDays <= act.ReminderDaysBefore || (act.DeadlineDate.Value - DateTime.UtcNow).TotalHours <= 48)
                    {
                        status = "Approaching";
                        approachingCount++;
                    }
                }

                itemizedList.Add(new StudentRiskActivityDetailViewModel
                {
                    ActivityType = act.ActivityType,
                    ActivityId = act.ActivityId,
                    Title = act.Title,
                    DeadlineDate = act.DeadlineDate,
                    RemainingText = FormatRemainingTime(act.DeadlineDate),
                    Status = status,
                    ActionUrl = actionUrl
                });
            }

            string risk = "LOW";
            if (overdueCount >= 2) risk = "CRITICAL";
            else if (overdueCount == 1) risk = "HIGH";
            else if (approachingCount >= 1) risk = "MEDIUM";

            return new StudentRiskDetailViewModel
            {
                StudentId = student.Id,
                StudentName = student.Name,
                StudentEmail = student.Email ?? "N/A",
                CourseId = course.Id,
                CourseName = course.Name ?? "Course",
                RiskLevel = risk,
                Activities = itemizedList
            };
        }

        public async Task<StudentAlertsPageViewModel> GetStudentAlertsAsync(int studentId, string filter = "All")
        {
            await EnsureSchemaAsync();

            // 1. Fetch student enrolled courses
            var enrolledCourseIds = await _db.CourseEnrollments
                .AsNoTracking()
                .Where(e => e.StudentId == studentId && e.IsActive && e.Status == 1)
                .Select(e => e.CourseId)
                .Distinct()
                .ToListAsync();

            // 2. High-performance targeted synchronization for THIS student only
            await SyncStudentAlertsForStudentAsync(studentId, enrolledCourseIds);

            // 3. Query all alerts for student
            var alertsQuery = _db.StudentRiskAlerts
                .AsNoTracking()
                .Include(a => a.Course)
                .Where(a => a.StudentId == studentId && a.IsActive && !a.IsResolved);

            var rawAlerts = await alertsQuery
                .OrderByDescending(a => a.RiskLevel == "Critical")
                .ThenByDescending(a => a.RiskLevel == "High")
                .ThenByDescending(a => a.RiskLevel == "Medium")
                .ThenByDescending(a => a.CreatedOn)
                .ToListAsync();

            var alertItems = rawAlerts.Select(a => new StudentAlertItemViewModel
            {
                AlertId = a.Id,
                CourseId = a.CourseId,
                CourseName = a.Course?.Name ?? (a.ActivityType == "Moderation" ? "System Conduct Notice" : "Course Activity"),
                ActivityType = a.ActivityType,
                ActivityId = a.ActivityId,
                Title = a.Title,
                Message = a.Message,
                RiskLevel = a.RiskLevel,
                DeadlineDate = a.DeadlineDate,
                RemainingText = FormatRemainingTime(a.DeadlineDate),
                IsResolved = a.IsResolved,
                IsRead = a.IsRead,
                CreatedOn = a.CreatedOn,
                ActionUrl = a.ActionUrl
            }).ToList();

            int total = alertItems.Count;
            int overdue = alertItems.Count(a => a.RiskLevel == "High" || a.RiskLevel == "Critical");
            int approaching = alertItems.Count(a => a.RiskLevel == "Medium");
            int unread = alertItems.Count(a => !a.IsRead);
            int warning = alertItems.Count(a => a.RiskLevel == "Warning" || a.ActivityType == "Moderation");

            var filtered = alertItems;
            if (!string.IsNullOrWhiteSpace(filter) && !filter.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                if (filter.Equals("Overdue", StringComparison.OrdinalIgnoreCase))
                {
                    filtered = alertItems.Where(a => a.RiskLevel == "High" || a.RiskLevel == "Critical").ToList();
                }
                else if (filter.Equals("Approaching", StringComparison.OrdinalIgnoreCase))
                {
                    filtered = alertItems.Where(a => a.RiskLevel == "Medium").ToList();
                }
                else if (filter.Equals("Unread", StringComparison.OrdinalIgnoreCase))
                {
                    filtered = alertItems.Where(a => !a.IsRead).ToList();
                }
                else if (filter.Equals("Warnings", StringComparison.OrdinalIgnoreCase))
                {
                    filtered = alertItems.Where(a => a.RiskLevel == "Warning" || a.ActivityType == "Moderation").ToList();
                }
            }

            return new StudentAlertsPageViewModel
            {
                Alerts = filtered,
                TotalCount = total,
                OverdueCount = overdue,
                ApproachingCount = approaching,
                UnreadCount = unread,
                WarningCount = warning,
                Filter = filter
            };
        }

        public async Task<bool> MarkAlertReadAsync(int alertId, int studentId, bool isRead)
        {
            await EnsureSchemaAsync();
            var alert = await _db.StudentRiskAlerts.FirstOrDefaultAsync(a => a.Id == alertId && a.StudentId == studentId);
            if (alert == null) return false;

            alert.IsRead = isRead;
            alert.ReadOn = isRead ? DateTime.UtcNow : null;
            await _db.SaveChangesAsync();
            StudentSyncThrottle.TryRemove(studentId, out _);
            return true;
        }

        public async Task<bool> MarkAllAlertsReadAsync(int studentId)
        {
            await EnsureSchemaAsync();
            var alerts = await _db.StudentRiskAlerts
                .Where(a => a.StudentId == studentId && !a.IsRead && !a.IsResolved)
                .ToListAsync();

            foreach (var a in alerts)
            {
                a.IsRead = true;
                a.ReadOn = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            StudentSyncThrottle.TryRemove(studentId, out _);
            return true;
        }

        public async Task<int> GetStudentUnreadAlertsCountAsync(int studentId)
        {
            await EnsureSchemaAsync();
            return await _db.StudentRiskAlerts
                .CountAsync(a => a.StudentId == studentId && !a.IsRead && !a.IsResolved && a.IsActive);
        }

        public async Task<AdminAtRiskAnalyticsViewModel> GetAdminAnalyticsAsync(
            int? programId = null,
            int? courseId = null,
            string riskFilter = "All",
            string? search = null)
        {
            await EnsureSchemaAsync();

            var programs = await _db.CourseCategoryMasters
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .Select(p => new ValueTuple<int, string>(p.Id, p.Name ?? "Program"))
                .ToListAsync();

            var coursesQuery = _db.CourseMasters.AsNoTracking().Where(c => c.IsActive);
            if (programId.HasValue && programId > 0)
            {
                coursesQuery = coursesQuery.Where(c => c.CourseCategoryId == programId.Value);
            }
            var courses = await coursesQuery
                .OrderBy(c => c.Name)
                .Select(c => new ValueTuple<int, string, int>(c.Id, c.Name ?? "Course", c.CourseCategoryId))
                .ToListAsync();

            // Query active enrollments
            var enrollQuery = _db.CourseEnrollments
                .AsNoTracking()
                .Include(e => e.Student)
                .Include(e => e.Course)
                .ThenInclude(c => c!.CourseCategory)
                .Where(e => e.IsActive && e.Status == 1 && e.Student != null && e.Course != null);

            if (courseId.HasValue && courseId > 0)
            {
                enrollQuery = enrollQuery.Where(e => e.CourseId == courseId.Value);
            }
            else if (programId.HasValue && programId > 0)
            {
                enrollQuery = enrollQuery.Where(e => e.Course!.CourseCategoryId == programId.Value);
            }

            var enrollments = await enrollQuery.ToListAsync();

            // Run alerts sync for unique courses
            var targetCourseIds = enrollments.Select(e => e.CourseId).Distinct().ToList();
            foreach (var cid in targetCourseIds.Take(25)) // sync active courses
            {
                await SyncStudentAlertsForCourseAsync(cid);
            }

            var studentList = new List<AdminStudentRiskItemViewModel>();
            int totalPendingActivities = 0;
            int totalOverdueActivities = 0;
            int totalCompletedActivities = 0;

            // Group by student & course
            foreach (var enr in enrollments)
            {
                var s = enr.Student!;
                var c = enr.Course!;
                int sId = s.Id;
                int cId = c.Id;

                // Fetch student alerts for this course
                var alerts = await _db.StudentRiskAlerts
                    .AsNoTracking()
                    .Where(a => a.StudentId == sId && a.CourseId == cId && a.IsActive && !a.IsResolved && a.ActivityType != "Moderation")
                    .ToListAsync();

                int overdue = alerts.Count(a => a.RiskLevel == "High" || a.RiskLevel == "Critical");
                int pending = alerts.Count;
                totalPendingActivities += pending;
                totalOverdueActivities += overdue;

                string risk = "LOW";
                if (overdue >= 2) risk = "CRITICAL";
                else if (overdue == 1) risk = "HIGH";
                else if (alerts.Any(a => a.RiskLevel == "Medium")) risk = "MEDIUM";

                studentList.Add(new AdminStudentRiskItemViewModel
                {
                    StudentId = sId,
                    StudentName = s.Name,
                    StudentEmail = s.Email ?? "N/A",
                    ProgramName = c.CourseCategory?.Name ?? "General",
                    CourseId = cId,
                    CourseName = c.Name ?? "Course",
                    RiskLevel = risk,
                    PendingCount = pending,
                    OverdueCount = overdue,
                    CompletedCount = Math.Max(0, 5 - pending)
                });
            }

            int critical = studentList.Count(s => s.RiskLevel == "CRITICAL");
            int high = studentList.Count(s => s.RiskLevel == "HIGH");
            int medium = studentList.Count(s => s.RiskLevel == "MEDIUM");
            int low = studentList.Count(s => s.RiskLevel == "LOW");

            // Filter search
            if (!string.IsNullOrWhiteSpace(search))
            {
                string sLower = search.Trim().ToLowerInvariant();
                studentList = studentList.Where(s => s.StudentName.ToLowerInvariant().Contains(sLower) || s.StudentEmail.ToLowerInvariant().Contains(sLower) || s.CourseName.ToLowerInvariant().Contains(sLower)).ToList();
            }

            // Filter risk
            if (!string.IsNullOrWhiteSpace(riskFilter) && !riskFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                studentList = studentList.Where(s => s.RiskLevel.Equals(riskFilter, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Fetch Moderation flags
            var moderationFlags = await _moderationService.GetFlagsAsync("All");
            var modFlagItems = moderationFlags.Select(f => new ModerationFlagItemViewModel
            {
                Id = f.Id,
                ContentType = f.ContentType,
                ContentId = f.ContentId,
                AuthorUserId = f.AuthorUserId,
                AuthorName = f.AuthorName ?? f.AuthorUser?.Name ?? "Student",
                ProgramName = f.Program?.Name,
                CourseName = f.Course?.Name,
                FlaggedText = f.FlaggedText,
                DetectedKeywords = f.DetectedKeywords,
                Status = f.Status,
                CreatedOn = f.CreatedOn,
                ReviewedByName = f.ReviewedByUser?.Name,
                ReviewedOn = f.ReviewedOn,
                ReviewRemarks = f.ReviewRemarks
            }).ToList();

            int pendingMod = modFlagItems.Count(f => f.Status == "PendingReview");

            return new AdminAtRiskAnalyticsViewModel
            {
                TotalStudents = studentList.Count,
                CriticalCount = critical,
                HighCount = high,
                MediumCount = medium,
                LowCount = low,
                TotalPendingActivities = totalPendingActivities,
                TotalOverdueActivities = totalOverdueActivities,
                TotalCompletedActivities = totalCompletedActivities,
                PendingModerationCount = pendingMod,
                Programs = programs,
                Courses = courses,
                SelectedProgramId = programId,
                SelectedCourseId = courseId,
                SelectedRiskFilter = riskFilter,
                SearchTerm = search,
                Students = studentList.OrderByDescending(s => s.OverdueCount).ThenBy(s => s.StudentName).ToList(),
                ModerationFlags = modFlagItems
            };
        }

        private async Task SyncStudentAlertsForStudentAsync(int studentId, List<int> enrolledCourseIds)
        {
            if (studentId <= 0 || !enrolledCourseIds.Any()) return;

            // Debounce: don't re-sync the same student within 60 seconds
            if (StudentSyncThrottle.TryGetValue(studentId, out var lastSync) &&
                (DateTime.UtcNow - lastSync).TotalSeconds < 60)
            {
                return;
            }

            StudentSyncThrottle[studentId] = DateTime.UtcNow;

            // Fetch active deadline activities for student's enrolled courses
            var deadlines = await _db.CourseActivityDeadlines
                .AsNoTracking()
                .Where(d => enrolledCourseIds.Contains(d.CourseId) && d.IsActive)
                .ToListAsync();

            var courses = await _db.CourseMasters
                .AsNoTracking()
                .Where(c => enrolledCourseIds.Contains(c.Id) && c.IsActive)
                .ToDictionaryAsync(c => c.Id, c => c);

            var assignments = await _db.AssignmentMasters
                .AsNoTracking()
                .Where(a => a.CourseId.HasValue && enrolledCourseIds.Contains(a.CourseId.Value) && a.IsActive && !a.IsDeleted)
                .ToListAsync();

            var deadlineMap = deadlines.ToDictionary(d => $"{d.ActivityType}_{d.ActivityId}", d => d);

            var activityItems = new List<(string ActivityType, int ActivityId, int CourseId, string Title, DateTime DeadlineDate, int ReminderDays)>();

            foreach (var a in assignments)
            {
                string key = $"Assignment_{a.Id}";
                deadlineMap.TryGetValue(key, out var savedDl);
                DateTime dlDate = savedDl?.DeadlineDate ?? a.DueDate;
                int remDays = savedDl?.ReminderDaysBefore ?? 2;
                activityItems.Add(("Assignment", a.Id, a.CourseId!.Value, a.Title, dlDate, remDays));
            }

            foreach (var d in deadlines.Where(d => d.ActivityType != "Assignment"))
            {
                activityItems.Add((d.ActivityType, d.ActivityId, d.CourseId, d.ActivityTitle, d.DeadlineDate, d.ReminderDaysBefore));
            }

            if (!activityItems.Any()) return;

            // Fetch completion records for THIS student ONLY (ultra-fast index queries)
            var matIds = activityItems.Where(a => a.ActivityType == "Video" || a.ActivityType == "Pdf").Select(a => a.ActivityId).ToList();
            var assignIds = activityItems.Where(a => a.ActivityType == "Assignment").Select(a => a.ActivityId).ToList();
            var quizIds = activityItems.Where(a => a.ActivityType == "Quiz").Select(a => a.ActivityId).ToList();
            var threadIds = activityItems.Where(a => a.ActivityType == "Discussion").Select(a => a.ActivityId).ToList();

            var completedMatIds = await _db.LectureVideoProgresses
                .AsNoTracking()
                .Where(p => p.StudentId == studentId && matIds.Contains(p.LectureMaterialId) && p.IsActive && (p.IsCompleted || p.ProgressPercentage >= 75))
                .Select(p => p.LectureMaterialId)
                .Distinct()
                .ToListAsync();

            var completedAssignIds = await _db.AssignmentSubmissions
                .AsNoTracking()
                .Where(s => s.StudentId == studentId && assignIds.Contains(s.AssignmentId) && !s.IsDeleted)
                .Select(s => s.AssignmentId)
                .Distinct()
                .ToListAsync();

            var completedQuizIds = await _db.StudentQuizAttempt
                .AsNoTracking()
                .Where(qa => qa.StudentId == studentId && quizIds.Contains(qa.QuizId) && qa.IsSubmitted)
                .Select(qa => qa.QuizId)
                .Distinct()
                .ToListAsync();

            var completedThreadIds = await _db.DiscussionReplies
                .AsNoTracking()
                .Where(r => r.UserId == studentId && threadIds.Contains(r.ThreadId) && r.IsActive)
                .Select(r => r.ThreadId)
                .Distinct()
                .ToListAsync();

            var currentAlerts = await _db.StudentRiskAlerts
                .Where(a => a.StudentId == studentId && a.IsActive && a.ActivityType != "Moderation")
                .ToListAsync();

            var newAlerts = new List<StudentRiskAlert>();
            bool hasModifications = false;

            int overdueCount = 0;
            foreach (var act in activityItems)
            {
                bool isCompleted = act.ActivityType switch
                {
                    "Video" or "Pdf" => completedMatIds.Contains(act.ActivityId),
                    "Assignment" => completedAssignIds.Contains(act.ActivityId),
                    "Quiz" => completedQuizIds.Contains(act.ActivityId),
                    "Discussion" => completedThreadIds.Contains(act.ActivityId),
                    _ => false
                };

                if (!isCompleted && DateTime.UtcNow > act.DeadlineDate)
                {
                    overdueCount++;
                }
            }

            foreach (var act in activityItems)
            {
                bool isCompleted = act.ActivityType switch
                {
                    "Video" or "Pdf" => completedMatIds.Contains(act.ActivityId),
                    "Assignment" => completedAssignIds.Contains(act.ActivityId),
                    "Quiz" => completedQuizIds.Contains(act.ActivityId),
                    "Discussion" => completedThreadIds.Contains(act.ActivityId),
                    _ => false
                };

                var existingAlert = currentAlerts.FirstOrDefault(a => a.CourseId == act.CourseId && a.ActivityType == act.ActivityType && a.ActivityId == act.ActivityId);

                if (isCompleted)
                {
                    if (existingAlert != null && !existingAlert.IsResolved)
                    {
                        existingAlert.IsResolved = true;
                        existingAlert.ResolvedOn = DateTime.UtcNow;
                        existingAlert.UpdatedOn = DateTime.UtcNow;
                        hasModifications = true;
                    }
                }
                else
                {
                    bool isOverdue = DateTime.UtcNow > act.DeadlineDate;
                    bool isApproaching = (act.DeadlineDate - DateTime.UtcNow).TotalDays <= act.ReminderDays || (act.DeadlineDate - DateTime.UtcNow).TotalHours <= 48;

                    if (isOverdue || isApproaching)
                    {
                        string risk = isOverdue ? (overdueCount >= 2 ? "Critical" : "High") : "Medium";
                        string courseName = courses.TryGetValue(act.CourseId, out var c) ? (c.Name ?? "Course") : "Course";
                        string remaining = FormatRemainingTime(act.DeadlineDate);
                        string title = isOverdue ? $"Overdue Activity: {act.Title}" : $"Upcoming Deadline: {act.Title}";
                        string message = isOverdue
                            ? $"The deadline for '{act.Title}' in {courseName} passed on {act.DeadlineDate:dd MMM yyyy, hh:mm tt}. Please complete it immediately."
                            : $"The deadline for '{act.Title}' in {courseName} is approaching on {act.DeadlineDate:dd MMM yyyy, hh:mm tt} ({remaining}).";

                        string actionUrl = GetActionUrl(act.ActivityType, act.ActivityId, act.CourseId);

                        if (existingAlert != null)
                        {
                            if (existingAlert.RiskLevel != risk || existingAlert.DeadlineDate != act.DeadlineDate || existingAlert.IsResolved)
                            {
                                existingAlert.Title = title;
                                existingAlert.Message = message;
                                existingAlert.RiskLevel = risk;
                                existingAlert.DeadlineDate = act.DeadlineDate;
                                existingAlert.ActionUrl = actionUrl;
                                existingAlert.IsResolved = false;
                                existingAlert.ResolvedOn = null;
                                existingAlert.UpdatedOn = DateTime.UtcNow;
                                hasModifications = true;
                            }
                        }
                        else
                        {
                            newAlerts.Add(new StudentRiskAlert
                            {
                                StudentId = studentId,
                                CourseId = act.CourseId,
                                ActivityType = act.ActivityType,
                                ActivityId = act.ActivityId,
                                Title = title,
                                Message = message,
                                RiskLevel = risk,
                                DeadlineDate = act.DeadlineDate,
                                IsResolved = false,
                                IsRead = false,
                                ActionUrl = actionUrl,
                                CreatedBy = studentId,
                                CreatedOn = DateTime.UtcNow,
                                IsActive = true
                            });
                            hasModifications = true;
                        }
                    }
                    else
                    {
                        if (existingAlert != null && !existingAlert.IsResolved)
                        {
                            existingAlert.IsResolved = true;
                            existingAlert.ResolvedOn = DateTime.UtcNow;
                            hasModifications = true;
                        }
                    }
                }
            }

            if (newAlerts.Any())
            {
                _db.StudentRiskAlerts.AddRange(newAlerts);
            }

            if (hasModifications)
            {
                await _db.SaveChangesAsync();
            }
        }

        public async Task SyncStudentAlertsForCourseAsync(int courseId)
        {
            await EnsureSchemaAsync();

            // Throttle course-level sync to avoid repeated expensive DB queries
            if (CourseSyncThrottle.TryGetValue(courseId, out var lastCourseSync) &&
                (DateTime.UtcNow - lastCourseSync).TotalMinutes < 2)
            {
                return;
            }
            CourseSyncThrottle[courseId] = DateTime.UtcNow;

            var course = await _db.CourseMasters
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == courseId && c.IsActive);

            if (course == null) return;

            var enrolledStudentIds = await _db.CourseEnrollments
                .AsNoTracking()
                .Where(e => e.CourseId == courseId && e.IsActive && e.Status == 1)
                .Select(e => e.StudentId)
                .Distinct()
                .ToListAsync();

            if (!enrolledStudentIds.Any()) return;

            // Fetch course activities that have deadlines
            var courseActivities = await GetCourseActivitiesAsync(courseId, 0, true, "All");
            var deadlineActivities = courseActivities.Activities.Where(a => a.DeadlineDate.HasValue).ToList();

            if (!deadlineActivities.Any()) return;

            // Fetch current alerts for this course
            var currentAlerts = await _db.StudentRiskAlerts
                .Where(a => a.CourseId == courseId && a.IsActive && a.ActivityType != "Moderation")
                .ToListAsync();

            // Check completions for all activities
            var matIds = deadlineActivities.Where(a => a.ActivityType == "Video" || a.ActivityType == "Pdf").Select(a => a.ActivityId).ToList();
            var assignIds = deadlineActivities.Where(a => a.ActivityType == "Assignment").Select(a => a.ActivityId).ToList();
            var quizIds = deadlineActivities.Where(a => a.ActivityType == "Quiz").Select(a => a.ActivityId).ToList();
            var threadIds = deadlineActivities.Where(a => a.ActivityType == "Discussion").Select(a => a.ActivityId).ToList();

            var videoProgress = await _db.LectureVideoProgresses
                .AsNoTracking()
                .Where(p => matIds.Contains(p.LectureMaterialId) && enrolledStudentIds.Contains(p.StudentId) && p.IsActive)
                .ToListAsync();

            var submissions = await _db.AssignmentSubmissions
                .AsNoTracking()
                .Where(s => assignIds.Contains(s.AssignmentId) && enrolledStudentIds.Contains(s.StudentId) && !s.IsDeleted)
                .ToListAsync();

            var quizAttempts = await _db.StudentQuizAttempt
                .AsNoTracking()
                .Where(qa => quizIds.Contains(qa.QuizId) && enrolledStudentIds.Contains(qa.StudentId) && qa.IsSubmitted)
                .ToListAsync();

            var replies = await _db.DiscussionReplies
                .AsNoTracking()
                .Where(r => threadIds.Contains(r.ThreadId) && enrolledStudentIds.Contains(r.UserId) && r.IsActive)
                .ToListAsync();

            var newAlerts = new List<StudentRiskAlert>();

            foreach (var studentId in enrolledStudentIds)
            {
                // Count overdue for this student in this course to establish Risk Level
                int studentOverdueCount = 0;
                foreach (var act in deadlineActivities)
                {
                    bool isComp = CheckCompletion(studentId, act.ActivityType, act.ActivityId, videoProgress, submissions, quizAttempts, replies);
                    if (!isComp && act.DeadlineDate.HasValue && DateTime.UtcNow > act.DeadlineDate.Value)
                    {
                        studentOverdueCount++;
                    }
                }

                foreach (var act in deadlineActivities)
                {
                    bool isCompleted = CheckCompletion(studentId, act.ActivityType, act.ActivityId, videoProgress, submissions, quizAttempts, replies);
                    var existingAlert = currentAlerts.FirstOrDefault(a => a.StudentId == studentId && a.ActivityType == act.ActivityType && a.ActivityId == act.ActivityId);

                    if (isCompleted)
                    {
                        // Verified complete: automatically resolve any existing alert
                        if (existingAlert != null && !existingAlert.IsResolved)
                        {
                            existingAlert.IsResolved = true;
                            existingAlert.ResolvedOn = DateTime.UtcNow;
                            existingAlert.UpdatedOn = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        // Pending activity: check deadline state
                        var dl = act.DeadlineDate!.Value;
                        bool isOverdue = DateTime.UtcNow > dl;
                        bool isApproaching = (dl - DateTime.UtcNow).TotalDays <= act.ReminderDaysBefore || (dl - DateTime.UtcNow).TotalHours <= 48;

                        if (isOverdue || isApproaching)
                        {
                            string risk = isOverdue
                                ? (studentOverdueCount >= 2 ? "Critical" : "High")
                                : "Medium";

                            string remaining = FormatRemainingTime(dl);
                            string title = isOverdue ? $"Overdue Activity: {act.Title}" : $"Upcoming Deadline: {act.Title}";
                            string message = isOverdue
                                ? $"The deadline for '{act.Title}' in {course.Name} passed on {dl:dd MMM yyyy, hh:mm tt}. Please complete it immediately."
                                : $"The deadline for '{act.Title}' in {course.Name} is approaching on {dl:dd MMM yyyy, hh:mm tt} ({remaining}).";

                            string actionUrl = GetActionUrl(act.ActivityType, act.ActivityId, courseId);

                            if (existingAlert != null)
                            {
                                // Update existing alert to reflect changed deadline or state without duplication
                                existingAlert.Title = title;
                                existingAlert.Message = message;
                                existingAlert.RiskLevel = risk;
                                existingAlert.DeadlineDate = dl;
                                existingAlert.ActionUrl = actionUrl;
                                existingAlert.IsResolved = false;
                                existingAlert.ResolvedOn = null;
                                existingAlert.UpdatedOn = DateTime.UtcNow;
                            }
                            else
                            {
                                newAlerts.Add(new StudentRiskAlert
                                {
                                    StudentId = studentId,
                                    CourseId = courseId,
                                    ActivityType = act.ActivityType,
                                    ActivityId = act.ActivityId,
                                    Title = title,
                                    Message = message,
                                    RiskLevel = risk,
                                    DeadlineDate = dl,
                                    IsResolved = false,
                                    IsRead = false,
                                    ActionUrl = actionUrl,
                                    CreatedBy = studentId,
                                    CreatedOn = DateTime.UtcNow,
                                    IsActive = true
                                });
                            }
                        }
                        else
                        {
                            // Beyond reminder window: resolve/clear alert if any
                            if (existingAlert != null && !existingAlert.IsResolved)
                            {
                                existingAlert.IsResolved = true;
                                existingAlert.ResolvedOn = DateTime.UtcNow;
                            }
                        }
                    }
                }
            }

            if (newAlerts.Any())
            {
                _db.StudentRiskAlerts.AddRange(newAlerts);
            }

            await _db.SaveChangesAsync();
        }

        private static bool CheckCompletion(
            int studentId,
            string activityType,
            int activityId,
            List<LectureVideoProgress> videoProgress,
            List<AssignmentSubmission> submissions,
            List<StudentQuizAttempt> quizAttempts,
            List<DiscussionReply> replies)
        {
            if (activityType == "Video")
            {
                return videoProgress.Any(p => p.StudentId == studentId && p.LectureMaterialId == activityId && (p.IsCompleted || p.ProgressPercentage >= 75));
            }
            if (activityType == "Pdf")
            {
                return videoProgress.Any(p => p.StudentId == studentId && p.LectureMaterialId == activityId && p.IsCompleted);
            }
            if (activityType == "Assignment")
            {
                return submissions.Any(s => s.StudentId == studentId && s.AssignmentId == activityId);
            }
            if (activityType == "Quiz")
            {
                return quizAttempts.Any(qa => qa.StudentId == studentId && qa.QuizId == activityId && qa.IsSubmitted);
            }
            if (activityType == "Discussion")
            {
                return replies.Any(r => r.UserId == studentId && r.ThreadId == activityId);
            }
            return false;
        }

        private static string GetActionUrl(string activityType, int activityId, int courseId)
        {
            return activityType switch
            {
                "Video" => $"/DocumentViewer/View/{activityId}",
                "Pdf" => $"/DocumentViewer/View/{activityId}",
                "Assignment" => $"/Assignment/Details/{activityId}",
                "Quiz" => $"/StudentQuiz/Attempt?quizId={activityId}",
                "Discussion" => $"/Discussion/Details/{activityId}",
                _ => $"/SubjectHub/{courseId}"
            };
        }

        private static string FormatRemainingTime(DateTime? deadline)
        {
            if (!deadline.HasValue) return "No deadline";

            var now = DateTime.UtcNow;
            if (now > deadline.Value)
            {
                var diff = now - deadline.Value;
                if (diff.TotalDays >= 1)
                {
                    return $"Overdue by {(int)Math.Floor(diff.TotalDays)}d {diff.Hours}h";
                }
                return $"Overdue by {diff.Hours}h {diff.Minutes}m";
            }
            else
            {
                var diff = deadline.Value - now;
                if (diff.TotalDays >= 1)
                {
                    return $"{(int)Math.Floor(diff.TotalDays)}d {diff.Hours}h remaining";
                }
                return $"{diff.Hours}h {diff.Minutes}m remaining";
            }
        }
    }
}
