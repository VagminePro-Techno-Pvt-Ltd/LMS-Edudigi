using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using TMS.Common;
using System;
using System.Threading.Tasks;

namespace TMS.Repository
{
    public static class DatabaseInitializer
    {
        public static async Task EnsureTablesCreatedAsync(
            ApplicationDBContext context,
            bool disposableDatabaseConfirmed = false)
        {
            if (!disposableDatabaseConfirmed)
                throw new InvalidOperationException(
                    "Database initialization is disabled unless a disposable database was explicitly verified.");

            string? adminPassword = Environment.GetEnvironmentVariable("TMS_BOOTSTRAP_ADMIN_PASSWORD");
            string? facultyPassword = Environment.GetEnvironmentVariable("TMS_BOOTSTRAP_FACULTY_PASSWORD");
            string? studentPassword = Environment.GetEnvironmentVariable("TMS_BOOTSTRAP_STUDENT_PASSWORD");

            if (string.IsNullOrWhiteSpace(adminPassword) ||
                string.IsNullOrWhiteSpace(facultyPassword) ||
                string.IsNullOrWhiteSpace(studentPassword))
            {
                throw new InvalidOperationException(
                    "Disposable database initialization requires TMS_BOOTSTRAP_ADMIN_PASSWORD, " +
                    "TMS_BOOTSTRAP_FACULTY_PASSWORD, and TMS_BOOTSTRAP_STUDENT_PASSWORD environment variables.");
            }

            string adminPasswordHash = SecurityUtility.HashPassword(adminPassword);
            string facultyPasswordHash = SecurityUtility.HashPassword(facultyPassword);
            string studentPasswordHash = SecurityUtility.HashPassword(studentPassword);

            try
            {
                string sql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LectureVideoProgress')
BEGIN
    CREATE TABLE [dbo].[LectureVideoProgress](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [StudentId] [int] NOT NULL,
        [LectureMaterialId] [int] NOT NULL,
        [DurationSeconds] [int] NOT NULL DEFAULT 0,
        [WatchedSeconds] [int] NOT NULL DEFAULT 0,
        [ProgressPercentage] [decimal](5,2) NOT NULL DEFAULT 0,
        [IsCompleted] [bit] NOT NULL DEFAULT 0,
        [LastPositionSeconds] [int] NOT NULL DEFAULT 0,
        [LastWatchedAt] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [CreatedBy] [int] NULL,
        [CreatedOn] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedOn] [datetime2](7) NULL,
        CONSTRAINT [PK_LectureVideoProgress] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE UNIQUE NONCLUSTERED INDEX [IX_LectureVideoProgress_Student_Material]
    ON [dbo].[LectureVideoProgress]([StudentId], [LectureMaterialId]);
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PPTSlideInteractions')
BEGIN
    CREATE TABLE [dbo].[PPTSlideInteractions](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [InteractivePPTId] [int] NOT NULL,
        [SlideNumber] [int] NOT NULL,
        [InteractionType] [nvarchar](50) NOT NULL DEFAULT 'Quiz',
        [QuestionText] [nvarchar](max) NOT NULL,
        [OptionsJson] [nvarchar](max) NULL,
        [CorrectOptionIndex] [int] NOT NULL DEFAULT 0,
        [Explanation] [nvarchar](max) NULL,
        [IsRequiredToProceed] [bit] NOT NULL DEFAULT 1,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [CreatedBy] [int] NULL,
        [CreatedOn] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedOn] [datetime2](7) NULL,
        CONSTRAINT [PK_PPTSlideInteractions] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END

-- ─── KEEP ONLY BBA & BCA PROGRAMS & COURSES ───
-- 1. Identify or create primary BCA category
DECLARE @BcaId INT = (
    SELECT TOP 1 [Id] FROM [dbo].[CourseCategoryMaster]
    WHERE [Name] LIKE '%Computer Application%' OR [Name] LIKE '%BCA%'
    ORDER BY [Id] ASC
);

IF @BcaId IS NULL
BEGIN
    INSERT INTO [dbo].[CourseCategoryMaster] ([Name], [IsActive], [CreatedOn], [CreatedBy])
    VALUES ('Bachelor of Computer Applications (BCA)', 1, GETDATE(), 1);
    SET @BcaId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE [dbo].[CourseCategoryMaster]
    SET [Name] = 'Bachelor of Computer Applications (BCA)', [IsActive] = 1
    WHERE [Id] = @BcaId;
END

-- 2. Identify or create primary BBA category
DECLARE @BbaId INT = (
    SELECT TOP 1 [Id] FROM [dbo].[CourseCategoryMaster]
    WHERE [Name] LIKE '%Business Administration%' OR [Name] LIKE '%BBA%' OR [Name] LIKE '%Bussiness%'
    ORDER BY [Id] ASC
);

IF @BbaId IS NULL
BEGIN
    INSERT INTO [dbo].[CourseCategoryMaster] ([Name], [IsActive], [CreatedOn], [CreatedBy])
    VALUES ('Bachelor of Business Administration (BBA)', 1, GETDATE(), 1);
    SET @BbaId = SCOPE_IDENTITY();
END
ELSE
BEGIN
    UPDATE [dbo].[CourseCategoryMaster]
    SET [Name] = 'Bachelor of Business Administration (BBA)', [IsActive] = 1
    WHERE [Id] = @BbaId;
END

-- Deactivate all other programs
UPDATE [dbo].[CourseCategoryMaster]
SET [IsActive] = 0
WHERE [Id] NOT IN (ISNULL(@BbaId, -1), ISNULL(@BcaId, -1));

-- Ensure Primary Course exists under BCA
DECLARE @PrimaryCourseId INT = (
    SELECT TOP 1 [Id] FROM [dbo].[CourseMaster]
    WHERE [CourseCategoryId] = @BcaId AND [IsActive] = 1
    ORDER BY [Id] ASC
);

IF @PrimaryCourseId IS NULL
BEGIN
    INSERT INTO [dbo].[CourseMaster] ([Name], [CourseCode], [CourseCategoryId], [CourseDurationMonths], [NoofCredit], [IsActive], [CreatedOn], [CreatedBy])
    VALUES ('Web Technologies & Database Systems', 'BCA101', @BcaId, 6, 4, 1, GETDATE(), 1);
    SET @PrimaryCourseId = SCOPE_IDENTITY();
END

-- Ensure 4 Standard Quadrants exist in CourseQuadrantMaster
IF OBJECT_ID('dbo.CourseQuadrantMaster', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [dbo].[CourseQuadrantMaster] WHERE [QuadrantNumber] = 1)
        INSERT INTO [dbo].[CourseQuadrantMaster] ([QuadrantNumber], [Name], [IsActive], [CreatedOn], [CreatedBy]) VALUES (1, 'e-Tutorial (Video & Audio)', 1, GETDATE(), 1);
    IF NOT EXISTS (SELECT 1 FROM [dbo].[CourseQuadrantMaster] WHERE [QuadrantNumber] = 2)
        INSERT INTO [dbo].[CourseQuadrantMaster] ([QuadrantNumber], [Name], [IsActive], [CreatedOn], [CreatedBy]) VALUES (2, 'e-Content (Notes & Reading)', 1, GETDATE(), 1);
    IF NOT EXISTS (SELECT 1 FROM [dbo].[CourseQuadrantMaster] WHERE [QuadrantNumber] = 3)
        INSERT INTO [dbo].[CourseQuadrantMaster] ([QuadrantNumber], [Name], [IsActive], [CreatedOn], [CreatedBy]) VALUES (3, 'Discussion Forum & Doubts', 1, GETDATE(), 1);
    IF NOT EXISTS (SELECT 1 FROM [dbo].[CourseQuadrantMaster] WHERE [QuadrantNumber] = 4)
        INSERT INTO [dbo].[CourseQuadrantMaster] ([QuadrantNumber], [Name], [IsActive], [CreatedOn], [CreatedBy]) VALUES (4, 'Assessment & Quizzes', 1, GETDATE(), 1);
END

-- Ensure Semester exists
DECLARE @PrimarySemesterId INT = (
    SELECT TOP 1 [Id] FROM [dbo].[SemesterMaster]
    WHERE [IsActive] = 1
    ORDER BY [Id] ASC
);

IF @PrimarySemesterId IS NULL AND OBJECT_ID('dbo.SemesterMaster', 'U') IS NOT NULL
BEGIN
    INSERT INTO [dbo].[SemesterMaster] ([Name], [IsActive], [CreatedOn], [CreatedBy])
    VALUES ('Semester 1', 1, GETDATE(), 1);
    SET @PrimarySemesterId = SCOPE_IDENTITY();
END

-- Ensure Subject exists for primary semester
DECLARE @SubjectId INT = (
    SELECT TOP 1 [Id] FROM [dbo].[SubjectMaster]
    WHERE ([SemesterId] = @PrimarySemesterId OR [IsActive] = 1)
    ORDER BY [Id] ASC
);

IF @SubjectId IS NULL AND @PrimarySemesterId IS NOT NULL AND OBJECT_ID('dbo.SubjectMaster', 'U') IS NOT NULL
BEGIN
    INSERT INTO [dbo].[SubjectMaster] ([Name], [SemesterId], [SubjectCode], [IsActive], [CreatedOn], [CreatedBy])
    VALUES ('Core Web Technologies', @PrimarySemesterId, 'BCA-101', 1, GETDATE(), 1);
    SET @SubjectId = SCOPE_IDENTITY();
END

-- Ensure Unit exists for primary subject
IF @SubjectId IS NOT NULL AND OBJECT_ID('dbo.UnitMaster', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [dbo].[UnitMaster] WHERE [SubjectId] = @SubjectId)
    BEGIN
        INSERT INTO [dbo].[UnitMaster] ([SubjectId], [Name], [SortOrder], [IsActive], [CreatedOn], [CreatedBy])
        VALUES (@SubjectId, 'Unit 1: Fundamentals of Web Technologies & Modern Database Architectures', 1, 1, GETDATE(), 1);
    END
END

-- Ensure Quiz Master & Quiz Assessment exist in FormMaster
IF OBJECT_ID('dbo.FormMaster', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [dbo].[FormMaster] WHERE [Controller] = 'Quiz')
    BEGIN
        INSERT INTO [dbo].[FormMaster] ([Id], [Name], [Area], [Controller], [Action], [IconClass], [Sequence], [MenuId], [IsAdmin], [ExcludeFromMenu])
        VALUES (26, 'Quiz Master', 'Admin', 'Quiz', 'Index', 'fa-solid fa-clipboard-question', 14, 1, 1, 0);
    END

    IF NOT EXISTS (SELECT 1 FROM [dbo].[FormMaster] WHERE [Controller] = 'FacultyQuizAssessment')
    BEGIN
        INSERT INTO [dbo].[FormMaster] ([Id], [Name], [Area], [Controller], [Action], [IconClass], [Sequence], [MenuId], [IsAdmin], [ExcludeFromMenu])
        VALUES (28, 'Quiz Assessment', '', 'FacultyQuizAssessment', 'Index', 'fa-solid fa-spell-check', 15, 1, 1, 0);
    END
END

-- Ensure TopicMaster table exists
IF OBJECT_ID('dbo.TopicMaster', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TopicMaster] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UnitId] INT NOT NULL,
        [Name] NVARCHAR(250) NOT NULL,
        [SortOrder] INT NOT NULL DEFAULT 0,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedBy] INT NULL,
        [CreatedOn] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [UpdatedBy] INT NULL,
        [UpdatedOn] DATETIME2 NULL
    );
END

-- ─── SEED DEMO ACCOUNTS ───
DECLARE @AdminRoleId INT = (SELECT TOP 1 [Id] FROM [dbo].[RoleMaster] WHERE [Name] LIKE '%Admin%' ORDER BY [Id] ASC);
DECLARE @FacultyRoleId INT = (SELECT TOP 1 [Id] FROM [dbo].[RoleMaster] WHERE [Name] LIKE '%Faculty%' OR [Name] LIKE '%Teacher%' ORDER BY [Id] ASC);
DECLARE @StudentRoleId INT = (SELECT TOP 1 [Id] FROM [dbo].[RoleMaster] WHERE [Name] LIKE '%Student%' ORDER BY [Id] ASC);

-- 1. Super Admin: newadmin@edudigi.com
IF NOT EXISTS (SELECT 1 FROM [dbo].[UserMaster] WHERE [Email] = 'newadmin@edudigi.com')
BEGIN
    INSERT INTO [dbo].[UserMaster] ([Name], [Email], [Password], [RoleId], [IsActive], [ForcePasswordChange], [CreatedOn], [CreatedBy])
    VALUES ('Edudigi Super Admin', 'newadmin@edudigi.com', @AdminPasswordHash, ISNULL(@AdminRoleId, 1), 1, 1, GETDATE(), 1);
END
ELSE
BEGIN
    UPDATE [dbo].[UserMaster]
    SET [RoleId] = ISNULL(@AdminRoleId, [RoleId]), [IsActive] = 1, [ForcePasswordChange] = 0
    WHERE [Email] = 'newadmin@edudigi.com';
END

-- 2. Faculty: newfaculty@edudigi.com
IF NOT EXISTS (SELECT 1 FROM [dbo].[UserMaster] WHERE [Email] = 'newfaculty@edudigi.com')
BEGIN
    INSERT INTO [dbo].[UserMaster] ([Name], [Email], [Password], [RoleId], [IsActive], [ForcePasswordChange], [CreatedOn], [CreatedBy])
    VALUES ('Edudigi Faculty', 'newfaculty@edudigi.com', @FacultyPasswordHash, ISNULL(@FacultyRoleId, 2), 1, 1, GETDATE(), 1);
END
ELSE
BEGIN
    UPDATE [dbo].[UserMaster]
    SET [RoleId] = ISNULL(@FacultyRoleId, [RoleId]), [IsActive] = 1, [ForcePasswordChange] = 0
    WHERE [Email] = 'newfaculty@edudigi.com';
END

-- Map Faculty to all active courses
DECLARE @FacultyUserId INT = (SELECT TOP 1 [Id] FROM [dbo].[UserMaster] WHERE [Email] = 'newfaculty@edudigi.com');
IF @FacultyUserId IS NOT NULL
BEGIN
    IF OBJECT_ID('dbo.CourseFacultyMaps', 'U') IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[CourseFacultyMaps] ([CourseId], [FacultyId], [IsActive], [CreatedOn], [CreatedBy])
        SELECT c.[Id], @FacultyUserId, 1, GETDATE(), 1
        FROM [dbo].[CourseMaster] c
        WHERE c.[IsActive] = 1
          AND NOT EXISTS (SELECT 1 FROM [dbo].[CourseFacultyMaps] m WHERE m.[CourseId] = c.[Id] AND m.[FacultyId] = @FacultyUserId);

