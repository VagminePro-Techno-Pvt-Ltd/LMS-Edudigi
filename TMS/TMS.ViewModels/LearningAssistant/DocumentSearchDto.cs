using System;
using System.Collections.Generic;
using TMS.Models.LearningAssistant;

namespace TMS.ViewModels.LearningAssistant
{
    public class DocumentChunkSearchResult
    {
        public int ChunkId { get; set; }
        public int LectureMaterialId { get; set; }
        public string DocumentTitle { get; set; } = string.Empty;
        public string? OriginalFileName { get; set; }
        public int? PageNumber { get; set; }
        public string? Heading { get; set; }
        public string Content { get; set; } = string.Empty;
        public double Score { get; set; }
        public int? CourseId { get; set; }
        public string? CourseName { get; set; }
        public string? CourseCode { get; set; }

        public string CitationReference
        {
            get
            {
                var title = !string.IsNullOrWhiteSpace(DocumentTitle) ? DocumentTitle : (OriginalFileName ?? "Course Document");
                if (PageNumber.HasValue && PageNumber.Value > 0)
                {
                    return $"{title} (Page {PageNumber.Value})";
                }
                return title;
            }
        }
    }

    public class ExtractedDocumentResult
    {
        public bool Success { get; set; }
        public DocumentProcessingStatus Status { get; set; } = DocumentProcessingStatus.Pending;
        public string? ErrorMessage { get; set; }
        public int PageCount { get; set; }
        public List<ExtractedDocumentPage> Pages { get; set; } = new();
        public List<ExtractedChunkDto> Chunks { get; set; } = new();
    }

    public class ExtractedDocumentPage
    {
        public int PageNumber { get; set; }
        public string? Heading { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    public class ExtractedChunkDto
    {
        public int ChunkIndex { get; set; }
        public int? PageNumber { get; set; }
        public string? Heading { get; set; }
        public string Content { get; set; } = string.Empty;
        public int WordCount { get; set; }
    }

    public class DocumentIndexResult
    {
        public bool Success { get; set; }
        public int LectureMaterialId { get; set; }
        public DocumentProcessingStatus Status { get; set; }
        public int TotalChunks { get; set; }
        public int PageCount { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsReprocessed { get; set; }
    }
}
