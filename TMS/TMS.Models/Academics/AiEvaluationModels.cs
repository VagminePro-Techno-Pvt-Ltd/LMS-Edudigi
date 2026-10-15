using System;

namespace TMS.Models.Academics;

public class AiEvaluationRequest
{
    public int QuestionId { get; set; }
    public string QuestionType { get; set; } = "Written";
    public string QuestionText { get; set; } = "";
    public decimal MaxMarks { get; set; }
    public string? ExpectedAnswer { get; set; }
    public string? Rubric { get; set; }
    public int RubricVersion { get; set; } = 1;
    public string? StudentAnswer { get; set; }
}

public class AiEvaluationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public bool RubricRequired { get; set; }
    public decimal ConceptualMatchPercentage { get; set; }
    public decimal SuggestedMarks { get; set; }
    public decimal MaxMarks { get; set; }
    public bool ThresholdMet { get; set; } // true if ConceptualMatchPercentage >= 50.0m
    public string? Strengths { get; set; }
    public string? MissingConcepts { get; set; }
    public string? Feedback { get; set; }
    public string? Confidence { get; set; } = "Medium";
    public string? Model { get; set; }
    public int? RubricVersion { get; set; }
    public DateTime EvaluatedOn { get; set; } = DateTime.UtcNow;
    public string? RawResponseJson { get; set; }
}

public class GeminiEvaluationPayload
{
    public decimal ConceptualMatchPercentage { get; set; }
    public decimal SuggestedMarks { get; set; }
    public decimal MaxMarks { get; set; }
    public bool ThresholdMet { get; set; }
    public string? Strengths { get; set; }
    public string? MissingConcepts { get; set; }
    public string? Feedback { get; set; }
    public string? Confidence { get; set; }
}
