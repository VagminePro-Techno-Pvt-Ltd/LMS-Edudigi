using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TMS.Web.Services.ContentSync
{
    public class GoogleDriveFolderNode
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? ParentId { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }

    public class GoogleDriveContentStorageProvider : IContentStorageProvider
    {
        private readonly ILogger<GoogleDriveContentStorageProvider> _logger;
        private readonly HttpClient _httpClient;
        private bool _isEnabled = false;
        private bool _networkDisabled = false;
        private string _rootFolderName = "EduDigiClass_LMS_Content";
        private string? _explicitRootFolderId;
        private string? _serviceAccountEmail;
        private string? _apiKey;

        // In-memory / persistent provider drive store for simulated / offline mode and idempotency
        private static readonly ConcurrentDictionary<string, GoogleDriveFolderNode> _folderStore = new(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, List<StorageFileItem>> _fileStore = new(StringComparer.OrdinalIgnoreCase);

        public string ProviderKey => "GoogleDrive";
        public string DisplayName => "Google Drive (Folders & Shared Drives)";
        public bool IsEnabled => _isEnabled;
        public bool NetworkDisabled => _networkDisabled;

        public GoogleDriveContentStorageProvider(
            ILogger<GoogleDriveContentStorageProvider> logger,
            HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        public void UpdateConfig(bool enabled, bool networkDisabled, string? rootFolderName, string? rootFolderId, string? serviceAccountEmail)
        {
            _isEnabled = enabled;
            _networkDisabled = networkDisabled;
            if (!string.IsNullOrWhiteSpace(rootFolderName)) _rootFolderName = rootFolderName;
            _explicitRootFolderId = rootFolderId;
            _serviceAccountEmail = serviceAccountEmail;
        }

        public void SetNetworkAccess(bool enabled)
        {
            _networkDisabled = !enabled;
            _logger.LogInformation("[GoogleDrive] Network access set to: {Status}", enabled ? "ONLINE" : "DISABLED");
        }

        private void CheckNetworkAvailability()
        {
            if (_networkDisabled)
            {
                throw new HttpRequestException("Google Drive service unreachable. Network connection failed (simulated offline).");
            }
        }

        public async Task<ProviderFolderResult> EnsureRootFolderAsync(string rootName, CancellationToken cancellationToken = default)
        {
            CheckNetworkAvailability();

            var effectiveName = string.IsNullOrWhiteSpace(rootName) ? _rootFolderName : rootName;
            var cleanName = LocalContentStorageProvider.SanitizeFolderName(effectiveName);

            // Check if explicit root folder ID is provided
            if (!string.IsNullOrWhiteSpace(_explicitRootFolderId) && _folderStore.TryGetValue(_explicitRootFolderId, out var existingRoot))
            {
                return ProviderFolderResult.Ok(existingRoot.Id, existingRoot.Name, null, alreadyExisted: true);
            }

            // Find or create root folder
            var root = _folderStore.Values.FirstOrDefault(f => f.ParentId == null && string.Equals(f.Name, cleanName, StringComparison.OrdinalIgnoreCase));
            if (root != null)
            {
                return ProviderFolderResult.Ok(root.Id, root.Name, null, alreadyExisted: true);
            }

            var rootId = !string.IsNullOrWhiteSpace(_explicitRootFolderId)
                ? _explicitRootFolderId
                : $"gdrive_root_{GenerateDriveId(cleanName)}";

            var newNode = new GoogleDriveFolderNode
            {
                Id = rootId,
                Name = cleanName,
                ParentId = null
            };
            _folderStore[rootId] = newNode;

            _logger.LogInformation("[GoogleDrive] Provisioned root folder: {Name} ({Id})", cleanName, rootId);
            await Task.CompletedTask;
            return ProviderFolderResult.Ok(rootId, cleanName, null, alreadyExisted: false);
        }

        public async Task<ProviderFolderResult> EnsureFolderAsync(string folderName, string parentFolderId, CancellationToken cancellationToken = default)
        {
            CheckNetworkAvailability();

            if (string.IsNullOrWhiteSpace(parentFolderId))
            {
                var rootRes = await EnsureRootFolderAsync(_rootFolderName, cancellationToken);
                if (!rootRes.Success) return rootRes;
                parentFolderId = rootRes.FolderId!;
            }

            var cleanName = LocalContentStorageProvider.SanitizeFolderName(folderName);

            // Idempotency: verify if folder with this name already exists under this parent
            var existing = _folderStore.Values.FirstOrDefault(f =>
                f.ParentId == parentFolderId &&
                string.Equals(f.Name, cleanName, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                return ProviderFolderResult.Ok(existing.Id, existing.Name, parentFolderId, alreadyExisted: true);
            }

            // Generate stable, unique provider folder ID
            var folderId = $"gdrive_fld_{GenerateDriveId($"{parentFolderId}/{cleanName}")}";
            var node = new GoogleDriveFolderNode
            {
                Id = folderId,
                Name = cleanName,
                ParentId = parentFolderId
            };
            _folderStore[folderId] = node;

            _logger.LogInformation("[GoogleDrive] Created folder '{Name}' ({Id}) under parent '{ParentId}'", cleanName, folderId, parentFolderId);
            return ProviderFolderResult.Ok(folderId, cleanName, parentFolderId, alreadyExisted: false);
        }

        public Task<ProviderFolderResult> RenameFolderAsync(string folderId, string newFolderName, CancellationToken cancellationToken = default)
        {
            CheckNetworkAvailability();

            if (!_folderStore.TryGetValue(folderId, out var node))
            {
                return Task.FromResult(ProviderFolderResult.Fail($"Google Drive folder '{folderId}' not found"));
            }

            var cleanName = LocalContentStorageProvider.SanitizeFolderName(newFolderName);
            if (string.Equals(node.Name, cleanName, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(ProviderFolderResult.Ok(folderId, cleanName, node.ParentId, alreadyExisted: true));
            }

            node.Name = cleanName;
            _logger.LogInformation("[GoogleDrive] Renamed folder {Id} to '{NewName}'", folderId, cleanName);
            return Task.FromResult(ProviderFolderResult.Ok(folderId, cleanName, node.ParentId, alreadyExisted: false));
        }

        public Task<ProviderFolderResult> MoveFolderAsync(string folderId, string newParentFolderId, CancellationToken cancellationToken = default)
        {
            CheckNetworkAvailability();

            if (!_folderStore.TryGetValue(folderId, out var node))
            {
                return Task.FromResult(ProviderFolderResult.Fail($"Google Drive folder '{folderId}' not found"));
            }

            node.ParentId = newParentFolderId;
            _logger.LogInformation("[GoogleDrive] Moved folder {Id} to parent '{NewParent}'", folderId, newParentFolderId);
            return Task.FromResult(ProviderFolderResult.Ok(folderId, node.Name, newParentFolderId, alreadyExisted: false));
        }

        public Task<List<StorageFileItem>> ListFilesAsync(string folderId, CancellationToken cancellationToken = default)
        {
            CheckNetworkAvailability();

            if (_fileStore.TryGetValue(folderId, out var files))
            {
                return Task.FromResult(files.ToList());
            }

            return Task.FromResult(new List<StorageFileItem>());
        }

        public Task<Stream> DownloadFileAsync(string fileId, CancellationToken cancellationToken = default)
        {
            CheckNetworkAvailability();

            // Locate file in fileStore
            foreach (var kvp in _fileStore)
            {
                var file = kvp.Value.FirstOrDefault(f => f.FileId == fileId);
                if (file != null)
                {
                    var dummyContent = Encoding.UTF8.GetBytes($"[GoogleDrive Mock File Content for {file.FileName}]");
                    return Task.FromResult<Stream>(new MemoryStream(dummyContent));
                }
            }

            throw new FileNotFoundException($"File '{fileId}' not found on Google Drive.");
        }

        public Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                CheckNetworkAvailability();
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[GoogleDrive] Connection test failed: {Message}", ex.Message);
                return Task.FromResult(false);
            }
        }

        public void AddMockFile(string folderId, string fileName, byte[] content)
        {
            var list = _fileStore.GetOrAdd(folderId, _ => new List<StorageFileItem>());
            var fileId = $"gdrive_file_{GenerateDriveId(fileName)}";
            list.RemoveAll(f => f.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));
            list.Add(new StorageFileItem
            {
                FileId = fileId,
                FileName = fileName,
                FileSize = content.Length,
                LastModifiedUtc = DateTime.UtcNow,
                Hash = BitConverter.ToString(SHA256.HashData(content)).Replace("-", "")
            });
        }

        private static string GenerateDriveId(string input)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder();
            for (int i = 0; i < 12; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
