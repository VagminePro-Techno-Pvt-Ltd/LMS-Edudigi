using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TMS.Models.Academics;
using TMS.Models.ContentSync;
using TMS.Models.Masters;
using TMS.Repository;

namespace TMS.Web.Services.ContentSync
{
    public class ContentFolderProvisioner : IContentFolderProvisioner
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly LocalContentStorageProvider _localProvider;
        private readonly GoogleDriveContentStorageProvider _googleDriveProvider;
        private readonly ILogger<ContentFolderProvisioner> _logger;

        public ContentFolderProvisioner(
            IServiceScopeFactory scopeFactory,
            LocalContentStorageProvider localProvider,
            GoogleDriveContentStorageProvider googleDriveProvider,
            ILogger<ContentFolderProvisioner> logger)
        {
            _scopeFactory = scopeFactory;
            _localProvider = localProvider;
            _googleDriveProvider = googleDriveProvider;
            _logger = logger;
        }

        private IContentStorageProvider? GetProvider(string providerKey)
        {
            if (string.Equals(providerKey, _localProvider.ProviderKey, StringComparison.OrdinalIgnoreCase))
                return _localProvider;
            if (string.Equals(providerKey, _googleDriveProvider.ProviderKey, StringComparison.OrdinalIgnoreCase))
                return _googleDriveProvider;
            return null;
        }

