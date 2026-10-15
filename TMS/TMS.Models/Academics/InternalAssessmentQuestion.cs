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
}
