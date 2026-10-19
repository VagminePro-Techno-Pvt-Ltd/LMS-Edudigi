
using System;
using System.Collections.Generic;
using System.Linq;

namespace TMS.Web.ViewModels;

public sealed class InternalGradeCardRow
{
    public int AssessmentId { get; set; }
    public string AssessmentTitle { get; set; } = "";
    public string AssessmentType { get; set; } = "";
    public string CourseName { get; set; } = "";
    public int? SemesterId { get; set; }
    public decimal MaximumMarks { get; set; }
    public decimal? ObtainedMarks { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? EvaluatedOn { get; set; }
    public string SubmissionStatus { get; set; } = "";
    public string Outcome { get; set; } = "";
}

public sealed class InternalGradeCardViewModel
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = "";
    public int CourseId { get; set; }
    public string CourseName { get; set; } = "";
    public int? SemesterId { get; set; }

    public List<InternalGradeCardRow> Rows { get; set; } = new();

    public decimal TotalMaximumMarks { get; set; }
    public decimal TotalObtainedMarks { get; set; }
    public decimal? Percentage { get; set; }
    public int PendingCount { get; set; }
    public string ResultStatus => Rows.Any(r => r.Outcome == "FAIL")
        ? "FAIL"
        : PendingCount > 0 || Rows.Count == 0 ? "PENDING" : "PASS";

    public DateTime GeneratedOn { get; set; } = DateTime.Now;
}

public sealed class InternalGradeCardListItem
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = "";
    public int CourseId { get; set; }
    public string CourseName { get; set; } = "";
    public int? SemesterId { get; set; }
    public int PublishedAssessments { get; set; }
    public int EvaluatedAssessments { get; set; }
    public int AbsentAssessments { get; set; }
    public int PendingAssessments { get; set; }
    public string ResultStatus { get; set; } = "PENDING";
}


public sealed class InternalSemesterGradeCardCourse
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = "";
    public int PublishedAssessments { get; set; }
    public int EvaluatedAssessments { get; set; }
    public int AbsentAssessments { get; set; }
    public int PendingAssessments { get; set; }
    public decimal? MaximumMarks { get; set; }
    public decimal? ObtainedMarks { get; set; }
    public decimal? Percentage { get; set; }
    public bool Complete => PublishedAssessments > 0 && PendingAssessments == 0;
    public string SubjectCode { get; set; } = "";
    public bool HasFailedAssessment { get; set; }
    public string ResultStatus => HasFailedAssessment ? "FAIL" : !Complete || !Percentage.HasValue ? "PENDING" : "PASS";
}

public sealed class InternalSemesterGradeCardViewModel
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = "";
    public int SemesterId { get; set; }
    public List<InternalSemesterGradeCardCourse> Courses { get; set; } = new();
    public bool Complete => Courses.Count > 0 && Courses.All(c => c.Complete);
    public string ResultStatus => Courses.Count == 0 ? "PENDING" : Courses.Any(c => c.ResultStatus == "FAIL") ? "FAIL" : !Complete ? "PENDING" : "PASS";
    public List<InternalSemesterGradeCardCourse> FailedCourses => Courses.Where(c => c.ResultStatus == "FAIL").ToList();
    public decimal? TotalMaximumMarks { get; set; }
    public decimal? TotalObtainedMarks { get; set; }
    public decimal? Percentage { get; set; }
    public DateTime GeneratedOn { get; set; } = DateTime.Now;
}
