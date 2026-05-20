using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using TMS.Repository;
using TMS.ViewModels;
using UoN.ExpressiveAnnotations.NetCore.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
#if DEBUG
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
#else
 builder.Services.AddControllersWithViews();
#endif
builder.Services.Configure<ZoomSettings>(
    builder.Configuration.GetSection("ZoomSettings")
);
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = int.MaxValue;
});
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = int.MaxValue;
});
builder.WebHost.ConfigureKestrel((context, options) =>
{
    options.Limits.MaxRequestBodySize = 209715200; // 200MB
});


builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(option => option.LoginPath = "/auth/LoginOptional");
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddExpressiveAnnotations();
builder.Services.AddSession();

//Additional dependencies
builder.Services.AddRepositoryDependencies();
builder.Services.AddHostedService<TMS.Web.BackgroundServices.AttendanceBackgroundJob>();


builder.Services.Configure<TMS.ViewModels.StorageSettings>(builder.Configuration.GetSection("StorageSettings"));
builder.Services.Configure<TMS.ViewModels.RateConfiguration>(builder.Configuration.GetSection("RateConfiguration"));

builder.Services.AddScoped<TMS.Web.Services.IViewRenderService, TMS.Web.Services.ViewRenderService>();
builder.Services.AddSingleton<TMS.Web.Services.IWebHostEnvironmentAccessor, TMS.Web.Services.WebHostEnvironmentAccessor>();
builder.Services.AddScoped<TMS.Web.Services.IInteractivePPTService, TMS.Web.Services.InteractivePPTService>();
builder.Services.AddScoped<TMS.Web.Services.IStudentLearningService, TMS.Web.Services.StudentLearningService>();


// Reverse-proxy / ngrok: honor X-Forwarded-Proto, X-Forwarded-Host so Request.Scheme and Request.Host
// match the public URL (required for Microsoft Office Online Viewer src= absolute URLs).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedProto
        | ForwardedHeaders.XForwardedHost;
    if (builder.Environment.IsDevelopment())
    {
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    }
});

var app = builder.Build();

app.UseForwardedHeaders();

var defaultDateCulture = "en-IN";
var cultureInfo = new System.Globalization.CultureInfo(defaultDateCulture);
cultureInfo.NumberFormat.CurrencySymbol = "₹";
cultureInfo.DateTimeFormat.ShortDatePattern = "dd'/'MM'/'yyyy";

// Configure the Localization middleware
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(cultureInfo),
    SupportedCultures = new List<System.Globalization.CultureInfo>
    {
        cultureInfo,
    },
    SupportedUICultures = new List<System.Globalization.CultureInfo>
    {
        cultureInfo,
    }
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Configure static files with no-cache headers for development
if (app.Environment.IsDevelopment())
{
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            ctx.Context.Response.Headers.Append("Expires", "0");
        }
    });
}
else
{
    app.UseStaticFiles();
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;
    if (response.StatusCode == StatusCodes.Status403Forbidden)
    {
        response.Redirect("/Home/UnauthorizedAccess");
    }
    else if (response.StatusCode == StatusCodes.Status404NotFound)
    {
        response.Redirect("/Home/ItemNotFound");
    }
    await Task.CompletedTask;
});

app.MapControllerRoute(
    name: "studentCourses",
    pattern: "MyCourses",
    defaults: new { controller = "StudentCourse", action = "Index" });

app.MapControllerRoute(
    name: "subjectHub",
    pattern: "SubjectHub/{courseId:int}",
    defaults: new { controller = "StudentCourse", action = "Details" });

app.MapControllerRoute(
    name: "unitHub",
    pattern: "Subject/{courseId:int}/Unit/{unitId:int}",
    defaults: new { controller = "StudentUnit", action = "Details" });

app.MapControllerRoute(
    name: "quadrantContent",
    pattern: "Subject/{courseId:int}/Unit/{unitId:int}/Quadrant/{quadrantId:int}",
    defaults: new { controller = "StudentQuadrant", action = "Content" });

app.MapControllerRoute(
    name: "defaultArea",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

//app.Run(async context =>
//{
//    context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize = 200000000;
//});
app.Run();
