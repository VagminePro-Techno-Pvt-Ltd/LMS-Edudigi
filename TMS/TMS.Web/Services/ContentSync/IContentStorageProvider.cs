using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TMS.Web.Services.ContentSync
{
    public class ProviderFolderResult
    {
        public bool Success { get; set; }
        public string? FolderId { get; set; }
        public string? FolderName { get; set; }
        public string? ParentFolderId { get; set; }
        public string? ErrorMessage { get; set; }
        public bool AlreadyExisted { get; set; }

        public static ProviderFolderResult Ok(string folderId, string folderName, string? parentFolderId = null, bool alreadyExisted = false) =>
            new() { Success = true, FolderId = folderId, FolderName = folderName, ParentFolderId = parentFolderId, AlreadyExisted = alreadyExisted };

        public static ProviderFolderResult Fail(string error) =>
            new() { Success = false, ErrorMessage = error };
    }

    public class StorageFileItem
    {
        public string FileId { get; set; } = default!;
        public string FileName { get; set; } = default!;
        public long FileSize { get; set; }
        public DateTime? LastModifiedUtc { get; set; }
        public string? Hash { get; set; }
    }

    public interface IContentStorageProvider
    {
        string ProviderKey { get; } // "LocalDrive", "GoogleDrive"
        string DisplayName { get; }
        bool IsEnabled { get; }

        Task<ProviderFolderResult> EnsureRootFolderAsync(string rootName, CancellationToken cancellationToken = default);
        Task<ProviderFolderResult> EnsureFolderAsync(string folderName, string parentFolderId, CancellationToken cancellationToken = default);
        Task<ProviderFolderResult> RenameFolderAsync(string folderId, string newFolderName, CancellationToken cancellationToken = default);
        Task<ProviderFolderResult> MoveFolderAsync(string folderId, string newParentFolderId, CancellationToken cancellationToken = default);
        Task<List<StorageFileItem>> ListFilesAsync(string folderId, CancellationToken cancellationToken = default);
        Task<Stream> DownloadFileAsync(string fileId, CancellationToken cancellationToken = default);
        Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
        string GetFolderUrl(string folderId);
    }
}
