using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Academics;

[Table("InternalAssessments")]
public class InternalAssessment
{
    [Key] public int Id { get; set; }
    [Required, StringLength(200)] public string Title { get; set; } = "";
    [StringLength(4000)] public string? Instructions { get; set; }
    [Required, StringLength(100)] public string Type { get; set; } = "Written";
    public int CourseId { get; set; }
    public int? SubjectId { get; set; }
    public DateTime DueDate { get; set; }
    [Column(TypeName="decimal(10,2)")] public decimal MaxMarks { get; set; }
    public bool AllowLateSubmission { get; set; }
    public bool IsPublished { get; set; }
    public bool ResultsPublished { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; }
    public bool IsDeleted { get; set; }
    public int? WindowId { get; set; }
    public bool ResultApprovalRequested { get; set; }
    public bool ResultApproved { get; set; }
    public int? ResultApprovedBy { get; set; }
    public DateTime? ResultApprovedOn { get; set; }
    public bool IsCompartmental { get; set; } = false;
    public int AttemptNumber { get; set; } = 1;
    public int? OriginalAssessmentId { get; set; }
    [ForeignKey(nameof(OriginalAssessmentId))]
    public virtual InternalAssessment? OriginalAssessment { get; set; }
}

[Table("InternalAssessmentSubmissions")]
public class InternalAssessmentSubmission
{
    [Key] public int Id { get; set; }
    public int AssessmentId { get; set; }
    public int StudentId { get; set; }
    [Column(TypeName="nvarchar(max)"), StringLength(10000)] public string? AnswerText { get; set; }
    [StringLength(500)] public string? FilePath { get; set; }
    public DateTime SubmittedOn { get; set; }
    public bool IsLate { get; set; }
    [Column(TypeName="decimal(10,2)")] public decimal? Marks { get; set; }
    [StringLength(2000)] public string? Feedback { get; set; }
    public int? EvaluatedBy { get; set; }
    public DateTime? EvaluatedOn { get; set; }
}