        UPDATE [dbo].[CourseFacultyMaps]
        SET [IsActive] = 1
        WHERE [FacultyId] = @FacultyUserId
          AND [CourseId] IN (SELECT [Id] FROM [dbo].[CourseMaster] WHERE [IsActive] = 1);
    END

    IF OBJECT_ID('dbo.CourseFacultyMap', 'U') IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[CourseFacultyMap] ([CourseId], [FacultyId], [IsActive], [CreatedOn], [CreatedBy])
        SELECT c.[Id], @FacultyUserId, 1, GETDATE(), 1
        FROM [dbo].[CourseMaster] c
        WHERE c.[IsActive] = 1
          AND NOT EXISTS (SELECT 1 FROM [dbo].[CourseFacultyMap] m WHERE m.[CourseId] = c.[Id] AND m.[FacultyId] = @FacultyUserId);

        UPDATE [dbo].[CourseFacultyMap]
        SET [IsActive] = 1
        WHERE [FacultyId] = @FacultyUserId
          AND [CourseId] IN (SELECT [Id] FROM [dbo].[CourseMaster] WHERE [IsActive] = 1);
    END
END

-- 3. Student: newstudent@edudigi.com
IF NOT EXISTS (SELECT 1 FROM [dbo].[UserMaster] WHERE [Email] = 'newstudent@edudigi.com')
BEGIN
    INSERT INTO [dbo].[UserMaster] ([Name], [Email], [Password], [RoleId], [IsActive], [ForcePasswordChange], [CreatedOn], [CreatedBy])
    VALUES ('Edudigi Student', 'newstudent@edudigi.com', @StudentPasswordHash, ISNULL(@StudentRoleId, 3), 1, 1, GETDATE(), 1);
END
ELSE
BEGIN
    UPDATE [dbo].[UserMaster]
    SET [RoleId] = ISNULL(@StudentRoleId, [RoleId]),
        [IsActive] = 1,
        [ForcePasswordChange] = 0
    WHERE [Email] = 'newstudent@edudigi.com';
END

-- Enroll Student in all active courses
DECLARE @StudentUserId INT = (SELECT TOP 1 [Id] FROM [dbo].[UserMaster] WHERE [Email] = 'newstudent@edudigi.com');
IF @StudentUserId IS NOT NULL
BEGIN
    IF OBJECT_ID('dbo.CourseEnrollments', 'U') IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[CourseEnrollments] ([CourseId], [StudentId], [IsActive], [Status], [CreatedOn], [EnrolledOn], [CreatedBy])
        SELECT c.[Id], @StudentUserId, 1, 1, GETDATE(), GETDATE(), 1
        FROM [dbo].[CourseMaster] c
        WHERE c.[IsActive] = 1
          AND NOT EXISTS (SELECT 1 FROM [dbo].[CourseEnrollments] e WHERE e.[CourseId] = c.[Id] AND e.[StudentId] = @StudentUserId);

