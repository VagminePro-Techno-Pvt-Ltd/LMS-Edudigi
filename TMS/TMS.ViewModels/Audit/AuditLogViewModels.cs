using System;
using System.Collections.Generic;

namespace TMS.ViewModels.Audit
{
    public class AuditLogFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Username { get; set; }
        public string? UserRole { get; set; }
        public string? ModuleName { get; set; }
        public string? ActionType { get; set; }
        public string? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }

    public class AuditLogItemViewModel
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string? Username { get; set; }
        public string? UserRole { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string ModuleName { get; set; } = string.Empty;
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public string? ChangedFields { get; set; }
        public DateTime TimestampUtc { get; set; }
        public string FormattedTimestampUtc => TimestampUtc.ToString("yyyy-MM-dd HH:mm:ss");
        public string? IpAddress { get; set; }
        public string Status { get; set; } = "SUCCESS";
        public string? CorrelationId { get; set; }
        public string? Details { get; set; }
        public bool HasChanges => !string.IsNullOrEmpty(ChangedFields);
    }

    public class FieldChangeItem
    {
        public string FieldName { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public bool IsChanged { get; set; } = true;
    }

    public class AuditLogDetailViewModel
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string? Username { get; set; }
        public string? UserRole { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string ModuleName { get; set; } = string.Empty;
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public string? OldValuesJson { get; set; }
        public string? NewValuesJson { get; set; }
        public string? ChangedFieldsJson { get; set; }
        public DateTime TimestampUtc { get; set; }
        public string FormattedTimestampUtc => TimestampUtc.ToString("yyyy-MM-dd HH:mm:ss");
        public string? IpAddress { get; set; }
        public string Status { get; set; } = "SUCCESS";
        public string? CorrelationId { get; set; }
        public string? Details { get; set; }
        public List<FieldChangeItem> FieldChanges { get; set; } = new();
        public string OperationType { get; set; } = "UPDATE"; // CREATE, UPDATE, DELETE, EVENT
    }

    public class AuditLogSummaryViewModel
    {
        public int TotalLogs { get; set; }
        public int TodayActivities { get; set; }
        public int ActiveUsers { get; set; }
        public int FailedActions { get; set; }
    }

    public class AuditLogIndexViewModel
    {
        public AuditLogFilterViewModel Filter { get; set; } = new();
        public AuditLogSummaryViewModel Summary { get; set; } = new();
        public List<AuditLogItemViewModel> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / (PageSize > 0 ? PageSize : 25));
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public List<string> AvailableRoles { get; set; } = new();
        public List<string> AvailableModules { get; set; } = new();
        public List<string> AvailableActionTypes { get; set; } = new();
        public List<string> AvailableStatuses { get; set; } = new();
    }
}

