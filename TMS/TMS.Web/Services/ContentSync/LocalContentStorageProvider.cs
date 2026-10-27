using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TMS.Web.Services.ContentSync
{
    public class LocalContentStorageProvider : IContentStorageProvider
    {
        private readonly ILogger<LocalContentStorageProvider> _logger;
        private string _rootPath;
        private bool _isEnabled = true;

        public string ProviderKey => "LocalDrive";
        public string DisplayName => "Local Server / UNC Shared Network Drive";
        public bool IsEnabled => _isEnabled;

        public LocalContentStorageProvider(ILogger<LocalContentStorageProvider> logger, string? configuredRootPath = null)
        {
            _logger = logger;
            _rootPath = ResolveRootPath(configuredRootPath);
        }

        public void UpdateConfig(bool enabled, string? rootPath)
        {
            _isEnabled = enabled;
            if (!string.IsNullOrWhiteSpace(rootPath))
            {
                _rootPath = ResolveRootPath(rootPath);
            }
        }

        private static string ResolveRootPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                path = "EduDigiClass_LMS_Content";
            }

            if (Path.IsPathRooted(path) || path.StartsWith(@"\\") || path.StartsWith("//"))
            {
                return Path.GetFullPath(path);
            }

            // Put relative paths in the workspace root or storage folder
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var workspaceDir = Directory.GetParent(baseDir)?.Parent?.Parent?.Parent?.FullName ?? Directory.GetCurrentDirectory();
            return Path.GetFullPath(Path.Combine(workspaceDir, path));
        }

        public Task<ProviderFolderResult> EnsureRootFolderAsync(string rootName, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Directory.Exists(_rootPath))
                {
                    Directory.CreateDirectory(_rootPath);
                    _logger.LogInformation("[LocalDrive] Created root folder: {Path}", _rootPath);
                    return Task.FromResult(ProviderFolderResult.Ok(_rootPath, Path.GetFileName(_rootPath), null, alreadyExisted: false));
                }

                return Task.FromResult(ProviderFolderResult.Ok(_rootPath, Path.GetFileName(_rootPath), null, alreadyExisted: true));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LocalDrive] Failed to ensure root folder at {Path}: {Message}", _rootPath, ex.Message);
                return Task.FromResult(ProviderFolderResult.Fail($"Failed to create root directory '{_rootPath}': {ex.Message}"));
            }
        }

        public Task<ProviderFolderResult> EnsureFolderAsync(string folderName, string parentFolderId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(parentFolderId))
                {
                    parentFolderId = _rootPath;
                }

                if (!Directory.Exists(parentFolderId))
                {
                    Directory.CreateDirectory(parentFolderId);
                }

                var cleanFolderName = SanitizeFolderName(folderName);
                var targetPath = Path.Combine(parentFolderId, cleanFolderName);

                if (Directory.Exists(targetPath))
                {
                    return Task.FromResult(ProviderFolderResult.Ok(targetPath, cleanFolderName, parentFolderId, alreadyExisted: true));
                }

                Directory.CreateDirectory(targetPath);
                _logger.LogInformation("[LocalDrive] Created folder '{Folder}' in '{Parent}'", cleanFolderName, parentFolderId);
                return Task.FromResult(ProviderFolderResult.Ok(targetPath, cleanFolderName, parentFolderId, alreadyExisted: false));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LocalDrive] Error creating folder '{Folder}' in '{Parent}': {Message}", folderName, parentFolderId, ex.Message);
                return Task.FromResult(ProviderFolderResult.Fail(ex.Message));
            }
        }

        public Task<ProviderFolderResult> RenameFolderAsync(string folderId, string newFolderName, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Directory.Exists(folderId))
                {
                    return Task.FromResult(ProviderFolderResult.Fail($"Directory '{folderId}' does not exist"));
                }

                var parentDir = Path.GetDirectoryName(folderId);
                if (string.IsNullOrWhiteSpace(parentDir))
                {
                    return Task.FromResult(ProviderFolderResult.Fail("Cannot rename root folder without parent directory"));
                }

                var cleanNewName = SanitizeFolderName(newFolderName);
                var targetPath = Path.Combine(parentDir, cleanNewName);

                if (string.Equals(Path.GetFullPath(folderId), Path.GetFullPath(targetPath), StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(ProviderFolderResult.Ok(folderId, cleanNewName, parentDir, alreadyExisted: true));
                }

                if (Directory.Exists(targetPath))
                {
                    // Target already exists, folder is considered already at target
                    return Task.FromResult(ProviderFolderResult.Ok(targetPath, cleanNewName, parentDir, alreadyExisted: true));
                }

                Directory.Move(folderId, targetPath);
                _logger.LogInformation("[LocalDrive] Renamed folder from '{Old}' to '{New}'", folderId, targetPath);
                return Task.FromResult(ProviderFolderResult.Ok(targetPath, cleanNewName, parentDir, alreadyExisted: false));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LocalDrive] Error renaming folder '{Folder}' to '{New}': {Message}", folderId, newFolderName, ex.Message);
                return Task.FromResult(ProviderFolderResult.Fail(ex.Message));
            }
        }

        public Task<ProviderFolderResult> MoveFolderAsync(string folderId, string newParentFolderId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Directory.Exists(folderId))
                {
                    return Task.FromResult(ProviderFolderResult.Fail($"Directory '{folderId}' does not exist"));
                }

                if (!Directory.Exists(newParentFolderId))
                {
                    Directory.CreateDirectory(newParentFolderId);
                }

                var currentDirName = Path.GetFileName(folderId);
                var targetPath = Path.Combine(newParentFolderId, currentDirName);

                if (string.Equals(Path.GetFullPath(folderId), Path.GetFullPath(targetPath), StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(ProviderFolderResult.Ok(folderId, currentDirName, newParentFolderId, alreadyExisted: true));
                }

                if (Directory.Exists(targetPath))
                {
                    return Task.FromResult(ProviderFolderResult.Ok(targetPath, currentDirName, newParentFolderId, alreadyExisted: true));
                }

                Directory.Move(folderId, targetPath);
                _logger.LogInformation("[LocalDrive] Moved folder from '{Old}' to '{New}'", folderId, targetPath);
                return Task.FromResult(ProviderFolderResult.Ok(targetPath, currentDirName, newParentFolderId, alreadyExisted: false));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LocalDrive] Error moving folder '{Folder}' to '{NewParent}': {Message}", folderId, newParentFolderId, ex.Message);
                return Task.FromResult(ProviderFolderResult.Fail(ex.Message));
            }
        }

        public Task<List<StorageFileItem>> ListFilesAsync(string folderId, CancellationToken cancellationToken = default)
        {
            var results = new List<StorageFileItem>();
            try
            {
                if (!Directory.Exists(folderId))
                {
                    return Task.FromResult(results);
                }

                var dirInfo = new DirectoryInfo(folderId);
                foreach (var file in dirInfo.GetFiles())
                {
                    // Ignore hidden or system files
                    if (file.Name.StartsWith(".") || file.Attributes.HasFlag(FileAttributes.Hidden))
                    {
                        continue;
                    }

                    results.Add(new StorageFileItem
                    {
                        FileId = file.FullName,
                        FileName = file.Name,
                        FileSize = file.Length,
                        LastModifiedUtc = file.LastWriteTimeUtc,
                        Hash = null
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[LocalDrive] Error listing files in '{Folder}': {Message}", folderId, ex.Message);
            }

            return Task.FromResult(results);
        }

        public Task<Stream> DownloadFileAsync(string fileId, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(fileId))
            {
                throw new FileNotFoundException($"File '{fileId}' not found on local storage.");
            }

            var memoryStream = new MemoryStream();
            using (var fileStream = File.OpenRead(fileId))
            {
                fileStream.CopyTo(memoryStream);
            }
            memoryStream.Position = 0;
            return Task.FromResult<Stream>(memoryStream);
        }

        public Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Directory.Exists(_rootPath))
                {
                    Directory.CreateDirectory(_rootPath);
                }

                var testFile = Path.Combine(_rootPath, $".connection_test_{Guid.NewGuid():N}.tmp");
                File.WriteAllText(testFile, "OK");
                File.Delete(testFile);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LocalDrive] Connection test failed at '{Path}': {Message}", _rootPath, ex.Message);
                return Task.FromResult(false);
            }
        }

        public static string SanitizeFolderName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Unnamed";
            var invalidChars = Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).Distinct().ToArray();
            var sanitized = string.Join("_", name.Trim().Split(invalidChars, StringSplitOptions.RemoveEmptyEntries)).Trim();
            // Replace multiple whitespace with underscore
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"\s+", "_");
            return string.IsNullOrWhiteSpace(sanitized) ? "Folder" : sanitized;
        }
    }
}
