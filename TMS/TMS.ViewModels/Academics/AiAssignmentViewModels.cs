using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;

namespace TMS.ViewModels.Academics
{
    /// <summary>
    /// Request payload for generating an assignment using Gemini AI.
    /// </summary>
    public class AiAssignmentGenerateRequest
    {
        [Required]
        public int ProgramId { get; set; }

        public int? SemesterId { get; set; }

        [Required]
        public int CourseId { get; set; }

        public int? SubjectId { get; set; }

        public int? UnitId { get; set; }

        public int? TopicId { get; set; }

        [Required, StringLength(50)]
        public string Difficulty { get; set; } = "Medium"; // Easy, Medium, Hard

        [Required, StringLength(100)]
        public string AssignmentType { get; set; } = "Theory / Descriptive";

        [Range(1, 15)]
        public int NumberOfQuestions { get; set; } = 5;

        [Range(5, 500)]
        public decimal TotalMarks { get; set; } = 50;

        public string? SubmissionInstructions { get; set; }

        public DateTime? DueDate { get; set; }

        public decimal? PassMarks { get; set; }

        /// <summary>
        /// Optional topic context if not registered in syllabus
        /// </summary>
        public string? CustomTopic { get; set; }
    }

    /// <summary>
    /// Single question item in an AI-generated assignment.
    /// </summary>
    public class AiAssignmentQuestionItem
    {
        public int QuestionNumber { get; set; }

        public string QuestionText { get; set; } = string.Empty;

        public decimal Marks { get; set; }

        public string? ExpectedAnswerGuidelines { get; set; }
    }

    /// <summary>
    /// High-level marking criteria breakdown.
    /// </summary>
    public class AiMarkingCriterionItem
    {
        public string Criterion { get; set; } = string.Empty;

        public decimal Weightage { get; set; }

        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Detailed 4-level evaluation rubric row.
    /// </summary>
    public class AiAssignmentRubricRow
    {
        public string Criterion { get; set; } = string.Empty;

        public decimal MaxMarks { get; set; }

        public string Excellent { get; set; } = string.Empty; // 80-100%

        public string Good { get; set; } = string.Empty;      // 65-79%

        public string Fair { get; set; } = string.Empty;      // 50-64%

        public string Poor { get; set; } = string.Empty;      // 0-49%
    }

    /// <summary>
    /// Output result of the AI assignment generation.
    /// </summary>
    public class AiAssignmentGenerateResult
    {
        public bool Success { get; set; }

        public string? ErrorMessage { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Instructions { get; set; } = string.Empty;

        public decimal TotalMarks { get; set; }

        public decimal PassMarks { get; set; }

        public List<AiAssignmentQuestionItem> Questions { get; set; } = new();

        public List<AiMarkingCriterionItem> MarkingCriteria { get; set; } = new();

        public List<AiAssignmentRubricRow> Rubrics { get; set; } = new();

        public int ProgramId { get; set; }

        public int CourseId { get; set; }

        public int? SemesterId { get; set; }

        public int? SubjectId { get; set; }

        public int? UnitId { get; set; }

        public int? TopicId { get; set; }

        public string Difficulty { get; set; } = "Medium";

        public string AssignmentType { get; set; } = "Theory / Descriptive";
    }

    /// <summary>
    /// Request to persist or publish an assignment.
    /// </summary>
    public class SaveAiAssignmentRequest
    {
        public int? AssignmentId { get; set; }

        [Required]
        public int ProgramId { get; set; }

        [Required]
        public int CourseId { get; set; }

        public int? SemesterId { get; set; }

        public int? SubjectId { get; set; }

        public int? UnitId { get; set; }

        public int? TopicId { get; set; }

        public string Difficulty { get; set; } = "Medium";

        public string AssignmentType { get; set; } = "Theory / Descriptive";

        [Required, StringLength(250)]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Instructions { get; set; } = string.Empty;

        [Required]
        public DateTime DueDate { get; set; }

        [Required]
        public decimal TotalMarks { get; set; }

        [Required]
        public decimal PassMarks { get; set; }

        public bool AllowLateSubmission { get; set; } = false;

        public decimal LatePenalty { get; set; } = 0;

        public List<AiAssignmentQuestionItem> Questions { get; set; } = new();

        public List<AiMarkingCriterionItem> MarkingCriteria { get; set; } = new();

