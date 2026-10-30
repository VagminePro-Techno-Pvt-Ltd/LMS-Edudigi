namespace TMS.Common
{
    public interface IAuditUserProvider
    {
        int? GetUserId();
        string? GetUsername();
        string? GetUserRole();
        string? GetIpAddress();
        string? GetCorrelationId();
        bool IsTrackingSuppressed();
    }
}

