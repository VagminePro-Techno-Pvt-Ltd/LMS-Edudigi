using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TMS.Models.Academics;

[Table("InternalAssessmentPolicies")]
public class InternalAssessmentPolicy
{
    [Key] public int Id { get; set; }
    public int SemesterId { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = "Internal Assessment Policy";
    [Column(TypeName="decimal(10,2)")] public decimal MaxMarks { get; set; } = 100;
    public bool AllowLateSubmission { get; set; }
    public bool RequireResultApproval { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}

[Table("InternalAssessmentWindows")]
public class InternalAssessmentWindow
{
    [Key] public int Id { get; set; }
    public int? CourseId { get; set; } // Null = legacy semester-wide schedule
    public int PolicyId { get; set; }
    [Required, StringLength(120)] public string Name { get; set; } = "";
    public DateTime StartsOn { get; set; }
    public DateTime EndsOn { get; set; }
    public bool IsActive { get; set; } = true;
}
