using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TMS.Repository.Managers;
using TMS.ViewModels.Masters;
using TMS.Web.Services;

namespace TMS.Web.Controllers
{
    /// <summary>
    /// Hybrid document viewer:
    ///   Localhost/dev  → PDF served via iframe; DOC/DOCX/PPT/PPTX converted to PDF via LibreOffice then served in iframe.
    ///   Production HTTPS → PDF served via iframe; Office files sent to Microsoft Office Online Viewer.
    /// </summary>
    [Authorize]
    public class DocumentViewerController : BaseController
    {
        private readonly IMasterBaseManager<LectureMaterialViewModel> _materialManager;
        private readonly IStudentLearningService _learningService;
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
            IStudentLearningService learningService,
            IWebHostEnvironment env,
            IConfiguration configuration,
            ILogger<DocumentViewerController> logger)
        {
            _materialManager = materialManager;
            _learningService = learningService;
            _env = env;
            _logger = logger;
            var configPath = configuration["LibreOfficePath"];
            if (!string.IsNullOrWhiteSpace(configPath))
            {
                _libreOfficePath = configPath;
            }
            else if (OperatingSystem.IsMacOS())
            {
                _libreOfficePath = System.IO.File.Exists("/Applications/LibreOffice.app/Contents/MacOS/soffice")
                    ? "/Applications/LibreOffice.app/Contents/MacOS/soffice"
                    : "soffice";
            }
            else if (OperatingSystem.IsLinux())
            {
                _libreOfficePath = System.IO.File.Exists("/usr/bin/soffice") ? "/usr/bin/soffice" : "soffice";
            }
            else
            {
                _libreOfficePath = @"C:\Program Files\LibreOffice\program\soffice.exe";
            }
        }

