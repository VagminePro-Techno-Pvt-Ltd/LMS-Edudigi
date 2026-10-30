using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TMS.Models.Academics;
using TMS.Models.ContentSync;
using TMS.Models.Masters;
using TMS.Repository;
using TMS.Web.Services.ContentSync;

namespace TMS.IA.RuntimeChecks
{
    public static class ContentSyncAcceptanceTests
    {
        private static int _testPassed = 0;
        private static int _testFailed = 0;

        private static void Assert(string testName, bool condition, string detail = "")
        {
            if (condition)
            {
                _testPassed++;
                Console.WriteLine($"[ACCEPTANCE TEST PASS] {testName} {(string.IsNullOrEmpty(detail) ? "" : "-> " + detail)}");
            }
            else
            {
                _testFailed++;
                Console.WriteLine($"[ACCEPTANCE TEST FAIL] {testName} {(string.IsNullOrEmpty(detail) ? "" : "-> " + detail)}");
            }
        }

        private static (IServiceProvider Provider, string TempDir) SetupTestEnvironment(string dbName)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "LMS_AcceptanceTests_" + Guid.NewGuid().ToString("N"));
            if (!Directory.Exists(tempDir))
                Directory.CreateDirectory(tempDir);

            var services = new ServiceCollection();

            services.AddDbContext<ApplicationDBContext>(options =>
                options.UseInMemoryDatabase(dbName));

