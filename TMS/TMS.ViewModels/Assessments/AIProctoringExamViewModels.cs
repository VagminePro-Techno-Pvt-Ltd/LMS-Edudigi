using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TMS.ViewModels.Assessments
{
    public class ProctorExamDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("durationMinutes")]
        public int DurationMinutes { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("quizId")]
        public int? QuizId { get; set; }

        [JsonPropertyName("startTime")]
        public DateTime? StartTime { get; set; }

        [JsonPropertyName("endTime")]
        public DateTime? EndTime { get; set; }

        [JsonPropertyName("scheduledStart")]
        public DateTime? ScheduledStart { get; set; }

        [JsonPropertyName("scheduledEnd")]
        public DateTime? ScheduledEnd { get; set; }

        [JsonPropertyName("instructions")]
        public string? Instructions { get; set; }

        [JsonPropertyName("passingPercentage")]
        public decimal PassingPercentage { get; set; } = 40;

        [JsonPropertyName("negativeMarking")]
        public bool NegativeMarking { get; set; } = false;

        [JsonPropertyName("shuffleQuestions")]
        public bool ShuffleQuestions { get; set; } = false;

        [JsonPropertyName("showResultInstantly")]
        public bool ShowResultInstantly { get; set; } = false;

        [JsonPropertyName("maxAttempts")]
        public int MaxAttempts { get; set; } = 1;

        [JsonPropertyName("examType")]
        public string ExamType { get; set; } = "Standard";

        [JsonPropertyName("totalMarks")]
        public decimal TotalMarks { get; set; } = 0;

        [JsonPropertyName("requiresSEB")]
        public bool RequiresSEB { get; set; } = true;

        [JsonPropertyName("browserExamKey")]
        public string? BrowserExamKey { get; set; }

        [JsonPropertyName("allowRevisit")]
        public bool AllowRevisit { get; set; } = true;

        [JsonPropertyName("aiEvaluationEnabled")]
        public bool AiEvaluationEnabled { get; set; } = true;

        [JsonPropertyName("aiEvaluationDelay")]
        public int AiEvaluationDelay { get; set; } = 0;

        [JsonPropertyName("aiEvaluationDelayUnit")]
        public string AiEvaluationDelayUnit { get; set; } = "Hours";

        [JsonPropertyName("targetCourseId")]
        public int? TargetCourseId { get; set; }

        [JsonPropertyName("targetCourseName")]
        public string? TargetCourseName { get; set; }

        [JsonPropertyName("targetBatch")]
        public string? TargetBatch { get; set; }

        [JsonPropertyName("targetSection")]
        public string? TargetSection { get; set; }

        [JsonPropertyName("targetDepartment")]
        public string? TargetDepartment { get; set; }

        [JsonPropertyName("targetSemester")]
        public string? TargetSemester { get; set; }

        [JsonPropertyName("isGlobal")]
        public bool IsGlobal { get; set; } = false;

        [JsonPropertyName("createdOn")]
        public DateTime? CreatedOn { get; set; }
    }

    public class CreateProctorExamRequest
    {
        [Required]
        [StringLength(200)]
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [Required]
        [Range(1, 600)]
        [JsonPropertyName("durationMinutes")]
        public int DurationMinutes { get; set; } = 60;

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;

        [JsonPropertyName("startTime")]
        public DateTime? StartTime { get; set; }

        [JsonPropertyName("endTime")]
        public DateTime? EndTime { get; set; }

        [JsonPropertyName("instructions")]
        public string? Instructions { get; set; }

        [JsonPropertyName("passingPercentage")]
        public decimal PassingPercentage { get; set; } = 40;

        [JsonPropertyName("negativeMarking")]
        public bool NegativeMarking { get; set; } = false;

        [JsonPropertyName("shuffleQuestions")]
        public bool ShuffleQuestions { get; set; } = false;

        [JsonPropertyName("showResultInstantly")]
        public bool ShowResultInstantly { get; set; } = false;

        [JsonPropertyName("maxAttempts")]
        public int MaxAttempts { get; set; } = 1;

        [JsonPropertyName("examType")]
        public string ExamType { get; set; } = "Standard";

        [JsonPropertyName("totalMarks")]
        public decimal TotalMarks { get; set; } = 100;

        [JsonPropertyName("targetCourseId")]
        public int? TargetCourseId { get; set; }

        [JsonPropertyName("targetCourseName")]
        public string? TargetCourseName { get; set; }

        [JsonPropertyName("targetBatch")]
        public string? TargetBatch { get; set; }

        [JsonPropertyName("targetSection")]
        public string? TargetSection { get; set; }

        [JsonPropertyName("targetDepartment")]
        public string? TargetDepartment { get; set; }

        [JsonPropertyName("targetSemester")]
        public string? TargetSemester { get; set; }

        [JsonPropertyName("isGlobal")]
        public bool IsGlobal { get; set; } = false;

        [JsonPropertyName("requiresSEB")]
        public bool RequiresSEB { get; set; } = true;

        [JsonPropertyName("browserExamKey")]
        public string? BrowserExamKey { get; set; }

        [JsonPropertyName("allowRevisit")]
        public bool AllowRevisit { get; set; } = true;

        [JsonPropertyName("aiEvaluationEnabled")]
        public bool AiEvaluationEnabled { get; set; } = true;

        [JsonPropertyName("aiEvaluationDelay")]
        public int? AiEvaluationDelay { get; set; } = 0;

        [JsonPropertyName("aiEvaluationDelayUnit")]
        public string? AiEvaluationDelayUnit { get; set; } = "Hours";
    }

    public class UpdateProctorExamDto
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("durationMinutes")]
        public int? DurationMinutes { get; set; }

        [JsonPropertyName("startTime")]
        public DateTime? StartTime { get; set; }

        [JsonPropertyName("isActive")]
        public bool? IsActive { get; set; }

        [JsonPropertyName("targetCourseId")]
        public int? TargetCourseId { get; set; }

        [JsonPropertyName("targetCourseName")]
        public string? TargetCourseName { get; set; }

        [JsonPropertyName("targetBatch")]
        public string? TargetBatch { get; set; }

        [JsonPropertyName("targetSection")]
        public string? TargetSection { get; set; }

        [JsonPropertyName("targetDepartment")]
        public string? TargetDepartment { get; set; }

        [JsonPropertyName("targetSemester")]
        public string? TargetSemester { get; set; }

        [JsonPropertyName("isGlobal")]
        public bool? IsGlobal { get; set; }

        [JsonPropertyName("aiEvaluationEnabled")]
        public bool? AiEvaluationEnabled { get; set; }

        [JsonPropertyName("aiEvaluationDelay")]
        public int? AiEvaluationDelay { get; set; }

        [JsonPropertyName("aiEvaluationDelayUnit")]
        public string? AiEvaluationDelayUnit { get; set; }
    }

    public class ExamResultRowDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("candidateId")]
        public string CandidateId { get; set; } = string.Empty;

        [JsonPropertyName("candidateName")]
        public string? CandidateName { get; set; }

        [JsonPropertyName("candidateEmail")]
        public string? CandidateEmail { get; set; }

        [JsonPropertyName("attemptId")]
        public Guid AttemptId { get; set; }

        [JsonPropertyName("totalQuestions")]
        public int TotalQuestions { get; set; }

        [JsonPropertyName("attempted")]
        public int Attempted { get; set; }

        [JsonPropertyName("correct")]
        public int Correct { get; set; }

        [JsonPropertyName("wrong")]
        public int Wrong { get; set; }

        [JsonPropertyName("score")]
        public decimal Score { get; set; }

        [JsonPropertyName("totalMarks")]
        public decimal TotalMarks { get; set; }

        [JsonPropertyName("percentage")]
        public decimal Percentage { get; set; }

        [JsonPropertyName("isPassed")]
        public bool? IsPassed { get; set; }

        [JsonPropertyName("isPublished")]
        public bool IsPublished { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "Completed";

        [JsonPropertyName("evaluatedAt")]
        public DateTime? EvaluatedAt { get; set; }
    }

    public class ExamResultsResponseDto
    {
        [JsonPropertyName("examTitle")]
        public string ExamTitle { get; set; } = string.Empty;

        [JsonPropertyName("totalMarks")]
        public decimal TotalMarks { get; set; }

        [JsonPropertyName("passingPercentage")]
        public decimal PassingPercentage { get; set; }

        [JsonPropertyName("results")]
        public List<ExamResultRowDto> Results { get; set; } = new();
    }

    public class CandidateScriptAnswerDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("questionId")]
        public Guid QuestionId { get; set; }

        [JsonPropertyName("questionType")]
        public string QuestionType { get; set; } = "MCQ";

        [JsonPropertyName("answerText")]
        public string? AnswerText { get; set; }

        [JsonPropertyName("codeContent")]
        public string? CodeContent { get; set; }

        [JsonPropertyName("codeLanguage")]
        public string? CodeLanguage { get; set; }

        [JsonPropertyName("fileUrl")]
        public string? FileUrl { get; set; }

        [JsonPropertyName("fileName")]
        public string? FileName { get; set; }

        [JsonPropertyName("marksAwarded")]
        public decimal MarksAwarded { get; set; }

        [JsonPropertyName("isAutoGraded")]
        public bool IsAutoGraded { get; set; }

        [JsonPropertyName("isManuallyGraded")]
        public bool IsManuallyGraded { get; set; }

        [JsonPropertyName("gradingRemarks")]
        public string? GradingRemarks { get; set; }

        [JsonPropertyName("gradedAt")]
        public DateTime? GradedAt { get; set; }

        [JsonPropertyName("questionText")]
        public string QuestionText { get; set; } = string.Empty;

        [JsonPropertyName("totalMarks")]
        public decimal TotalMarks { get; set; }

        [JsonPropertyName("questionData")]
        public string? QuestionData { get; set; }
    }

    public class CandidateScriptResponseDto
    {
        [JsonPropertyName("examTitle")]
        public string ExamTitle { get; set; } = string.Empty;

        [JsonPropertyName("candidateId")]
        public string CandidateId { get; set; } = string.Empty;

        [JsonPropertyName("candidateName")]
        public string? CandidateName { get; set; }

        [JsonPropertyName("candidateEmail")]
        public string? CandidateEmail { get; set; }

        [JsonPropertyName("attemptId")]
        public Guid? AttemptId { get; set; }

        [JsonPropertyName("totalScore")]
        public decimal TotalScore { get; set; }

        [JsonPropertyName("totalQuestions")]
        public int TotalQuestions { get; set; }

        [JsonPropertyName("evaluatedAt")]
        public DateTime? EvaluatedAt { get; set; }

        [JsonPropertyName("answers")]
        public List<CandidateScriptAnswerDto> Answers { get; set; } = new();
    }

    public class ExamQuestionItemDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("examId")]
        public Guid ExamId { get; set; }

        [JsonPropertyName("questionText")]
        public string QuestionText { get; set; } = string.Empty;

        [JsonPropertyName("questionType")]
        public string QuestionType { get; set; } = "MCQ";

        [JsonPropertyName("marks")]
        public decimal Marks { get; set; } = 1;

        [JsonPropertyName("negativeMarks")]
        public decimal? NegativeMarks { get; set; } = 0;

        [JsonPropertyName("difficulty")]
        public string? Difficulty { get; set; } = "Medium";

        [JsonPropertyName("optionA")]
        public string? OptionA { get; set; }

        [JsonPropertyName("optionB")]
        public string? OptionB { get; set; }

        [JsonPropertyName("optionC")]
        public string? OptionC { get; set; }

        [JsonPropertyName("optionD")]
        public string? OptionD { get; set; }

        [JsonPropertyName("correctOption")]
        public string? CorrectOption { get; set; }

        [JsonPropertyName("sortOrder")]
        public int SortOrder { get; set; }

        [JsonPropertyName("answerText")]
        public string? AnswerText { get; set; }

        [JsonPropertyName("codeSnippet")]
        public string? CodeSnippet { get; set; }

        [JsonPropertyName("codeLanguage")]
        public string? CodeLanguage { get; set; }

        [JsonPropertyName("formula")]
        public string? Formula { get; set; }

        [JsonPropertyName("variableRanges")]
        public string? VariableRanges { get; set; }

        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; set; }

        [JsonPropertyName("tolerance")]
        public string? Tolerance { get; set; }

        [JsonPropertyName("unit")]
        public string? Unit { get; set; }

        [JsonPropertyName("testCases")]
        public List<CreateCodingTestCaseDto>? TestCases { get; set; }
    }

    public class CandidateItemDto
    {
        [JsonPropertyName("candidateId")]
        public string CandidateId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("loginID")]
        public string? LoginID { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "Active";
    }

    public class ExamAssignmentItemDto
    {
        [JsonPropertyName("assignmentID")]
        public Guid AssignmentID { get; set; }

        [JsonPropertyName("examID")]
        public Guid ExamID { get; set; }

        [JsonPropertyName("candidateID")]
        public Guid CandidateID { get; set; }

        [JsonPropertyName("proctorID")]
        public Guid? ProctorID { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "Assigned";

        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate { get; set; }
    }

    public class CreateExamAssignmentDto
    {
        [JsonPropertyName("examID")]
        public Guid ExamID { get; set; }

        [JsonPropertyName("candidateID")]
        public Guid CandidateID { get; set; }

        [JsonPropertyName("proctorID")]
        public Guid? ProctorID { get; set; }
    }

    public class ViolationItemDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("examId")]
        public Guid ExamId { get; set; }

        [JsonPropertyName("candidateId")]
        public string CandidateId { get; set; } = string.Empty;

        [JsonPropertyName("attemptId")]
        public string? AttemptId { get; set; }

        [JsonPropertyName("violationType")]
        public string ViolationType { get; set; } = string.Empty;

        [JsonPropertyName("severity")]
        public string Severity { get; set; } = "Medium";

        [JsonPropertyName("details")]
        public string? Details { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        public string DisplayDetails => !string.IsNullOrEmpty(Details) ? Details : (!string.IsNullOrEmpty(Description) ? Description : ViolationType);

        [JsonPropertyName("evidenceUrl")]
        public string? EvidenceUrl { get; set; }

        [JsonPropertyName("sourceType")]
        public string SourceType { get; set; } = "WEBCAM";

        [JsonPropertyName("actionTaken")]
        public string? ActionTaken { get; set; }

        [JsonPropertyName("isReviewed")]
        public bool IsReviewed { get; set; }

        [JsonPropertyName("reviewNotes")]
        public string? ReviewNotes { get; set; }

        [JsonPropertyName("occurredAt")]
        public DateTime? OccurredAt { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        public DateTime DisplayTime => OccurredAt ?? CreatedAt ?? DateTime.UtcNow;
    }

    public class ExamDashboardViewModel
    {
        public int TotalExams { get; set; }
        public int DraftExams { get; set; }
        public int PublishedExams { get; set; }
        public int ScheduledExams { get; set; }
        public int ActiveExams { get; set; }
        public int CompletedExams { get; set; }
        public int TotalAttempts { get; set; }
        public decimal AveragePassPercentage { get; set; }

        public List<ProctorExamDto> Exams { get; set; } = new();
        public List<ExamResultRowDto> RecentSubmissions { get; set; } = new();

        // Filters
        public string? SearchQuery { get; set; }
        public string? StatusFilter { get; set; }
        public string? CourseFilter { get; set; }
    }

    public class ExamDetailsHubViewModel
    {
        public ProctorExamDto Exam { get; set; } = new();
        public List<ExamQuestionItemDto> Questions { get; set; } = new();
        public List<CandidateItemDto> EligibleCandidates { get; set; } = new();
        public List<ExamAssignmentItemDto> DirectAssignments { get; set; } = new();
        public List<ExamResultRowDto> Results { get; set; } = new();
        public List<ViolationItemDto> Violations { get; set; } = new();

        public decimal TotalCalculatedMarks { get; set; }
        public int AttemptCount { get; set; }
        public int PassedCount { get; set; }
        public int FailedCount { get; set; }

        public string ActiveTab { get; set; } = "overview";
    }

    public class MasterDataResultDto
    {
        public List<CourseItemDto> Courses { get; set; } = new();
        public List<DepartmentItemDto> Departments { get; set; } = new();
        public List<SemesterItemDto> Semesters { get; set; } = new();
        public List<BatchItemDto> Batches { get; set; } = new();
    }

    public class CourseItemDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("courseCode")]
        public string? CourseCode { get; set; }
    }

    public class DepartmentItemDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class SemesterItemDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class BatchItemDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class CreateCodingTestCaseDto
    {
        [JsonPropertyName("inputData")]
        public string? InputData { get; set; }

        [JsonPropertyName("expectedOutput")]
        public string ExpectedOutput { get; set; } = string.Empty;

        [JsonPropertyName("isHidden")]
        public bool IsHidden { get; set; }

        [JsonPropertyName("weightage")]
        public decimal Weightage { get; set; } = 1;

        [JsonPropertyName("orderIndex")]
        public int OrderIndex { get; set; } = 1;

        [JsonPropertyName("explanation")]
        public string? Explanation { get; set; }
    }

    public class QuestionBankItemDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("questionType")]
        public string QuestionType { get; set; } = "MCQ";

        [JsonPropertyName("questionText")]
        public string QuestionText { get; set; } = string.Empty;

        [JsonPropertyName("categoryId")]
        public Guid? CategoryId { get; set; }

        [JsonPropertyName("categoryName")]
        public string? CategoryName { get; set; }

        [JsonPropertyName("difficultyLevel")]
        public string? DifficultyLevel { get; set; } = "Medium";

        [JsonPropertyName("marks")]
        public decimal Marks { get; set; } = 1;

        [JsonPropertyName("negativeMarks")]
        public decimal? NegativeMarks { get; set; }

        [JsonPropertyName("partialMarking")]
        public bool PartialMarking { get; set; }

        [JsonPropertyName("isAutoGraded")]
        public bool IsAutoGraded { get; set; }

        [JsonPropertyName("questionData")]
        public string? QuestionData { get; set; }

        [JsonPropertyName("explanation")]
        public string? Explanation { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }
    }

    public class QuestionBankResultDto
    {
        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; } = 1;

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; } = 50;

        [JsonPropertyName("items")]
        public List<QuestionBankItemDto> Items { get; set; } = new();
    }

    public class AddQuestionToExamDto
    {
        [JsonPropertyName("questionId")]
        public Guid QuestionId { get; set; }

        [JsonPropertyName("sortOrder")]
        public int SortOrder { get; set; } = 0;

        [JsonPropertyName("overrideMarks")]
        public decimal? OverrideMarks { get; set; }

        [JsonPropertyName("isRequired")]
        public bool IsRequired { get; set; } = true;

        [JsonPropertyName("sectionName")]
        public string? SectionName { get; set; }
    }

    public class ExtractedQuestionDto
    {
        [JsonPropertyName("questionText")]
        public string QuestionText { get; set; } = string.Empty;

        [JsonPropertyName("questionType")]
        public string QuestionType { get; set; } = "MCQ_SINGLE";

        [JsonPropertyName("difficultyLevel")]
        public string DifficultyLevel { get; set; } = "Medium";

        [JsonPropertyName("marks")]
        public decimal Marks { get; set; } = 1;

        [JsonPropertyName("negativeMarks")]
        public decimal NegativeMarks { get; set; } = 0;

        [JsonPropertyName("questionData")]
        public string? QuestionData { get; set; }

        [JsonPropertyName("explanation")]
        public string? Explanation { get; set; }
    }

    public class PdfExtractionResultDto
    {
        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("rawText")]
        public string? RawText { get; set; }

        [JsonPropertyName("questions")]
        public List<ExtractedQuestionDto> Questions { get; set; } = new();
    }

    public class SaveExtractedQuestionsDto
    {
        [JsonPropertyName("examId")]
        public Guid? ExamId { get; set; }

        [JsonPropertyName("categoryId")]
        public Guid? CategoryId { get; set; }

        [JsonPropertyName("questions")]
        public List<ExtractedQuestionDto> Questions { get; set; } = new();
    }

    public class ManageQuestionsViewModel
    {
        public ProctorExamDto Exam { get; set; } = new();
        public List<ExamQuestionItemDto> ExistingQuestions { get; set; } = new();
        public decimal TotalCalculatedMarks { get; set; }
        public QuestionBankResultDto QuestionBank { get; set; } = new();
        public string ActiveTab { get; set; } = "review";
        public string? BankSearch { get; set; }
        public string? BankTypeFilter { get; set; }
        public ExamQuestionItemDto NewQuestion { get; set; } = new();
    }
}
