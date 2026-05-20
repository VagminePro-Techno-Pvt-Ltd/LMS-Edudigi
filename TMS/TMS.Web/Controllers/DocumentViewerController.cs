using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using TMS.Repository.Managers;
using TMS.ViewModels.Masters;

namespace TMS.Web.Controllers
{
    /// <summary>
    /// Hybrid document viewer:
    ///   Localhost/dev  → PDF served via iframe; DOC/DOCX/PPT/PPTX converted to PDF via LibreOffice then served in iframe.
    ///   Production HTTPS → PDF served via iframe; Office files sent to Microsoft Office Online Viewer.
    /// </summary>
    public class DocumentViewerController : BaseController
    {
        private readonly IMasterBaseManager<LectureMaterialViewModel> _materialManager;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<DocumentViewerController> _logger;

        // ── LibreOffice executable path (override in appsettings.json → "LibreOfficePath") ──
        // Typical paths:
        //   Windows : C:\Program Files\LibreOffice\program\soffice.exe
        //   Linux   : /usr/bin/soffice  OR  /usr/lib/libreoffice/program/soffice
        private readonly string _libreOfficePath;

        // Directory under wwwroot where converted PDFs are cached.
        private const string ConvertedDir = "converted-documents";

        public DocumentViewerController(
            IMasterBaseManager<LectureMaterialViewModel> materialManager,
            IWebHostEnvironment env,
            IConfiguration configuration,
            ILogger<DocumentViewerController> logger)
        {
            _materialManager = materialManager;
            _env = env;
            _logger = logger;
            _libreOfficePath = configuration["LibreOfficePath"]
                               ?? @"C:\Program Files\LibreOffice\program\soffice.exe";
        }