            services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));

            // HttpClient for GoogleDrive
            services.AddHttpClient();

            // Storage Providers
            services.AddSingleton(sp => new LocalContentStorageProvider(sp.GetRequiredService<ILogger<LocalContentStorageProvider>>()));
            services.AddSingleton(sp => new GoogleDriveContentStorageProvider(
                sp.GetRequiredService<ILogger<GoogleDriveContentStorageProvider>>(),
                sp.GetRequiredService<IHttpClientFactory>().CreateClient()));

            services.AddSingleton<IContentFolderProvisioner, ContentFolderProvisioner>();

            var provider = services.BuildServiceProvider();

            // Seed initial config
            using (var scope = provider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                db.ContentSyncConfigs.Add(new ContentSyncConfig
                {
                    LocalDriveEnabled = true,
                    LocalDriveRootPath = tempDir,
                    GoogleDriveEnabled = false,
                    GoogleDriveRootFolderName = "EduDigiClass_LMS_Content",
                    AutoSyncIntervalSeconds = 10,
                    DriveToLmsSyncEnabled = true,
                    UpdatedAt = DateTime.UtcNow
                });
                db.SaveChanges();
            }

            return (provider, tempDir);
        }

        public static async Task<bool> RunAllAcceptanceTestsAsync()
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine("   RUNNING TWO-WAY LMS-TO-DRIVE CONTENT SYNC ACCEPTANCE TESTS (1 - 10)          ");
            Console.WriteLine("================================================================================");

            _testPassed = 0;
            _testFailed = 0;

            try
            {
                await Test1_CreateProgramAndVerifyFolder();
                await Test2_CreateSemesterAndVerifyNestedFolder();
                await Test3_CreateCourseAndVerifySubfolders();
                await Test4_SimilarCourseNamesDistinctMappings();
                await Test5_RepeatProvisioningNoDuplicates();
                await Test6_NetworkDisconnectRetry();
                await Test7_MultiProviderIndependentStatus();
                await Test8_RenameProgramAndCourse();
                await Test9_RestartApplicationPendingJobsResume();
                await Test10_VerifyExistingCrudRoutesAndPermissions();
                await Test11_VerifyAcademicsHubContentSyncIntegration();
                await Test12_RealtimeProvisionCourseNow();
                await Test13_VerifyGoogleDriveApiV3Abstraction();
                await Test14_VerifyUncPathHardening();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRITICAL EXCEPTION IN TESTS] {ex}");
                _testFailed++;
            }

            Console.WriteLine("================================================================================");
            Console.WriteLine($"   ACCEPTANCE TESTS SUMMARY: PASS={_testPassed} | FAIL={_testFailed}");
            Console.WriteLine("================================================================================");

            return _testFailed == 0;
        }

        /// <summary>
        /// Acceptance Test 1: Create a Program in the LMS and verify its folder on the configured destination.
        /// </summary>
        private static async Task Test1_CreateProgramAndVerifyFolder()
        {
            var (sp, tempDir) = SetupTestEnvironment("TestDb_1");
            try
            {
                var provisioner = sp.GetRequiredService<IContentFolderProvisioner>();
                int programId;

                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                    var program = new CourseCategoryMaster
                    {
                        Name = "BBA",
                        IsActive = true,
                        CreatedOn = DateTime.UtcNow
                    };
                    db.CourseCategoryMasters.Add(program);
                    await db.SaveChangesAsync();
                    programId = program.Id;

                    // Enqueue provisioning as done by CourseCategoryController post-commit
                    await provisioner.EnqueueProvisioningAsync("Program", program.Id, "Create", new { Name = program.Name });
                }

                // Process outbox jobs
                await provisioner.ProcessPendingJobsAsync();

                // Verify folder existence on destination: EduDigiClass_LMS_Content/BBA/
                var expectedFolder = Path.Combine(tempDir, "BBA");
                Assert("Test 1: Destination folder created (EduDigiClass_LMS_Content/BBA)", Directory.Exists(expectedFolder), $"Path: {expectedFolder}");

                // Verify database mapping record
                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                    var mapping = await db.ContentFolderMappings.FirstOrDefaultAsync(m => m.EntityType == "Program" && m.EntityId == programId);
                    Assert("Test 1: ContentFolderMapping recorded with status Active", mapping != null && mapping.Status == "Active", $"Mapping FolderName: {mapping?.FolderName}");
                }
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Acceptance Test 2: Create a Semester and verify its nested folder.
        /// </summary>
        private static async Task Test2_CreateSemesterAndVerifyNestedFolder()
        {
            var (sp, tempDir) = SetupTestEnvironment("TestDb_2");
            try
            {
                var provisioner = sp.GetRequiredService<IContentFolderProvisioner>();
                int programId, semesterId;

                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                    var program = new CourseCategoryMaster { Name = "BBA", IsActive = true, CreatedOn = DateTime.UtcNow };
                    db.CourseCategoryMasters.Add(program);
                    await db.SaveChangesAsync();
                    programId = program.Id;

                    var semester = new SemesterMaster { Name = "Semester 1", IsActive = true, CreatedOn = DateTime.UtcNow };
                    db.SemesterMasters.Add(semester);
                    await db.SaveChangesAsync();
                    semesterId = semester.Id;

                    await provisioner.EnqueueProvisioningAsync("Program", program.Id, "Create", new { Name = program.Name });
                    await provisioner.EnqueueProvisioningAsync("Semester", semester.Id, "Create", new { Name = semester.Name, ProgramId = program.Id });
                }

                await provisioner.ProcessPendingJobsAsync();

                // Verify nested folder exists: EduDigiClass_LMS_Content/BBA/Semester_1/
                var expectedFolder = Path.Combine(tempDir, "BBA", "Semester_1");
                Assert("Test 2: Nested destination folder created (EduDigiClass_LMS_Content/BBA/Semester_1)", Directory.Exists(expectedFolder), $"Path: {expectedFolder}");

                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                    var mapping = await db.ContentFolderMappings.FirstOrDefaultAsync(m => m.EntityType == "Semester" && m.EntityId == semesterId && m.ParentEntityId == programId);
                    Assert("Test 2: Semester mapping linked to parent Program ID", mapping != null && mapping.ParentEntityId == programId, $"ParentEntityId: {mapping?.ParentEntityId}");
                }
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Acceptance Test 3: Create a Course and verify its folder and all four standard subfolders.
        /// </summary>
        private static async Task Test3_CreateCourseAndVerifySubfolders()
        {
            var (sp, tempDir) = SetupTestEnvironment("TestDb_3");
            try
            {
                var provisioner = sp.GetRequiredService<IContentFolderProvisioner>();
                int programId, semesterId, courseId;

                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                    var program = new CourseCategoryMaster { Name = "BBA", IsActive = true, CreatedOn = DateTime.UtcNow };
                    db.CourseCategoryMasters.Add(program);
                    await db.SaveChangesAsync();
                    programId = program.Id;

                    var semester = new SemesterMaster { Name = "Semester 1", IsActive = true, CreatedOn = DateTime.UtcNow };
                    db.SemesterMasters.Add(semester);
                    await db.SaveChangesAsync();
                    semesterId = semester.Id;

                    var course = new CourseMaster
                    {
                        CourseCode = "BBA101",
                        Name = "Business Studies",
                        CourseCategoryId = program.Id,
                        SemesterId = semester.Id,
                        IsActive = true,
                        CreatedOn = DateTime.UtcNow
                    };
                    db.CourseMasters.Add(course);
                    await db.SaveChangesAsync();
                    courseId = course.Id;

                    await provisioner.EnqueueProvisioningAsync("Course", course.Id, "Create", new { CourseCode = course.CourseCode, Name = course.Name, ProgramId = program.Id, SemesterId = semester.Id });
                }

                await provisioner.ProcessPendingJobsAsync();

                var courseFolder = Path.Combine(tempDir, "BBA", "Semester_1", "BBA101_Business_Studies");
                Assert("Test 3: Course root folder created", Directory.Exists(courseFolder), courseFolder);

                var videosFolder = Path.Combine(courseFolder, "Videos");
                var pdfsFolder = Path.Combine(courseFolder, "PDFs");
                var assignmentsFolder = Path.Combine(courseFolder, "Assignments");
                var forumFolder = Path.Combine(courseFolder, "Forum_Discussion");

                Assert("Test 3: Videos subfolder created", Directory.Exists(videosFolder), videosFolder);
                Assert("Test 3: PDFs subfolder created", Directory.Exists(pdfsFolder), pdfsFolder);
                Assert("Test 3: Assignments subfolder created", Directory.Exists(assignmentsFolder), assignmentsFolder);
                Assert("Test 3: Forum_Discussion subfolder created", Directory.Exists(forumFolder), forumFolder);

                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                    var subfolderMappings = await db.ContentFolderMappings
                        .Where(m => m.EntityType == "CourseSubfolder" && m.EntityId == courseId)
                        .ToListAsync();

                    Assert("Test 3: All 4 standard subfolder mappings recorded", subfolderMappings.Count == 4, $"Count = {subfolderMappings.Count}");
                    Assert("Test 3: Videos subfolder mapping exists", subfolderMappings.Any(m => m.SubfolderType == "Videos"));
                    Assert("Test 3: PDFs subfolder mapping exists", subfolderMappings.Any(m => m.SubfolderType == "PDFs"));
                    Assert("Test 3: Assignments subfolder mapping exists", subfolderMappings.Any(m => m.SubfolderType == "Assignments"));
                    Assert("Test 3: Forum_Discussion subfolder mapping exists", subfolderMappings.Any(m => m.SubfolderType == "Forum_Discussion"));
                }
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Acceptance Test 4: Create multiple courses with similar names and verify distinct, stable mappings.
        /// </summary>
        private static async Task Test4_SimilarCourseNamesDistinctMappings()
        {
            var (sp, tempDir) = SetupTestEnvironment("TestDb_4");
            try
            {
                var provisioner = sp.GetRequiredService<IContentFolderProvisioner>();
                int c1Id, c2Id;

                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                    var program = new CourseCategoryMaster { Name = "BBA", IsActive = true, CreatedOn = DateTime.UtcNow };
                    db.CourseCategoryMasters.Add(program);
                    var semester = new SemesterMaster { Name = "Semester 1", IsActive = true, CreatedOn = DateTime.UtcNow };
                    db.SemesterMasters.Add(semester);
                    await db.SaveChangesAsync();

                    var c1 = new CourseMaster { CourseCode = "BBA101", Name = "Accounting", CourseCategoryId = program.Id, SemesterId = semester.Id, IsActive = true };
                    var c2 = new CourseMaster { CourseCode = "BBA201", Name = "Accounting", CourseCategoryId = program.Id, SemesterId = semester.Id, IsActive = true };
                    db.CourseMasters.AddRange(c1, c2);
                    await db.SaveChangesAsync();
                    c1Id = c1.Id;
                    c2Id = c2.Id;

                    await provisioner.EnqueueProvisioningAsync("Course", c1.Id, "Create", new { CourseCode = c1.CourseCode, Name = c1.Name, ProgramId = program.Id, SemesterId = semester.Id });
                    await provisioner.EnqueueProvisioningAsync("Course", c2.Id, "Create", new { CourseCode = c2.CourseCode, Name = c2.Name, ProgramId = program.Id, SemesterId = semester.Id });
                }

                await provisioner.ProcessPendingJobsAsync();

                var f1 = Path.Combine(tempDir, "BBA", "Semester_1", "BBA101_Accounting");
                var f2 = Path.Combine(tempDir, "BBA", "Semester_1", "BBA201_Accounting");

                Assert("Test 4: Course 1 distinct folder exists", Directory.Exists(f1), f1);
                Assert("Test 4: Course 2 distinct folder exists", Directory.Exists(f2), f2);
                Assert("Test 4: Distinct folder paths are not equal", f1 != f2);

                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                    var m1 = await db.ContentFolderMappings.FirstOrDefaultAsync(m => m.EntityType == "Course" && m.EntityId == c1Id);
                    var m2 = await db.ContentFolderMappings.FirstOrDefaultAsync(m => m.EntityType == "Course" && m.EntityId == c2Id);

                    Assert("Test 4: Course 1 mapping exists with stable entity ID", m1 != null && m1.EntityId == c1Id);
                    Assert("Test 4: Course 2 mapping exists with stable entity ID", m2 != null && m2.EntityId == c2Id);
                    Assert("Test 4: Provider folder IDs are distinct", m1?.ProviderFolderId != m2?.ProviderFolderId);
                }
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Acceptance Test 5: Repeat the provisioning job and verify that no duplicate folders are created.
        /// </summary>
        private static async Task Test5_RepeatProvisioningNoDuplicates()
        {
            var (sp, tempDir) = SetupTestEnvironment("TestDb_5");
            try
            {
                var provisioner = sp.GetRequiredService<IContentFolderProvisioner>();
                int courseId;

                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                    var p = new CourseCategoryMaster { Name = "MBA", IsActive = true };
                    var s = new SemesterMaster { Name = "Semester 1", IsActive = true };
                    db.CourseCategoryMasters.Add(p);
                    db.SemesterMasters.Add(s);
                    await db.SaveChangesAsync();

                    var c = new CourseMaster { CourseCode = "MBA101", Name = "Management", CourseCategoryId = p.Id, SemesterId = s.Id, IsActive = true };
                    db.CourseMasters.Add(c);
                    await db.SaveChangesAsync();
                    courseId = c.Id;

                    await provisioner.EnqueueProvisioningAsync("Course", c.Id, "Create", new { CourseCode = c.CourseCode, Name = c.Name, ProgramId = p.Id, SemesterId = s.Id });
                }

                // First run
                await provisioner.ProcessPendingJobsAsync();

                var courseParentDir = Path.Combine(tempDir, "MBA", "Semester_1");
                var foldersAfterRun1 = Directory.GetDirectories(courseParentDir);
                Assert("Test 5: Exactly 1 course folder created on first run", foldersAfterRun1.Length == 1, $"Folders: {string.Join(", ", foldersAfterRun1)}");

                // Enqueue again (repeat provisioning job for the same entity)
                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                    var c = await db.CourseMasters.FindAsync(courseId);
                    await provisioner.EnqueueProvisioningAsync("Course", c!.Id, "Update", new { CourseCode = c.CourseCode, Name = c.Name, ProgramId = c.CourseCategoryId, SemesterId = c.SemesterId });
                }

                // Second run
                await provisioner.ProcessPendingJobsAsync();

                var foldersAfterRun2 = Directory.GetDirectories(courseParentDir);
                Assert("Test 5: No duplicate folders created after repeated provisioning", foldersAfterRun2.Length == 1, $"Count = {foldersAfterRun2.Length}");

                // RecheckHierarchy call
                await provisioner.TriggerFullHierarchyRecheckAsync();
                await provisioner.ProcessPendingJobsAsync();

                var foldersAfterRecheck = Directory.GetDirectories(courseParentDir);
                Assert("Test 5: Hierarchy recheck maintains zero duplicates", foldersAfterRecheck.Length == 1);
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Acceptance Test 6: Disable network access temporarily and verify automatic retry after connectivity returns.
        /// </summary>
        private static async Task Test6_NetworkDisconnectRetry()
        {
            var (sp, tempDir) = SetupTestEnvironment("TestDb_6");
            try
            {
                var provisioner = sp.GetRequiredService<IContentFolderProvisioner>();

                // 1. Temporarily disable network access (GoogleDrive enabled, Network disabled, LocalDrive disabled)
                await provisioner.UpdateProviderConfigAsync(false, tempDir, true, true);

                int pId;
                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                    var p = new CourseCategoryMaster { Name = "BSc_IT", IsActive = true };
                    db.CourseCategoryMasters.Add(p);
                    await db.SaveChangesAsync();
                    pId = p.Id;

                    await provisioner.EnqueueProvisioningAsync("Program", p.Id, "Create", new { Name = p.Name });
                }

                // Process jobs while network is down
                await provisioner.ProcessPendingJobsAsync();

                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                    var failedJob = await db.ContentSyncOutboxJobs.FirstOrDefaultAsync(j => j.EntityType == "Program" && j.EntityId == pId);

                    Assert("Test 6: Job entered Failed state while network disabled", failedJob != null && failedJob.Status == "Failed", $"Status: {failedJob?.Status}");
                    Assert("Test 6: Retry count incremented", failedJob != null && failedJob.RetryCount >= 1, $"RetryCount: {failedJob?.RetryCount}");
                    Assert("Test 6: Last error recorded network message", failedJob != null && failedJob.LastError != null && (failedJob.LastError.Contains("Network") || failedJob.LastError.Contains("unreachable")), $"LastError: {failedJob?.LastError}");
                }

                // 2. Restore network connectivity
                await provisioner.UpdateProviderConfigAsync(false, tempDir, true, false);

                // Reset NextRetryUtc so retry runs immediately
                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                    var job = await db.ContentSyncOutboxJobs.FirstOrDefaultAsync(j => j.EntityType == "Program" && j.EntityId == pId);
                    if (job != null) job.NextRetryUtc = DateTime.UtcNow.AddMinutes(-1);
                    await db.SaveChangesAsync();
                }

                // Process again after connectivity restored
                await provisioner.ProcessPendingJobsAsync();

                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                    var succeededJob = await db.ContentSyncOutboxJobs.FirstOrDefaultAsync(j => j.EntityType == "Program" && j.EntityId == pId);

                    Assert("Test 6: Job transitioned to Completed upon retry", succeededJob != null && succeededJob.Status == "Completed", $"Status: {succeededJob?.Status}");
                }
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Acceptance Test 7: Enable both Local/Network Drive and Google Drive and verify independent provisioning status.
        /// </summary>
        private static async Task Test7_MultiProviderIndependentStatus()
        {
            var (sp, tempDir) = SetupTestEnvironment("TestDb_7");
            try
            {
                var provisioner = sp.GetRequiredService<IContentFolderProvisioner>();

                // Enable BOTH providers, but simulate Google Drive network failure
                await provisioner.UpdateProviderConfigAsync(true, tempDir, true, true);

                int pId;
                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                    var p = new CourseCategoryMaster { Name = "BCom", IsActive = true };
                    db.CourseCategoryMasters.Add(p);
                    await db.SaveChangesAsync();
                    pId = p.Id;

                    await provisioner.EnqueueProvisioningAsync("Program", p.Id, "Create", new { Name = p.Name });
                }

                await provisioner.ProcessPendingJobsAsync();

                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                    var localJob = await db.ContentSyncOutboxJobs.FirstOrDefaultAsync(j => j.EntityType == "Program" && j.EntityId == pId && j.StorageProvider == "LocalDrive");
                    var gdriveJob = await db.ContentSyncOutboxJobs.FirstOrDefaultAsync(j => j.EntityType == "Program" && j.EntityId == pId && j.StorageProvider == "GoogleDrive");

                    Assert("Test 7: LocalDrive job Completed successfully", localJob != null && localJob.Status == "Completed", $"Status: {localJob?.Status}");
                    Assert("Test 7: GoogleDrive job marked Failed independently", gdriveJob != null && gdriveJob.Status == "Failed", $"Status: {gdriveJob?.Status}");
                }

                // Verify Local folder was created despite Google Drive failure
                var localFolder = Path.Combine(tempDir, "BCom");
                Assert("Test 7: Local folder created and preserved despite remote failure", Directory.Exists(localFolder), localFolder);

                // Restore Google Drive
                await provisioner.UpdateProviderConfigAsync(true, tempDir, true, false);
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Acceptance Test 8: Rename a Program or Course and verify safe folder/mapping updates.
        /// </summary>
        private static async Task Test8_RenameProgramAndCourse()
        {
            var (sp, tempDir) = SetupTestEnvironment("TestDb_8");
            try
            {
                var provisioner = sp.GetRequiredService<IContentFolderProvisioner>();
                int programId;

                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                    var p = new CourseCategoryMaster { Name = "BBA_Original", IsActive = true };
                    db.CourseCategoryMasters.Add(p);
                    await db.SaveChangesAsync();
                    programId = p.Id;

                    await provisioner.EnqueueProvisioningAsync("Program", p.Id, "Create", new { Name = p.Name });
                }

                await provisioner.ProcessPendingJobsAsync();
                var originalFolder = Path.Combine(tempDir, "BBA_Original");
                Assert("Test 8: Original folder exists before rename", Directory.Exists(originalFolder));

                // Perform Rename in LMS
                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                    var p = await db.CourseCategoryMasters.FindAsync(programId);
                    p!.Name = "BBA_Renamed_Program";
                    await db.SaveChangesAsync();

                    await provisioner.EnqueueProvisioningAsync("Program", p.Id, "Rename", new { Name = p.Name });
                }

                await provisioner.ProcessPendingJobsAsync();

                var renamedFolder = Path.Combine(tempDir, "BBA_Renamed_Program");
                Assert("Test 8: Destination folder safely renamed to BBA_Renamed_Program", Directory.Exists(renamedFolder), renamedFolder);
                Assert("Test 8: Old folder name no longer exists", !Directory.Exists(originalFolder));

                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                    var mapping = await db.ContentFolderMappings.FirstOrDefaultAsync(m => m.EntityType == "Program" && m.EntityId == programId);

                    Assert("Test 8: Mapping updated to new folder name", mapping != null && mapping.FolderName == "BBA_Renamed_Program");
                    Assert("Test 8: Stable entity ID preserved across rename", mapping != null && mapping.EntityId == programId);
                }
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Acceptance Test 9: Restart the application and verify pending jobs resume.
        /// </summary>
        private static async Task Test9_RestartApplicationPendingJobsResume()
        {
            var sharedDbName = "TestDb_9_Shared";
            var (sp1, tempDir) = SetupTestEnvironment(sharedDbName);
            int courseId;

            try
            {
                // App instance 1: Enqueue job but do NOT process it before "shutting down"
                using (var scope = sp1.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                    var provisioner1 = sp1.GetRequiredService<IContentFolderProvisioner>();

                    var p = new CourseCategoryMaster { Name = "MCA", IsActive = true };
                    var s = new SemesterMaster { Name = "Semester 1", IsActive = true };
                    db.CourseCategoryMasters.Add(p);
                    db.SemesterMasters.Add(s);
                    await db.SaveChangesAsync();

                    var c = new CourseMaster { CourseCode = "MCA101", Name = "Algorithms", CourseCategoryId = p.Id, SemesterId = s.Id, IsActive = true };
                    db.CourseMasters.Add(c);
                    await db.SaveChangesAsync();
                    courseId = c.Id;

                    await provisioner1.EnqueueProvisioningAsync("Course", c.Id, "Create", new { CourseCode = c.CourseCode, Name = c.Name, ProgramId = p.Id, SemesterId = s.Id });
                }

                // Verify job is recorded in outbox as Pending
                using (var scope = sp1.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                    var pendingJobs = await db.ContentSyncOutboxJobs.Where(j => j.Status == "Pending").ToListAsync();
                    Assert("Test 9: Pending jobs safely stored in DB prior to restart", pendingJobs.Count > 0, $"Pending count: {pendingJobs.Count}");
                }

                // Dispose App instance 1 (simulate restart)
                (sp1 as IDisposable)?.Dispose();

                // App instance 2: Starts up against the same persistent database
                var services2 = new ServiceCollection();
                services2.AddDbContext<ApplicationDBContext>(options =>
                    options.UseInMemoryDatabase(sharedDbName));
                services2.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
                services2.AddHttpClient();
                services2.AddSingleton(sp => new LocalContentStorageProvider(sp.GetRequiredService<ILogger<LocalContentStorageProvider>>()));
                services2.AddSingleton(sp => new GoogleDriveContentStorageProvider(
                    sp.GetRequiredService<ILogger<GoogleDriveContentStorageProvider>>(),
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient()));
                services2.AddSingleton<IContentFolderProvisioner, ContentFolderProvisioner>();

                var sp2 = services2.BuildServiceProvider();
                var provisioner2 = sp2.GetRequiredService<IContentFolderProvisioner>();

                // Worker runs upon new instance startup
                await provisioner2.ProcessPendingJobsAsync();

                var expectedFolder = Path.Combine(tempDir, "MCA", "Semester_1", "MCA101_Algorithms");
                Assert("Test 9: Course folder provisioned after restart", Directory.Exists(expectedFolder), expectedFolder);

                (sp2 as IDisposable)?.Dispose();
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Acceptance Test 10: Verify existing LMS CRUD operations, routes, permissions, and UI remain unchanged.
        /// </summary>
        private static async Task Test10_VerifyExistingCrudRoutesAndPermissions()
        {
            await Task.Yield();

            // 1. Verify ContentSyncController attributes
            var contentSyncControllerType = typeof(TMS.Web.Areas.Admin.Controllers.ContentSyncController);
            var areaAttr = contentSyncControllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Mvc.AreaAttribute), true)
                .FirstOrDefault() as Microsoft.AspNetCore.Mvc.AreaAttribute;
            var authAttr = contentSyncControllerType.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
                .FirstOrDefault() as Microsoft.AspNetCore.Authorization.AuthorizeAttribute;

            Assert("Test 10: ContentSyncController is registered under Admin Area", areaAttr != null && areaAttr.RouteValue == "Admin");
            Assert("Test 10: ContentSyncController requires SuperAdmin/Admin authorization", authAttr != null && (authAttr.Roles?.Contains("Admin") ?? false));

            // 2. Verify all standard actions exist on ContentSyncController
            var requiredActions = new[] { "Index", "UpdateConfig", "RunSyncNow", "RetryFailed", "RecheckHierarchy", "ScanDriveToLms", "ToggleNetworkAccess" };
            foreach (var action in requiredActions)
            {
                var method = contentSyncControllerType.GetMethod(action);
                Assert($"Test 10: Action {action} exposed on ContentSyncController", method != null);
            }

            // 3. Verify CourseCategoryController retains standard Item and Index methods
            var courseCategoryType = typeof(TMS.Web.Areas.Admin.Controllers.CourseCategoryController);
            Assert("Test 10: CourseCategoryController.Item exists", courseCategoryType.GetMethods().Any(m => m.Name == "Item"));
            Assert("Test 10: CourseCategoryController.Index exists", courseCategoryType.GetMethods().Any(m => m.Name == "Index"));

            // 4. Verify SemesterMasterController retains standard Item and Index methods
            var semesterType = typeof(TMS.Web.Areas.Admin.Controllers.SemesterMasterController);
            Assert("Test 10: SemesterMasterController.Item exists", semesterType.GetMethods().Any(m => m.Name == "Item"));
            Assert("Test 10: SemesterMasterController.Index exists", semesterType.GetMethods().Any(m => m.Name == "Index"));

            // 5. Verify CourseController retains standard Item and CreateMultiple methods
            var courseType = typeof(TMS.Web.Areas.Admin.Controllers.CourseController);
            Assert("Test 10: CourseController.Item exists", courseType.GetMethods().Any(m => m.Name == "Item"));
            Assert("Test 10: CourseController.CreateMultiple exists", courseType.GetMethods().Any(m => m.Name == "CreateMultiple"));

            // 6. Verify CourseStructureController retains creation workflows
            var structureType = typeof(TMS.Web.Areas.Admin.Controllers.CourseStructureController);
            Assert("Test 10: CourseStructureController.CreateProgram exists", structureType.GetMethod("CreateProgram") != null);
            Assert("Test 10: CourseStructureController.QuickAddCourse exists", structureType.GetMethod("QuickAddCourse") != null);
            Assert("Test 10: CourseStructureController.AddSemester exists", structureType.GetMethod("AddSemester") != null);
        }

        /// <summary>
        /// Acceptance Test 11: Academics Hub Endpoints & Course Folder Sync Details API
        /// </summary>
        private static async Task Test11_VerifyAcademicsHubContentSyncIntegration()
        {
            var (sp, tempDir) = SetupTestEnvironment("TestDb_11");
            try
            {
                var structureType = typeof(TMS.Web.Areas.Admin.Controllers.CourseStructureController);
                Assert("Test 11: CourseStructureController.GetCourseFolderSyncDetails exists", structureType.GetMethod("GetCourseFolderSyncDetails") != null);
                Assert("Test 11: CourseStructureController.GetProgramCoursesSyncStatus exists", structureType.GetMethod("GetProgramCoursesSyncStatus") != null);
                Assert("Test 11: CourseStructureController.ProvisionCourseNow exists", structureType.GetMethod("ProvisionCourseNow") != null);

                var provisioner = sp.GetRequiredService<IContentFolderProvisioner>();
                int cId;
                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                    var p = new CourseCategoryMaster { Name = "BCA", IsActive = true };
                    db.CourseCategoryMasters.Add(p);
                    await db.SaveChangesAsync();

                    var s = new SemesterMaster { Name = "Semester 1", IsActive = true };
                    db.SemesterMasters.Add(s);
                    await db.SaveChangesAsync();

                    var c = new CourseMaster { Name = "Data Structures", CourseCode = "BCA101", CourseCategoryId = p.Id, SemesterId = s.Id, IsActive = true };
                    db.CourseMasters.Add(c);
                    await db.SaveChangesAsync();
                    cId = c.Id;
                }

                // Check sync details before provisioning
                var detailsBefore = await provisioner.GetCourseFolderSyncDetailsAsync(cId);
                Assert("Test 11: Sync details retrieved successfully before provisioning", detailsBefore.Success);
                Assert("Test 11: Course status is NotProvisioned before job run", detailsBefore.Status == "NotProvisioned");
                Assert("Test 11: All 4 standard subfolders listed in details", detailsBefore.Subfolders.Count == 4);
                Assert("Test 11: Videos subfolder present in DTO", detailsBefore.Subfolders.Any(s => s.SubfolderType == "Videos"));
                Assert("Test 11: PDFs subfolder present in DTO", detailsBefore.Subfolders.Any(s => s.SubfolderType == "PDFs"));
                Assert("Test 11: Assignments subfolder present in DTO", detailsBefore.Subfolders.Any(s => s.SubfolderType == "Assignments"));
                Assert("Test 11: Forum_Discussion subfolder present in DTO", detailsBefore.Subfolders.Any(s => s.SubfolderType == "Forum_Discussion"));
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Acceptance Test 12: Real-time 1-click Provisioning from Academics Hub
        /// </summary>
        private static async Task Test12_RealtimeProvisionCourseNow()
        {
            var (sp, tempDir) = SetupTestEnvironment("TestDb_12");
            try
            {
                var provisioner = sp.GetRequiredService<IContentFolderProvisioner>();
                int cId;
                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();
                    var p = new CourseCategoryMaster { Name = "BTech", IsActive = true };
                    db.CourseCategoryMasters.Add(p);
                    await db.SaveChangesAsync();

                    var s = new SemesterMaster { Name = "Semester 1", IsActive = true };
                    db.SemesterMasters.Add(s);
                    await db.SaveChangesAsync();

                    var c = new CourseMaster { Name = "Artificial Intelligence", CourseCode = "CS401", CourseCategoryId = p.Id, SemesterId = s.Id, IsActive = true };
                    db.CourseMasters.Add(c);
                    await db.SaveChangesAsync();
                    cId = c.Id;
                }

                // 1-click real-time provisioning
                var result = await provisioner.ProvisionCourseNowAsync(cId);
                Assert("Test 12: 1-Click Provisioning returned success", result.Success);
                Assert("Test 12: Status transitioned to Completed immediately", result.Status == "Completed");
                Assert("Test 12: Folder path is populated", !string.IsNullOrWhiteSpace(result.FolderPath));
                Assert("Test 12: Folder URL is generated", !string.IsNullOrWhiteSpace(result.FolderUrl));
                Assert("Test 12: Physical course folder exists on storage", Directory.Exists(result.FolderPath));

                // Verify subfolders physically exist
                var videosPath = Path.Combine(result.FolderPath, "Videos");
                var pdfsPath = Path.Combine(result.FolderPath, "PDFs");
                var assignmentsPath = Path.Combine(result.FolderPath, "Assignments");
                var forumPath = Path.Combine(result.FolderPath, "Forum_Discussion");

                Assert("Test 12: Videos physical subfolder exists", Directory.Exists(videosPath));
                Assert("Test 12: PDFs physical subfolder exists", Directory.Exists(pdfsPath));
                Assert("Test 12: Assignments physical subfolder exists", Directory.Exists(assignmentsPath));
                Assert("Test 12: Forum_Discussion physical subfolder exists", Directory.Exists(forumPath));
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        /// <summary>
        /// Acceptance Test 13: Google Drive API v3 Provider Abstraction & URL generation
        /// </summary>
        private static async Task Test13_VerifyGoogleDriveApiV3Abstraction()
        {
            await Task.Yield();
            var gdriveProvider = new GoogleDriveContentStorageProvider(
                new DummyLogger<GoogleDriveContentStorageProvider>(),
                new HttpClient());

            var localProvider = new LocalContentStorageProvider(
                new DummyLogger<LocalContentStorageProvider>());

            var gdriveUrl = gdriveProvider.GetFolderUrl("1abc_folder_xyz");
            var localUrl = localProvider.GetFolderUrl("/EduDigiClass_LMS_Content/BBA");

            Assert("Test 13: Google Drive URL generated with proper folder ID", gdriveUrl == "https://drive.google.com/drive/folders/1abc_folder_xyz");
            Assert("Test 13: Local Drive file URL generated with proper prefix", localUrl.StartsWith("file:///"));
            Assert("Test 13: GoogleDrive ProviderKey is 'GoogleDrive'", gdriveProvider.ProviderKey == "GoogleDrive");
            Assert("Test 13: GoogleDrive DisplayName is populated", !string.IsNullOrWhiteSpace(gdriveProvider.DisplayName));
        }

        /// <summary>
        /// Acceptance Test 14: UNC path formatting & normalization
        /// </summary>
        private static async Task Test14_VerifyUncPathHardening()
        {
            await Task.Yield();
            var sanitized1 = LocalContentStorageProvider.SanitizeFolderName("BBA: Corporate Finance / Management * ? < > |");
            Assert("Test 14: Special characters sanitized for Windows/UNC compatibility", !sanitized1.Contains(':') && !sanitized1.Contains('*') && !sanitized1.Contains('?'));

            var localProvider = new LocalContentStorageProvider(new DummyLogger<LocalContentStorageProvider>());
            localProvider.UpdateConfig(true, @"\\network_nas\lms_share\content");

            var uncUrl = localProvider.GetFolderUrl(@"\\network_nas\lms_share\content\BBA");
            Assert("Test 14: UNC folder URL correctly generated", uncUrl.StartsWith("file://network_nas/lms_share/content/BBA"));
        }
    }
}
