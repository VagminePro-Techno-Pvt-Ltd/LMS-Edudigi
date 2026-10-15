using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace TMS.Models.Academics;
[Table("InternalAssessmentQuestions")]
public class InternalAssessmentQuestion
{
 [Key] public int Id {get;set;}
 public int AssessmentId {get;set;}
 [Required,StringLength(30)] public string Type {get;set;}="Written";
 [Required,StringLength(4000)] public string Text {get;set;}="";
 [Column(TypeName="decimal(10,2)")] public decimal Marks {get;set;}
 public int SortOrder {get;set;}
 [StringLength(1000)] public string? OptionA {get;set;}
 [StringLength(1000)] public string? OptionB {get;set;}
 [StringLength(1000)] public string? OptionC {get;set;}
 [StringLength(1000)] public string? OptionD {get;set;}
 [StringLength(1)] public string? CorrectOption {get;set;}
 [Column(TypeName="nvarchar(max)")] public string? ExpectedAnswer {get;set;}
 [Column(TypeName="nvarchar(max)")] public string? Rubric {get;set;}
 public int RubricVersion {get;set;} = 1;
}
[Table("InternalAssessmentQuestionAnswers")]
public class InternalAssessmentQuestionAnswer
{
 [Key] public int Id {get;set;}
 public int SubmissionId {get;set;}
 public int QuestionId {get;set;}
 [Column(TypeName="nvarchar(max)")] public string? AnswerText {get;set;}
 [Column(TypeName="decimal(10,2)")] public decimal? Marks {get;set;}
 [StringLength(1000)] public string? Feedback {get;set;}
 [Column(TypeName="decimal(5,2)")] public decimal? AiConceptualMatchPercentage {get;set;}
 [Column(TypeName="decimal(10,2)")] public decimal? AiSuggestedMarks {get;set;}
 public bool? AiThresholdMet {get;set;}
 [StringLength(1000)] public string? AiFeedback {get;set;}
 [Column(TypeName="nvarchar(max)")] public string? AiStrengths {get;set;}
 [Column(TypeName="nvarchar(max)")] public string? AiMissingConcepts {get;set;}
 [StringLength(50)] public string? AiConfidence {get;set;}
 [StringLength(100)] public string? AiModel {get;set;}
 public DateTime? AiEvaluatedOn {get;set;}
 public bool AiAccepted {get;set;} = false;
 public int? AiRubricVersion {get;set;}
 [Column(TypeName="nvarchar(max)")] public string? AiEvaluationMetadata {get;set;}
 [StringLength(500)] public string? FilePath {get;set;}
}
