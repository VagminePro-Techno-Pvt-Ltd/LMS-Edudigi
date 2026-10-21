using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TMS.ViewModels.Academics
{
    public class QuestionOptionDto
    {
        public int Id { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }

    public class QuestionBankItemViewModel
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public int? SemesterId { get; set; }
        public string? SemesterName { get; set; }
        public int? SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public string? SubjectCode { get; set; }
        public int? UnitId { get; set; }
        public string? UnitName { get; set; }
        public int? TopicId { get; set; }
        public string? TopicName { get; set; }
        public string? LearningOutcome { get; set; }
        public string QuestionType { get; set; } = "MCQ"; // MCQ, Short Answer, Long Answer
        public string DifficultyLevel { get; set; } = "Medium"; // Easy, Medium, Hard
        public string QuestionText { get; set; } = string.Empty;
        public decimal Marks { get; set; } = 1;
        public decimal NegativeMarks { get; set; } = 0;
        public string? CorrectAnswer { get; set; }
        public string? Explanation { get; set; }
        public string Status { get; set; } = "Active";
        public bool IsActive { get; set; } = true;
        public int FacultyId { get; set; }
        public string? FacultyName { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public List<QuestionOptionDto> Options { get; set; } = new();
    }

    public class SuggestedQuestionRequestViewModel
    {
        [Required]
        public int CourseId { get; set; }

        public int? SemesterId { get; set; }

        [Required]
        public int SubjectId { get; set; }

        public int? UnitId { get; set; }

        public int? TopicId { get; set; }

        public string? LearningOutcome { get; set; }

        [Required]
        public string QuestionType { get; set; } = "MCQ"; // MCQ, Short Answer, Long Answer

        [Required]
        public string DifficultyLevel { get; set; } = "Medium"; // Easy, Medium, Hard

        [Range(1, 20)]
        public int QuestionCount { get; set; } = 5;

        [Range(0.5, 100)]
        public decimal MarksPerQuestion { get; set; } = 1;

        public string? TopicContext { get; set; }
    }

    public class SuggestedQuestionItemViewModel
    {
        public string TempId { get; set; } = Guid.NewGuid().ToString("N");
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = "MCQ";
        public string DifficultyLevel { get; set; } = "Medium";
        public decimal Marks { get; set; } = 1;
        public int? UnitId { get; set; }
        public string? UnitName { get; set; }
        public int? TopicId { get; set; }
        public string? TopicName { get; set; }
        public string? LearningOutcome { get; set; }
        public List<string> Options { get; set; } = new();
        public string CorrectAnswer { get; set; } = string.Empty;
        public string? Explanation { get; set; }
        public bool IsAccepted { get; set; }
        public bool IsRejected { get; set; }
    }

    public class SaveSuggestedQuestionsRequestViewModel
    {
        public int CourseId { get; set; }
        public int? SemesterId { get; set; }
        public int SubjectId { get; set; }
        public int? UnitId { get; set; }
        public int? TopicId { get; set; }
        public string? LearningOutcome { get; set; }
        public List<SuggestedQuestionItemViewModel> Questions { get; set; } = new();
    }

    public class QuestionPaperQuestionItemViewModel
    {
        public int QuestionBankId { get; set; }
        public string SectionName { get; set; } = "Section A"; // Section A, Section B, Section C
        public string? SectionInstructions { get; set; }
        public int DisplayOrder { get; set; } = 1;
        public decimal Marks { get; set; } = 1;
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = "MCQ";
        public string DifficultyLevel { get; set; } = "Medium";
        public string? LearningOutcome { get; set; }
        public List<QuestionOptionDto> Options { get; set; } = new();
        public string? CorrectAnswer { get; set; }
        public string? Explanation { get; set; }
    }

    public class QuestionPaperCreateViewModel
    {
        public int Id { get; set; } = 0;

        [Required]
        public int CourseId { get; set; }

        public int? SemesterId { get; set; }

        [Required]
        public int SubjectId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string AssessmentType { get; set; } = "Internal Assessment";

        [Range(1, 1000)]
        public decimal TotalMarks { get; set; } = 20;

        public int? DurationMinutes { get; set; } = 180;

        [StringLength(1000)]
        public string? Instructions { get; set; }

        public string Status { get; set; } = "Draft"; // Draft, Published

        public List<QuestionPaperQuestionItemViewModel> Questions { get; set; } = new();
    }

    public class QuestionPaperListViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public int? SemesterId { get; set; }
        public string? SemesterName { get; set; }
        public int? SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string AssessmentType { get; set; } = string.Empty;
        public decimal TotalMarks { get; set; }
        public decimal TotalAssignedMarks { get; set; }
        public int QuestionCount { get; set; }
        public string Status { get; set; } = "Draft";
        public DateTime CreatedOn { get; set; }
        public DateTime? PublishedOn { get; set; }
        public string FacultyName { get; set; } = string.Empty;
    }

    public class QuestionPaperDetailViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public int? SemesterId { get; set; }
        public string? SemesterName { get; set; }
        public int? SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public string AssessmentType { get; set; } = string.Empty;
        public decimal TotalMarks { get; set; }
        public int? DurationMinutes { get; set; } = 180;
        public string? Instructions { get; set; }
        public string Status { get; set; } = "Draft";
        public DateTime CreatedOn { get; set; }
        public DateTime? PublishedOn { get; set; }
        public string FacultyName { get; set; } = string.Empty;
        public List<QuestionPaperQuestionItemViewModel> Questions { get; set; } = new();
    }

    public class QuestionIdDto
    {
        public int Id { get; set; }
    }

    public class QuestionPaperIdDto
    {
        public int Id { get; set; }
    }

    public class QuestionPaperPublishDto
    {
        public int Id { get; set; }
    }

    public class RegenerateQuestionRequestDto
    {
        public SuggestedQuestionItemViewModel Question { get; set; } = new();
        public SuggestedQuestionRequestViewModel Context { get; set; } = new();
    }

    public class DuplicateCheckRequestDto
    {
        public int CourseId { get; set; }
        public int SubjectId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public int? ExcludeQuestionId { get; set; }
    }

    public class DuplicateCheckResultDto
    {
        public bool IsDuplicate { get; set; }
        public string DuplicateType { get; set; } = "None"; // Exact, Normalized, Similar, None
        public int? ExistingQuestionId { get; set; }
        public string? ExistingQuestionText { get; set; }
        public double SimilarityScore { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class QuestionBankAnalyticsViewModel
    {
        public int TotalQuestions { get; set; }
        public Dictionary<string, int> QuestionsBySubject { get; set; } = new();
        public Dictionary<string, int> QuestionsByDifficulty { get; set; } = new();
        public Dictionary<string, int> QuestionsByUnit { get; set; } = new();
        public Dictionary<string, int> QuestionsByType { get; set; } = new();
        public Dictionary<string, int> QuestionsByLearningOutcome { get; set; } = new();
    }

    public class QuestionBankFilterRequestDto
    {
        public int? CourseId { get; set; }
        public int? SemesterId { get; set; }
        public int? SubjectId { get; set; }
        public int? UnitId { get; set; }
        public int? TopicId { get; set; }
        public string? QuestionType { get; set; }
        public string? DifficultyLevel { get; set; }
        public string? LearningOutcome { get; set; }
        public string? Search { get; set; }
        public decimal? MinMarks { get; set; }
        public decimal? MaxMarks { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class PagedQuestionBankResultDto
    {
        public List<QuestionBankItemViewModel> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / (PageSize > 0 ? PageSize : 10));
    }

    // ─── FEATURE 25: AI QUESTION PAPER BLUEPRINT & AUTO-ASSEMBLY ───
    public class PaperBlueprintSectionDto
    {
        public string SectionName { get; set; } = "Section A"; // Section A, Section B, Section C
        public string? SectionInstructions { get; set; } // e.g., "Answer all 10 questions. Each question carries 1 mark."
        public string QuestionType { get; set; } = "MCQ"; // MCQ, Short Answer, Long Answer
        public int NumberOfQuestions { get; set; } = 5;
        public decimal MarksPerQuestion { get; set; } = 1;
        public string? DifficultyLevel { get; set; } = "Easy"; // Easy, Medium, Hard, Any
        public string? LearningOutcome { get; set; } // Bloom's Taxonomy / Course Outcome
        public int? UnitId { get; set; }
        public int? TopicId { get; set; }
    }

    public class AutoAssemblePaperRequestDto
    {
        [Required]
        public int CourseId { get; set; }
        public int? SemesterId { get; set; }
        public int? SubjectId { get; set; }
        [Required]
        public string Title { get; set; } = string.Empty;
        public string AssessmentType { get; set; } = "Internal Assessment";
        public int? DurationMinutes { get; set; } = 180;
        public string? Instructions { get; set; }
        public List<PaperBlueprintSectionDto> Sections { get; set; } = new();
        public bool AutoGenerateMissingWithAi { get; set; } = false;
    }

    public class AutoAssemblePaperResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? QuestionPaperId { get; set; }
        public decimal TotalMarks { get; set; }
        public int TotalQuestions { get; set; }
        public int ExistingQuestionsUsed { get; set; }
        public int AiGeneratedQuestionsCount { get; set; }
        public List<string> Warnings { get; set; } = new();
        public List<SuggestedQuestionItemViewModel> GeneratedQuestionsNeedingApproval { get; set; } = new();
    }

    // ─── FEATURE 26: QUESTION BANK INTELLIGENCE & CLASSIFICATION ───
    public class LegacyQuestionToClassifyDto
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string? CurrentType { get; set; }
        public decimal Marks { get; set; }
    }

    public class QuestionClassificationSuggestionDto
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public int? SuggestedUnitId { get; set; }
        public string? SuggestedUnitName { get; set; }
        public int? SuggestedTopicId { get; set; }
        public string? SuggestedTopicName { get; set; }
        public string SuggestedDifficultyLevel { get; set; } = "Medium";
        public string? SuggestedLearningOutcome { get; set; }
        public string Rationale { get; set; } = string.Empty;
    }

    public class QuestionClassificationApplyDto
    {
        public int QuestionId { get; set; }
        public int? UnitId { get; set; }
        public int? TopicId { get; set; }
        public string? DifficultyLevel { get; set; }
        public string? LearningOutcome { get; set; }
    }

    public class BatchClassifyQuestionsRequestDto
    {
        public int CourseId { get; set; }
        public int SubjectId { get; set; }
        public List<int> QuestionIds { get; set; } = new();
    }

    public class BulkQuestionImportRowDto
    {
        public int RowNumber { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = "MCQ";
        public string DifficultyLevel { get; set; } = "Medium";
        public decimal Marks { get; set; } = 1;
        public decimal NegativeMarks { get; set; } = 0;
        public string? LearningOutcome { get; set; }
        public string? UnitName { get; set; }
        public string? TopicName { get; set; }
        public string? OptionA { get; set; }
        public string? OptionB { get; set; }
        public string? OptionC { get; set; }
        public string? OptionD { get; set; }
        public string? CorrectOption { get; set; } // A, B, C, D or exact text
        public string? Explanation { get; set; }
        public bool IsValid { get; set; } = true;
        public string? ErrorMessage { get; set; }
    }

    public class BulkQuestionImportResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public int ImportedCount { get; set; }
        public int DuplicateCount { get; set; }
        public int ErrorCount { get; set; }
        public List<string> RowErrors { get; set; } = new();
    }
}