        UPDATE [dbo].[CourseEnrollments]
        SET [IsActive] = 1, [Status] = 1
        WHERE [StudentId] = @StudentUserId
          AND [CourseId] IN (SELECT [Id] FROM [dbo].[CourseMaster] WHERE [IsActive] = 1);
    END

    IF OBJECT_ID('dbo.CourseEnrollment', 'U') IS NOT NULL
    BEGIN
        INSERT INTO [dbo].[CourseEnrollment] ([CourseId], [StudentId], [IsActive], [Status], [CreatedOn], [EnrolledOn], [CreatedBy])
        SELECT c.[Id], @StudentUserId, 1, 1, GETDATE(), GETDATE(), 1
        FROM [dbo].[CourseMaster] c
        WHERE c.[IsActive] = 1
          AND NOT EXISTS (SELECT 1 FROM [dbo].[CourseEnrollment] e WHERE e.[CourseId] = c.[Id] AND e.[StudentId] = @StudentUserId);

        UPDATE [dbo].[CourseEnrollment]
        SET [IsActive] = 1, [Status] = 1
        WHERE [StudentId] = @StudentUserId
          AND [CourseId] IN (SELECT [Id] FROM [dbo].[CourseMaster] WHERE [IsActive] = 1);
    END
END

-- Seed Live Zoom Session if none exist today
IF @PrimaryCourseId IS NOT NULL AND OBJECT_ID('dbo.CourseMeetings', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [dbo].[CourseMeetings] WHERE [IsActive] = 1 AND [ScheduledAt] >= CAST(GETDATE() AS DATE))
    BEGIN
        INSERT INTO [dbo].[CourseMeetings] ([MeetingTitle], [CourseId], [Duration], [CourseQuadrantId], [ScheduledAt], [MeetingUrl], [IsActive], [CreatedBy], [CreatedOn])
        VALUES ('Interactive Live Lecture: Core Fundamentals & Case Analysis', @PrimaryCourseId, 60, 1, DATEADD(minute, 15, GETDATE()), 'https://zoom.us/j/8492048192?pwd=EduDigiLive2026', 1, ISNULL(@FacultyUserId, 1), GETDATE());
    END
END

-- Seed Demo Quiz under Primary Course Quadrant 4
DECLARE @Q4Id INT = (SELECT TOP 1 [Id] FROM [dbo].[CourseQuadrantMaster] WHERE [QuadrantNumber] = 4 OR [Name] LIKE '%Quiz%' OR [Name] LIKE '%Assessment%' ORDER BY [Id] ASC);
IF @PrimaryCourseId IS NOT NULL AND @Q4Id IS NOT NULL AND OBJECT_ID('dbo.QuizMaster', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [dbo].[QuizMaster] WHERE [CourseId] = @PrimaryCourseId AND [IsActive] = 1)
    BEGIN
        INSERT INTO [dbo].[QuizMaster] ([CourseId], [CourseQuadrantId], [Title], [TotalMarks], [PassingMarks], [IsActive], [CreatedBy], [CreatedOn])
        VALUES (@PrimaryCourseId, @Q4Id, 'Web Technologies & Database Fundamentals Quiz', 20, 10, 1, ISNULL(@FacultyUserId, 1), GETDATE());

