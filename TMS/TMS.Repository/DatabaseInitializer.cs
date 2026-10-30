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
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogs')
BEGIN
    CREATE TABLE [dbo].[AuditLogs](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [UserId] [int] NULL,
        [Username] [nvarchar](150) NULL,
        [UserRole] [nvarchar](50) NULL,
        [ActionType] [nvarchar](100) NOT NULL,
        [ModuleName] [nvarchar](100) NOT NULL,
        [EntityType] [nvarchar](100) NULL,
        [EntityId] [nvarchar](100) NULL,
        [OldValuesJson] [nvarchar](max) NULL,
        [NewValuesJson] [nvarchar](max) NULL,
        [ChangedFieldsJson] [nvarchar](max) NULL,
        [TimestampUtc] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [IpAddress] [nvarchar](50) NULL,
        [Status] [nvarchar](50) NOT NULL DEFAULT 'SUCCESS',
        [CorrelationId] [nvarchar](100) NULL,
        [Details] [nvarchar](max) NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE NONCLUSTERED INDEX [IX_AuditLogs_TimestampUtc] ON [dbo].[AuditLogs]([TimestampUtc] DESC);
    CREATE NONCLUSTERED INDEX [IX_AuditLogs_UserId] ON [dbo].[AuditLogs]([UserId]);
    CREATE NONCLUSTERED INDEX [IX_AuditLogs_ModuleName] ON [dbo].[AuditLogs]([ModuleName]);
    CREATE NONCLUSTERED INDEX [IX_AuditLogs_ActionType] ON [dbo].[AuditLogs]([ActionType]);
    CREATE NONCLUSTERED INDEX [IX_AuditLogs_Status] ON [dbo].[AuditLogs]([Status]);
END

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

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DocumentIndexStatuses')
BEGIN
    CREATE TABLE [dbo].[DocumentIndexStatuses](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [LectureMaterialId] [int] NOT NULL,
        [Status] [nvarchar](50) NOT NULL,
        [FileHash] [nvarchar](100) NULL,
        [PageCount] [int] NOT NULL DEFAULT 0,
        [TotalChunks] [int] NOT NULL DEFAULT 0,
        [ErrorMessage] [nvarchar](max) NULL,
        [LastIndexedOn] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [CreatedBy] [int] NOT NULL DEFAULT 0,
        [CreatedOn] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedOn] [datetime2](7) NULL,
        CONSTRAINT [PK_DocumentIndexStatuses] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE UNIQUE NONCLUSTERED INDEX [IX_DocumentIndexStatuses_LectureMaterialId]
    ON [dbo].[DocumentIndexStatuses]([LectureMaterialId]);
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DocumentChunks')
BEGIN
    CREATE TABLE [dbo].[DocumentChunks](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [LectureMaterialId] [int] NOT NULL,
        [ChunkIndex] [int] NOT NULL,
        [PageNumber] [int] NULL,
        [Heading] [nvarchar](300) NULL,
        [Content] [nvarchar](max) NOT NULL,
        [WordCount] [int] NOT NULL DEFAULT 0,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [CreatedBy] [int] NOT NULL DEFAULT 0,
        [CreatedOn] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedOn] [datetime2](7) NULL,
        CONSTRAINT [PK_DocumentChunks] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE NONCLUSTERED INDEX [IX_DocumentChunks_LectureMaterialId]
    ON [dbo].[DocumentChunks]([LectureMaterialId]);
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LearningAssistantAnswerCaches')
BEGIN
    CREATE TABLE [dbo].[LearningAssistantAnswerCaches](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [CacheKey] [nvarchar](128) NOT NULL,
        [NormalizedQuery] [nvarchar](500) NOT NULL,
        [Intent] [nvarchar](50) NOT NULL DEFAULT 'Tutoring',
        [CourseId] [int] NULL,
        [LectureMaterialId] [int] NULL,
        [DocumentHash] [nvarchar](100) NULL,
        [ModelName] [nvarchar](50) NOT NULL DEFAULT 'gemini-1.5-flash',
        [Language] [nvarchar](20) NOT NULL DEFAULT 'en',
        [Reply] [nvarchar](max) NOT NULL,
        [SourcesJson] [nvarchar](max) NULL,
        [SuggestedActionsJson] [nvarchar](max) NULL,
        [HitCount] [int] NOT NULL DEFAULT 0,
        [PromptTokens] [int] NOT NULL DEFAULT 0,
        [CandidateTokens] [int] NOT NULL DEFAULT 0,
        [TotalTokens] [int] NOT NULL DEFAULT 0,
        [ExpiresOn] [datetime2](7) NOT NULL,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [CreatedBy] [int] NOT NULL DEFAULT 0,
        [CreatedOn] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedOn] [datetime2](7) NULL,
        CONSTRAINT [PK_LearningAssistantAnswerCaches] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE UNIQUE NONCLUSTERED INDEX [IX_LearningAssistantAnswerCaches_CacheKey]
    ON [dbo].[LearningAssistantAnswerCaches]([CacheKey]);
    CREATE NONCLUSTERED INDEX [IX_LearningAssistantAnswerCaches_LectureMaterialId]
    ON [dbo].[LearningAssistantAnswerCaches]([LectureMaterialId]);
    CREATE NONCLUSTERED INDEX [IX_LearningAssistantAnswerCaches_ExpiresOn]
    ON [dbo].[LearningAssistantAnswerCaches]([ExpiresOn]);
END

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'LearningAssistantAnswerCaches')
BEGIN
    DELETE FROM [dbo].[LearningAssistantAnswerCaches]
    WHERE [Reply] LIKE '%not covered in the current syllabus%'
       OR [Reply] LIKE '%temporarily unavailable%'
       OR [Reply] LIKE '%unable to formulate an explanation%';
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('QuestionBank') AND name = 'SemesterId')
BEGIN
    ALTER TABLE QuestionBank ADD SemesterId INT NULL;
    ALTER TABLE QuestionBank ADD CONSTRAINT FK_QuestionBank_SemesterMaster_SemesterId FOREIGN KEY (SemesterId) REFERENCES SemesterMaster(Id);
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('QuestionBank') AND name = 'SubjectId')
BEGIN
    ALTER TABLE QuestionBank ADD SubjectId INT NULL;
    ALTER TABLE QuestionBank ADD CONSTRAINT FK_QuestionBank_SubjectMaster_SubjectId FOREIGN KEY (SubjectId) REFERENCES SubjectMaster(Id);
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('QuestionBank') AND name = 'CorrectAnswer')
BEGIN
    ALTER TABLE QuestionBank ADD CorrectAnswer NVARCHAR(MAX) NULL;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('QuestionBank') AND name = 'Status')
