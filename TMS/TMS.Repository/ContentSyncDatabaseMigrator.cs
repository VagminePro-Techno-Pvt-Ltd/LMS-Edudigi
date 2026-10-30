using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace TMS.Repository
{
    public static class ContentSyncDatabaseMigrator
    {
        public static async Task EnsureTablesCreatedAsync(ApplicationDBContext context, ILogger? logger = null)
        {
            try
            {
                string sql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ContentSyncConfig')
BEGIN
    CREATE TABLE [dbo].[ContentSyncConfig](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [LocalDriveEnabled] [bit] NOT NULL DEFAULT 1,
        [LocalDriveRootPath] [nvarchar](500) NOT NULL DEFAULT 'EduDigiClass_LMS_Content',
        [GoogleDriveEnabled] [bit] NOT NULL DEFAULT 0,
        [GoogleDriveRootFolderName] [nvarchar](200) NOT NULL DEFAULT 'EduDigiClass_LMS_Content',
        [GoogleDriveRootFolderId] [nvarchar](200) NULL,
        [GoogleDriveServiceAccountEmail] [nvarchar](200) NULL,
        [GoogleDriveNetworkDisabled] [bit] NOT NULL DEFAULT 0,
        [AutoSyncIntervalSeconds] [int] NOT NULL DEFAULT 10,
        [DriveToLmsSyncEnabled] [bit] NOT NULL DEFAULT 1,
        [LastDriveToLmsScanUtc] [datetime2](7) NULL,
        [UpdatedAt] [datetime2](7) NULL,
        CONSTRAINT [PK_ContentSyncConfig] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    INSERT INTO [dbo].[ContentSyncConfig] ([LocalDriveEnabled], [LocalDriveRootPath], [GoogleDriveEnabled], [GoogleDriveRootFolderName], [GoogleDriveNetworkDisabled], [AutoSyncIntervalSeconds], [DriveToLmsSyncEnabled], [UpdatedAt])
    VALUES (1, 'EduDigiClass_LMS_Content', 0, 'EduDigiClass_LMS_Content', 0, 10, 1, GETUTCDATE());
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ContentFolderMapping')
BEGIN
    CREATE TABLE [dbo].[ContentFolderMapping](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [EntityType] [nvarchar](50) NOT NULL,
        [EntityId] [int] NOT NULL,
        [ParentEntityId] [int] NULL,
        [SubfolderType] [nvarchar](50) NULL,
        [StorageProvider] [nvarchar](50) NOT NULL,
        [ProviderFolderId] [nvarchar](500) NOT NULL,
        [ProviderParentFolderId] [nvarchar](500) NULL,
        [FolderName] [nvarchar](250) NOT NULL,
        [Status] [nvarchar](50) NOT NULL DEFAULT 'Active',
        [CreatedAt] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] [datetime2](7) NULL,
        CONSTRAINT [PK_ContentFolderMapping] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE NONCLUSTERED INDEX [IX_ContentFolderMapping_Entity] ON [dbo].[ContentFolderMapping]([EntityType], [EntityId], [StorageProvider]);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ContentSyncOutboxJob')
BEGIN
    CREATE TABLE [dbo].[ContentSyncOutboxJob](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [EntityType] [nvarchar](50) NOT NULL,
        [EntityId] [int] NOT NULL,
        [Action] [nvarchar](50) NOT NULL,
        [StorageProvider] [nvarchar](50) NOT NULL,
        [Payload] [nvarchar](max) NULL,
        [Status] [nvarchar](50) NOT NULL DEFAULT 'Pending',
        [RetryCount] [int] NOT NULL DEFAULT 0,
        [MaxRetries] [int] NOT NULL DEFAULT 5,
        [NextRetryUtc] [datetime2](7) NULL,
        [LastError] [nvarchar](max) NULL,
        [CreatedAt] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [ProcessedAt] [datetime2](7) NULL,
        CONSTRAINT [PK_ContentSyncOutboxJob] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE NONCLUSTERED INDEX [IX_ContentSyncOutboxJob_Status] ON [dbo].[ContentSyncOutboxJob]([Status], [NextRetryUtc]);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ContentSyncFileRecord')
BEGIN
    CREATE TABLE [dbo].[ContentSyncFileRecord](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [StorageProvider] [nvarchar](50) NOT NULL,
        [CourseId] [int] NOT NULL,
        [SubfolderType] [nvarchar](50) NOT NULL,
        [ProviderFileId] [nvarchar](500) NOT NULL,
        [FileName] [nvarchar](255) NOT NULL,
        [FileSize] [bigint] NOT NULL DEFAULT 0,
        [FileHash] [nvarchar](100) NULL,
        [LmsRecordType] [nvarchar](50) NOT NULL,
        [LmsRecordId] [int] NOT NULL,
        [SyncStatus] [nvarchar](50) NOT NULL DEFAULT 'Synced',
        [SyncedAt] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [ErrorMessage] [nvarchar](max) NULL,
        CONSTRAINT [PK_ContentSyncFileRecord] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE NONCLUSTERED INDEX [IX_ContentSyncFileRecord_Course] ON [dbo].[ContentSyncFileRecord]([CourseId], [SubfolderType]);
END;
";
                await context.Database.ExecuteSqlRawAsync(sql);
                logger?.LogInformation("Content sync database tables ensured successfully.");
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to ensure ContentSync tables via ExecuteSqlRawAsync: {Message}", ex.Message);
            }
        }
    }
}
