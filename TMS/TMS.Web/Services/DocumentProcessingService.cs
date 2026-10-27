using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using TMS.Models.LearningAssistant;
using TMS.ViewModels.LearningAssistant;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Exceptions;

namespace TMS.Web.Services
{
    public class DocumentProcessingService : IDocumentProcessingService
    {
        private readonly ILogger<DocumentProcessingService>? _logger;

        public DocumentProcessingService(ILogger<DocumentProcessingService>? logger = null)
        {
            _logger = logger;
        }

        public ExtractedDocumentResult Extract(string physicalFilePath, string? title = null, int materialId = 0)
        {
            if (string.IsNullOrWhiteSpace(physicalFilePath))
            {
                return new ExtractedDocumentResult
                {
                    Success = false,
                    Status = DocumentProcessingStatus.FileNotFound,
                    ErrorMessage = "File path was not provided."
                };
            }

            if (!File.Exists(physicalFilePath))
            {
                return new ExtractedDocumentResult
                {
                    Success = false,
                    Status = DocumentProcessingStatus.FileNotFound,
                    ErrorMessage = $"File does not exist on disk: {physicalFilePath}"
                };
            }

            var fileInfo = new FileInfo(physicalFilePath);
            if (fileInfo.Length == 0)
            {
                return new ExtractedDocumentResult
                {
                    Success = false,
                    Status = DocumentProcessingStatus.Empty,
                    ErrorMessage = "Document file is 0 bytes (empty)."
                };
            }

            string ext = Path.GetExtension(physicalFilePath).ToLowerInvariant();
            string docTitle = !string.IsNullOrWhiteSpace(title) ? title : Path.GetFileNameWithoutExtension(physicalFilePath);

            try
            {
                using var stream = new FileStream(physicalFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                return Extract(stream, ext, docTitle, materialId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error reading file from disk: {FilePath}", physicalFilePath);
                return new ExtractedDocumentResult
                {
                    Success = false,
                    Status = DocumentProcessingStatus.Failed,
                    ErrorMessage = $"Failed reading file: {ex.Message}"
                };
            }
        }

        public ExtractedDocumentResult Extract(Stream stream, string fileExtension, string? title = null, int materialId = 0)
        {
            if (stream == null || stream.Length == 0)
            {
                return new ExtractedDocumentResult
                {
                    Success = false,
                    Status = DocumentProcessingStatus.Empty,
                    ErrorMessage = "Document stream is empty (0 bytes)."
                };
            }

            var ext = (fileExtension ?? string.Empty).ToLowerInvariant().TrimStart('.');
            var docTitle = !string.IsNullOrWhiteSpace(title) ? title : "Course Material";

            try
            {
                return ext switch
                {
                    "pdf" => ExtractPdf(stream, docTitle),
                    "docx" => ExtractDocx(stream, docTitle),
                    "pptx" => ExtractPptx(stream, docTitle),
                    "txt" or "md" => ExtractPlainText(stream, docTitle),
                    _ => new ExtractedDocumentResult
                    {
                        Success = false,
                        Status = DocumentProcessingStatus.Unsupported,
                        ErrorMessage = $"Unsupported document type '.{ext}'. Supported formats are: PDF, DOCX, PPTX, TXT, MD."
                    }
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error processing document of type .{Ext}", ext);
                return new ExtractedDocumentResult
                {
                    Success = false,
                    Status = DocumentProcessingStatus.Corrupted,
                    ErrorMessage = $"Corrupted or unreadable document: {ex.Message}"
                };
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // PDF EXTRACTION (via PdfPig)
        // ═════════════════════════════════════════════════════════════════════
        private ExtractedDocumentResult ExtractPdf(Stream stream, string docTitle)
        {
            var result = new ExtractedDocumentResult();

            // Ensure stream is seekable (memory stream copy if needed)
            MemoryStream? ms = null;
            Stream sourceStream = stream;
            if (!stream.CanSeek)
            {
                ms = new MemoryStream();
                stream.CopyTo(ms);
                ms.Position = 0;
                sourceStream = ms;
            }
            else
            {
                sourceStream.Position = 0;
            }

            try
            {
                using var document = PdfDocument.Open(sourceStream);
                result.PageCount = document.NumberOfPages;

                if (result.PageCount == 0)
                {
                    result.Success = false;
                    result.Status = DocumentProcessingStatus.Empty;
                    result.ErrorMessage = "PDF contains 0 pages.";
                    return result;
                }

                var totalChars = 0;
                var hasAnyImages = false;

                for (int pageNum = 1; pageNum <= document.NumberOfPages; pageNum++)
                {
                    var page = document.GetPage(pageNum);
                    var pageText = page.Text ?? string.Empty;

                    // Clean whitespace
                    pageText = NormalizeWhitespace(pageText);
                    totalChars += pageText.Length;

                    if (!hasAnyImages)
                    {
                        try
                        {
                            hasAnyImages = page.GetImages().Any();
                        }
                        catch
                        {
                            // Ignore image parsing errors during image check
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(pageText))
                    {
                        result.Pages.Add(new ExtractedDocumentPage
                        {
                            PageNumber = pageNum,
                            Heading = $"{docTitle} (Page {pageNum})",
                            Text = pageText
                        });
                    }
                }

                // Identify scanned PDF:
                // If total extracted text is negligible (< 25 characters across the entire PDF or average < 10 chars/page)
                if (totalChars < 25 || (result.PageCount > 1 && (totalChars / result.PageCount) < 10))
                {
                    if (hasAnyImages || totalChars < 5)
                    {
                        result.Success = false;
                        result.Status = DocumentProcessingStatus.ScannedOcrRequired;
                        result.ErrorMessage = "This document appears to be a scanned image or image-only PDF. OCR (Optical Character Recognition) is required and not yet supported.";
                        return result;
                    }
                }

                if (result.Pages.Count == 0 || totalChars == 0)
                {
                    result.Success = false;
                    result.Status = DocumentProcessingStatus.Empty;
                    result.ErrorMessage = "PDF contains no readable text content.";
                    return result;
                }

                // Chunk pages into searchable passages
                result.Chunks = ChunkPages(result.Pages, docTitle);
                result.Success = true;
                result.Status = DocumentProcessingStatus.Indexed;
                return result;
            }
            catch (PdfDocumentEncryptedException)
            {
                result.Success = false;
                result.Status = DocumentProcessingStatus.Encrypted;
                result.ErrorMessage = "The PDF is password-protected or encrypted and cannot be read.";
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to parse PDF document");
                result.Success = false;
                result.Status = DocumentProcessingStatus.Corrupted;
                result.ErrorMessage = $"Failed to parse PDF document: {ex.Message}";
                return result;
            }
            finally
            {
                ms?.Dispose();
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // DOCX EXTRACTION (OpenXML via ZipArchive)
        // ═════════════════════════════════════════════════════════════════════
        private ExtractedDocumentResult ExtractDocx(Stream stream, string docTitle)
        {
            var result = new ExtractedDocumentResult();

            MemoryStream? ms = null;
            Stream sourceStream = stream;
            if (!stream.CanSeek)
            {
                ms = new MemoryStream();
                stream.CopyTo(ms);
                ms.Position = 0;
                sourceStream = ms;
            }
            else
            {
                sourceStream.Position = 0;
            }

            try
            {
                using var zip = new ZipArchive(sourceStream, ZipArchiveMode.Read, leaveOpen: true);
                var docEntry = zip.GetEntry("word/document.xml");
                if (docEntry == null)
                {
                    result.Success = false;
                    result.Status = DocumentProcessingStatus.Corrupted;
                    result.ErrorMessage = "Invalid DOCX format: word/document.xml missing.";
                    return result;
                }

                using var s = docEntry.Open();
                var doc = XDocument.Load(s);
                XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

                var body = doc.Root?.Element(w + "body");
                if (body == null)
                {
                    result.Success = false;
                    result.Status = DocumentProcessingStatus.Empty;
                    result.ErrorMessage = "DOCX document body is missing or empty.";
                    return result;
                }

                var pages = new List<ExtractedDocumentPage>();
                var currentPageNumber = 1;
                var currentSb = new StringBuilder();
                string? currentHeading = null;
                var wordCountInCurrentPage = 0;

                foreach (var elem in body.Elements())
                {
                    if (elem.Name == w + "p")
                    {
                        // Check for explicit page break
                        var hasPageBreak = elem.Descendants(w + "br").Any(b => b.Attribute(w + "type")?.Value == "page") ||
                                           elem.Descendants(w + "lastRenderedPageBreak").Any();

                        var pStyle = elem.Descendants(w + "pStyle").FirstOrDefault()?.Attribute(w + "val")?.Value?.ToLowerInvariant();
                        var textRuns = elem.Descendants(w + "t").Select(t => t.Value).ToList();
                        var text = string.Join("", textRuns).Trim();

                        if (string.IsNullOrWhiteSpace(text)) continue;

                        bool isHeading = pStyle != null && (pStyle.Contains("heading") || pStyle.Contains("title"));
                        if (isHeading)
                        {
                            currentHeading = text;
                        }

                        currentSb.AppendLine(text);
                        wordCountInCurrentPage += text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

                        // Split virtual pages if explicit page break OR page reaches ~400 words
                        if (hasPageBreak || wordCountInCurrentPage >= 400)
                        {
                            var pageContent = currentSb.ToString().Trim();
                            if (!string.IsNullOrEmpty(pageContent))
                            {
                                pages.Add(new ExtractedDocumentPage
                                {
                                    PageNumber = currentPageNumber,
                                    Heading = currentHeading ?? $"{docTitle} (Section {currentPageNumber})",
                                    Text = pageContent
                                });
                                currentPageNumber++;
                                currentSb.Clear();
                                wordCountInCurrentPage = 0;
                            }
                        }
                    }
                    else if (elem.Name == w + "tbl")
                    {
                        var tableSb = new StringBuilder();
                        foreach (var tr in elem.Descendants(w + "tr"))
                        {
                            var rowCells = tr.Descendants(w + "tc")
                                .Select(tc => string.Join(" ", tc.Descendants(w + "t").Select(t => t.Value)).Trim())
                                .ToList();

                            if (rowCells.Any(c => !string.IsNullOrEmpty(c)))
                            {
                                tableSb.AppendLine(string.Join(" | ", rowCells));
                            }
                        }

                        if (tableSb.Length > 0)
                        {
                            currentSb.AppendLine(tableSb.ToString());
                        }
                    }
                }

                // Add trailing page
                var remaining = currentSb.ToString().Trim();
                if (!string.IsNullOrEmpty(remaining))
                {
                    pages.Add(new ExtractedDocumentPage
                    {
                        PageNumber = currentPageNumber,
                        Heading = currentHeading ?? $"{docTitle} (Section {currentPageNumber})",
                        Text = remaining
                    });
                }

                if (pages.Count == 0)
                {
                    result.Success = false;
                    result.Status = DocumentProcessingStatus.Empty;
                    result.ErrorMessage = "DOCX document contains no readable text content.";
                    return result;
                }

                result.PageCount = pages.Count;
                result.Pages = pages;
                result.Chunks = ChunkPages(pages, docTitle);
                result.Success = true;
                result.Status = DocumentProcessingStatus.Indexed;
                return result;
            }
            catch (InvalidDataException ex)
            {
                result.Success = false;
                result.Status = DocumentProcessingStatus.Corrupted;
                result.ErrorMessage = $"Corrupted DOCX archive: {ex.Message}";
                return result;
            }
            finally
            {
                ms?.Dispose();
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // PPTX EXTRACTION (OpenXML via ZipArchive)
        // ═════════════════════════════════════════════════════════════════════
        private ExtractedDocumentResult ExtractPptx(Stream stream, string docTitle)
        {
            var result = new ExtractedDocumentResult();

            MemoryStream? ms = null;
            Stream sourceStream = stream;
            if (!stream.CanSeek)
            {
                ms = new MemoryStream();
                stream.CopyTo(ms);
                ms.Position = 0;
                sourceStream = ms;
            }
            else
            {
                sourceStream.Position = 0;
            }

            try
            {
                using var zip = new ZipArchive(sourceStream, ZipArchiveMode.Read, leaveOpen: true);

                // 1. Determine presentation slide order
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
                            var normalized = target.StartsWith("slides/") ? "ppt/" + target : "ppt/slides/" + target;
                            slideRelMap[rId] = normalized.Replace('\\', '/');
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
                            slideEntries.Add(slidePath);
                        }
                    }
                }

                // Fallback if presentation.xml was missing or incomplete
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

                if (!slideEntries.Any())
                {
                    result.Success = false;
                    result.Status = DocumentProcessingStatus.Empty;
                    result.ErrorMessage = "PPTX presentation contains no slides.";
                    return result;
                }

                XNamespace p = "http://schemas.openxmlformats.org/presentationml/2006/main";
                XNamespace a = "http://schemas.openxmlformats.org/drawingml/2006/main";

                var pages = new List<ExtractedDocumentPage>();
                var chunks = new List<ExtractedChunkDto>();
                int slideNum = 1;

                foreach (var slidePath in slideEntries)
                {
                    var entry = zip.GetEntry(slidePath);
                    if (entry == null) continue;

                    using var s = entry.Open();
                    var slideDoc = XDocument.Load(s);

                    string slideTitle = string.Empty;
                    var bodyLines = new List<string>();

                    foreach (var sp in slideDoc.Descendants(p + "sp"))
                    {
                        var phType = sp.Descendants(p + "ph").FirstOrDefault()?.Attribute("type")?.Value;
                        var isTitle = phType == "title" || phType == "ctrTitle";

                        var paras = sp.Descendants(a + "p").ToList();
                        foreach (var para in paras)
                        {
                            var textRuns = para.Descendants(a + "t").Select(t => t.Value).ToList();
                            var line = string.Join("", textRuns).Trim();
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            if (isTitle && string.IsNullOrEmpty(slideTitle))
                            {
                                slideTitle = line;
                            }
                            else
                            {
                                bodyLines.Add(line);
                            }
                        }
                    }

                    // Check tables in slide
                    foreach (var tbl in slideDoc.Descendants(a + "tbl"))
                    {
                        foreach (var tr in tbl.Descendants(a + "tr"))
                        {
                            var cellTexts = tr.Descendants(a + "tc")
                                .Select(tc => string.Join(" ", tc.Descendants(a + "t").Select(t => t.Value)).Trim())
                                .ToList();
                            if (cellTexts.Any(c => !string.IsNullOrEmpty(c)))
                            {
                                bodyLines.Add(string.Join(" | ", cellTexts));
                            }
                        }
                    }

                    var sbSlide = new StringBuilder();
                    if (!string.IsNullOrEmpty(slideTitle))
                    {
                        sbSlide.AppendLine($"Slide Title: {slideTitle}");
                    }
                    foreach (var l in bodyLines)
                    {
                        sbSlide.AppendLine($"- {l}");
                    }

                    var fullSlideText = sbSlide.ToString().Trim();
                    var heading = !string.IsNullOrEmpty(slideTitle)
                        ? $"{docTitle} - Slide {slideNum}: {slideTitle}"
                        : $"{docTitle} (Slide {slideNum})";

                    pages.Add(new ExtractedDocumentPage
                    {
                        PageNumber = slideNum,
                        Heading = heading,
                        Text = fullSlideText
                    });

                    if (!string.IsNullOrWhiteSpace(fullSlideText))
                    {
                        chunks.Add(new ExtractedChunkDto
                        {
                            ChunkIndex = chunks.Count,
                            PageNumber = slideNum,
                            Heading = heading,
                            Content = fullSlideText,
                            WordCount = fullSlideText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length
                        });
                    }

                    slideNum++;
                }

                result.PageCount = pages.Count;
                result.Pages = pages;

                if (!chunks.Any())
                {
                    result.Success = false;
                    result.Status = DocumentProcessingStatus.Empty;
                    result.ErrorMessage = "PPTX presentation contains no readable text content.";
                    return result;
                }

                result.Chunks = chunks;
                result.Success = true;
                result.Status = DocumentProcessingStatus.Indexed;
                return result;
            }
            catch (InvalidDataException ex)
            {
                result.Success = false;
                result.Status = DocumentProcessingStatus.Corrupted;
                result.ErrorMessage = $"Corrupted PPTX archive: {ex.Message}";
                return result;
            }
            finally
            {
                ms?.Dispose();
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // PLAIN TEXT / MARKDOWN EXTRACTION
        // ═════════════════════════════════════════════════════════════════════
        private ExtractedDocumentResult ExtractPlainText(Stream stream, string docTitle)
        {
            var result = new ExtractedDocumentResult();

            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true);
            var content = reader.ReadToEnd();

            if (string.IsNullOrWhiteSpace(content))
            {
                result.Success = false;
                result.Status = DocumentProcessingStatus.Empty;
                result.ErrorMessage = "Text file is empty.";
                return result;
            }

            var paragraphs = content.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            var pages = new List<ExtractedDocumentPage>();
            var chunks = new List<ExtractedChunkDto>();

            var currentSb = new StringBuilder();
            var pageNum = 1;

            foreach (var para in paragraphs)
            {
                var clean = para.Trim();
                if (string.IsNullOrEmpty(clean)) continue;

                currentSb.AppendLine(clean);
                currentSb.AppendLine();

                if (currentSb.Length >= 1500)
                {
                    var text = currentSb.ToString().Trim();
                    pages.Add(new ExtractedDocumentPage
                    {
                        PageNumber = pageNum,
                        Heading = $"{docTitle} (Section {pageNum})",
                        Text = text
                    });

                    chunks.Add(new ExtractedChunkDto
                    {
                        ChunkIndex = chunks.Count,
                        PageNumber = pageNum,
                        Heading = $"{docTitle} (Section {pageNum})",
                        Content = text,
                        WordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length
                    });

                    pageNum++;
                    currentSb.Clear();
                }
            }

            var remaining = currentSb.ToString().Trim();
            if (!string.IsNullOrEmpty(remaining))
            {
                pages.Add(new ExtractedDocumentPage
                {
                    PageNumber = pageNum,
                    Heading = $"{docTitle} (Section {pageNum})",
                    Text = remaining
                });

                chunks.Add(new ExtractedChunkDto
                {
                    ChunkIndex = chunks.Count,
                    PageNumber = pageNum,
                    Heading = $"{docTitle} (Section {pageNum})",
                    Content = remaining,
                    WordCount = remaining.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length
                });
            }

            result.PageCount = pages.Count;
            result.Pages = pages;
            result.Chunks = chunks;
            result.Success = true;
            result.Status = DocumentProcessingStatus.Indexed;
            return result;
        }

        // ═════════════════════════════════════════════════════════════════════
        // CHUNKING LOGIC: Splits pages into searchable chunks with boundary awareness
        // ═════════════════════════════════════════════════════════════════════
        private static List<ExtractedChunkDto> ChunkPages(List<ExtractedDocumentPage> pages, string docTitle)
        {
            var chunks = new List<ExtractedChunkDto>();

            foreach (var page in pages)
            {
                var text = page.Text.Trim();
                if (string.IsNullOrWhiteSpace(text)) continue;

                // If page text is under 2000 characters, keep it as 1 cohesive chunk with page number
                if (text.Length <= 2000)
                {
                    chunks.Add(new ExtractedChunkDto
                    {
                        ChunkIndex = chunks.Count,
                        PageNumber = page.PageNumber,
                        Heading = page.Heading ?? $"{docTitle} (Page {page.PageNumber})",
                        Content = text,
                        WordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length
                    });
                }
                else
                {
                    // For longer pages, split by paragraphs or sentences
                    var paragraphs = text.Split(new[] { "\r\n\r\n", "\n\n", "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    var chunkSb = new StringBuilder();
                    var partIdx = 1;

                    foreach (var para in paragraphs)
                    {
                        var p = para.Trim();
                        if (string.IsNullOrEmpty(p)) continue;

                        if (chunkSb.Length + p.Length > 1800 && chunkSb.Length > 0)
                        {
                            var chunkText = chunkSb.ToString().Trim();
                            chunks.Add(new ExtractedChunkDto
                            {
                                ChunkIndex = chunks.Count,
                                PageNumber = page.PageNumber,
                                Heading = $"{page.Heading ?? docTitle + " (Page " + page.PageNumber + ")"} - Part {partIdx}",
                                Content = chunkText,
                                WordCount = chunkText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length
                            });
                            partIdx++;
                            chunkSb.Clear();
                        }

                        chunkSb.AppendLine(p);
                    }

                    if (chunkSb.Length > 0)
                    {
                        var chunkText = chunkSb.ToString().Trim();
                        chunks.Add(new ExtractedChunkDto
                        {
                            ChunkIndex = chunks.Count,
                            PageNumber = page.PageNumber,
                            Heading = partIdx > 1
                                ? $"{page.Heading ?? docTitle + " (Page " + page.PageNumber + ")"} - Part {partIdx}"
                                : page.Heading ?? $"{docTitle} (Page {page.PageNumber})",
                            Content = chunkText,
                            WordCount = chunkText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length
                        });
                    }
                }
            }

            return chunks;
        }

        private static string NormalizeWhitespace(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            // Normalize non-breaking spaces and irregular line breaks
            text = text.Replace('\u00A0', ' ');
            text = Regex.Replace(text, @"[ \t]+", " ");
            text = Regex.Replace(text, @"(\r?\n){3,}", "\n\n");
            return text.Trim();
        }
    }
}
