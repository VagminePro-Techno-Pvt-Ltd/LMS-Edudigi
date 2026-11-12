using System;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TMS.ViewModels.Academics;

namespace TMS.Web.Services
{
    public interface ITranscriptExportService
    {
        byte[] ExportToText(LectureTranscriptViewModel transcript, string videoTitle, string? courseName = null);
        byte[] ExportToPdf(LectureTranscriptViewModel transcript, string videoTitle, string? courseName = null);
    }

    public class TranscriptExportService : ITranscriptExportService
    {
        static TranscriptExportService()
        {
            try
            {
                QuestPDF.Settings.License = LicenseType.Community;
                QuestPDF.Settings.UseSystemFonts = true;
            }
            catch
            {
                // Non-fatal if already set elsewhere
            }
        }

        public byte[] ExportToText(LectureTranscriptViewModel transcript, string videoTitle, string? courseName = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("                    EduDigiClass LMS — Video Lecture Transcript                 ");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"Lecture Title : {videoTitle}");
            if (!string.IsNullOrWhiteSpace(courseName))
                sb.AppendLine($"Course        : {courseName}");
            sb.AppendLine($"Language      : {transcript.Language ?? "English"}");
            if (transcript.DurationSeconds > 0)
                sb.AppendLine($"Duration      : {LectureTranscriptSegmentViewModel.FormatTimestamp(transcript.DurationSeconds)}");
            sb.AppendLine($"Generated On  : {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine("================================================================================");
            sb.AppendLine();

            if (transcript.Segments == null || transcript.Segments.Count == 0)
            {
                sb.AppendLine("No transcript segments available.");
            }
            else
            {
                foreach (var seg in transcript.Segments)
                {
                    string speakerPrefix = !string.IsNullOrWhiteSpace(seg.Speaker) ? $"[{seg.Speaker}] " : "";
                    sb.AppendLine($"[{seg.FormattedStartTime} - {seg.FormattedEndTime}] {speakerPrefix}{seg.Text.Trim()}");
                    sb.AppendLine();
                }
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public byte[] ExportToPdf(LectureTranscriptViewModel transcript, string videoTitle, string? courseName = null)
        {
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36); // 0.5 inch margins
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor("#1e293b").FontFamily(Fonts.Lato));

                    // Header
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(headerCol =>
                            {
                                headerCol.Item().Text("EduDigiClass LMS").FontSize(16).Bold().FontColor("#2563eb");
                                headerCol.Item().Text("AI-Generated Lecture Transcript").FontSize(11).SemiBold().FontColor("#64748b");
                            });

                            row.ConstantItem(120).AlignRight().Column(metaCol =>
                            {
                                metaCol.Item().Text($"Language: {transcript.Language ?? "English"}").FontSize(9).FontColor("#64748b");
                                if (transcript.DurationSeconds > 0)
                                    metaCol.Item().Text($"Duration: {LectureTranscriptSegmentViewModel.FormatTimestamp(transcript.DurationSeconds)}").FontSize(9).FontColor("#64748b");
                            });
                        });

                        col.Item().PaddingTop(8).LineHorizontal(1).LineColor("#e2e8f0");

                        // Title banner
                        col.Item().PaddingTop(10).Background("#f8fafc").Padding(10).Column(bCol =>
                        {
                            bCol.Item().Text(videoTitle).FontSize(13).Bold().FontColor("#0f172a");
                            if (!string.IsNullOrWhiteSpace(courseName))
                                bCol.Item().PaddingTop(2).Text($"Course: {courseName}").FontSize(10).FontColor("#475569");
                        });

                        col.Item().PaddingTop(8);
                    });

                    // Body
                    page.Content().PaddingTop(10).Column(col =>
                    {
                        if (transcript.Segments == null || transcript.Segments.Count == 0)
                        {
                            col.Item().Padding(20).AlignCenter().Text("No transcript segments available for this lecture.").FontColor("#94a3b8").Italic();
                        }
                        else
                        {
                            foreach (var seg in transcript.Segments)
                            {
                                col.Item().PaddingBottom(6).Row(row =>
                                {
                                    // Timestamp chip column
                                    row.ConstantItem(90).Background("#eff6ff").Padding(4).AlignCenter().Text(seg.FormattedStartTime).FontSize(9).Bold().FontColor("#2563eb");

                                    // Spacing
                                    row.ConstantItem(10);

                                    // Segment text column
                                    row.RelativeItem().Column(textCol =>
                                    {
                                        if (!string.IsNullOrWhiteSpace(seg.Speaker))
                                        {
                                            textCol.Item().Text(seg.Speaker).FontSize(9).Bold().FontColor("#64748b");
                                        }
                                        textCol.Item().Text(seg.Text.Trim()).FontSize(10).LineHeight(1.35f);
                                    });
                                });
                            }
                        }
                    });

                    // Footer
                    page.Footer().Column(fCol =>
                    {
                        fCol.Item().LineHorizontal(1).LineColor("#e2e8f0");
                        fCol.Item().PaddingTop(6).Row(row =>
                        {
                            row.RelativeItem().Text($"Exported on {DateTime.UtcNow:MMM dd, yyyy HH:mm} UTC • EduDigiClass LMS").FontSize(8).FontColor("#94a3b8");
                            row.RelativeItem().AlignRight().Text(text =>
                            {
                                text.Span("Page ");
                                text.CurrentPageNumber();
                                text.Span(" of ");
                                text.TotalPages();
                            });
                        });
                    });
                });
            });

            return doc.GeneratePdf();
        }
    }
}