        // ══════════════════════════════════════════════════════════
        // GET  /DocumentViewer/View/{id}
        // Main viewer page — renders appropriate iframe based on host
        // ══════════════════════════════════════════════════════════
        [HttpGet]
        [Route("DocumentViewer/View/{id}")]
        public async Task<IActionResult> View(int id)
        {
            int studentId = GetUserId();
            bool isStaff = User.IsInRole("Admin") || User.IsInRole("Super Admin") || User.IsInRole("Faculty") || User.IsInRole("Exam Coordinator");
            if (!isStaff)
            {
                bool enrolled = await _learningService.IsStudentEnrolledInMaterialAsync(studentId, id);
                if (!enrolled)
                    return Forbid();

                if (studentId > 0)
                {
                    await _learningService.MarkMaterialViewedAsync(studentId, id);
                }
            }

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
            else if (ext == ".pptx" || ext == ".ppt")
            {
                // PPT/PPTX: Try converted PDF first, otherwise use our rich built-in PPTX presentation player
                var (converted, _) = await TryConvertToPdfAsync(filePath);
                if (converted != null)
                {
                    viewerType = "ConvertedPDF";
                    iframeSrc = $"{request.Scheme}://{request.Host}/{ConvertedDir}/{Path.GetFileName(converted)}";
                }
                else
                {
                    viewerType = "PPTXViewer";
                    iframeSrc = "";
                }
            }
            else if (ext == ".docx" || ext == ".doc")
            {
                // DOC/DOCX: Try converted PDF first, otherwise use rich built-in Word Document reader
                var (converted, _) = await TryConvertToPdfAsync(filePath);
                if (converted != null)
                {
                    viewerType = "ConvertedPDF";
                    iframeSrc = $"{request.Scheme}://{request.Host}/{ConvertedDir}/{Path.GetFileName(converted)}";
                }
                else
                {
                    viewerType = "DOCXViewer";
                    iframeSrc = "";
                }
            }
            else if (isExcel)
            {
                if (isLocal)
                {
                    viewerType = "DOCXViewer";
                    iframeSrc = "";
                }
                else
                {
                    viewerType = "OfficeOnline";
                    iframeSrc = "https://view.officeapps.live.com/op/embed.aspx?src=" + Uri.EscapeDataString(publicFileUrl);
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
            bool isStaff = User.IsInRole("Admin") || User.IsInRole("Super Admin") || User.IsInRole("Faculty") || User.IsInRole("Exam Coordinator");
            if (!isStaff)
            {
                int studentId = GetUserId();
                bool enrolled = await _learningService.IsStudentEnrolledInMaterialAsync(studentId, id);
                if (!enrolled)
                    return Forbid();

                if (studentId > 0)
                {
                    await _learningService.MarkMaterialViewedAsync(studentId, id);
                }
            }

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
            bool isStaff = User.IsInRole("Admin") || User.IsInRole("Super Admin") || User.IsInRole("Faculty") || User.IsInRole("Exam Coordinator");
            if (!isStaff)
            {
                int studentId = GetUserId();
                bool enrolled = await _learningService.IsStudentEnrolledInMaterialAsync(studentId, id);
                if (!enrolled)
                    return Forbid();

                if (studentId > 0)
                {
                    await _learningService.MarkMaterialViewedAsync(studentId, id);
                }
            }

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

        // ══════════════════════════════════════════════════════════
        // GET  /DocumentViewer/GetPptxData/{id}
        // Extracts slides, titles, bullet points, tables, and images from PPTX
        // ══════════════════════════════════════════════════════════
        [HttpGet]
        [Route("DocumentViewer/GetPptxData/{id:int}")]
        public async Task<IActionResult> GetPptxData(int id)
        {
            var material = await _materialManager.GetAsync(id);
            if (material == null || !material.IsActive || string.IsNullOrEmpty(material.FilePath))
                return NotFound(new { success = false, message = "Material not found." });

            var absolutePath = ResolvePhysicalPath(material.FilePath);
            if (!System.IO.File.Exists(absolutePath))
                return NotFound(new { success = false, message = "File not found on server." });

            try
            {
                var slides = new List<PptxSlideDto>();

                using var zip = System.IO.Compression.ZipFile.OpenRead(absolutePath);

                // 1. Find slides in presentation order
                var presEntry = zip.GetEntry("ppt/presentation.xml");
                var presRelsEntry = zip.GetEntry("ppt/_rels/presentation.xml.rels");
                var slideRelMap = new Dictionary<string, string>();

                if (presRelsEntry != null)
                {
                    using var s = presRelsEntry.Open();
                    var relsDoc = XDocument.Load(s);
                    XNamespace relNs = "http://schemas.openxmlformats.org/package/2006/relationships";
                    foreach (var rel in relsDoc.Descendants(relNs + "Relationship"))
                    {
                        var rId = rel.Attribute("Id")?.Value;
                        var target = rel.Attribute("Target")?.Value;
                        if (!string.IsNullOrEmpty(rId) && !string.IsNullOrEmpty(target))
                        {
                            slideRelMap[rId] = target.StartsWith("slides/") ? "ppt/" + target : "ppt/slides/" + target;
                        }
                    }
                }

                var slideEntries = new List<string>();
                if (presEntry != null)
                {
                    using var s = presEntry.Open();
                    var presDoc = XDocument.Load(s);
                    XNamespace pNs = "http://schemas.openxmlformats.org/presentationml/2006/main";
                    XNamespace rNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

                    foreach (var sldId in presDoc.Descendants(pNs + "sldId"))
                    {
                        var rId = sldId.Attribute(rNs + "id")?.Value;
                        if (!string.IsNullOrEmpty(rId) && slideRelMap.TryGetValue(rId, out var slidePath))
                        {
                            slideEntries.Add(slidePath.Replace('\\', '/'));
                        }
                    }
                }

                // Fallback: alphabetical slide entries if presentation.xml was missing
                if (!slideEntries.Any())
                {
                    slideEntries = zip.Entries
                        .Where(e => e.FullName.StartsWith("ppt/slides/slide") && e.FullName.EndsWith(".xml"))
                        .OrderBy(e =>
                        {
                            var numPart = Path.GetFileNameWithoutExtension(e.FullName).Replace("slide", "");
                            return int.TryParse(numPart, out var n) ? n : 999;
                        })
                        .Select(e => e.FullName)
                        .ToList();
                }

                int slideIdx = 1;
                XNamespace p = "http://schemas.openxmlformats.org/presentationml/2006/main";
                XNamespace a = "http://schemas.openxmlformats.org/drawingml/2006/main";
                XNamespace r = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
                XNamespace pkgRels = "http://schemas.openxmlformats.org/package/2006/relationships";

                foreach (var slidePath in slideEntries)
                {
                    var entry = zip.GetEntry(slidePath);
                    if (entry == null) continue;

                    // Load slide relationships (for images)
                    var slideDir = Path.GetDirectoryName(slidePath)?.Replace('\\', '/') ?? "ppt/slides";
                    var slideFile = Path.GetFileName(slidePath);
                    var relsPath = $"{slideDir}/_rels/{slideFile}.rels";
                    var slideImgMap = new Dictionary<string, string>();

                    var relsEntry = zip.GetEntry(relsPath);
                    if (relsEntry != null)
                    {
                        try
                        {
                            using var relsStream = relsEntry.Open();
                            var relsDoc = XDocument.Load(relsStream);
                            foreach (var rel in relsDoc.Descendants(pkgRels + "Relationship"))
                            {
                                var rId = rel.Attribute("Id")?.Value;
                                var target = rel.Attribute("Target")?.Value;
                                if (!string.IsNullOrEmpty(rId) && !string.IsNullOrEmpty(target))
                                {
                                    var resolved = target.Replace("../", "ppt/").TrimStart('/');
                                    if (!resolved.StartsWith("ppt/")) resolved = "ppt/" + resolved;
                                    slideImgMap[rId] = resolved;
                                }
                            }
                        }
                        catch { }
                    }

                    var slideDto = new PptxSlideDto { SlideNumber = slideIdx++ };

                    using (var s = entry.Open())
                    {
                        var doc = XDocument.Load(s);

                        // 1. Shapes / Text
                        foreach (var sp in doc.Descendants(p + "sp"))
                        {
                            bool isTitleShape = sp.Descendants(p + "ph").Any(ph =>
                            {
                                var t = ph.Attribute("type")?.Value;
                                return t == "title" || t == "ctrTitle";
                            });

                            var paras = sp.Descendants(a + "p").ToList();
                            foreach (var para in paras)
                            {
                                var textRuns = para.Descendants(a + "r").Select(rElem => rElem.Element(a + "t")?.Value ?? "").ToList();
                                var fldRuns = para.Descendants(a + "fld").Select(f => f.Element(a + "t")?.Value ?? "").ToList();
                                var fullText = string.Join("", textRuns.Concat(fldRuns)).Trim();

                                if (string.IsNullOrWhiteSpace(fullText)) continue;

                                if (isTitleShape && string.IsNullOrEmpty(slideDto.Title))
                                {
                                    slideDto.Title = fullText;
                                }
                                else
                                {
                                    var lvlAttr = para.Element(a + "pPr")?.Attribute("lvl")?.Value;
                                    int lvl = int.TryParse(lvlAttr, out var parsedLvl) ? parsedLvl : 0;
                                    slideDto.Paragraphs.Add(new SlideParagraphDto { Text = fullText, Level = lvl });
                                }
                            }
                        }

                        // If title was not found from placeholder, use the first paragraph
                        if (string.IsNullOrEmpty(slideDto.Title) && slideDto.Paragraphs.Any())
                        {
                            slideDto.Title = slideDto.Paragraphs.First().Text;
                            slideDto.Paragraphs.RemoveAt(0);
                        }

                        if (string.IsNullOrEmpty(slideDto.Title))
                        {
                            slideDto.Title = $"Slide {slideDto.SlideNumber}";
                        }

                        // 2. Tables
                        foreach (var tbl in doc.Descendants(a + "tbl"))
                        {
                            var tableDto = new List<List<string>>();
                            foreach (var tr in tbl.Descendants(a + "tr"))
                            {
                                var row = new List<string>();
                                foreach (var tc in tr.Descendants(a + "tc"))
                                {
                                    var cellTexts = tc.Descendants(a + "t").Select(tElem => tElem.Value).ToList();
                                    row.Add(string.Join(" ", cellTexts).Trim());
                                }
                                if (row.Any(c => !string.IsNullOrEmpty(c)))
                                    tableDto.Add(row);
                            }
                            if (tableDto.Any())
                                slideDto.Tables.Add(tableDto);
                        }

                        // 3. Images
                        foreach (var pic in doc.Descendants(p + "pic"))
                        {
                            var blip = pic.Descendants(a + "blip").FirstOrDefault();
                            var rId = blip?.Attribute(r + "embed")?.Value;
                            if (!string.IsNullOrEmpty(rId) && slideImgMap.TryGetValue(rId, out var mediaPath))
                            {
                                var mediaEntry = zip.GetEntry(mediaPath);
                                if (mediaEntry != null)
                                {
                                    using var ms = new MemoryStream();
                                    using var mediaStream = mediaEntry.Open();
                                    await mediaStream.CopyToAsync(ms);
                                    var bytes = ms.ToArray();
                                    var ext = Path.GetExtension(mediaPath).ToLowerInvariant().TrimStart('.');
                                    var mime = ext == "svg" ? "image/svg+xml" : (ext == "jpg" || ext == "jpeg" ? "image/jpeg" : "image/png");
                                    slideDto.Images.Add($"data:{mime};base64,{Convert.ToBase64String(bytes)}");
                                }
                            }
                        }
                    }

                    slides.Add(slideDto);
                }

                return Json(new
                {
                    success = true,
                    title = material.Title ?? "Presentation",
                    slideCount = slides.Count,
                    slides = slides
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ══════════════════════════════════════════════════════════
        // GET  /DocumentViewer/GetDocxData/{id}
        // Extracts formatted HTML, headings, bullet lists, tables, and images from DOCX
        // ══════════════════════════════════════════════════════════
        [HttpGet]
        [Route("DocumentViewer/GetDocxData/{id:int}")]
        public async Task<IActionResult> GetDocxData(int id)
        {
            var material = await _materialManager.GetAsync(id);
            if (material == null || !material.IsActive || string.IsNullOrEmpty(material.FilePath))
                return NotFound(new { success = false, message = "Material not found." });

            var absolutePath = ResolvePhysicalPath(material.FilePath);
            if (!System.IO.File.Exists(absolutePath))
                return NotFound(new { success = false, message = "File not found on server." });

            try
            {
                using var zip = System.IO.Compression.ZipFile.OpenRead(absolutePath);
                var docEntry = zip.GetEntry("word/document.xml");
                if (docEntry == null)
                    return Json(new { success = false, message = "Invalid DOCX format." });

                // Load media relationships
                var relsEntry = zip.GetEntry("word/_rels/document.xml.rels");
                var imgMap = new Dictionary<string, string>();
                if (relsEntry != null)
                {
                    using var s = relsEntry.Open();
                    var relsDoc = XDocument.Load(s);
                    XNamespace pkgRels = "http://schemas.openxmlformats.org/package/2006/relationships";
                    foreach (var rel in relsDoc.Descendants(pkgRels + "Relationship"))
                    {
                        var rId = rel.Attribute("Id")?.Value;
                        var target = rel.Attribute("Target")?.Value;
                        if (!string.IsNullOrEmpty(rId) && !string.IsNullOrEmpty(target))
                        {
                            var resolved = target.Replace("../", "word/").TrimStart('/');
                            if (!resolved.StartsWith("word/")) resolved = "word/" + resolved;
                            imgMap[rId] = resolved;
                        }
                    }
                }

                var sb = new StringBuilder();
                using (var s = docEntry.Open())
                {
                    var doc = XDocument.Load(s);
                    XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
                    XNamespace a = "http://schemas.openxmlformats.org/drawingml/2006/main";
                    XNamespace r = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

                    foreach (var elem in doc.Root?.Element(w + "body")?.Elements() ?? Enumerable.Empty<XElement>())
                    {
                        if (elem.Name == w + "p")
                        {
                            var pStyle = elem.Descendants(w + "pStyle").FirstOrDefault()?.Attribute(w + "val")?.Value?.ToLowerInvariant();
                            var textRuns = elem.Descendants(w + "t").Select(t => t.Value).ToList();
                            var pText = string.Join("", textRuns).Trim();

                            // Check for embedded image in paragraph
                            var blip = elem.Descendants(a + "blip").FirstOrDefault();
                            var rId = blip?.Attribute(r + "embed")?.Value;
                            if (!string.IsNullOrEmpty(rId) && imgMap.TryGetValue(rId, out var mediaPath))
                            {
                                var mediaEntry = zip.GetEntry(mediaPath);
                                if (mediaEntry != null)
                                {
                                    using var ms = new MemoryStream();
                                    using var mediaStream = mediaEntry.Open();
                                    await mediaStream.CopyToAsync(ms);
                                    var bytes = ms.ToArray();
                                    var ext = Path.GetExtension(mediaPath).ToLowerInvariant().TrimStart('.');
                                    var mime = ext == "svg" ? "image/svg+xml" : (ext == "jpg" || ext == "jpeg" ? "image/jpeg" : "image/png");
                                    sb.Append($"<div class='docx-img-wrap my-3 text-center'><img src='data:{mime};base64,{Convert.ToBase64String(bytes)}' class='img-fluid rounded shadow-sm' style='max-height:480px;' /></div>");
                                }
                            }

                            if (string.IsNullOrWhiteSpace(pText)) continue;

                            if (pStyle != null && (pStyle.Contains("heading1") || pStyle.Contains("title")))
                            {
                                sb.Append($"<h1 class='docx-h1'>{System.Net.WebUtility.HtmlEncode(pText)}</h1>");
                            }
                            else if (pStyle != null && pStyle.Contains("heading2"))
                            {
                                sb.Append($"<h2 class='docx-h2'>{System.Net.WebUtility.HtmlEncode(pText)}</h2>");
                            }
                            else if (pStyle != null && pStyle.Contains("heading3"))
                            {
                                sb.Append($"<h3 class='docx-h3'>{System.Net.WebUtility.HtmlEncode(pText)}</h3>");
                            }
                            else
                            {
                                sb.Append($"<p class='docx-p'>{System.Net.WebUtility.HtmlEncode(pText)}</p>");
                            }
                        }
                        else if (elem.Name == w + "tbl")
                        {
                            sb.Append("<div class='table-responsive my-3'><table class='table table-bordered docx-tbl'>");
                            foreach (var tr in elem.Descendants(w + "tr"))
                            {
                                sb.Append("<tr>");
                                foreach (var tc in tr.Descendants(w + "tc"))
                                {
                                    var cellText = string.Join(" ", tc.Descendants(w + "t").Select(t => t.Value)).Trim();
                                    sb.Append($"<td>{System.Net.WebUtility.HtmlEncode(cellText)}</td>");
                                }
                                sb.Append("</tr>");
                            }
                            sb.Append("</table></div>");
                        }
                    }
                }

                return Json(new
                {
                    success = true,
                    title = material.Title ?? "Document",
                    html = sb.ToString()
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    public class PptxSlideDto
    {
        public int SlideNumber { get; set; }
        public string Title { get; set; } = "";
        public List<SlideParagraphDto> Paragraphs { get; set; } = new();
        public List<string> Images { get; set; } = new();
        public List<List<List<string>>> Tables { get; set; } = new();
    }

    public class SlideParagraphDto
    {
        public string Text { get; set; } = "";
        public int Level { get; set; }
    }
}
