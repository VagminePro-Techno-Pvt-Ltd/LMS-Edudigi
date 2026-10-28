using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TMS.Common;

namespace TMS.Web.Services
{
    public class HttpAuditUserProvider : IAuditUserProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpAuditUserProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int? GetUserId()
        {
            var context = AuditLogContext.Current;
            if (context?.UserId.HasValue == true) return context.UserId;

            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null) return null;

            var claim = user.FindFirst("userId") ?? user.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out var id))
            {
                return id;
            }
            return null;
        }

        public string? GetUsername()
        {
            var context = AuditLogContext.Current;
            if (!string.IsNullOrWhiteSpace(context?.Username)) return context.Username;

            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirst("userName")?.Value ??
                   user?.FindFirst("user")?.Value ??
                   user?.Identity?.Name;
        }

        public string? GetUserRole()
        {
            var context = AuditLogContext.Current;
            if (!string.IsNullOrWhiteSpace(context?.UserRole)) return context.UserRole;

            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirst("role")?.Value ??
                   user?.FindFirst(ClaimTypes.Role)?.Value;
        }

        public string? GetIpAddress()
        {
            var context = AuditLogContext.Current;
            if (!string.IsNullOrWhiteSpace(context?.IpAddress)) return context.IpAddress;

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return null;

            var forwarded = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwarded))
            {
                var first = forwarded.Split(',')[0].Trim();
                if (!string.IsNullOrWhiteSpace(first)) return first;
            }
            return httpContext.Connection.RemoteIpAddress?.ToString();
        }

        public string? GetCorrelationId()
        {
            var context = AuditLogContext.Current;
            if (!string.IsNullOrWhiteSpace(context?.CorrelationId)) return context.CorrelationId;

            return _httpContextAccessor.HttpContext?.TraceIdentifier;
        }

        public bool IsTrackingSuppressed()
        {
            return AuditLogContext.Current?.SuppressChangeTracker == true;
        }
    }
}