        public List<AiAssignmentRubricRow> Rubrics { get; set; } = new();

        /// <summary>
        /// true = Publish (visible to students, IsActive = true)
        /// false = Save Draft (hidden from students, IsActive = false)
        /// </summary>
        public bool Publish { get; set; }
    }

    /// <summary>
    /// Structured metadata stored alongside assignment text.
    /// </summary>
    public class AiAssignmentMetadata
    {
        public int? SemesterId { get; set; }
        public string? SemesterName { get; set; }
        public int? SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public int? UnitId { get; set; }
        public string? UnitName { get; set; }
        public int? TopicId { get; set; }
        public string? TopicName { get; set; }
        public string? Difficulty { get; set; }
        public string? AssignmentType { get; set; }
        public string? Instructions { get; set; }
        public List<AiAssignmentQuestionItem> Questions { get; set; } = new();
        public List<AiMarkingCriterionItem> MarkingCriteria { get; set; } = new();
        public List<AiAssignmentRubricRow> Rubrics { get; set; } = new();
        public bool IsAiGenerated { get; set; } = true;
        public string Status { get; set; } = "Draft"; // "Draft", "Published"
        public DateTime? PublishedOn { get; set; }

        private const string MetadataStart = "<!-- AI_ASSIGNMENT_METADATA_START -->";
        private const string MetadataEnd = "<!-- AI_ASSIGNMENT_METADATA_END -->";

        /// <summary>
        /// Formats clean plain text human-readable assignment description and embeds structured metadata JSON.
        /// </summary>
        public static string BuildDescriptionWithMetadata(
            string summary,
            string instructions,
            List<AiAssignmentQuestionItem> questions,
            List<AiMarkingCriterionItem> criteria,
            List<AiAssignmentRubricRow> rubrics,
            AiAssignmentMetadata metadata)
        {
            var sb = new System.Text.StringBuilder();

            // 1. Human-Readable Overview
            sb.AppendLine("=== ASSIGNMENT OVERVIEW ===");
            sb.AppendLine(summary?.Trim() ?? "Curriculum-aligned course assignment.");
            sb.AppendLine();

            // 2. Student Instructions
            if (!string.IsNullOrWhiteSpace(instructions))
            {
                sb.AppendLine("=== STUDENT INSTRUCTIONS ===");
                sb.AppendLine(instructions.Trim());
                sb.AppendLine();
            }

            // 3. Questions
            if (questions != null && questions.Any())
            {
                sb.AppendLine("=== QUESTIONS ===");
                foreach (var q in questions.OrderBy(x => x.QuestionNumber))
                {
                    sb.AppendLine($"[Question {q.QuestionNumber}] ({q.Marks} Marks)");
                    sb.AppendLine(q.QuestionText?.Trim() ?? string.Empty);
                    if (!string.IsNullOrWhiteSpace(q.ExpectedAnswerGuidelines))
                    {
                        sb.AppendLine($"Guidelines: {q.ExpectedAnswerGuidelines.Trim()}");
                    }
                    sb.AppendLine();
                }
            }

            // 4. Marking Criteria
            if (criteria != null && criteria.Any())
            {
                sb.AppendLine("=== MARKING CRITERIA ===");
                foreach (var c in criteria)
                {
                    sb.AppendLine($"• {c.Criterion} ({c.Weightage}%): {c.Description}");
                }
                sb.AppendLine();
            }

            // 5. Rubrics
            if (rubrics != null && rubrics.Any())
            {
                sb.AppendLine("=== EVALUATION RUBRICS ===");
                foreach (var r in rubrics)
                {
                    sb.AppendLine($"• {r.Criterion} (Max {r.MaxMarks} Marks)");
                    sb.AppendLine($"  - Excellent (80-100%): {r.Excellent}");
                    sb.AppendLine($"  - Good (65-79%): {r.Good}");
                    sb.AppendLine($"  - Fair (50-64%): {r.Fair}");
                    sb.AppendLine($"  - Poor (0-49%): {r.Poor}");
                }
                sb.AppendLine();
            }

            // 6. Embed structured JSON block
            metadata.Questions = questions ?? new();
            metadata.MarkingCriteria = criteria ?? new();
            metadata.Rubrics = rubrics ?? new();
            metadata.Instructions = instructions;

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var json = JsonSerializer.Serialize(metadata, jsonOptions);

            sb.AppendLine(MetadataStart);
            sb.AppendLine(json);
            sb.AppendLine(MetadataEnd);

            return sb.ToString();
        }

