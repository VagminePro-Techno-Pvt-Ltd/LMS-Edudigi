using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TMS.Repository.Managers;
using TMS.ViewModels;
using TMS.Web.Controllers;
using ExcelDataReader;
using System.Data;
using System.Text;

namespace TMS.Web.Areas.Admin.Controllers
{
    /// <summary>
    /// Bulk User Upload Controller — INDEPENDENT of Course Enrollment.
    /// Creates Admin, Faculty, and Student users via CSV/Excel upload.
    /// Supports: Upload → Preview → Confirm → Process
    /// </summary>
    [Area("Admin")]
    [Authorize]
    public class BulkUserUploadController : BaseController
    {
        private readonly IBulkUserManager _bulkUserManager;

        public BulkUserUploadController(IBulkUserManager bulkUserManager)
        {
            _bulkUserManager = bulkUserManager;
        }

        // =============================
        // GET: Index — Show upload page
        // =============================
        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "Bulk Upload Users";
            return View();
        }

        // =============================
        // GET: DownloadTemplate — CSV template
        // =============================
        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            var csv = "Name,Email,Phone,Role\n" +
                      "\"Rahul Sharma\",\"rahul@example.com\",\"9876543210\",\"Student\"\n" +
                      "\"Priya Verma\",\"priya@example.com\",\"9123456789\",\"Faculty\"\n" +
                      "\"Admin User\",\"admin@example.com\",\"9988776655\",\"Admin\"";
            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", "User_Upload_Template.csv");
        }

        // =============================
        // POST: Preview — Parse + Validate (no save)
        // =============================
        [HttpPost]
        public async Task<IActionResult> Preview(IFormFile bulkFile)
        {
            ViewData["Title"] = "Bulk Upload Users";

            if (bulkFile == null || bulkFile.Length == 0)
            {
                SetApplicationResult(false, "Please upload a valid Excel or CSV file.");
                return View("Index");
            }

            var rows = ParseFile(bulkFile);

            if (!rows.Any())
            {
                SetApplicationResult(false, "No data found in file.");
                return View("Index");
            }

            // Validate rows for preview
            var validatedRows = await _bulkUserManager.ValidateRowsAsync(rows);

            // Store in TempData for the confirm step
            TempData.Put("BulkPreviewRows", validatedRows);
            ViewBag.PreviewRows = validatedRows;
            ViewBag.ValidCount = validatedRows.Count(r => r.Status == "Valid");
            ViewBag.ErrorCount = validatedRows.Count(r => r.Status == "Error");
            ViewBag.ExistsCount = validatedRows.Count(r => r.Status == "Exists" || r.Status == "Duplicate");

            return View("Preview");
        }

        // =============================
        // POST: Confirm — Process after preview
        // =============================
        [HttpPost]
        public async Task<IActionResult> Confirm(string uploadMode)
        {
            ViewData["Title"] = "Bulk Upload Users";

            var rows = TempData.Get<List<BulkUserUploadRow>>("BulkPreviewRows");
            if (rows == null || !rows.Any())
            {
                SetApplicationResult(false, "Session expired. Please upload the file again.");
                return View("Index");
            }

            var mode = uploadMode switch
            {
                "CreateOnly" => BulkUploadMode.CreateOnly,
                "UpdateExisting" => BulkUploadMode.UpdateExisting,
                _ => BulkUploadMode.SkipExisting
            };

            var result = await _bulkUserManager.ProcessBulkUploadAsync(rows, GetUserId(), mode);

            ViewBag.BulkResult = result;

            var statusMsg = result.FailedCount == 0
                ? $"Upload complete! {result.CreatedCount} created, {result.SkippedCount} skipped, {result.UpdatedCount} updated."
                : $"Upload done. Created: {result.CreatedCount}, Skipped: {result.SkippedCount}, Updated: {result.UpdatedCount}, Failed: {result.FailedCount}";

            SetApplicationResult(result.FailedCount == 0, statusMsg);

            return View("Result");
        }

        // =============================
        // POST: Upload (legacy direct upload — still works)
        // =============================
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile bulkFile, string uploadMode)
        {
            ViewData["Title"] = "Bulk Upload Users";

            if (bulkFile == null || bulkFile.Length == 0)
            {
                SetApplicationResult(false, "Please upload a valid Excel or CSV file.");
                return View("Index");
            }

            var rows = ParseFile(bulkFile);
            if (!rows.Any())
            {
                SetApplicationResult(false, "No data found in file.");
                return View("Index");
            }

            var mode = uploadMode switch
            {
                "CreateOnly" => BulkUploadMode.CreateOnly,
                "UpdateExisting" => BulkUploadMode.UpdateExisting,
                _ => BulkUploadMode.SkipExisting
            };

            var result = await _bulkUserManager.ProcessBulkUploadAsync(rows, GetUserId(), mode);
            ViewBag.BulkResult = result;

            var statusMsg = result.FailedCount == 0
                ? $"Upload complete! {result.CreatedCount} created, {result.SkippedCount} skipped, {result.UpdatedCount} updated."
                : $"Upload done. Created: {result.CreatedCount}, Skipped: {result.SkippedCount}, Updated: {result.UpdatedCount}, Failed: {result.FailedCount}";

            SetApplicationResult(result.FailedCount == 0, statusMsg);
            return View("Result");
        }

        // =============================
        // GET: DownloadErrors — Download error report
        // =============================
        [HttpGet]
        public IActionResult DownloadErrors()
        {
            var result = TempData.Get<BulkUserUploadResult>("LastBulkResult");
            if (result == null || !result.Errors.Any())
            {
                return Content("No errors to download.");
            }

            var sb = new StringBuilder();
            sb.AppendLine("Row,Type,Message");
            foreach (var err in result.Errors)
            {
                sb.AppendLine($"\"{err.Replace("\"", "\"\"")}\"");
            }
            foreach (var warn in result.Warnings)
            {
                sb.AppendLine($"\"{warn.Replace("\"", "\"\"")}\"");
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "Upload_Error_Report.csv");
        }

        // =============================
        // HELPER: Parse CSV/Excel file
        // =============================
        private List<BulkUserUploadRow> ParseFile(IFormFile file)
        {
            var rows = new List<BulkUserUploadRow>();
            var fileName = file.FileName;

            try
            {
                using var stream = file.OpenReadStream();

                if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    using var reader = new StreamReader(stream);
                    int line = 0;
                    while (!reader.EndOfStream)
                    {
                        line++;
                        var content = reader.ReadLine();
                        if (line == 1 || string.IsNullOrWhiteSpace(content)) continue;

                        var parts = content.Split(',');
                        rows.Add(new BulkUserUploadRow
                        {
                            RowNumber = line,
                            Name = parts.Length > 0 ? parts[0].Trim().Trim('"') : "",
                            Email = parts.Length > 1 ? parts[1].Trim().Trim('"') : "",
                            Phone = parts.Length > 2 ? parts[2].Trim().Trim('"') : null,
                            Role = parts.Length > 3 ? parts[3].Trim().Trim('"') : ""
                        });
                    }
                }
                else
                {
                    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                    using var reader = ExcelReaderFactory.CreateReader(stream);
                    var resultDs = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true }
                    });

                    var dt = resultDs.Tables[0];
                    int rowNum = 1;
                    foreach (DataRow dr in dt.Rows)
                    {
                        rowNum++;
                        rows.Add(new BulkUserUploadRow
                        {
                            RowNumber = rowNum,
                            Name = dr[0]?.ToString()?.Trim() ?? "",
                            Email = dr[1]?.ToString()?.Trim() ?? "",
                            Phone = dr[2]?.ToString()?.Trim(),
                            Role = dr[3]?.ToString()?.Trim() ?? ""
                        });
                    }
                }
            }
            catch (Exception)
            {
                // Return empty list on parse failure
            }

            return rows;
        }
    }
}