BEGIN
    ALTER TABLE QuestionBank ADD Status NVARCHAR(50) NOT NULL DEFAULT 'Active';
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'QuestionPaper')
BEGIN
    CREATE TABLE [dbo].[QuestionPaper](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [CourseId] [int] NOT NULL,
        [SemesterId] [int] NULL,
        [SubjectId] [int] NULL,
        [Title] [nvarchar](250) NOT NULL,
        [AssessmentType] [nvarchar](100) NOT NULL,
        [TotalMarks] [decimal](18,2) NOT NULL,
        [Status] [nvarchar](50) NOT NULL DEFAULT 'Draft',
        [FacultyId] [int] NOT NULL,
        [PublishedOn] [datetime2](7) NULL,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [CreatedBy] [int] NOT NULL,
        [CreatedOn] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedOn] [datetime2](7) NULL,
        CONSTRAINT [PK_QuestionPaper] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_QuestionPaper_CourseMaster_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [dbo].[CourseMaster]([Id]),
        CONSTRAINT [FK_QuestionPaper_SemesterMaster_SemesterId] FOREIGN KEY ([SemesterId]) REFERENCES [dbo].[SemesterMaster]([Id]),
        CONSTRAINT [FK_QuestionPaper_SubjectMaster_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [dbo].[SubjectMaster]([Id]),
        CONSTRAINT [FK_QuestionPaper_UserMaster_FacultyId] FOREIGN KEY ([FacultyId]) REFERENCES [dbo].[UserMaster]([Id])
    );
    CREATE NONCLUSTERED INDEX [IX_QuestionPaper_CourseId] ON [dbo].[QuestionPaper]([CourseId]);
    CREATE NONCLUSTERED INDEX [IX_QuestionPaper_FacultyId] ON [dbo].[QuestionPaper]([FacultyId]);
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'QuestionPaperQuestion')
BEGIN
    CREATE TABLE [dbo].[QuestionPaperQuestion](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [QuestionPaperId] [int] NOT NULL,
        [QuestionBankId] [int] NOT NULL,
        [DisplayOrder] [int] NOT NULL DEFAULT 1,
        [Marks] [decimal](18,2) NOT NULL DEFAULT 1,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [CreatedOn] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_QuestionPaperQuestion] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_QuestionPaperQuestion_QuestionPaper_QuestionPaperId] FOREIGN KEY ([QuestionPaperId]) REFERENCES [dbo].[QuestionPaper]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_QuestionPaperQuestion_QuestionBank_QuestionBankId] FOREIGN KEY ([QuestionBankId]) REFERENCES [dbo].[QuestionBank]([Id])
    );
    CREATE NONCLUSTERED INDEX [IX_QuestionPaperQuestion_QuestionPaperId] ON [dbo].[QuestionPaperQuestion]([QuestionPaperId]);
    CREATE NONCLUSTERED INDEX [IX_QuestionPaperQuestion_QuestionBankId] ON [dbo].[QuestionPaperQuestion]([QuestionBankId]);
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('QuestionPaper') AND name = 'DurationMinutes')
    ALTER TABLE QuestionPaper ADD DurationMinutes INT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('QuestionPaper') AND name = 'Instructions')
    ALTER TABLE QuestionPaper ADD Instructions NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('QuestionPaperQuestion') AND name = 'SectionName')
    ALTER TABLE QuestionPaperQuestion ADD SectionName NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('QuestionPaperQuestion') AND name = 'SectionInstructions')
    ALTER TABLE QuestionPaperQuestion ADD SectionInstructions NVARCHAR(MAX) NULL;

