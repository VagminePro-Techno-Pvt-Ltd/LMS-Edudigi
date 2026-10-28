using System;
using System.Threading;

namespace TMS.Web.Services
{
    public class AuditLogContextInfo
    {
        public int? UserId { get; set; }
        public string? Username { get; set; }
        public string? UserRole { get; set; }
        public string? IpAddress { get; set; }
        public string? CorrelationId { get; set; }
        public bool SuppressChangeTracker { get; set; }
    }

    public static class AuditLogContext
    {
        private static readonly AsyncLocal<AuditLogContextInfo?> _currentContext = new();

        public static AuditLogContextInfo? Current
        {
            get => _currentContext.Value;
            set => _currentContext.Value = value;
        }

        public static IDisposable SetContext(AuditLogContextInfo info)
        {
            var previous = _currentContext.Value;
            _currentContext.Value = info;
            return new ContextScope(previous);
        }

        public static IDisposable SuppressTracking()
        {
            var current = _currentContext.Value ?? new AuditLogContextInfo();
            current.SuppressChangeTracker = true;
            return SetContext(current);
        }

        private sealed class ContextScope : IDisposable
        {
            private readonly AuditLogContextInfo? _previous;
            public ContextScope(AuditLogContextInfo? previous) => _previous = previous;
            public void Dispose() => _currentContext.Value = _previous;
        }
    }
}