        private async Task<ContentSyncConfig> GetOrCreateConfigAsync(ApplicationDBContext db)
        {
            var config = await db.ContentSyncConfigs.FirstOrDefaultAsync();
            if (config == null)
            {
                config = new ContentSyncConfig
                {
                    LocalDriveEnabled = true,
                    LocalDriveRootPath = "EduDigiClass_LMS_Content",
                    GoogleDriveEnabled = false,
                    GoogleDriveRootFolderName = "EduDigiClass_LMS_Content",
                    GoogleDriveNetworkDisabled = false,
                    AutoSyncIntervalSeconds = 10,
                    DriveToLmsSyncEnabled = true,
                    UpdatedAt = DateTime.UtcNow
                };
                db.ContentSyncConfigs.Add(config);
                await db.SaveChangesAsync();
            }

            bool cfgChanged = false;
            if (!config.GoogleDriveEnabled)
            {
                config.GoogleDriveEnabled = true;
                cfgChanged = true;
            }
            if (string.IsNullOrWhiteSpace(config.GoogleDriveRootFolderId))
            {
                config.GoogleDriveRootFolderId = "1AzKnULTWKDYkOKdehUyp8OlWCg08bOPj";
                cfgChanged = true;
            }
            if (cfgChanged)
            {
                config.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            _localProvider.UpdateConfig(config.LocalDriveEnabled, config.LocalDriveRootPath);
            _googleDriveProvider.UpdateConfig(
                config.GoogleDriveEnabled,
                config.GoogleDriveNetworkDisabled,
                config.GoogleDriveRootFolderName,
                config.GoogleDriveRootFolderId,
                config.GoogleDriveServiceAccountEmail);

            return config;
        }

        public async Task EnqueueProvisioningAsync(string entityType, int entityId, string action, object? payload = null)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

            var config = await GetOrCreateConfigAsync(db);
            var payloadJson = payload != null ? JsonSerializer.Serialize(payload) : null;

            var enabledProviders = new List<string>();
            if (config.LocalDriveEnabled) enabledProviders.Add(_localProvider.ProviderKey);
            if (config.GoogleDriveEnabled) enabledProviders.Add(_googleDriveProvider.ProviderKey);

            if (enabledProviders.Count == 0)
            {
                // Default to LocalDrive if nothing enabled
                enabledProviders.Add(_localProvider.ProviderKey);
            }

            foreach (var providerKey in enabledProviders)
            {
                // Avoid redundant pending duplicate jobs for same entity & provider
                var existingPending = await db.ContentSyncOutboxJobs
                    .Where(j => j.EntityType == entityType &&
                                j.EntityId == entityId &&
                                j.StorageProvider == providerKey &&
                                (j.Status == "Pending" || j.Status == "Processing"))
                    .FirstOrDefaultAsync();

                if (existingPending != null)
                {
                    existingPending.Action = action;
                    existingPending.Payload = payloadJson;
                    existingPending.NextRetryUtc = null;
                }
                else
                {
                    db.ContentSyncOutboxJobs.Add(new ContentSyncOutboxJob
                    {
                        EntityType = entityType,
                        EntityId = entityId,
                        Action = action,
                        StorageProvider = providerKey,
                        Payload = payloadJson,
                        Status = "Pending",
                        RetryCount = 0,
                        MaxRetries = 5,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await db.SaveChangesAsync();
            _logger.LogInformation("Enqueued provisioning job: EntityType={EntityType}, EntityId={EntityId}, Action={Action} for providers: {Providers}",
                entityType, entityId, action, string.Join(", ", enabledProviders));
        }

        public async Task ProcessPendingJobsAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            var config = await GetOrCreateConfigAsync(db);

            var now = DateTime.UtcNow;
            var jobs = await db.ContentSyncOutboxJobs
                .Where(j => (j.Status == "Pending" || (j.Status == "Failed" && j.NextRetryUtc <= now && j.RetryCount < j.MaxRetries)))
                .OrderBy(j => j.CreatedAt)
                .Take(25)
                .ToListAsync(cancellationToken);

            foreach (var job in jobs)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var provider = GetProvider(job.StorageProvider);
                if (provider == null || !provider.IsEnabled)
                {
                    // If provider is disabled, leave pending until enabled
                    continue;
                }

                job.Status = "Processing";
                await db.SaveChangesAsync(cancellationToken);

                try
                {
                    await ExecuteProvisioningJobAsync(db, config, provider, job, cancellationToken);
                    job.Status = "Completed";
                    job.ProcessedAt = DateTime.UtcNow;
                    job.LastError = null;
                    _logger.LogInformation("Completed provisioning job #{Id} for {EntityType} #{EntityId} on {Provider}",
                        job.Id, job.EntityType, job.EntityId, job.StorageProvider);
                }
                catch (Exception ex)
                {
                    job.RetryCount++;
                    job.Status = "Failed";
                    job.LastError = ex.Message;
                    var delaySeconds = Math.Min(300, (int)Math.Pow(2, job.RetryCount) * 5);
                    job.NextRetryUtc = DateTime.UtcNow.AddSeconds(delaySeconds);

                    _logger.LogWarning(ex, "Failed provisioning job #{Id} for {EntityType} #{EntityId} on {Provider} (Attempt {Count}/{Max}): {Message}",
                        job.Id, job.EntityType, job.EntityId, job.StorageProvider, job.RetryCount, job.MaxRetries, ex.Message);
                }

                await db.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task ExecuteProvisioningJobAsync(
            ApplicationDBContext db,
            ContentSyncConfig config,
            IContentStorageProvider provider,
            ContentSyncOutboxJob job,
            CancellationToken cancellationToken)
        {
            var rootName = provider.ProviderKey == "GoogleDrive"
                ? config.GoogleDriveRootFolderName
                : (Path.GetFileName(config.LocalDriveRootPath) ?? "EduDigiClass_LMS_Content");

            var rootResult = await provider.EnsureRootFolderAsync(rootName, cancellationToken);
            if (!rootResult.Success)
            {
                throw new InvalidOperationException($"Failed to ensure root folder on {provider.DisplayName}: {rootResult.ErrorMessage}");
            }

            switch (job.EntityType.ToUpperInvariant())
            {
                case "PROGRAM":
                    await ProvisionProgramAsync(db, provider, rootResult.FolderId!, job.EntityId, cancellationToken);
                    break;

                case "SEMESTER":
                    await ProvisionSemesterAsync(db, provider, rootResult.FolderId!, job.EntityId, cancellationToken);
                    break;

                case "COURSE":
                    await ProvisionCourseAsync(db, provider, rootResult.FolderId!, job.EntityId, cancellationToken);
                    break;

                default:
                    _logger.LogWarning("Unknown entity type for provisioning: {Type}", job.EntityType);
                    break;
            }
        }

        private async Task<string> ProvisionProgramAsync(
            ApplicationDBContext db,
            IContentStorageProvider provider,
            string rootFolderId,
            int programId,
            CancellationToken cancellationToken)
        {
            var program = await db.CourseCategoryMasters.FirstOrDefaultAsync(p => p.Id == programId, cancellationToken);
            if (program == null)
            {
                throw new InvalidOperationException($"Program with ID {programId} not found in database.");
            }

            var cleanProgramName = LocalContentStorageProvider.SanitizeFolderName(program.Name);

            var mapping = await db.ContentFolderMappings
                .FirstOrDefaultAsync(m => m.EntityType == "Program" && m.EntityId == programId && m.StorageProvider == provider.ProviderKey, cancellationToken);

            string programFolderId;

            if (mapping != null)
            {
                // Check if program was renamed
                if (!string.Equals(mapping.FolderName, cleanProgramName, StringComparison.OrdinalIgnoreCase))
                {
                    var renameResult = await provider.RenameFolderAsync(mapping.ProviderFolderId, cleanProgramName, cancellationToken);
                    if (renameResult.Success)
                    {
                        mapping.FolderName = cleanProgramName;
                        mapping.ProviderFolderId = renameResult.FolderId ?? mapping.ProviderFolderId;
                        mapping.UpdatedAt = DateTime.UtcNow;
                    }
                }
                programFolderId = mapping.ProviderFolderId;
            }
            else
            {
                var folderResult = await provider.EnsureFolderAsync(cleanProgramName, rootFolderId, cancellationToken);
                if (!folderResult.Success)
                {
                    throw new InvalidOperationException($"Failed to provision program folder '{cleanProgramName}': {folderResult.ErrorMessage}");
                }

                programFolderId = folderResult.FolderId!;
                mapping = new ContentFolderMapping
                {
                    EntityType = "Program",
                    EntityId = programId,
                    ParentEntityId = null,
                    StorageProvider = provider.ProviderKey,
                    ProviderFolderId = programFolderId,
                    ProviderParentFolderId = rootFolderId,
                    FolderName = cleanProgramName,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                };
                db.ContentFolderMappings.Add(mapping);
            }

            await db.SaveChangesAsync(cancellationToken);

            // Ensure associated semester folders exist under this program
            int semCount = program.NoOfSemester ?? 0;
            if (semCount > 0)
            {
                var allSemesters = await db.SemesterMasters.Where(s => s.IsActive).ToListAsync(cancellationToken);
                for (int i = 1; i <= semCount; i++)
                {
                    string semName = $"Semester {i}";
                    var sem = allSemesters.FirstOrDefault(s => s.Name != null && s.Name.Trim().Equals(semName, StringComparison.OrdinalIgnoreCase));
                    if (sem != null)
                    {
                        await EnsureSemesterUnderProgramAsync(db, provider, programId, programFolderId, sem, cancellationToken);
                    }
                }
            }

            return programFolderId;
        }

        private async Task ProvisionSemesterAsync(
            ApplicationDBContext db,
            IContentStorageProvider provider,
            string rootFolderId,
            int semesterId,
            CancellationToken cancellationToken)
        {
            var semester = await db.SemesterMasters.FirstOrDefaultAsync(s => s.Id == semesterId, cancellationToken);
            if (semester == null)
            {
                throw new InvalidOperationException($"Semester with ID {semesterId} not found.");
            }

            // Find all active programs
            var programs = await db.CourseCategoryMasters.Where(p => p.IsActive).ToListAsync(cancellationToken);

            foreach (var prog in programs)
            {
                var progFolderId = await ProvisionProgramAsync(db, provider, rootFolderId, prog.Id, cancellationToken);
                await EnsureSemesterUnderProgramAsync(db, provider, prog.Id, progFolderId, semester, cancellationToken);
            }
        }

        private async Task<string> EnsureSemesterUnderProgramAsync(
            ApplicationDBContext db,
            IContentStorageProvider provider,
            int programId,
            string programFolderId,
            SemesterMaster semester,
            CancellationToken cancellationToken)
        {
            var cleanSemName = LocalContentStorageProvider.SanitizeFolderName(semester.Name);

            var mapping = await db.ContentFolderMappings
                .FirstOrDefaultAsync(m => m.EntityType == "Semester" &&
                                          m.EntityId == semester.Id &&
                                          m.ParentEntityId == programId &&
                                          m.StorageProvider == provider.ProviderKey, cancellationToken);

            string semesterFolderId;
            if (mapping != null)
            {
                if (!string.Equals(mapping.FolderName, cleanSemName, StringComparison.OrdinalIgnoreCase))
                {
                    var renameRes = await provider.RenameFolderAsync(mapping.ProviderFolderId, cleanSemName, cancellationToken);
                    if (renameRes.Success)
                    {
                        mapping.FolderName = cleanSemName;
                        mapping.ProviderFolderId = renameRes.FolderId ?? mapping.ProviderFolderId;
                        mapping.UpdatedAt = DateTime.UtcNow;
                    }
                }
                semesterFolderId = mapping.ProviderFolderId;
            }
            else
            {
                var createRes = await provider.EnsureFolderAsync(cleanSemName, programFolderId, cancellationToken);
                if (!createRes.Success)
                {
                    throw new InvalidOperationException($"Failed to provision semester folder '{cleanSemName}': {createRes.ErrorMessage}");
                }

                semesterFolderId = createRes.FolderId!;
                mapping = new ContentFolderMapping
                {
                    EntityType = "Semester",
                    EntityId = semester.Id,
                    ParentEntityId = programId,
                    StorageProvider = provider.ProviderKey,
                    ProviderFolderId = semesterFolderId,
                    ProviderParentFolderId = programFolderId,
                    FolderName = cleanSemName,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                };
                db.ContentFolderMappings.Add(mapping);
                await db.SaveChangesAsync(cancellationToken);
            }

            return semesterFolderId;
        }

        private async Task ProvisionCourseAsync(
            ApplicationDBContext db,
            IContentStorageProvider provider,
            string rootFolderId,
            int courseId,
            CancellationToken cancellationToken)
        {
            var course = await db.CourseMasters
                .Include(c => c.CourseCategory)
                .Include(c => c.Semester)
                .FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);

            if (course == null)
            {
                throw new InvalidOperationException($"Course with ID {courseId} not found in database.");
            }

            if (course.CourseCategoryId <= 0)
            {
                throw new InvalidOperationException($"Course '{course.Name}' has no Program assigned.");
            }

            // 1. Ensure Program folder exists
            var programFolderId = await ProvisionProgramAsync(db, provider, rootFolderId, course.CourseCategoryId, cancellationToken);

            // 2. Ensure Semester folder exists under Program
            string semesterFolderId;
            if (course.SemesterId.HasValue && course.SemesterId.Value > 0)
            {
                var semester = course.Semester ?? await db.SemesterMasters.FirstOrDefaultAsync(s => s.Id == course.SemesterId.Value, cancellationToken);
                if (semester == null)
                {
                    semester = new SemesterMaster { Id = course.SemesterId.Value, Name = "Semester 1" };
                }
                semesterFolderId = await EnsureSemesterUnderProgramAsync(db, provider, course.CourseCategoryId, programFolderId, semester, cancellationToken);
            }
            else
            {
                // Fallback to General / Semester_1
                var defaultSem = await db.SemesterMasters.FirstOrDefaultAsync(s => s.Name == "Semester 1", cancellationToken)
                                 ?? new SemesterMaster { Name = "Semester 1" };
                semesterFolderId = await EnsureSemesterUnderProgramAsync(db, provider, course.CourseCategoryId, programFolderId, defaultSem, cancellationToken);
            }

            // 3. Form Course folder name: BBA101_Business_Studies
            var courseCode = !string.IsNullOrWhiteSpace(course.CourseCode) ? course.CourseCode.Trim() : $"COURSE{course.Id}";
            var courseTitle = !string.IsNullOrWhiteSpace(course.Name) ? course.Name.Trim() : "Untitled";
            var rawCourseFolderName = $"{courseCode}_{courseTitle}";
            var cleanCourseFolderName = LocalContentStorageProvider.SanitizeFolderName(rawCourseFolderName);

            var courseMapping = await db.ContentFolderMappings
                .FirstOrDefaultAsync(m => m.EntityType == "Course" && m.EntityId == courseId && m.StorageProvider == provider.ProviderKey, cancellationToken);

            string courseFolderId;

            if (courseMapping != null)
            {
                // Check if moved to a different semester/program parent
                if (!string.Equals(courseMapping.ProviderParentFolderId, semesterFolderId, StringComparison.OrdinalIgnoreCase))
                {
                    var moveRes = await provider.MoveFolderAsync(courseMapping.ProviderFolderId, semesterFolderId, cancellationToken);
                    if (moveRes.Success)
                    {
                        courseMapping.ProviderParentFolderId = semesterFolderId;
                        courseMapping.ParentEntityId = course.SemesterId;
                        courseMapping.UpdatedAt = DateTime.UtcNow;
                    }
                }

                // Check if renamed
                if (!string.Equals(courseMapping.FolderName, cleanCourseFolderName, StringComparison.OrdinalIgnoreCase))
                {
                    var renameRes = await provider.RenameFolderAsync(courseMapping.ProviderFolderId, cleanCourseFolderName, cancellationToken);
                    if (renameRes.Success)
                    {
                        courseMapping.FolderName = cleanCourseFolderName;
                        courseMapping.ProviderFolderId = renameRes.FolderId ?? courseMapping.ProviderFolderId;
                        courseMapping.UpdatedAt = DateTime.UtcNow;
                    }
                }

                courseFolderId = courseMapping.ProviderFolderId;
            }
            else
            {
                var folderRes = await provider.EnsureFolderAsync(cleanCourseFolderName, semesterFolderId, cancellationToken);
                if (!folderRes.Success)
                {
                    throw new InvalidOperationException($"Failed to provision course folder '{cleanCourseFolderName}': {folderRes.ErrorMessage}");
                }

                courseFolderId = folderRes.FolderId!;
                courseMapping = new ContentFolderMapping
                {
                    EntityType = "Course",
                    EntityId = courseId,
                    ParentEntityId = course.SemesterId,
                    StorageProvider = provider.ProviderKey,
                    ProviderFolderId = courseFolderId,
                    ProviderParentFolderId = semesterFolderId,
                    FolderName = cleanCourseFolderName,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                };
                db.ContentFolderMappings.Add(courseMapping);
            }

            await db.SaveChangesAsync(cancellationToken);

            // 4. Automatically create all 4 standard subfolders: Videos, PDFs, Assignments, Forum_Discussion
            var standardSubfolders = new[] { "Videos", "PDFs", "Assignments", "Forum_Discussion" };
            foreach (var subName in standardSubfolders)
            {
                var subMapping = await db.ContentFolderMappings
                    .FirstOrDefaultAsync(m => m.EntityType == "CourseSubfolder" &&
                                              m.EntityId == courseId &&
                                              m.SubfolderType == subName &&
                                              m.StorageProvider == provider.ProviderKey, cancellationToken);

                if (subMapping == null)
                {
                    var subRes = await provider.EnsureFolderAsync(subName, courseFolderId, cancellationToken);
                    if (!subRes.Success)
                    {
                        throw new InvalidOperationException($"Failed to create subfolder '{subName}' for course: {subRes.ErrorMessage}");
                    }

                    db.ContentFolderMappings.Add(new ContentFolderMapping
                    {
                        EntityType = "CourseSubfolder",
                        EntityId = courseId,
                        ParentEntityId = courseId,
                        SubfolderType = subName,
                        StorageProvider = provider.ProviderKey,
                        ProviderFolderId = subRes.FolderId!,
                        ProviderParentFolderId = courseFolderId,
                        FolderName = subName,
                        Status = "Active",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        public async Task RetryFailedJobsAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

            var failedJobs = await db.ContentSyncOutboxJobs
                .Where(j => j.Status == "Failed")
                .ToListAsync(cancellationToken);

            foreach (var job in failedJobs)
            {
                job.Status = "Pending";
                job.NextRetryUtc = null;
            }

            await db.SaveChangesAsync(cancellationToken);
            await ProcessPendingJobsAsync(cancellationToken);
        }

        public async Task TriggerFullHierarchyRecheckAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

            // Enqueue all active programs
            var programs = await db.CourseCategoryMasters.Where(p => p.IsActive).ToListAsync(cancellationToken);
            foreach (var prog in programs)
            {
                await EnqueueProvisioningAsync("Program", prog.Id, "CreateOrUpdate");
            }

            // Enqueue all active courses
            var courses = await db.CourseMasters.Where(c => c.IsActive).ToListAsync(cancellationToken);
            foreach (var c in courses)
            {
                await EnqueueProvisioningAsync("Course", c.Id, "CreateOrUpdate");
            }

            await ProcessPendingJobsAsync(cancellationToken);
        }

        public async Task SyncDriveFilesToLmsAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            var config = await GetOrCreateConfigAsync(db);

            if (!config.DriveToLmsSyncEnabled)
            {
                return;
            }

            var subfolderMappings = await db.ContentFolderMappings
                .Where(m => m.EntityType == "CourseSubfolder" && m.Status == "Active")
                .ToListAsync(cancellationToken);

            foreach (var sub in subfolderMappings)
            {
                var provider = GetProvider(sub.StorageProvider);
                if (provider == null || !provider.IsEnabled) continue;

                try
                {
                    var files = await provider.ListFilesAsync(sub.ProviderFolderId, cancellationToken);
                    foreach (var file in files)
                    {
                        var alreadySynced = await db.ContentSyncFileRecords
                            .AnyAsync(r => r.StorageProvider == sub.StorageProvider &&
                                           r.ProviderFileId == file.FileId &&
                                           r.SyncStatus == "Synced", cancellationToken);

                        if (alreadySynced) continue;

                        int createdLmsId = await ImportFileToLmsModuleAsync(db, sub.EntityId, sub.SubfolderType!, file, sub.StorageProvider, cancellationToken);

                        db.ContentSyncFileRecords.Add(new ContentSyncFileRecord
                        {
                            StorageProvider = sub.StorageProvider,
                            CourseId = sub.EntityId,
                            SubfolderType = sub.SubfolderType!,
                            ProviderFileId = file.FileId,
                            FileName = file.FileName,
                            FileSize = file.FileSize,
                            FileHash = file.Hash,
                            LmsRecordType = MapSubfolderToLmsType(sub.SubfolderType!),
                            LmsRecordId = createdLmsId,
                            SyncStatus = "Synced",
                            SyncedAt = DateTime.UtcNow
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[DriveToLms] Error scanning subfolder {Path} on {Provider}: {Message}",
                        sub.ProviderFolderId, sub.StorageProvider, ex.Message);
                }
            }

            config.LastDriveToLmsScanUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        private static string MapSubfolderToLmsType(string subfolderType) =>
            subfolderType.ToLowerInvariant() switch
            {
                "videos" => "LectureMaterial",
                "pdfs" => "LectureMaterial",
                "assignments" => "AssignmentMaster",
                "forum_discussion" => "DiscussionThread",
                _ => "LectureMaterial"
            };

        private async Task<int> ImportFileToLmsModuleAsync(
            ApplicationDBContext db,
            int courseId,
            string subfolderType,
            StorageFileItem file,
            string providerKey,
            CancellationToken cancellationToken)
        {
            var adminUser = await db.UserMasters.FirstOrDefaultAsync(u => u.RoleId == 1 && u.IsActive, cancellationToken)
                            ?? await db.UserMasters.FirstOrDefaultAsync(cancellationToken);
            int adminId = adminUser?.Id ?? 1;

            if (subfolderType.Equals("Videos", StringComparison.OrdinalIgnoreCase))
            {
                var qVideo = await db.CourseQuadrantMasters.FirstOrDefaultAsync(q => q.Name != null && q.Name.Contains("Video"), cancellationToken)
                             ?? await db.CourseQuadrantMasters.FirstOrDefaultAsync(q => q.QuadrantNumber == 2, cancellationToken)
                             ?? await db.CourseQuadrantMasters.FirstOrDefaultAsync(cancellationToken);
                int quadId = qVideo?.Id ?? 2;
                var material = new LectureMaterial
                {
                    CourseQuadrantId = quadId,
                    CourseId = courseId,
                    Title = Path.GetFileNameWithoutExtension(file.FileName),
                    OriginalFileName = file.FileName,
                    FilePath = file.FileId,
                    FileSizeBytes = file.FileSize,
                    MaterialType = "Video",
                    Source = providerKey,
                    ExternalFileId = file.FileId,
                    IsActive = true,
                    CreatedBy = adminId,
                    CreatedOn = DateTime.Now
                };
                db.LectureMaterials.Add(material);
                await db.SaveChangesAsync(cancellationToken);

                // Map to course
                db.CourseMaterialMappings.Add(new CourseMaterialMapping
                {
                    CourseId = courseId,
                    LectureMaterialId = material.Id,
                    CourseQuadrantId = quadId,
                    IsActive = true,
                    CreatedBy = adminId,
                    CreatedOn = DateTime.Now
                });
                await db.SaveChangesAsync(cancellationToken);
                return material.Id;
            }
            else if (subfolderType.Equals("PDFs", StringComparison.OrdinalIgnoreCase))
            {
                var qPdf = await db.CourseQuadrantMasters.FirstOrDefaultAsync(q => q.Name != null && (q.Name.Contains("PDF") || q.Name.Contains("Notes")), cancellationToken)
                           ?? await db.CourseQuadrantMasters.FirstOrDefaultAsync(q => q.QuadrantNumber == 1, cancellationToken)
                           ?? await db.CourseQuadrantMasters.FirstOrDefaultAsync(cancellationToken);
                int quadId = qPdf?.Id ?? 1;
                var material = new LectureMaterial
                {
                    CourseQuadrantId = quadId,
                    CourseId = courseId,
                    Title = Path.GetFileNameWithoutExtension(file.FileName),
                    OriginalFileName = file.FileName,
                    FilePath = file.FileId,
                    FileSizeBytes = file.FileSize,
                    MaterialType = "Document",
                    Source = providerKey,
                    ExternalFileId = file.FileId,
                    IsActive = true,
                    CreatedBy = adminId,
                    CreatedOn = DateTime.Now
                };
                db.LectureMaterials.Add(material);
                await db.SaveChangesAsync(cancellationToken);

                // Map to course
                db.CourseMaterialMappings.Add(new CourseMaterialMapping
                {
                    CourseId = courseId,
                    LectureMaterialId = material.Id,
                    CourseQuadrantId = quadId,
                    IsActive = true,
                    CreatedBy = adminId,
                    CreatedOn = DateTime.Now
                });
                await db.SaveChangesAsync(cancellationToken);
                return material.Id;
            }
            else if (subfolderType.Equals("Assignments", StringComparison.OrdinalIgnoreCase))
            {
                var course = await db.CourseMasters.FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);
                var assignment = new AssignmentMaster
                {
                    Title = Path.GetFileNameWithoutExtension(file.FileName),
                    Description = $"Auto-synchronized assignment file: {file.FileName} from {providerKey}",
                    CourseId = courseId,
                    ProgramId = course?.CourseCategoryId ?? 1,
                    DueDate = DateTime.UtcNow.AddDays(14),
                    MaxMarks = 20,
                    PassMarks = 8,
                    AttachmentPath = file.FileId,
                    IsActive = true,
                    CreatedBy = adminId,
                    CreatedOn = DateTime.UtcNow
                };
                db.AssignmentMasters.Add(assignment);
                await db.SaveChangesAsync(cancellationToken);
                return assignment.Id;
            }
            else
            {
                // Forum_Discussion
                var course = await db.CourseMasters.FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);
                var q3 = await db.CourseQuadrantMasters.FirstOrDefaultAsync(q => q.QuadrantNumber == 3, cancellationToken);
                var thread = new DiscussionThread
                {
                    ProgramId = course?.CourseCategoryId,
                    CourseQuadrantId = q3?.Id,
                    Title = Path.GetFileNameWithoutExtension(file.FileName),
                    CreatedByUserId = adminId,
                    IsActive = true,
                    CreatedOn = DateTime.UtcNow
                };
                db.DiscussionThreads.Add(thread);
                await db.SaveChangesAsync(cancellationToken);

                var attachment = new DiscussionAttachment
                {
                    ThreadId = thread.Id,
                    FileName = file.FileName,
                    FilePath = file.FileId,
                    FileSize = file.FileSize,
                    UploadedBy = adminId,
                    UploadedOn = DateTime.UtcNow,
                    IsActive = true
                };
                db.DiscussionAttachments.Add(attachment);
                await db.SaveChangesAsync(cancellationToken);
                return thread.Id;
            }
        }

        public async Task<ProvisioningStatusSummary> GetStatusSummaryAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            var config = await GetOrCreateConfigAsync(db);

            var summary = new ProvisioningStatusSummary
            {
                LocalDriveEnabled = config.LocalDriveEnabled,
                LocalDriveStatus = await _localProvider.TestConnectionAsync(cancellationToken) ? "Online" : "Error",
                LocalDriveRootPath = config.LocalDriveRootPath,

                GoogleDriveEnabled = config.GoogleDriveEnabled,
                GoogleDriveNetworkDisabled = config.GoogleDriveNetworkDisabled,
                GoogleDriveRootName = config.GoogleDriveRootFolderName,
                GoogleDriveStatus = !config.GoogleDriveEnabled
                    ? "Disabled"
                    : (config.GoogleDriveNetworkDisabled
                        ? "Simulated Offline (Network Disconnected)"
                        : (await _googleDriveProvider.TestConnectionAsync(cancellationToken) ? "Online" : "Disconnected")),

                TotalProgramsMapped = await db.ContentFolderMappings.CountAsync(m => m.EntityType == "Program", cancellationToken),
                TotalSemestersMapped = await db.ContentFolderMappings.CountAsync(m => m.EntityType == "Semester", cancellationToken),
                TotalCoursesMapped = await db.ContentFolderMappings.CountAsync(m => m.EntityType == "Course", cancellationToken),
                TotalSubfoldersMapped = await db.ContentFolderMappings.CountAsync(m => m.EntityType == "CourseSubfolder", cancellationToken),

                PendingJobsCount = await db.ContentSyncOutboxJobs.CountAsync(j => j.Status == "Pending" || j.Status == "Processing", cancellationToken),
                CompletedJobsCount = await db.ContentSyncOutboxJobs.CountAsync(j => j.Status == "Completed", cancellationToken),
                FailedJobsCount = await db.ContentSyncOutboxJobs.CountAsync(j => j.Status == "Failed", cancellationToken),

                RecentJobs = await db.ContentSyncOutboxJobs
                    .OrderByDescending(j => j.CreatedAt)
                    .Take(30)
                    .ToListAsync(cancellationToken),

                AllMappings = await db.ContentFolderMappings
                    .OrderBy(m => m.EntityType).ThenBy(m => m.FolderName)
                    .Take(100)
                    .ToListAsync(cancellationToken),

                RecentSyncedFiles = await db.ContentSyncFileRecords
                    .OrderByDescending(f => f.SyncedAt)
                    .Take(30)
                    .ToListAsync(cancellationToken),

                LastDriveScanUtc = config.LastDriveToLmsScanUtc
            };

            return summary;
        }

        public async Task<HierarchyNodeDto> GetFolderHierarchyAsync(string providerKey, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

            var mappings = await db.ContentFolderMappings
                .Where(m => m.StorageProvider == providerKey && m.Status == "Active")
                .ToListAsync(cancellationToken);

            var rootNode = new HierarchyNodeDto
            {
                Id = "root",
                Name = "EduDigiClass_LMS_Content",
                Type = "Root",
                StorageProvider = providerKey
            };

            var programMappings = mappings.Where(m => m.EntityType == "Program").ToList();
            foreach (var prog in programMappings)
            {
                var progNode = new HierarchyNodeDto
                {
                    Id = prog.ProviderFolderId,
                    Name = prog.FolderName,
                    Type = "Program",
                    ProviderFolderId = prog.ProviderFolderId,
                    StorageProvider = providerKey
                };

                // Semesters under this program
                var semesterMappings = mappings.Where(m => m.EntityType == "Semester" && m.ParentEntityId == prog.EntityId).ToList();
                foreach (var sem in semesterMappings)
                {
                    var semNode = new HierarchyNodeDto
                    {
                        Id = sem.ProviderFolderId,
                        Name = sem.FolderName,
                        Type = "Semester",
                        ProviderFolderId = sem.ProviderFolderId,
                        StorageProvider = providerKey
                    };

                    // Courses under this semester
                    var courseMappings = mappings.Where(m => m.EntityType == "Course" && m.ParentEntityId == sem.EntityId).ToList();
                    foreach (var c in courseMappings)
                    {
                        var courseNode = new HierarchyNodeDto
                        {
                            Id = c.ProviderFolderId,
                            Name = c.FolderName,
                            Type = "Course",
                            ProviderFolderId = c.ProviderFolderId,
                            StorageProvider = providerKey
                        };

                        // 4 standard subfolders
                        var subfolders = mappings.Where(m => m.EntityType == "CourseSubfolder" && m.EntityId == c.EntityId).ToList();
                        foreach (var sub in subfolders)
                        {
                            courseNode.Children.Add(new HierarchyNodeDto
                            {
                                Id = sub.ProviderFolderId,
                                Name = sub.FolderName,
                                Type = "Subfolder",
                                ProviderFolderId = sub.ProviderFolderId,
                                StorageProvider = providerKey
                            });
                        }

                        semNode.Children.Add(courseNode);
                    }

                    progNode.Children.Add(semNode);
                }

                rootNode.Children.Add(progNode);
            }

            return rootNode;
        }

        public async Task UpdateProviderConfigAsync(bool localEnabled, string localPath, bool gdriveEnabled, bool gdriveNetworkDisabled, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            var config = await GetOrCreateConfigAsync(db);

            config.LocalDriveEnabled = localEnabled;
            if (!string.IsNullOrWhiteSpace(localPath)) config.LocalDriveRootPath = localPath;
            config.GoogleDriveEnabled = gdriveEnabled;
            config.GoogleDriveNetworkDisabled = gdriveNetworkDisabled;
            config.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(cancellationToken);

            _localProvider.UpdateConfig(localEnabled, config.LocalDriveRootPath);
            _googleDriveProvider.UpdateConfig(gdriveEnabled, gdriveNetworkDisabled, config.GoogleDriveRootFolderName, config.GoogleDriveRootFolderId, config.GoogleDriveServiceAccountEmail);
            _googleDriveProvider.SetNetworkAccess(!gdriveNetworkDisabled);

            _logger.LogInformation("Updated storage configuration: Local={Local} ({Path}), GoogleDrive={GDrive} (NetworkDisabled={Net})",
                localEnabled, config.LocalDriveRootPath, gdriveEnabled, gdriveNetworkDisabled);
        }

        public async Task<CourseFolderSyncDetailsDto> GetCourseFolderSyncDetailsAsync(int courseId, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            var config = await GetOrCreateConfigAsync(db);

            var course = await db.CourseMasters.FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);
            if (course == null)
            {
                return new CourseFolderSyncDetailsDto { Success = false, ErrorMessage = "Course not found" };
            }

            var primaryProvider = config.GoogleDriveEnabled && !config.LocalDriveEnabled ? _googleDriveProvider : (IContentStorageProvider)_localProvider;

            var courseMapping = await db.ContentFolderMappings
                .FirstOrDefaultAsync(m => m.EntityType == "Course" && m.EntityId == courseId && m.StorageProvider == primaryProvider.ProviderKey, cancellationToken);

            var latestJob = await db.ContentSyncOutboxJobs
                .Where(j => j.EntityType == "Course" && j.EntityId == courseId && j.StorageProvider == primaryProvider.ProviderKey)
                .OrderByDescending(j => j.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            var subfolderMappings = await db.ContentFolderMappings
                .Where(m => m.EntityType == "CourseSubfolder" && m.EntityId == courseId && m.StorageProvider == primaryProvider.ProviderKey)
                .ToListAsync(cancellationToken);

            var syncedFiles = await db.ContentSyncFileRecords
                .Where(f => f.CourseId == courseId && f.StorageProvider == primaryProvider.ProviderKey)
                .ToListAsync(cancellationToken);

            string status = "NotProvisioned";
            if (courseMapping != null)
            {
                status = "Completed";
            }
            else if (latestJob != null)
            {
                status = latestJob.Status switch
                {
                    "Processing" => "Pending",
                    "Pending" => "Pending",
                    "Failed" => "Failed",
                    _ => "Pending"
                };
            }

            var folderPath = courseMapping?.ProviderFolderId ?? "";
            var folderUrl = primaryProvider.GetFolderUrl(folderPath);

            var standardSubtypes = new (string Key, string Label)[]
            {
                ("Videos", "Quadrant 1: Videos & E-Lectures"),
                ("PDFs", "Quadrant 2: E-Books & Handouts (PDFs)"),
                ("Assignments", "Assessments & Assignments"),
                ("Forum_Discussion", "Quadrant 3: Discussion & Collaboration")
            };

            var subfolderDtos = new List<SubfolderSyncDetailDto>();
            foreach (var (key, label) in standardSubtypes)
            {
                var subMap = subfolderMappings.FirstOrDefault(m => m.SubfolderType != null && m.SubfolderType.Equals(key, StringComparison.OrdinalIgnoreCase));
                var fileCount = syncedFiles.Count(f => f.SubfolderType != null && f.SubfolderType.Equals(key, StringComparison.OrdinalIgnoreCase));
                subfolderDtos.Add(new SubfolderSyncDetailDto
                {
                    SubfolderType = key,
                    Label = label,
                    Path = subMap?.ProviderFolderId ?? (string.IsNullOrWhiteSpace(folderPath) ? key : Path.Combine(folderPath, key)),
                    ProviderFolderId = subMap?.ProviderFolderId ?? "",
                    Status = subMap != null ? "Ready" : (status == "Completed" ? "Provisioning" : "Waiting"),
                    SyncedFilesCount = fileCount
                });
            }

            return new CourseFolderSyncDetailsDto
            {
                Success = true,
                CourseId = courseId,
                CourseName = course.Name,
                CourseCode = course.CourseCode ?? "",
                StorageProvider = primaryProvider.ProviderKey,
                Status = status,
                FolderPath = folderPath,
                ProviderFolderId = courseMapping?.ProviderFolderId ?? "",
                FolderUrl = folderUrl,
                Subfolders = subfolderDtos,
                RetryCount = latestJob?.RetryCount ?? 0,
                ErrorMessage = latestJob?.LastError,
                LastSyncTime = courseMapping?.CreatedAt ?? latestJob?.CreatedAt
            };
        }

        public async Task<Dictionary<int, CourseSyncStatusDto>> GetProgramCoursesSyncStatusAsync(int programId, CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
            var config = await GetOrCreateConfigAsync(db);

            var primaryProvider = config.GoogleDriveEnabled && !config.LocalDriveEnabled ? _googleDriveProvider : (IContentStorageProvider)_localProvider;

            var courseIds = await db.CourseMasters
                .Where(c => c.CourseCategoryId == programId && c.IsActive)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            var mappings = await db.ContentFolderMappings
                .Where(m => m.EntityType == "Course" && m.StorageProvider == primaryProvider.ProviderKey && courseIds.Contains(m.EntityId))
                .ToListAsync(cancellationToken);

            var pendingOrFailedJobs = await db.ContentSyncOutboxJobs
                .Where(j => j.EntityType == "Course" && j.StorageProvider == primaryProvider.ProviderKey && courseIds.Contains(j.EntityId) && (j.Status == "Pending" || j.Status == "Processing" || j.Status == "Failed"))
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync(cancellationToken);

            var result = new Dictionary<int, CourseSyncStatusDto>();

            foreach (var cid in courseIds)
            {
                var map = mappings.FirstOrDefault(m => m.EntityId == cid);
                var job = pendingOrFailedJobs.FirstOrDefault(j => j.EntityId == cid);

                string status = "NotProvisioned";
                bool hasError = false;
                string? err = null;

                if (map != null)
                {
                    status = "Completed";
                }
                else if (job != null)
                {
                    if (job.Status == "Failed")
                    {
                        status = "Failed";
                        hasError = true;
                        err = job.LastError;
                    }
                    else
                    {
                        status = "Pending";
                    }
                }

                var folderPath = map?.ProviderFolderId ?? "";
                var folderUrl = primaryProvider.GetFolderUrl(folderPath);

                result[cid] = new CourseSyncStatusDto
                {
                    CourseId = cid,
                    Status = status,
                    StorageProvider = primaryProvider.ProviderKey,
                    FolderPath = folderPath,
                    FolderUrl = folderUrl,
                    HasError = hasError,
                    ErrorMessage = err
                };
            }

            return result;
        }

        public async Task<CourseFolderSyncDetailsDto> ProvisionCourseNowAsync(int courseId, CancellationToken cancellationToken = default)
        {
            await EnqueueProvisioningAsync("Course", courseId, "CreateOrUpdate");
            await ProcessPendingJobsAsync(cancellationToken);
            return await GetCourseFolderSyncDetailsAsync(courseId, cancellationToken);
        }
    }
}