-- ─── PHASE 3: QUESTION BANK INTELLIGENCE & AUTOMATED RESULT PROCESSING ───
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('QuestionBank') AND name = 'UnitId')
    ALTER TABLE QuestionBank ADD UnitId INT NULL CONSTRAINT FK_QuestionBank_Unit FOREIGN KEY REFERENCES UnitMaster(Id);

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('QuestionBank') AND name = 'TopicId')
    ALTER TABLE QuestionBank ADD TopicId INT NULL CONSTRAINT FK_QuestionBank_Topic FOREIGN KEY REFERENCES TopicMaster(Id);

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('QuestionBank') AND name = 'LearningOutcome')
    ALTER TABLE QuestionBank ADD LearningOutcome NVARCHAR(250) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('QuestionBank') AND name = 'NormalizedText')
    ALTER TABLE QuestionBank ADD NormalizedText NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('InternalAssessments') AND name = 'IsCompartmental')
    ALTER TABLE InternalAssessments ADD IsCompartmental BIT NOT NULL CONSTRAINT DF_IA_IsCompartmental DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('InternalAssessments') AND name = 'AttemptNumber')
    ALTER TABLE InternalAssessments ADD AttemptNumber INT NOT NULL CONSTRAINT DF_IA_AttemptNumber DEFAULT 1;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('InternalAssessments') AND name = 'OriginalAssessmentId')
    ALTER TABLE InternalAssessments ADD OriginalAssessmentId INT NULL CONSTRAINT FK_IA_OriginalAssessment FOREIGN KEY REFERENCES InternalAssessments(Id);

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'InstitutionalGradingPolicy')
BEGIN
    CREATE TABLE InstitutionalGradingPolicy (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(150) NOT NULL,
        Code NVARCHAR(50) NOT NULL,
        PassingPercentage DECIMAL(5,2) NOT NULL DEFAULT 33.00,
        CompartmentalMinPercentage DECIMAL(5,2) NOT NULL DEFAULT 20.00,
        MaxCompartmentSubjects INT NOT NULL DEFAULT 2,
        RoundingMethod NVARCHAR(50) NOT NULL DEFAULT 'Standard',
        GradeBoundariesJson NVARCHAR(MAX) NOT NULL,
        IsDefault BIT NOT NULL DEFAULT 1,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedBy INT NOT NULL DEFAULT 0,
        CreatedOn DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedBy INT NULL,
        UpdatedOn DATETIME2 NULL
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ProcessedAssessmentResult')
BEGIN
    CREATE TABLE ProcessedAssessmentResult (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        AssessmentId INT NOT NULL CONSTRAINT FK_PAR_Assessment REFERENCES InternalAssessments(Id),
        StudentId INT NOT NULL CONSTRAINT FK_PAR_Student REFERENCES UserMaster(Id),
        CourseId INT NOT NULL CONSTRAINT FK_PAR_Course REFERENCES CourseMaster(Id),
        SemesterId INT NULL CONSTRAINT FK_PAR_Semester REFERENCES SemesterMaster(Id),
        PolicyId INT NULL CONSTRAINT FK_PAR_Policy REFERENCES InstitutionalGradingPolicy(Id),
        MaxMarks DECIMAL(10,2) NOT NULL,
        ObtainedMarks DECIMAL(10,2) NOT NULL DEFAULT 0,
        Percentage DECIMAL(5,2) NOT NULL DEFAULT 0,
        Grade NVARCHAR(10) NOT NULL DEFAULT 'F',
        GradePoint DECIMAL(4,2) NOT NULL DEFAULT 0,
        ResultStatus NVARCHAR(50) NOT NULL DEFAULT 'PENDING',
        IsCompartmental BIT NOT NULL DEFAULT 0,
        AttemptNumber INT NOT NULL DEFAULT 1,
        OriginalResultId INT NULL,
        IsEvaluated BIT NOT NULL DEFAULT 0,
        IsPublished BIT NOT NULL DEFAULT 0,
        PublishedOn DATETIME2 NULL,
        PublishedBy INT NULL,
        CalculatedOn DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CalculatedBy INT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        AuditRemark NVARCHAR(500) NULL
    );
    CREATE UNIQUE INDEX IX_PAR_Assess_Student_Attempt ON ProcessedAssessmentResult(AssessmentId, StudentId, AttemptNumber);
    CREATE INDEX IX_PAR_Course_Student ON ProcessedAssessmentResult(CourseId, StudentId);
END

-- ─── PHASE 3 COMPLETION: PROCTORING, BLUEPRINT & GRACE MARKS ───
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('QuestionPaper') AND name = 'Instructions')
    ALTER TABLE QuestionPaper ADD Instructions NVARCHAR(1000) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('QuestionPaper') AND name = 'DurationMinutes')
    ALTER TABLE QuestionPaper ADD DurationMinutes INT NULL CONSTRAINT DF_QP_Duration DEFAULT 180;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('QuestionPaperQuestion') AND name = 'SectionName')
    ALTER TABLE QuestionPaperQuestion ADD SectionName NVARCHAR(50) NOT NULL CONSTRAINT DF_QPQ_Section DEFAULT 'Section A';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('QuestionPaperQuestion') AND name = 'SectionInstructions')
    ALTER TABLE QuestionPaperQuestion ADD SectionInstructions NVARCHAR(500) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ExamMaster') AND name = 'IsProctored')
    ALTER TABLE ExamMaster ADD IsProctored BIT NOT NULL CONSTRAINT DF_EM_IsProctored DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ExamMaster') AND name = 'RequireCamera')
    ALTER TABLE ExamMaster ADD RequireCamera BIT NOT NULL CONSTRAINT DF_EM_RequireCamera DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ExamMaster') AND name = 'TrackTabSwitches')
    ALTER TABLE ExamMaster ADD TrackTabSwitches BIT NOT NULL CONSTRAINT DF_EM_TrackTabSwitches DEFAULT 1;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ExamMaster') AND name = 'RequireFullScreen')
    ALTER TABLE ExamMaster ADD RequireFullScreen BIT NOT NULL CONSTRAINT DF_EM_RequireFullScreen DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ExamMaster') AND name = 'MaxAllowedViolations')
    ALTER TABLE ExamMaster ADD MaxAllowedViolations INT NOT NULL CONSTRAINT DF_EM_MaxViolations DEFAULT 5;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ExamMaster') AND name = 'RetentionDays')
    ALTER TABLE ExamMaster ADD RetentionDays INT NOT NULL CONSTRAINT DF_EM_RetentionDays DEFAULT 90;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ExamMaster') AND name = 'EnableAiFaceCheck')
    ALTER TABLE ExamMaster ADD EnableAiFaceCheck BIT NOT NULL CONSTRAINT DF_EM_AiFaceCheck DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ProctoringSession')
