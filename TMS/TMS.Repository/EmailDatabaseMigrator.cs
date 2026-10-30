using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace TMS.Repository
{
    public static class EmailDatabaseMigrator
    {
        public static async Task EnsureTablesCreatedAsync(ApplicationDBContext context, ILogger? logger = null)
        {
            try
            {
                string sql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EmailOutboxJob')
BEGIN
    CREATE TABLE [dbo].[EmailOutboxJob](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [RecipientEmail] [nvarchar](255) NOT NULL,
        [RecipientName] [nvarchar](255) NULL,
        [Subject] [nvarchar](500) NOT NULL,
        [TemplateName] [nvarchar](100) NOT NULL,
        [SafePayloadJson] [nvarchar](max) NULL,
        [Status] [nvarchar](50) NOT NULL DEFAULT 'Pending',
        [CreatedAtUtc] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [ScheduledForUtc] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [AttemptCount] [int] NOT NULL DEFAULT 0,
        [MaxRetries] [int] NOT NULL DEFAULT 5,
        [NextAttemptAtUtc] [datetime2](7) NULL,
        [SentAtUtc] [datetime2](7) NULL,
        [LastError] [nvarchar](max) NULL,
        [IdempotencyKey] [nvarchar](255) NULL,
        [EventType] [nvarchar](100) NULL,
        [CorrelationId] [nvarchar](100) NULL,
        [LockedUntilUtc] [datetime2](7) NULL,
        CONSTRAINT [PK_EmailOutboxJob] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    CREATE NONCLUSTERED INDEX [IX_EmailOutboxJob_Status_Scheduled] 
    ON [dbo].[EmailOutboxJob]([Status], [ScheduledForUtc]);

    CREATE NONCLUSTERED INDEX [IX_EmailOutboxJob_IdempotencyKey] 
    ON [dbo].[EmailOutboxJob]([IdempotencyKey]) 
    WHERE [IdempotencyKey] IS NOT NULL;

    CREATE NONCLUSTERED INDEX [IX_EmailOutboxJob_EventType] 
    ON [dbo].[EmailOutboxJob]([EventType]);
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EmailDeliveryLog')
BEGIN
    CREATE TABLE [dbo].[EmailDeliveryLog](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [EmailOutboxJobId] [int] NOT NULL,
        [AttemptNumber] [int] NOT NULL,
        [AttemptedAtUtc] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [Status] [nvarchar](50) NOT NULL DEFAULT 'Success',
        [ErrorMessage] [nvarchar](max) NULL,
        [DurationMs] [int] NOT NULL DEFAULT 0,
        CONSTRAINT [PK_EmailDeliveryLog] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_EmailDeliveryLog_EmailOutboxJob] FOREIGN KEY ([EmailOutboxJobId]) 
            REFERENCES [dbo].[EmailOutboxJob] ([Id]) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_EmailDeliveryLog_EmailOutboxJobId] 
    ON [dbo].[EmailDeliveryLog]([EmailOutboxJobId]);
END;
";

                await context.Database.ExecuteSqlRawAsync(sql);
                logger?.LogInformation("EmailDatabaseMigrator: Verified EmailOutboxJob and EmailDeliveryLog tables exist.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "EmailDatabaseMigrator: Error ensuring email tables exist.");
                // Log and continue to preserve existing LMS functionality without crashing
            }
        }
    }
}
