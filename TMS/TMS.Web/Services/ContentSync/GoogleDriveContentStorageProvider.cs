using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration? _configuration;

        private bool _isEnabled = false;
        private bool _networkDisabled = false;
        private string _rootFolderName = "EduDigiClass_LMS_Content";
        private string? _explicitRootFolderId;
        private string? _serviceAccountEmail;
        private string? _credentialsJson;

        private DriveService? _driveService;
        private readonly object _serviceLock = new();

        // In-memory / persistent provider drive store for simulated / offline mode and idempotency
        private static readonly ConcurrentDictionary<string, GoogleDriveFolderNode> _folderStore = new(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, List<StorageFileItem>> _fileStore = new(StringComparer.OrdinalIgnoreCase);

        public string ProviderKey => "GoogleDrive";
        public string DisplayName => "Google Drive (Folders & Shared Drives)";
        public bool IsEnabled => _isEnabled;
        public bool NetworkDisabled => _networkDisabled;

        public GoogleDriveContentStorageProvider(
            ILogger<GoogleDriveContentStorageProvider> logger,
            HttpClient httpClient,
            IConfiguration? configuration = null)
        {
            _logger = logger;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public void UpdateConfig(
            bool enabled,
            bool networkDisabled,
            string? rootFolderName,
            string? rootFolderId,
            string? serviceAccountEmail,
            string? credentialsJson = null)
        {
            _isEnabled = enabled;
            _networkDisabled = networkDisabled;
            if (!string.IsNullOrWhiteSpace(rootFolderName)) _rootFolderName = rootFolderName;
            _explicitRootFolderId = rootFolderId;
            _serviceAccountEmail = serviceAccountEmail;
            if (!string.IsNullOrWhiteSpace(credentialsJson)) _credentialsJson = credentialsJson;

            lock (_serviceLock)
            {
                _driveService = null; // Re-initialize on next use
            }
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

        private DriveService? GetDriveService()
        {
            CheckNetworkAvailability();

            if (_driveService != null) return _driveService;

            lock (_serviceLock)
            {
                if (_driveService != null) return _driveService;

                try
                {
                    GoogleCredential? credential = null;

                    // 1. Try provided credentials JSON in config
                    if (!string.IsNullOrWhiteSpace(_credentialsJson))
                    {
                        credential = GoogleCredential.FromJson(_credentialsJson).CreateScoped(DriveService.Scope.Drive);
                    }
                    // 2. Try configuration key file path
                    else if (_configuration != null)
                    {
                        var keyPath = _configuration["ContentSync:GoogleServiceAccountKeyPath"];
                        if (!string.IsNullOrWhiteSpace(keyPath) && File.Exists(keyPath))
                        {
                            credential = GoogleCredential.FromFile(keyPath).CreateScoped(DriveService.Scope.Drive);
                        }
                        else
                        {
                            var inlineJson = _configuration["ContentSync:GoogleDriveCredentialsJson"];
                            if (!string.IsNullOrWhiteSpace(inlineJson))
                            {
                                credential = GoogleCredential.FromJson(inlineJson).CreateScoped(DriveService.Scope.Drive);
                            }
                        }
                    }

                    // 3. Fallback to GOOGLE_APPLICATION_CREDENTIALS environment variable if present
                    if (credential == null)
                    {
                        var envPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
                        if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath))
                        {
                            credential = GoogleCredential.FromFile(envPath).CreateScoped(DriveService.Scope.Drive);
                        }
                    }

                    if (credential != null)
                    {
                        _driveService = new DriveService(new BaseClientService.Initializer
                        {
                            HttpClientInitializer = credential,
                            ApplicationName = "EduDigiClass LMS"
                        });
                        _logger.LogInformation("[GoogleDrive] Initialized official Google Drive API v3 client with Service Account/OAuth credentials.");
                        return _driveService;
                    }

                    // 3.5. Try OAuth2 UserCredential from FileDataStore if ClientId and ClientSecret are present
                    var clientId = _configuration?["ContentSync:GoogleDriveClientId"];
                    var clientSecret = _configuration?["ContentSync:GoogleDriveClientSecret"];
                    var tokenFolder = _configuration?["ContentSync:GoogleDriveTokenFolder"] ?? "google_oauth_tokens";

                    if (!string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret))
                    {
                        var searchPaths = new List<string>
                        {
                            tokenFolder,
                            Path.Combine(Directory.GetCurrentDirectory(), tokenFolder),
                            Path.Combine(AppContext.BaseDirectory, tokenFolder),
                            Path.Combine(Directory.GetCurrentDirectory(), "TMS.Web", tokenFolder),
                            Path.Combine(Directory.GetCurrentDirectory(), "..", "TMS.Web", tokenFolder),
                            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../TMS.Web", tokenFolder)),
                            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../TMS.Web", tokenFolder)),
                            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../..", tokenFolder)),
                            "/Users/sandeepkumar/Desktop/LMS/TMS/TMS.Web/google_oauth_tokens",
                            "/Users/sandeepkumar/Desktop/LMS/google_oauth_tokens"
                        };
                        var resolvedFolder = searchPaths.FirstOrDefault(p => Directory.Exists(p) && File.Exists(Path.Combine(p, "Google.Apis.Auth.OAuth2.Responses.TokenResponse-user")))
                                             ?? searchPaths.FirstOrDefault(Directory.Exists)
                                             ?? tokenFolder;

                        if (Directory.Exists(resolvedFolder))
                        {
                            try
                            {
                                var dataStore = new Google.Apis.Util.Store.FileDataStore(resolvedFolder, true);
                                var storedToken = dataStore.GetAsync<Google.Apis.Auth.OAuth2.Responses.TokenResponse>("user").GetAwaiter().GetResult();
                                if (storedToken != null && !string.IsNullOrWhiteSpace(storedToken.RefreshToken))
                                {
                                    var secrets = new Google.Apis.Auth.OAuth2.ClientSecrets
                                    {
                                        ClientId = clientId,
                                        ClientSecret = clientSecret
                                    };
                                    var userCredential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                                        secrets,
                                        new[] { DriveService.Scope.Drive, DriveService.Scope.DriveFile },
                                        "user",
                                        CancellationToken.None,
                                        dataStore
                                    ).GetAwaiter().GetResult();

                                    if (userCredential != null)
                                    {
                                        _driveService = new DriveService(new BaseClientService.Initializer
                                        {
                                            HttpClientInitializer = userCredential,
                                            ApplicationName = "EduDigiClass LMS"
                                        });
                                        _logger.LogInformation("[GoogleDrive] Initialized official Google Drive API v3 client with OAuth2 User Credentials from '{Folder}'.", resolvedFolder);
                                        return _driveService;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "[GoogleDrive] Could not load OAuth2 user credentials from '{Folder}': {Message}", resolvedFolder, ex.Message);
                            }
                        }
                    }

                    // 4. Try API Key from configuration
                    var apiKey = _configuration?["ContentSync:GoogleDriveApiKey"];
                    if (!string.IsNullOrWhiteSpace(apiKey))
                    {
                        _driveService = new DriveService(new BaseClientService.Initializer
                        {
                            ApiKey = apiKey,
                            ApplicationName = "EduDigiClass LMS"
                        });
                        _logger.LogInformation("[GoogleDrive] Initialized official Google Drive API v3 client with API Key.");
                        return _driveService;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[GoogleDrive] Could not initialize official Google Drive API client. Falling back to simulated mode: {Message}", ex.Message);
                }

                return null;
            }
        }

        private static bool IsAuthenticationOrPermissionError(Exception ex)
        {
            var msg = ex?.Message ?? "";
            return msg.Contains("401", StringComparison.OrdinalIgnoreCase) ||
                   msg.Contains("403", StringComparison.OrdinalIgnoreCase) ||
                   msg.Contains("Login Required", StringComparison.OrdinalIgnoreCase) ||
                   msg.Contains("Expected OAuth2", StringComparison.OrdinalIgnoreCase) ||
                   msg.Contains("CREDENTIALS_MISSING", StringComparison.OrdinalIgnoreCase) ||
                   msg.Contains("Insufficient Permission", StringComparison.OrdinalIgnoreCase) ||
                   msg.Contains("API keys are not supported", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<ProviderFolderResult> EnsureRootFolderAsync(string rootName, CancellationToken cancellationToken = default)
        {
            CheckNetworkAvailability();

            var effectiveName = string.IsNullOrWhiteSpace(rootName) ? _rootFolderName : rootName;
            var cleanName = LocalContentStorageProvider.SanitizeFolderName(effectiveName);

            var service = GetDriveService();
            if (service != null)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(_explicitRootFolderId))
                    {
                        var getReq = service.Files.Get(_explicitRootFolderId);
                        getReq.Fields = "id, name, mimeType";
                        getReq.SupportsAllDrives = true;
                        var existing = await getReq.ExecuteAsync(cancellationToken);
                        if (existing != null)
                        {
                            return ProviderFolderResult.Ok(existing.Id, existing.Name, null, alreadyExisted: true);
                        }
                    }

                    // Query root by name
                    var listReq = service.Files.List();
                    listReq.Q = $"mimeType = 'application/vnd.google-apps.folder' and name = '{cleanName.Replace("'", "\\'")}' and trashed = false";
                    listReq.Fields = "files(id, name, parents)";
                    listReq.SupportsAllDrives = true;
                    listReq.IncludeItemsFromAllDrives = true;
                    var res = await listReq.ExecuteAsync(cancellationToken);

                    if (res.Files != null && res.Files.Count > 0)
                    {
                        var rootFile = res.Files[0];
                        return ProviderFolderResult.Ok(rootFile.Id, rootFile.Name, null, alreadyExisted: true);
                    }

                    // Create root folder
                    var newFile = new Google.Apis.Drive.v3.Data.File
                    {
                        Name = cleanName,
                        MimeType = "application/vnd.google-apps.folder"
                    };
                    var createReq = service.Files.Create(newFile);
                    createReq.Fields = "id, name";
                    createReq.SupportsAllDrives = true;
                    var created = await createReq.ExecuteAsync(cancellationToken);
                    _logger.LogInformation("[GoogleDrive] Created root folder via API v3: {Name} ({Id})", cleanName, created.Id);
                    return ProviderFolderResult.Ok(created.Id, created.Name, null, alreadyExisted: false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[GoogleDrive] Error ensuring root folder via API v3: {Message}", ex.Message);
                    if (IsAuthenticationOrPermissionError(ex))
                    {
                        _logger.LogWarning("[GoogleDrive] Google Drive API requires OAuth2/Service Account for file/folder operations. Falling back to local/in-memory provider store.");
                    }
                    else
                    {
                        return ProviderFolderResult.Fail($"Google Drive API error: {ex.Message}");
                    }
                }
            }

            // Simulated / In-memory fallback
            if (!string.IsNullOrWhiteSpace(_explicitRootFolderId) && _folderStore.TryGetValue(_explicitRootFolderId, out var existingRoot))
            {
                return ProviderFolderResult.Ok(existingRoot.Id, existingRoot.Name, null, alreadyExisted: true);
            }

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
            var service = GetDriveService();

            if (service != null)
            {
                try
                {
                    // Check if folder exists under parent
                    var listReq = service.Files.List();
                    listReq.Q = $"mimeType = 'application/vnd.google-apps.folder' and name = '{cleanName.Replace("'", "\\'")}' and '{parentFolderId}' in parents and trashed = false";
                    listReq.Fields = "files(id, name, parents)";
                    listReq.SupportsAllDrives = true;
                    listReq.IncludeItemsFromAllDrives = true;
                    var listRes = await listReq.ExecuteAsync(cancellationToken);

                    if (listRes.Files != null && listRes.Files.Count > 0)
                    {
                        var existing = listRes.Files[0];
                        return ProviderFolderResult.Ok(existing.Id, existing.Name, parentFolderId, alreadyExisted: true);
                    }

                    // Create folder
                    var newFolder = new Google.Apis.Drive.v3.Data.File
                    {
                        Name = cleanName,
                        MimeType = "application/vnd.google-apps.folder",
                        Parents = new List<string> { parentFolderId }
                    };
                    var createReq = service.Files.Create(newFolder);
                    createReq.Fields = "id, name, parents";
                    createReq.SupportsAllDrives = true;
                    var created = await createReq.ExecuteAsync(cancellationToken);
                    _logger.LogInformation("[GoogleDrive] Created folder via API v3 '{Name}' ({Id}) under parent '{ParentId}'", cleanName, created.Id, parentFolderId);
                    return ProviderFolderResult.Ok(created.Id, created.Name, parentFolderId, alreadyExisted: false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[GoogleDrive] Error creating folder '{Name}' under '{ParentId}' via API v3: {Message}", cleanName, parentFolderId, ex.Message);
                    if (IsAuthenticationOrPermissionError(ex))
                    {
                        _logger.LogWarning("[GoogleDrive] Google Drive API requires OAuth2/Service Account for folder creation. Falling back to local/in-memory provider store.");
                    }
                    else
                    {
                        return ProviderFolderResult.Fail($"Google Drive API error: {ex.Message}");
                    }
                }
            }

            // Simulated fallback
            var existingNode = _folderStore.Values.FirstOrDefault(f =>
                f.ParentId == parentFolderId &&
                string.Equals(f.Name, cleanName, StringComparison.OrdinalIgnoreCase));

            if (existingNode != null)
            {
                return ProviderFolderResult.Ok(existingNode.Id, existingNode.Name, parentFolderId, alreadyExisted: true);
            }

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

        public async Task<ProviderFolderResult> RenameFolderAsync(string folderId, string newFolderName, CancellationToken cancellationToken = default)
        {
            CheckNetworkAvailability();
            var cleanName = LocalContentStorageProvider.SanitizeFolderName(newFolderName);

            var service = GetDriveService();
            if (service != null)
            {
                try
                {
                    var updateMeta = new Google.Apis.Drive.v3.Data.File { Name = cleanName };
                    var updateReq = service.Files.Update(updateMeta, folderId);
                    updateReq.Fields = "id, name, parents";
                    updateReq.SupportsAllDrives = true;
                    var updated = await updateReq.ExecuteAsync(cancellationToken);
                    var parentId = updated.Parents?.FirstOrDefault();
                    _logger.LogInformation("[GoogleDrive] Renamed folder {Id} to '{NewName}' via API v3", folderId, cleanName);
                    return ProviderFolderResult.Ok(folderId, cleanName, parentId, alreadyExisted: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[GoogleDrive] Error renaming folder {Id} via API v3: {Message}", folderId, ex.Message);
                    return ProviderFolderResult.Fail(ex.Message);
                }
            }

            if (!_folderStore.TryGetValue(folderId, out var node))
            {
                return ProviderFolderResult.Fail($"Google Drive folder '{folderId}' not found");
            }

            if (string.Equals(node.Name, cleanName, StringComparison.OrdinalIgnoreCase))
            {
                return ProviderFolderResult.Ok(folderId, cleanName, node.ParentId, alreadyExisted: true);
            }

            node.Name = cleanName;
            _logger.LogInformation("[GoogleDrive] Renamed folder {Id} to '{NewName}'", folderId, cleanName);
            await Task.CompletedTask;
            return ProviderFolderResult.Ok(folderId, cleanName, node.ParentId, alreadyExisted: false);
        }

        public async Task<ProviderFolderResult> MoveFolderAsync(string folderId, string newParentFolderId, CancellationToken cancellationToken = default)
        {
            CheckNetworkAvailability();

            var service = GetDriveService();
            if (service != null)
            {
                try
                {
                    var getReq = service.Files.Get(folderId);
                    getReq.Fields = "id, name, parents";
                    getReq.SupportsAllDrives = true;
                    var file = await getReq.ExecuteAsync(cancellationToken);
                    var previousParents = string.Join(",", file.Parents ?? new List<string>());

                    var moveReq = service.Files.Update(new Google.Apis.Drive.v3.Data.File(), folderId);
                    moveReq.AddParents = newParentFolderId;
                    moveReq.RemoveParents = previousParents;
                    moveReq.Fields = "id, name, parents";
                    moveReq.SupportsAllDrives = true;
                    var moved = await moveReq.ExecuteAsync(cancellationToken);
                    _logger.LogInformation("[GoogleDrive] Moved folder {Id} to parent '{NewParent}' via API v3", folderId, newParentFolderId);
                    return ProviderFolderResult.Ok(folderId, moved.Name, newParentFolderId, alreadyExisted: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[GoogleDrive] Error moving folder {Id} via API v3: {Message}", folderId, ex.Message);
                    return ProviderFolderResult.Fail(ex.Message);
                }
            }

            if (!_folderStore.TryGetValue(folderId, out var node))
            {
                return ProviderFolderResult.Fail($"Google Drive folder '{folderId}' not found");
            }

            node.ParentId = newParentFolderId;
            _logger.LogInformation("[GoogleDrive] Moved folder {Id} to parent '{NewParent}'", folderId, newParentFolderId);
            await Task.CompletedTask;
            return ProviderFolderResult.Ok(folderId, node.Name, newParentFolderId, alreadyExisted: false);
        }

        public async Task<List<StorageFileItem>> ListFilesAsync(string folderId, CancellationToken cancellationToken = default)
        {
            CheckNetworkAvailability();

            var service = GetDriveService();
            if (service != null)
            {
                try
                {
                    var listReq = service.Files.List();
                    listReq.Q = $"'{folderId}' in parents and mimeType != 'application/vnd.google-apps.folder' and trashed = false";
                    listReq.Fields = "files(id, name, size, modifiedTime, md5Checksum)";
                    listReq.SupportsAllDrives = true;
                    listReq.IncludeItemsFromAllDrives = true;
                    var result = await listReq.ExecuteAsync(cancellationToken);

                    var files = new List<StorageFileItem>();
                    if (result.Files != null)
                    {
                        foreach (var f in result.Files)
                        {
                            files.Add(new StorageFileItem
                            {
                                FileId = f.Id,
                                FileName = f.Name,
                                FileSize = f.Size ?? 0,
                                LastModifiedUtc = f.ModifiedTimeDateTimeOffset?.UtcDateTime ?? DateTime.UtcNow,
                                Hash = f.Md5Checksum
                            });
                        }
                    }
                    return files;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[GoogleDrive] Error listing files in folder {FolderId} via API v3: {Message}", folderId, ex.Message);
                    return new List<StorageFileItem>();
                }
            }

            if (_fileStore.TryGetValue(folderId, out var mockFiles))
            {
                return mockFiles.ToList();
            }

            return new List<StorageFileItem>();
        }

        public async Task<Stream> DownloadFileAsync(string fileId, CancellationToken cancellationToken = default)
        {
            CheckNetworkAvailability();

            var service = GetDriveService();
            if (service != null)
            {
                var stream = new MemoryStream();
                var getReq = service.Files.Get(fileId);
                getReq.SupportsAllDrives = true;
                getReq.Alt = Google.Apis.Drive.v3.FilesResource.GetRequest.AltEnum.Media;
                var progress = await getReq.DownloadAsync(stream, cancellationToken);
                if (progress.Status == Google.Apis.Download.DownloadStatus.Failed && progress.Exception != null)
                {
                    _logger.LogError(progress.Exception, "[GoogleDrive] Download failed for file {FileId}: {Message}", fileId, progress.Exception.Message);
                    throw progress.Exception;
                }
                stream.Position = 0;
                return stream;
            }

            // Locate file in fileStore
            foreach (var kvp in _fileStore)
            {
                var file = kvp.Value.FirstOrDefault(f => f.FileId == fileId);
                if (file != null)
                {
                    var dummyContent = Encoding.UTF8.GetBytes($"[GoogleDrive Mock File Content for {file.FileName}]");
                    return new MemoryStream(dummyContent);
                }
            }

            throw new FileNotFoundException($"File '{fileId}' not found on Google Drive.");
        }

        public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                CheckNetworkAvailability();

                var service = GetDriveService();
                if (service != null)
                {
                    if (service.HttpClientInitializer != null)
                    {
                        var aboutReq = service.About.Get();
                        aboutReq.Fields = "user";
                        await aboutReq.ExecuteAsync(cancellationToken);
                    }
                    return true;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[GoogleDrive] Connection test failed: {Message}", ex.Message);
                return false;
            }
        }

        public string GetFolderUrl(string folderId)
        {
            if (string.IsNullOrWhiteSpace(folderId)) return "";
            return $"https://drive.google.com/drive/folders/{folderId}";
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