BEGIN
    CREATE TABLE ProctoringSession (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        AttemptId INT NOT NULL CONSTRAINT FK_ProcSession_Attempt REFERENCES StudentExamAttempt(Id),
        ExamId INT NOT NULL CONSTRAINT FK_ProcSession_Exam REFERENCES ExamMaster(Id),
        StudentId INT NOT NULL CONSTRAINT FK_ProcSession_Student REFERENCES UserMaster(Id),
        StartedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        EndedAt DATETIME2 NULL,
        CameraConsentGiven BIT NOT NULL DEFAULT 0,
        ConsentTimestamp DATETIME2 NULL,
        IdentityVerified BIT NOT NULL DEFAULT 0,
        VerificationMethod NVARCHAR(50) NOT NULL DEFAULT 'SystemProfile',
        TotalViolationsCount INT NOT NULL DEFAULT 0,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Active',
        ReviewerRemarks NVARCHAR(1000) NULL,
        ReviewedBy INT NULL,
        ReviewedOn DATETIME2 NULL
    );
    CREATE INDEX IX_ProcSession_Attempt ON ProctoringSession(AttemptId);
    CREATE INDEX IX_ProcSession_Exam ON ProctoringSession(ExamId);
END

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ProctoringIncident')
BEGIN
    CREATE TABLE ProctoringIncident (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        SessionId INT NOT NULL CONSTRAINT FK_ProcIncident_Session REFERENCES ProctoringSession(Id) ON DELETE CASCADE,
        AttemptId INT NULL,
        EventType NVARCHAR(50) NOT NULL,
        Severity NVARCHAR(20) NOT NULL DEFAULT 'Medium',
        ServerTimestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ClientTimestamp DATETIME2 NULL,
        Details NVARCHAR(1000) NULL,
        RequiresReview BIT NOT NULL DEFAULT 1,
        IsReviewed BIT NOT NULL DEFAULT 0,
        ReviewedBy INT NULL,
        ReviewNotes NVARCHAR(500) NULL
    );
    CREATE INDEX IX_ProcIncident_Session ON ProctoringIncident(SessionId);
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('InstitutionalGradingPolicy') AND name = 'MaxGraceMarks')
    ALTER TABLE InstitutionalGradingPolicy ADD MaxGraceMarks DECIMAL(5,2) NOT NULL CONSTRAINT DF_IGP_MaxGrace DEFAULT 0.00;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('InstitutionalGradingPolicy') AND name = 'EnableGraceMarks')
    ALTER TABLE InstitutionalGradingPolicy ADD EnableGraceMarks BIT NOT NULL CONSTRAINT DF_IGP_EnableGrace DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ProcessedAssessmentResult') AND name = 'GraceMarksAwarded')
    ALTER TABLE ProcessedAssessmentResult ADD GraceMarksAwarded DECIMAL(5,2) NOT NULL CONSTRAINT DF_PAR_GraceAwarded DEFAULT 0.00;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ProcessedAssessmentResult') AND name = 'IsGraceApplied')
    ALTER TABLE ProcessedAssessmentResult ADD IsGraceApplied BIT NOT NULL CONSTRAINT DF_PAR_IsGraceApplied DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ProcessedAssessmentResult') AND name = 'ExamMasterId')
    ALTER TABLE ProcessedAssessmentResult ADD ExamMasterId INT NULL CONSTRAINT FK_PAR_ExamMaster REFERENCES ExamMaster(Id);

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ProcessedAssessmentResult') AND name = 'StudentExamAttemptId')
    ALTER TABLE ProcessedAssessmentResult ADD StudentExamAttemptId INT NULL CONSTRAINT FK_PAR_StudentAttempt REFERENCES StudentExamAttempt(Id);

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ProcessedAssessmentResult') AND name = 'AssessmentId' AND is_nullable = 0)
    ALTER TABLE ProcessedAssessmentResult ALTER COLUMN AssessmentId INT NULL;

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

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('TopicMaster') AND name = 'Description')
BEGIN
    ALTER TABLE [dbo].[TopicMaster] ADD [Description] NVARCHAR(200) NULL;
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
