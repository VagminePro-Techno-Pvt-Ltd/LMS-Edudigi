namespace TMS.Web.Services
{
    public interface IViewRenderService
    {
        Task<string> RenderToStringAsync(string viewName, object model, object preference, object? imageBasePath, object? siteUrl);
    }
}