        /// <summary>
        /// Parses the full description into a clean human-readable text and optional structured metadata.
        /// </summary>
        public static (string cleanText, AiAssignmentMetadata? metadata) ParseDescription(string? fullDescription)
        {
            if (string.IsNullOrWhiteSpace(fullDescription))
            {
                return (string.Empty, null);
            }

            var startIdx = fullDescription.IndexOf(MetadataStart, StringComparison.Ordinal);
            if (startIdx < 0)
            {
                return (fullDescription.Trim(), null);
            }

            var cleanText = fullDescription.Substring(0, startIdx).Trim();
            var endIdx = fullDescription.IndexOf(MetadataEnd, startIdx, StringComparison.Ordinal);
            if (endIdx < 0)
            {
                return (cleanText, null);
            }

            var jsonStart = startIdx + MetadataStart.Length;
            var json = fullDescription.Substring(jsonStart, endIdx - jsonStart).Trim();

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var meta = JsonSerializer.Deserialize<AiAssignmentMetadata>(json, options);
                return (cleanText, meta);
            }
            catch
            {
                return (cleanText, null);
            }
        }

        /// <summary>
        /// Extracts only the overview text, removing structured section markers, instructions, and questions.
        /// Prevents duplicated assignment content in the overview section.
        /// </summary>
        public static string ExtractOverview(string? description)
        {
            if (string.IsNullOrWhiteSpace(description)) return string.Empty;

            var startIdx = description.IndexOf(MetadataStart, StringComparison.Ordinal);
            var text = startIdx >= 0 ? description.Substring(0, startIdx).Trim() : description.Trim();

            var overviewMarker = "=== ASSIGNMENT OVERVIEW ===";
            while (text.StartsWith(overviewMarker, StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring(overviewMarker.Length).TrimStart();
            }

            var overviewIdx = text.IndexOf(overviewMarker, StringComparison.OrdinalIgnoreCase);
            if (overviewIdx >= 0)
            {
                text = text.Substring(overviewIdx + overviewMarker.Length).TrimStart();
                while (text.StartsWith(overviewMarker, StringComparison.OrdinalIgnoreCase))
                {
                    text = text.Substring(overviewMarker.Length).TrimStart();
                }
            }

            var nextSectionIdx = text.IndexOf("===", StringComparison.Ordinal);
            if (nextSectionIdx >= 0)
            {
                return text.Substring(0, nextSectionIdx).Trim();
            }

            return text.Trim();
        }

        /// <summary>
        /// Extracts instructions from plain text if structured metadata was absent.
        /// </summary>
        public static string ExtractInstructions(string? description)
        {
            if (string.IsNullOrWhiteSpace(description)) return string.Empty;

            var marker = "=== STUDENT INSTRUCTIONS ===";
            var idx = description.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return string.Empty;

            var contentStart = idx + marker.Length;
            var nextSectionIdx = description.IndexOf("===", contentStart, StringComparison.Ordinal);
            if (nextSectionIdx >= 0)
            {
                return description.Substring(contentStart, nextSectionIdx - contentStart).Trim();
            }

            var metaIdx = description.IndexOf(MetadataStart, contentStart, StringComparison.Ordinal);
            if (metaIdx >= 0)
            {
                return description.Substring(contentStart, metaIdx - contentStart).Trim();
            }

            return description.Substring(contentStart).Trim();
        }
    }

    /// <summary>
    /// View model for the institutional printable/export A4 assignment paper.
    /// </summary>
    public class PrintAssignmentViewModel
    {
        public int AssignmentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ProgramName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public string? SemesterName { get; set; }
        public string? SubjectName { get; set; }
        public string? UnitName { get; set; }
        public string? TopicName { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TotalMarks { get; set; }
        public decimal PassMarks { get; set; }
        public string Difficulty { get; set; } = "Medium";
        public string AssignmentType { get; set; } = "Theory / Descriptive";
        public string Description { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public List<AiAssignmentQuestionItem> Questions { get; set; } = new();
        public List<AiMarkingCriterionItem> MarkingCriteria { get; set; } = new();
        public List<AiAssignmentRubricRow> Rubrics { get; set; } = new();
        public bool IsPublished { get; set; }
        public bool IsFacultyView { get; set; } = true;
    }
}

