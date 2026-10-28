using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMS.Models.Audit;
using TMS.ViewModels.Audit;

namespace TMS.Web.Services
{
    public interface IAuditLogService
    {
        Task EnsureSchemaAsync(CancellationToken ct = default);

        Task LogActionAsync(
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
            CancellationToken ct = default);

        Task<AuditLogIndexViewModel> GetLogsAsync(AuditLogFilterViewModel filter, CancellationToken ct = default);

        Task<AuditLogDetailViewModel?> GetLogByIdAsync(int id, CancellationToken ct = default);

        Task<AuditLogSummaryViewModel> GetSummaryAsync(AuditLogFilterViewModel filter, CancellationToken ct = default);

        Task<byte[]> ExportCsvAsync(AuditLogFilterViewModel filter, CancellationToken ct = default);
    }
}