        // ══════════════════════════════════════════════════════════
        // GET  /DocumentViewer/View/{id}
        // Main viewer page — renders appropriate iframe based on host
        // ══════════════════════════════════════════════════════════
        [HttpGet]
        [Route("DocumentViewer/View/{id}")]
        public async Task<IActionResult> View(int id)
        {
            var material = await _materialManager.GetAsync(id);
            if (material == null || !material.IsActive)
                return NotFound("Document not found.");

            var filePath = material.FilePath ?? "";
            var ext = Path.GetExtension(filePath).ToLowerInvariant();

            var request = HttpContext.Request;
            bool isLocal = IsLocalhostOrNonHttps(request);
            bool isPdf = ext == ".pdf";
            bool isOffice = new[] { ".doc", ".docx", ".ppt", ".pptx" }.Contains(ext);
            bool isExcel = new[] { ".xls", ".xlsx" }.Contains(ext);

            // Absolute public URL of the raw file (used by MS Office Viewer on production)
            string publicFileUrl = BuildAbsoluteFileUrl(request, filePath);
            // Stream endpoint URL (used for iframe on localhost)
            string streamUrl = Url.Action("Stream", "DocumentViewer", new { id }, request.Scheme) ?? "";

            // ── Viewer Type Decision ──
            string viewerType;
            string iframeSrc;
            bool showConversionWarning = false;

            if (isPdf)
            {
                // PDF: always serve locally through Stream endpoint
                viewerType = "PDF";
                iframeSrc = streamUrl;
            }
            else if (isOffice || isExcel)
            {
                if (isLocal)
                {
                    // Localhost: try LibreOffice conversion to PDF
                    var (converted, convError) = await TryConvertToPdfAsync(filePath);
                    if (converted != null)
                    {
                        viewerType = "ConvertedPDF";
                        // Build URL to the cached converted PDF
                        iframeSrc = $"{request.Scheme}://{request.Host}/{ConvertedDir}/{Path.GetFileName(converted)}";
                    }
                    else
                    {
                        // Conversion failed — show download-only notice
                        viewerType = "ConversionFailed";
                        iframeSrc = "";
                        showConversionWarning = true;
                        _logger.LogWarning("LibreOffice conversion failed for material {Id}: {Err}", id, convError);
                    }
                }
                else
                {
                    // Production HTTPS — use Microsoft Office Online Viewer
                    viewerType = "OfficeOnline";
                    iframeSrc = "https://view.officeapps.live.com/op/embed.aspx?src="
                                + Uri.EscapeDataString(publicFileUrl);
                }
            }
            else if (new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp", ".svg" }.Contains(ext))
            {
                viewerType = "Image";
                iframeSrc = streamUrl;
            }
            else
            {
                viewerType = "Unsupported";
                iframeSrc = "";
            }

            // ── ViewBag ──
            ViewBag.ViewerType = viewerType;
            ViewBag.IframeSrc = iframeSrc;
            ViewBag.FileExtension = ext;
            ViewBag.IsLocalhost = isLocal;
            ViewBag.ShowConversionWarning = showConversionWarning;
            ViewBag.DownloadUrl = Url.Action("Download", "DocumentViewer", new { id });
            ViewBag.PublicFileUrl = publicFileUrl;

            // Back-link context (try to resolve from material mapping)
            ViewBag.CourseName = "Course";
            ViewBag.BackUrl = Request.Headers["Referer"].ToString();
            if (string.IsNullOrEmpty(ViewBag.BackUrl))
                ViewBag.BackUrl = "/StudentCourseContent/Index";

            return View("~/Views/DocumentViewer/View.cshtml", material);
        }

        // ══════════════════════════════════════════════════════════
        // GET  /DocumentViewer/Stream/{id}
        // Streams the raw file inline (for PDF, Images, fallback)
        // ══════════════════════════════════════════════════════════
        [HttpGet]
        [Route("DocumentViewer/Stream/{id}")]
        public async Task<IActionResult> Stream(int id)
        {
            var material = await _materialManager.GetAsync(id);
            if (material == null || !material.IsActive || string.IsNullOrEmpty(material.FilePath))
                return NotFound();

            var filePath = material.FilePath;

            // Cloud redirect
            if (material.Source == "GoogleDrive" || material.Source == "OneDrive")
                return Redirect(filePath);

            var absolutePath = ResolvePhysicalPath(filePath);
            if (!System.IO.File.Exists(absolutePath))
                return NotFound("File not found on server.");

            var contentType = GetContentType(absolutePath);
            var fileBytes = await System.IO.File.ReadAllBytesAsync(absolutePath);

            // Serve inline so the browser renders it (not a download)
            Response.Headers["Content-Disposition"] =
                $"inline; filename=\"{Uri.EscapeDataString(material.OriginalFileName ?? Path.GetFileName(absolutePath))}\"";

            return File(fileBytes, contentType);
        }

        // ══════════════════════════════════════════════════════════
        // GET  /DocumentViewer/Download/{id}
        // Forces download with Content-Disposition: attachment
        // ══════════════════════════════════════════════════════════
        [HttpGet]
        [Route("DocumentViewer/Download/{id}")]
        public async Task<IActionResult> Download(int id)
        {
            var material = await _materialManager.GetAsync(id);
            if (material == null || !material.IsActive || string.IsNullOrEmpty(material.FilePath))
                return NotFound();

            var filePath = material.FilePath;
            if (material.Source == "GoogleDrive" || material.Source == "OneDrive")
                return Redirect(filePath);

            var absolutePath = ResolvePhysicalPath(filePath);
            if (!System.IO.File.Exists(absolutePath))
                return NotFound("File not found on server.");

            var contentType = GetContentType(absolutePath);
            var fileBytes = await System.IO.File.ReadAllBytesAsync(absolutePath);
            var fileName = material.OriginalFileName ?? Path.GetFileName(absolutePath);

            return File(fileBytes, contentType, fileName);
        }

        // ══════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Converts a DOC/DOCX/PPT/PPTX to PDF using LibreOffice headless.
        /// Returns (outputPdfPath, null) on success, or (null, errorMessage) on failure.
        /// Caches result in wwwroot/converted-documents/ by content hash so re-conversion is avoided.
        /// </summary>
        private async Task<(string? pdfPath, string? error)> TryConvertToPdfAsync(string relativeFilePath)
        {
            try
            {
                var absoluteSrc = ResolvePhysicalPath(relativeFilePath);
                if (!System.IO.File.Exists(absoluteSrc))
                    return (null, "Source file not found.");

                // Cache key: original filename + last-write timestamp (simple but effective)
                var info = new FileInfo(absoluteSrc);
                var cacheKey = $"{Path.GetFileNameWithoutExtension(absoluteSrc)}_{info.LastWriteTime:yyyyMMddHHmmss}";
                var safeKey = string.Concat(cacheKey.Split(Path.GetInvalidFileNameChars()));
                var outFileName = safeKey + ".pdf";

                var outDir = Path.Combine(_env.WebRootPath, ConvertedDir);
                Directory.CreateDirectory(outDir);
                var outPath = Path.Combine(outDir, outFileName);

                // Return cached conversion if it exists
                if (System.IO.File.Exists(outPath))
                    return (outPath, null);

                // Verify LibreOffice exists
                if (!System.IO.File.Exists(_libreOfficePath))
                    return (null, $"LibreOffice not found at: {_libreOfficePath}. Install LibreOffice and set 'LibreOfficePath' in appsettings.json.");

                // Run LibreOffice headless conversion
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _libreOfficePath,
                    Arguments = $"--headless --convert-to pdf --outdir \"{outDir}\" \"{absoluteSrc}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = System.Diagnostics.Process.Start(psi);
                if (proc == null)
                    return (null, "Failed to start LibreOffice process.");

                // Wait max 60 seconds for conversion
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                await proc.WaitForExitAsync(cts.Token);

                // LibreOffice outputs as <originalName>.pdf in the outDir
                var libreOutPath = Path.Combine(outDir,
                    Path.GetFileNameWithoutExtension(absoluteSrc) + ".pdf");

                if (System.IO.File.Exists(libreOutPath))
                {
                    // Rename to our cache-key filename
                    if (libreOutPath != outPath)
                        System.IO.File.Move(libreOutPath, outPath, overwrite: true);
                    return (outPath, null);
                }

                var stderr = await proc.StandardError.ReadToEndAsync();
                return (null, $"LibreOffice conversion produced no output. stderr: {stderr}");
            }
            catch (Exception ex)
            {
                return (null, ex.Message);
            }
        }

        private string ResolvePhysicalPath(string filePath)
        {
            var rel = filePath.Replace("~/", "").TrimStart('/');
            return Path.Combine(_env.WebRootPath, rel);
        }

        private string BuildAbsoluteFileUrl(HttpRequest request, string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return "";
            if (filePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                filePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return filePath;

            var rel = filePath.Replace("~/", "/").TrimStart('/');
            var pathBase = request.PathBase.Value?.TrimEnd('/') ?? "";
            return $"{request.Scheme}://{request.Host}{pathBase}/{rel}";
        }

        /// <summary>Returns true when the request is from localhost or is NOT using HTTPS.</summary>
        private static bool IsLocalhostOrNonHttps(HttpRequest request)
        {
            var host = request.Host.Host?.ToLowerInvariant() ?? "";
            if (host == "localhost" || host == "127.0.0.1" || host == "::1" || host == "[::1]")
                return true;
            if (request.Scheme?.ToLowerInvariant() != "https")
                return true;

            // Private IP ranges
            if (System.Net.IPAddress.TryParse(host, out var ip))
            {
                var b = ip.GetAddressBytes();
                if (b.Length == 4 &&
                    (b[0] == 10 ||
                     (b[0] == 192 && b[1] == 168) ||
                     (b[0] == 172 && b[1] >= 16 && b[1] <= 31)))
                    return true;
            }

            // Host with no dot (e.g., "myserver") = intranet
            if (!host.Contains('.'))
                return true;

            return false;
        }

        private static string GetContentType(string path)
        {
            var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
            return provider.TryGetContentType(path, out var ct) ? ct : "application/octet-stream";
        }
    }
}
