using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TMS.Models.Audit;
using TMS.Repository;
using TMS.ViewModels.Audit;

namespace TMS.Web.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly ApplicationDBContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditLogService> _logger;

        private static bool _schemaEnsured = false;
        private static readonly object SchemaLock = new();

        private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
        {
            "Password",
            "PasswordHash",
            "CurrentPassword",
            "NewPassword",
            "ConfirmPassword",
            "Token",
            "AuthToken",
            "Secret",
            "SecurityStamp",
            "ConcurrencyStamp",
            "ApiKey",
            "Pin"
        };

        public AuditLogService(
            ApplicationDBContext db,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuditLogService> logger)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task EnsureSchemaAsync(CancellationToken ct = default)
        {
            if (_schemaEnsured) return;

            lock (SchemaLock)
            {
                if (_schemaEnsured) return;
            }

            try
            {
                if (_db.Database.IsRelational())
                {
                    const string sql = @"
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
END";
                    await _db.Database.ExecuteSqlRawAsync(sql, ct);
                }

                lock (SchemaLock)
                {
                    _schemaEnsured = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not ensure AuditLogs schema: {Message}", ex.Message);
            }
        }

        public async Task LogActionAsync(
            string actionType,
            string moduleName,
            string? entityType = null,
            string? entityId = null,
            object? oldValues = null,
            object? newValues = null,
            string? changedFields = null,
            string status = "SUCCESS",
            string? details = null,
            int? userId = null,
            string? username = null,
            string? userRole = null,
            string? ipAddress = null,
            string? correlationId = null,
            CancellationToken ct = default)
        {
            try
            {
                await EnsureSchemaAsync(ct);

                var httpContext = _httpContextAccessor.HttpContext;
                var contextInfo = AuditLogContext.Current;

                // Resolve User ID
                int? resolvedUserId = userId;
                if (!resolvedUserId.HasValue)
                {
                    if (contextInfo?.UserId.HasValue == true)
                    {
                        resolvedUserId = contextInfo.UserId;
                    }
                    else if (httpContext?.User != null)
                    {
                        var claim = httpContext.User.FindFirst("userId") ??
                                    httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
                        if (claim != null && int.TryParse(claim.Value, out var parsed))
                        {
                            resolvedUserId = parsed;
                        }
                    }
                }

                // Resolve Username
                string? resolvedUsername = username;
                if (string.IsNullOrWhiteSpace(resolvedUsername))
                {
                    resolvedUsername = contextInfo?.Username ??
                        httpContext?.User?.FindFirst("userName")?.Value ??
                        httpContext?.User?.FindFirst("user")?.Value ??
                        httpContext?.User?.Identity?.Name ??
                        (resolvedUserId.HasValue ? $"User #{resolvedUserId}" : "Anonymous");
                }

                // Resolve Role
                string? resolvedRole = userRole;
                if (string.IsNullOrWhiteSpace(resolvedRole))
                {
                    resolvedRole = contextInfo?.UserRole ??
                        httpContext?.User?.FindFirst("role")?.Value ??
                        httpContext?.User?.FindFirst(ClaimTypes.Role)?.Value ??
                        (resolvedUserId.HasValue ? "User" : "Anonymous");
                }

                // Resolve IP
                string? resolvedIp = ipAddress;
                if (string.IsNullOrWhiteSpace(resolvedIp))
                {
                    resolvedIp = contextInfo?.IpAddress ??
                        httpContext?.Request?.Headers["X-Forwarded-For"].FirstOrDefault() ??
                        httpContext?.Connection?.RemoteIpAddress?.ToString();
                }

                // Resolve Correlation ID
                string? resolvedCorrelationId = correlationId;
                if (string.IsNullOrWhiteSpace(resolvedCorrelationId))
                {
                    resolvedCorrelationId = contextInfo?.CorrelationId ??
                        httpContext?.TraceIdentifier ??
                        Guid.NewGuid().ToString("N");
                }

                string? oldJson = SerializeSanitized(oldValues);
                string? newJson = SerializeSanitized(newValues);

                var entry = new AuditLog
                {
                    UserId = resolvedUserId,
                    Username = resolvedUsername,
                    UserRole = resolvedRole,
                    ActionType = actionType.Trim(),
                    ModuleName = moduleName.Trim(),
                    EntityType = entityType?.Trim(),
                    EntityId = entityId?.Trim(),
                    OldValuesJson = oldJson,
                    NewValuesJson = newJson,
                    ChangedFieldsJson = changedFields?.Trim(),
                    TimestampUtc = DateTime.UtcNow,
                    IpAddress = resolvedIp,
                    Status = string.IsNullOrWhiteSpace(status) ? "SUCCESS" : status.Trim().ToUpperInvariant(),
                    CorrelationId = resolvedCorrelationId,
                    Details = details?.Trim()
                };

                _db.AuditLogs.Add(entry);
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                // Never break business flow due to audit failure
                _logger.LogError(ex, "Failed to persist audit log for Action: {Action}, Module: {Module}", actionType, moduleName);
            }
        }

        public async Task<AuditLogIndexViewModel> GetLogsAsync(AuditLogFilterViewModel filter, CancellationToken ct = default)
        {
            await EnsureSchemaAsync(ct);

            var query = _db.AuditLogs.AsNoTracking().AsQueryable();

            query = ApplyFilters(query, filter);

            int totalCount = await query.CountAsync(ct);

            int pageNumber = filter.PageNumber > 0 ? filter.PageNumber : 1;
            int pageSize = filter.PageSize > 0 ? filter.PageSize : 25;

            var items = await query
                .OrderByDescending(x => x.TimestampUtc)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new AuditLogItemViewModel
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    Username = x.Username,
                    UserRole = x.UserRole,
                    ActionType = x.ActionType,
                    ModuleName = x.ModuleName,
                    EntityType = x.EntityType,
                    EntityId = x.EntityId,
                    ChangedFields = x.ChangedFieldsJson,
                    TimestampUtc = x.TimestampUtc,
                    IpAddress = x.IpAddress,
                    Status = x.Status,
                    CorrelationId = x.CorrelationId,
                    Details = x.Details
                })
                .ToListAsync(ct);

            var summary = await GetSummaryAsync(filter, ct);

            // Populate filter options
            var roles = await _db.AuditLogs.AsNoTracking()
                .Where(x => !string.IsNullOrEmpty(x.UserRole))
                .Select(x => x.UserRole!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync(ct);

            var modules = await _db.AuditLogs.AsNoTracking()
                .Where(x => !string.IsNullOrEmpty(x.ModuleName))
                .Select(x => x.ModuleName)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync(ct);

            var actions = await _db.AuditLogs.AsNoTracking()
                .Where(x => !string.IsNullOrEmpty(x.ActionType))
                .Select(x => x.ActionType)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync(ct);

            var statuses = new List<string> { "SUCCESS", "FAILURE" };

            return new AuditLogIndexViewModel
            {
                Filter = filter,
                Summary = summary,
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                AvailableRoles = roles,
                AvailableModules = modules,
                AvailableActionTypes = actions,
                AvailableStatuses = statuses
            };
        }

        public async Task<AuditLogSummaryViewModel> GetSummaryAsync(AuditLogFilterViewModel filter, CancellationToken ct = default)
        {
            await EnsureSchemaAsync(ct);

            var query = _db.AuditLogs.AsNoTracking().AsQueryable();

            int totalLogs = await query.CountAsync(ct);

            var todayUtcStart = DateTime.UtcNow.Date;
            var todayUtcEnd = todayUtcStart.AddDays(1);

            int todayActivities = await query
                .Where(x => x.TimestampUtc >= todayUtcStart && x.TimestampUtc < todayUtcEnd)
                .CountAsync(ct);

            int failedActions = await query
                .Where(x => x.Status == "FAILURE")
                .CountAsync(ct);

            // Active distinct users with activity in the last 30 days (or filtered range if provided)
            var activeUserQuery = query;
            if (filter.StartDate.HasValue)
            {
                activeUserQuery = activeUserQuery.Where(x => x.TimestampUtc >= filter.StartDate.Value);
            }
            else
            {
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                activeUserQuery = activeUserQuery.Where(x => x.TimestampUtc >= thirtyDaysAgo);
            }

            if (filter.EndDate.HasValue)
            {
                var endUtc = filter.EndDate.Value.Date.AddDays(1);
                activeUserQuery = activeUserQuery.Where(x => x.TimestampUtc < endUtc);
            }

            int activeUsers = await activeUserQuery
                .Where(x => !string.IsNullOrEmpty(x.Username) && x.Username != "Anonymous")
                .Select(x => x.Username)
                .Distinct()
                .CountAsync(ct);

            return new AuditLogSummaryViewModel
            {
                TotalLogs = totalLogs,
                TodayActivities = todayActivities,
                ActiveUsers = activeUsers,
                FailedActions = failedActions
            };
        }

        public async Task<AuditLogDetailViewModel?> GetLogByIdAsync(int id, CancellationToken ct = default)
        {
            await EnsureSchemaAsync(ct);

            var log = await _db.AuditLogs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (log == null) return null;

            var model = new AuditLogDetailViewModel
            {
                Id = log.Id,
                UserId = log.UserId,
                Username = log.Username,
                UserRole = log.UserRole,
                ActionType = log.ActionType,
                ModuleName = log.ModuleName,
                EntityType = log.EntityType,
                EntityId = log.EntityId,
                OldValuesJson = log.OldValuesJson,
                NewValuesJson = log.NewValuesJson,
                ChangedFieldsJson = log.ChangedFieldsJson,
                TimestampUtc = log.TimestampUtc,
                IpAddress = log.IpAddress,
                Status = log.Status,
                CorrelationId = log.CorrelationId,
                Details = log.Details
            };

            // Determine operation type
            if (string.IsNullOrEmpty(log.OldValuesJson) && !string.IsNullOrEmpty(log.NewValuesJson))
            {
                model.OperationType = "CREATE";
            }
            else if (!string.IsNullOrEmpty(log.OldValuesJson) && string.IsNullOrEmpty(log.NewValuesJson))
            {
                model.OperationType = "DELETE";
            }
            else if (!string.IsNullOrEmpty(log.OldValuesJson) && !string.IsNullOrEmpty(log.NewValuesJson))
            {
                model.OperationType = "UPDATE";
            }
            else
            {
                model.OperationType = "EVENT";
            }

            // Parse changes
            model.FieldChanges = ParseFieldChanges(log.OldValuesJson, log.NewValuesJson, log.ChangedFieldsJson);

            return model;
        }

        public async Task<byte[]> ExportCsvAsync(AuditLogFilterViewModel filter, CancellationToken ct = default)
        {
            await EnsureSchemaAsync(ct);

            var query = _db.AuditLogs.AsNoTracking().AsQueryable();
            query = ApplyFilters(query, filter);

            // Fetch logs for export (limit to top 10000 to prevent memory exhaustion)
            var logs = await query
                .OrderByDescending(x => x.TimestampUtc)
                .Take(10000)
                .ToListAsync(ct);

            var sb = new StringBuilder();

            // CSV Header
            sb.AppendLine("ID,Date & Time (UTC),Username,User Role,Module,Action Type,Entity Type,Entity ID,Status,Changed Fields,IP Address,Correlation ID,Details");

            foreach (var log in logs)
            {
                sb.AppendLine(string.Join(",",
                    EscapeCsv(log.Id.ToString()),
                    EscapeCsv(log.TimestampUtc.ToString("yyyy-MM-dd HH:mm:ss")),
                    EscapeCsv(log.Username ?? ""),
                    EscapeCsv(log.UserRole ?? ""),
                    EscapeCsv(log.ModuleName),
                    EscapeCsv(log.ActionType),
                    EscapeCsv(log.EntityType ?? ""),
                    EscapeCsv(log.EntityId ?? ""),
                    EscapeCsv(log.Status),
                    EscapeCsv(log.ChangedFieldsJson ?? ""),
                    EscapeCsv(log.IpAddress ?? ""),
                    EscapeCsv(log.CorrelationId ?? ""),
                    EscapeCsv(log.Details ?? "")
                ));
            }

            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        }

        private static IQueryable<AuditLog> ApplyFilters(IQueryable<AuditLog> query, AuditLogFilterViewModel filter)
        {
            if (filter.StartDate.HasValue)
            {
                var startUtc = filter.StartDate.Value.Date;
                query = query.Where(x => x.TimestampUtc >= startUtc);
            }

            if (filter.EndDate.HasValue)
            {
                var endUtc = filter.EndDate.Value.Date.AddDays(1);
                query = query.Where(x => x.TimestampUtc < endUtc);
            }

            if (!string.IsNullOrWhiteSpace(filter.Username))
            {
                var u = filter.Username.Trim();
                query = query.Where(x => x.Username != null && x.Username.Contains(u));
            }

            if (!string.IsNullOrWhiteSpace(filter.UserRole))
            {
                var r = filter.UserRole.Trim();
                query = query.Where(x => x.UserRole != null && x.UserRole.ToLower() == r.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(filter.ModuleName))
            {
                var m = filter.ModuleName.Trim();
                query = query.Where(x => x.ModuleName.ToLower() == m.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(filter.ActionType))
            {
                var a = filter.ActionType.Trim();
                query = query.Where(x => x.ActionType.ToLower() == a.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                var s = filter.Status.Trim();
                query = query.Where(x => x.Status.ToLower() == s.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var st = filter.SearchTerm.Trim().ToLower();
                query = query.Where(x =>
                    (x.Username != null && x.Username.ToLower().Contains(st)) ||
                    x.ActionType.ToLower().Contains(st) ||
                    x.ModuleName.ToLower().Contains(st) ||
                    (x.EntityType != null && x.EntityType.ToLower().Contains(st)) ||
                    (x.EntityId != null && x.EntityId.ToLower().Contains(st)) ||
                    (x.ChangedFieldsJson != null && x.ChangedFieldsJson.ToLower().Contains(st)) ||
                    (x.Details != null && x.Details.ToLower().Contains(st)) ||
                    (x.IpAddress != null && x.IpAddress.ToLower().Contains(st)) ||
                    (x.CorrelationId != null && x.CorrelationId.ToLower().Contains(st)));
            }

            return query;
        }

        private static List<FieldChangeItem> ParseFieldChanges(string? oldJson, string? newJson, string? changedFields)
        {
            var result = new List<FieldChangeItem>();

            Dictionary<string, object?> oldDict = DeserializeDict(oldJson);
            Dictionary<string, object?> newDict = DeserializeDict(newJson);

            var changedFieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(changedFields))
            {
                // Try JSON array first
                try
                {
                    var arr = JsonSerializer.Deserialize<List<string>>(changedFields);
                    if (arr != null)
                    {
                        foreach (var f in arr) changedFieldNames.Add(f.Trim());
                    }
                }
                catch
                {
                    // Fall back to comma separated
                    foreach (var f in changedFields.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        changedFieldNames.Add(f.Trim());
                    }
                }
            }

            var allKeys = new SortedSet<string>(oldDict.Keys.Concat(newDict.Keys), StringComparer.OrdinalIgnoreCase);

            foreach (var key in allKeys)
            {
                if (SensitiveKeys.Contains(key)) continue;

                oldDict.TryGetValue(key, out var oldVal);
                newDict.TryGetValue(key, out var newVal);

                string? oldStr = FormatValue(oldVal);
                string? newStr = FormatValue(newVal);

                bool isChanged = changedFieldNames.Contains(key) || !string.Equals(oldStr, newStr, StringComparison.Ordinal);

                result.Add(new FieldChangeItem
                {
                    FieldName = key,
                    OldValue = oldStr,
                    NewValue = newStr,
                    IsChanged = isChanged
                });
            }

            return result;
        }

        private static Dictionary<string, object?> DeserializeDict(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var doc = JsonDocument.Parse(json);
                var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (SensitiveKeys.Contains(prop.Name)) continue;
                    dict[prop.Name] = prop.Value.Clone();
                }
                return dict;
            }
            catch
            {
                return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private static string? FormatValue(object? val)
        {
            if (val == null) return null;
            if (val is JsonElement je)
            {
                switch (je.ValueKind)
                {
                    case JsonValueKind.Null:
                    case JsonValueKind.Undefined:
                        return null;
                    case JsonValueKind.String:
                        return je.GetString();
                    case JsonValueKind.True:
                        return "True";
                    case JsonValueKind.False:
                        return "False";
                    case JsonValueKind.Number:
                        return je.GetRawText();
                    default:
                        return je.GetRawText();
                }
            }
            return val.ToString();
        }

        private static string? SerializeSanitized(object? obj)
        {
            if (obj == null) return null;
            if (obj is string str)
            {
                if (string.IsNullOrWhiteSpace(str)) return null;
                // If already JSON, sanitize sensitive keys
                try
                {
                    var doc = JsonDocument.Parse(str);
                    var dict = new Dictionary<string, object?>();
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        if (SensitiveKeys.Contains(prop.Name)) continue;
                        dict[prop.Name] = prop.Value.Clone();
                    }
                    return JsonSerializer.Serialize(dict);
                }
                catch
                {
                    return str;
                }
            }

            try
            {
                var json = JsonSerializer.Serialize(obj);
                var doc = JsonDocument.Parse(json);
                var dict = new Dictionary<string, object?>();
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        if (SensitiveKeys.Contains(prop.Name)) continue;
                        dict[prop.Name] = prop.Value.Clone();
                    }
                    return JsonSerializer.Serialize(dict);
                }
                return json;
            }
            catch
            {
                return null;
            }
        }

        private static string EscapeCsv(string field)
        {
            if (string.IsNullOrEmpty(field)) return "\"\"";

            // CSV Formula Injection mitigation:
            // If field starts with =, +, -, @, tab, carriage return, prefix with single quote '
            string safeField = field;
            if (safeField.StartsWith("=") || safeField.StartsWith("+") ||
                safeField.StartsWith("-") || safeField.StartsWith("@") ||
                safeField.StartsWith("\t") || safeField.StartsWith("\r"))
            {
                safeField = "'" + safeField;
            }

            if (safeField.Contains("\"") || safeField.Contains(",") || safeField.Contains("\n") || safeField.Contains("\r"))
            {
                return "\"" + safeField.Replace("\"", "\"\"") + "\"";
            }

            return "\"" + safeField + "\"";
        }
    }
}

