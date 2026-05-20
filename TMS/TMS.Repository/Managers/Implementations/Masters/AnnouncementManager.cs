using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.Models.Academics;
using TMS.Models.Account;
using TMS.Models.Masters;
using TMS.Repository.Repositories;
using TMS.ViewModels.Academics;
using AutoMapper;

namespace TMS.Repository.Managers.Implementations.Masters
{
    internal class AnnouncementManager : MasterBaseManager<AnnouncementViewModel, Announcement>, IAnnouncementManager
    {
        private readonly ApplicationDBContext _context;
        private readonly IBaseModelRepository<CourseMaster> _courseRepository;
        private readonly IBaseModelRepository<RoleMaster> _roleRepository;
        private readonly IBaseModelRepository<DepartmentMaster> _departmentRepository;

        public AnnouncementManager(
            ApplicationDBContext context,
            IBaseModelRepository<Announcement> repository,
            IMapper mapper,
            IBaseModelRepository<QuizMaster> quizRepository,
            IBaseModelRepository<StudentQuizAttempt> studentQuizAttemptRepository,
            IBaseModelRepository<StudentQuizAnswer> studentQuizAnswerRepository,
            IBaseModelRepository<FacultyQuizAssessment> facultyQuizAssessmentRepository,
            IBaseModelRepository<CourseMaster> courseRepository,
            IBaseModelRepository<RoleMaster> roleRepository,
            IBaseModelRepository<DepartmentMaster> departmentRepository)
            : base(repository, mapper, quizRepository, studentQuizAttemptRepository, studentQuizAnswerRepository, facultyQuizAssessmentRepository)
        {
            _context = context;
            _courseRepository = courseRepository;
            _roleRepository = roleRepository;
            _departmentRepository = departmentRepository;
        }

        public async Task<List<AnnouncementViewModel>> GetVisibleAnnouncementsAsync(int userId, int? roleId, List<int>? courseIds = null)
        {
            var now = DateTime.Now;

            var query = _repository.GetAsync()
                .Where(a => a.IsActive && !a.IsDeleted
                    && a.PublishDate <= now
                    && (a.ExpiryDate == null || a.ExpiryDate >= now));

            // Apply targeting filter
            query = query.Where(a =>
                a.TargetType == 0 // All Users
                || (a.TargetType == 1 && roleId.HasValue && a.TargetId == roleId.Value) // Specific Role
                || (a.TargetType == 2 && courseIds != null && a.TargetId.HasValue && courseIds.Contains(a.TargetId.Value)) // Specific Course
                || (a.TargetType == 3 && a.TargetId.HasValue && false) // placeholder for Department if added to user
            );

            // Order: pinned first, then by publish date (newest first)
            var records = await query
                .OrderByDescending(a => a.IsPinned)
                .ThenByDescending(a => a.PublishDate)
                .ToListAsync();

            var result = _mapper.Map<List<AnnouncementViewModel>>(records);

            // Populate target names for display
            await PopulateTargetNamesAsync(result);

            return result;
        }

        public async Task<List<AnnouncementViewModel>> GetAllForManagementAsync()
        {
            var records = await _repository.GetAsync()
                .Where(a => !a.IsDeleted)
                .OrderByDescending(a => a.IsPinned)
                .ThenByDescending(a => a.CreatedOn)
                .ToListAsync();

            var result = _mapper.Map<List<AnnouncementViewModel>>(records);
            await PopulateTargetNamesAsync(result);
            return result;
        }

        public async Task<bool> SoftDeleteAsync(int id, int userId)
        {
            var entity = await _repository.GetAsync(id);
            if (entity == null) return false;

            entity.IsDeleted = true;
            entity.UpdatedBy = userId;
            entity.UpdatedOn = DateTime.Now;

            await _repository.UpdateAsync(entity);
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> TogglePinAsync(int id, int userId)
        {
            var entity = await _repository.GetAsync(id);
            if (entity == null) return false;

            entity.IsPinned = !entity.IsPinned;
            entity.UpdatedBy = userId;
            entity.UpdatedOn = DateTime.Now;

            await _repository.UpdateAsync(entity);
            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> FixDatabaseAsync()
        {
            try
            {
                string sql = @"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Announcements]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Announcements](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Title] [nvarchar](250) NOT NULL,
        [Description] [nvarchar](max) NOT NULL,
        [TargetType] [int] NOT NULL,
        [TargetId] [int] NULL,
        [AnnouncementType] [int] NOT NULL,
        [PublishDate] [datetime2](7) NOT NULL,
        [ExpiryDate] [datetime2](7) NULL,
        [AttachmentUrl] [nvarchar](500) NULL,
        [IsPinned] [bit] NOT NULL,
        [IsDeleted] [bit] NOT NULL,
        [IsActive] [bit] NOT NULL,
        [CreatedBy] [int] NOT NULL,
        [CreatedOn] [datetime2](7) NOT NULL,
        [UpdatedBy] [int] NULL,
        [UpdatedOn] [datetime2](7) NULL,
        CONSTRAINT [PK_Announcements] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Announcements_UserMaster_CreatedBy')
    BEGIN
        ALTER TABLE [dbo].[Announcements] WITH CHECK ADD CONSTRAINT [FK_Announcements_UserMaster_CreatedBy] FOREIGN KEY([CreatedBy])
        REFERENCES [dbo].[UserMaster] ([Id]);
    END

    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Announcements_UserMaster_UpdatedBy')
    BEGIN
        ALTER TABLE [dbo].[Announcements] WITH CHECK ADD CONSTRAINT [FK_Announcements_UserMaster_UpdatedBy] FOREIGN KEY([UpdatedBy])
        REFERENCES [dbo].[UserMaster] ([Id]);
    END
END

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DailyAttendances]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DailyAttendances](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [StudentId] [int] NOT NULL,
        [Date] [datetime2](7) NOT NULL,
        [AttendanceStatus] [int] NOT NULL,
        [Remarks] [nvarchar](max) NULL,
        [IsDeleted] [bit] NOT NULL,
        [IsActive] [bit] NOT NULL,
        [CreatedBy] [int] NOT NULL,
        [CreatedOn] [datetime2](7) NOT NULL,
        [UpdatedBy] [int] NULL,
        [UpdatedOn] [datetime2](7) NULL,
        CONSTRAINT [PK_DailyAttendances] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END";
                await _context.Database.ExecuteSqlRawAsync(sql);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Populate TargetName for display purposes based on TargetType and TargetId.
        /// </summary>
        private async Task PopulateTargetNamesAsync(List<AnnouncementViewModel> announcements)
        {
            foreach (var ann in announcements)
            {
                if (ann.TargetType == 0 || !ann.TargetId.HasValue)
                {
                    ann.TargetName = "All Users";
                    continue;
                }

                switch (ann.TargetType)
                {
                    case 1: // Role
                        var roles = await _roleRepository.Find(r => r.Id == ann.TargetId.Value);
                        ann.TargetName = roles.FirstOrDefault()?.Name ?? "Unknown Role";
                        break;
                    case 2: // Course
                        var courses = await _courseRepository.Find(c => c.Id == ann.TargetId.Value);
                        ann.TargetName = courses.FirstOrDefault()?.Name ?? "Unknown Course";
                        break;
                    case 3: // Department
                        var depts = await _departmentRepository.Find(d => d.Id == ann.TargetId.Value);
                        ann.TargetName = depts.FirstOrDefault()?.Name ?? "Unknown Department";
                        break;
                    default:
                        ann.TargetName = "Unknown";
                        break;
                }
            }
        }
    }
}
