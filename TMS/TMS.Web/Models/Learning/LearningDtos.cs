namespace TMS.Web.Models.Learning
{
    // ═══════════════════════════════════════════════════════════════════════════
    // GENERIC API RESPONSE WRAPPER
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Standard API response envelope used by all VideoLearning endpoints.
    /// </summary>
    public class ApiResponse<T>
    {
        public bool IsSuccess { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }

        public static ApiResponse<T> Ok(T data, string? message = null) =>
            new() { IsSuccess = true, Data = data, Message = message };

        public static ApiResponse<T> Fail(string message) =>
            new() { IsSuccess = false, Data = default, Message = message };

        public static ApiResponse<T> Success(T data, string? message = null) =>
            new() { IsSuccess = true, Data = data, Message = message };
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // RESPONSE DTOs
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Subject info returned to student.</summary>
    public class SubjectDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public string? SubjectCode { get; set; }
        public int SortOrder { get; set; }
    }

    /// <summary>Unit info under a subject.</summary>
    public class UnitDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public int SortOrder { get; set; }
    }

    /// <summary>Topic info under a unit.</summary>
    public class TopicDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public int SortOrder { get; set; }
        /// <summary>True if at least one active video is attached to this topic.</summary>
        public bool HasVideo { get; set; }
    }

    /// <summary>Video metadata. Never includes internal IDs beyond what the player needs.</summary>
    public class VideoDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = default!;
        public string? FilePath { get; set; }
        public string? VideoUrl { get; set; }
        public int DurationSeconds { get; set; }
        public string Source { get; set; } = "Local"; // Local | YouTube | Vimeo
    }

    /// <summary>Student's watch progress for a video.</summary>
    public class VideoProgressDto
    {
        public int VideoId { get; set; }
        public int LastWatchedSeconds { get; set; }
        /// <summary>How far ahead the student is allowed to seek.</summary>
        public int MaxAllowedSeconds { get; set; }
        public bool IsCompleted { get; set; }
    }

    /// <summary>
    /// A quiz option shown to the student.
    /// NOTE: IsCorrectOption is intentionally omitted — never sent to frontend.
    /// </summary>
    public class VideoQuizOptionDto
    {
        /// <summary>Option letter: "A", "B", "C", or "D".</summary>
        public string Key { get; set; } = default!;
        public string Text { get; set; } = default!;
    }

    /// <summary>
    /// A quiz question returned to the frontend.
    /// CorrectOption is intentionally excluded from this DTO.
    /// </summary>
    public class VideoQuizQuestionDto
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = default!;
        public int TriggerTimeSeconds { get; set; }
        public List<VideoQuizOptionDto> Options { get; set; } = new();
        /// <summary>True if this student already submitted an answer for this question.</summary>
        public bool AlreadyAttempted { get; set; }
        /// <summary>The option the student previously selected (only if AlreadyAttempted = true).</summary>
        public string? PreviousAnswer { get; set; }
    }

    /// <summary>Result returned after quiz submission.</summary>
    public class QuizSubmitResultDto
    {
        public int TotalSubmitted { get; set; }
        public int NewAnswers { get; set; }
        public int SkippedDuplicates { get; set; }
        public int CorrectCount { get; set; }
        public int WrongCount { get; set; }
        /// <summary>New MaxAllowedSeconds after unlock.</summary>
        public int NewMaxAllowedSeconds { get; set; }
        public bool VideoFullyUnlocked { get; set; }
        public string Message { get; set; } = default!;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // REQUEST DTOs
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>Body for POST /api/learning/videos/{videoId}/progress</summary>
    public class SaveProgressRequest
    {
        /// <summary>Current playback position in seconds. Backend clamps if > maxAllowed + 5.</summary>
        public int CurrentTimeSeconds { get; set; }
    }

    /// <summary>A single answer within a quiz submission.</summary>
    public class QuizAnswerRequest
    {
        /// <summary>VideoQuizQuestion.Id</summary>
        public int QuestionId { get; set; }
        /// <summary>One of: "A", "B", "C", "D"</summary>
        public string SelectedOption { get; set; } = default!;
    }

    /// <summary>Body for POST /api/learning/videos/{videoId}/quiz/submit</summary>
    public class QuizSubmitRequest
    {
        /// <summary>Must match the triggerTimeSeconds of the questions being answered.</summary>
        public int TriggerTimeSeconds { get; set; }
        public List<QuizAnswerRequest> Answers { get; set; } = new();
    }
}