        DECLARE @NewQuizId INT = SCOPE_IDENTITY();
        IF @NewQuizId IS NOT NULL AND OBJECT_ID('dbo.QuizQuestion', 'U') IS NOT NULL
        BEGIN
            INSERT INTO [dbo].[QuizQuestion] ([QuizId], [QuestionText], [QuestionType], [MaxMarks], [IsActive], [CreatedBy], [CreatedOn])
            VALUES (@NewQuizId, 'Which HTTP status code indicates a successful client request in REST APIs?', 1, 10, 1, ISNULL(@FacultyUserId, 1), GETDATE());

            DECLARE @Q1Id INT = SCOPE_IDENTITY();
            IF @Q1Id IS NOT NULL AND OBJECT_ID('dbo.QuizOption', 'U') IS NOT NULL
            BEGIN
                INSERT INTO [dbo].[QuizOption] ([QuizQuestionId], [OptionText], [IsCorrectOption], [IsActive], [CreatedBy], [CreatedOn]) VALUES (@Q1Id, '200 OK', 1, 1, ISNULL(@FacultyUserId, 1), GETDATE());
                DECLARE @CorrectOptId INT = SCOPE_IDENTITY();
                INSERT INTO [dbo].[QuizOption] ([QuizQuestionId], [OptionText], [IsCorrectOption], [IsActive], [CreatedBy], [CreatedOn]) VALUES (@Q1Id, '404 Not Found', 0, 1, ISNULL(@FacultyUserId, 1), GETDATE());
                INSERT INTO [dbo].[QuizOption] ([QuizQuestionId], [OptionText], [IsCorrectOption], [IsActive], [CreatedBy], [CreatedOn]) VALUES (@Q1Id, '500 Internal Server Error', 0, 1, ISNULL(@FacultyUserId, 1), GETDATE());
                INSERT INTO [dbo].[QuizOption] ([QuizQuestionId], [OptionText], [IsCorrectOption], [IsActive], [CreatedBy], [CreatedOn]) VALUES (@Q1Id, '301 Moved Permanently', 0, 1, ISNULL(@FacultyUserId, 1), GETDATE());

