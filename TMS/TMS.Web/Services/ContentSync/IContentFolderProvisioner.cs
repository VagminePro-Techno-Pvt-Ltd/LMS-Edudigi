using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMS.Models.ContentSync;

namespace TMS.Web.Services.ContentSync
{
    public class ProvisioningStatusSummary
    {
        public bool LocalDriveEnabled { get; set; }
        public string LocalDriveStatus { get; set; } = "Online";
        public string LocalDriveRootPath { get; set; } = "";

        public bool GoogleDriveEnabled { get; set; }
        public string GoogleDriveStatus { get; set; } = "Offline";
        public bool GoogleDriveNetworkDisabled { get; set; }
        public string GoogleDriveRootName { get; set; } = "";

        public int TotalProgramsMapped { get; set; }
        public int TotalSemestersMapped { get; set; }
        public int TotalCoursesMapped { get; set; }
        public int TotalSubfoldersMapped { get; set; }

        public int PendingJobsCount { get; set; }
        public int CompletedJobsCount { get; set; }
        public int FailedJobsCount { get; set; }

        public List<ContentSyncOutboxJob> RecentJobs { get; set; } = new();
        public List<ContentFolderMapping> AllMappings { get; set; } = new();
        public List<ContentSyncFileRecord> RecentSyncedFiles { get; set; } = new();

        public DateTime? LastDriveScanUtc { get; set; }
    }

    public class HierarchyNodeDto
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Type { get; set; } = default!; // "Root", "Program", "Semester", "Course", "Subfolder"
        public string? ProviderFolderId { get; set; }
        public string StorageProvider { get; set; } = default!;
        public string Status { get; set; } = "Active";
        public List<HierarchyNodeDto> Children { get; set; } = new();
    }

    public interface IContentFolderProvisioner
    {
        Task EnqueueProvisioningAsync(string entityType, int entityId, string action, object? payload = null);
        Task ProcessPendingJobsAsync(CancellationToken cancellationToken = default);
        Task RetryFailedJobsAsync(CancellationToken cancellationToken = default);
        Task<ProvisioningStatusSummary> GetStatusSummaryAsync(CancellationToken cancellationToken = default);
        Task<HierarchyNodeDto> GetFolderHierarchyAsync(string providerKey, CancellationToken cancellationToken = default);
        Task TriggerFullHierarchyRecheckAsync(CancellationToken cancellationToken = default);
        Task SyncDriveFilesToLmsAsync(CancellationToken cancellationToken = default);
        Task UpdateProviderConfigAsync(bool localEnabled, string localPath, bool gdriveEnabled, bool gdriveNetworkDisabled, CancellationToken cancellationToken = default);
    }
}