                UPDATE [dbo].[QuizQuestion] SET [CorrectOptionId] = @CorrectOptId WHERE [Id] = @Q1Id;
            END

            INSERT INTO [dbo].[QuizQuestion] ([QuizId], [QuestionText], [QuestionType], [MaxMarks], [IsActive], [CreatedBy], [CreatedOn])
            VALUES (@NewQuizId, 'Which SQL constraint uniquely identifies each record in a relational database table?', 1, 10, 1, ISNULL(@FacultyUserId, 1), GETDATE());

            DECLARE @Q2Id INT = SCOPE_IDENTITY();
            IF @Q2Id IS NOT NULL AND OBJECT_ID('dbo.QuizOption', 'U') IS NOT NULL
            BEGIN
                INSERT INTO [dbo].[QuizOption] ([QuizQuestionId], [OptionText], [IsCorrectOption], [IsActive], [CreatedBy], [CreatedOn]) VALUES (@Q2Id, 'PRIMARY KEY', 1, 1, ISNULL(@FacultyUserId, 1), GETDATE());
                DECLARE @CorrectOpt2Id INT = SCOPE_IDENTITY();
                INSERT INTO [dbo].[QuizOption] ([QuizQuestionId], [OptionText], [IsCorrectOption], [IsActive], [CreatedBy], [CreatedOn]) VALUES (@Q2Id, 'FOREIGN KEY', 0, 1, ISNULL(@FacultyUserId, 1), GETDATE());
                INSERT INTO [dbo].[QuizOption] ([QuizQuestionId], [OptionText], [IsCorrectOption], [IsActive], [CreatedBy], [CreatedOn]) VALUES (@Q2Id, 'CHECK', 0, 1, ISNULL(@FacultyUserId, 1), GETDATE());
                INSERT INTO [dbo].[QuizOption] ([QuizQuestionId], [OptionText], [IsCorrectOption], [IsActive], [CreatedBy], [CreatedOn]) VALUES (@Q2Id, 'DEFAULT', 0, 1, ISNULL(@FacultyUserId, 1), GETDATE());

                UPDATE [dbo].[QuizQuestion] SET [CorrectOptionId] = @CorrectOpt2Id WHERE [Id] = @Q2Id;
            END
        END
    END
END
";
                await context.Database.ExecuteSqlRawAsync(
                    sql,
                    new SqlParameter("@AdminPasswordHash", adminPasswordHash),
                    new SqlParameter("@FacultyPasswordHash", facultyPasswordHash),
                    new SqlParameter("@StudentPasswordHash", studentPasswordHash));
            }
            catch (Exception ex)
            {
                // Ignore if permission denied or already created
                Console.WriteLine($"[DatabaseInitializer] Warning: {ex.Message}");
            }
        }
    }
}
