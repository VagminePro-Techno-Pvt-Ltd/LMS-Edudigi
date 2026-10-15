using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.Models.Academics;
using TMS.Repository;

namespace TMS.Web.Controllers;

[Authorize]
public class InternalMarksEntryController : BaseController
{
    private readonly ApplicationDBContext db;
    public InternalMarksEntryController(ApplicationDBContext db) => this.db = db;
    private bool Admin => User.IsInRole("Admin") || User.IsInRole("Super Admin") || User.IsInRole("super admin") || User.IsInRole("Management");
    private bool Faculty => User.IsInRole("Faculty") || User.IsInRole("Exam Coordinator") || IsFacultyRole();
    private bool Staff => Admin || Faculty;
    private async Task<bool> CanManage(InternalAssessment assessment)
    {
        if (Admin) return true;
        if (!Faculty || !await db.CourseFacultyMaps.AnyAsync(m => m.CourseId == assessment.CourseId && m.FacultyId == GetUserId() && m.IsActive))
            return false;
        if (assessment.CreatedBy == GetUserId()) return true;
        if (await db.CourseFacultyMaps.AnyAsync(m =>
            m.CourseId == assessment.CourseId && m.FacultyId == assessment.CreatedBy && m.IsActive))
            return false;
        var successorFacultyId = await db.CourseFacultyMaps
            .Where(m => m.CourseId == assessment.CourseId && m.IsActive)
            .OrderByDescending(m => m.CreatedOn).ThenByDescending(m => m.Id)
            .Select(m => (int?)m.FacultyId).FirstOrDefaultAsync();
        return successorFacultyId == GetUserId();
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? assessmentId, int? semesterId, int? courseId)
    {
        if (!Staff) return Forbid();
        var userId = GetUserId();
        // Faculty ownership AND active course mapping are both required.
        var mappedCourseIds = Admin
            ? await db.CourseMasters.AsNoTracking().Select(c => c.Id).ToListAsync()
            : await db.CourseFacultyMaps.AsNoTracking()
                .Where(m => m.FacultyId == userId && m.IsActive)
                .Select(m => m.CourseId).Distinct().ToListAsync();
        var coursesList = await db.CourseMasters.AsNoTracking()
            .Where(c => mappedCourseIds.Contains(c.Id) && c.IsActive)
            .Select(c => new { c.Id, c.Name, c.SemesterId }).ToListAsync();
        var courseIds = coursesList.Select(c => c.Id).ToList();
        var assessments = await db.InternalAssessments.AsNoTracking()
            .Where(a => !a.IsDeleted && courseIds.Contains(a.CourseId))
            .OrderByDescending(a => a.CreatedOn).ToListAsync();
        if (!Admin)
        {
            var authorized = new List<InternalAssessment>();
            foreach (var candidate in assessments)
                if (await CanManage(candidate)) authorized.Add(candidate);
            assessments = authorized;
        }
        ViewBag.Courses = coursesList.ToDictionary(c => c.Id, c => c.Name);
        ViewBag.CourseSemesters = coursesList.ToDictionary(c => c.Id, c => c.SemesterId);
        ViewBag.Semesters = coursesList.Where(c => c.SemesterId.HasValue)
            .Select(c => c.SemesterId.Value).Distinct().OrderBy(x => x).ToList();
        ViewBag.SelectedSemesterId = semesterId;
        ViewBag.SelectedCourseId = courseId;
        ViewBag.Assessments = assessments;
        if (!assessmentId.HasValue) return View(new List<MarksEntryStudentRow>());
        var assessment = assessments.FirstOrDefault(a => a.Id == assessmentId.Value);
        if (assessment == null) return NotFound();
        ViewBag.Selected = assessment;
        ViewBag.HasQuestions = await db.InternalAssessmentQuestions.AsNoTracking()
            .AnyAsync(q => q.AssessmentId == assessment.Id);
        var ids = await db.CourseEnrollments.AsNoTracking()
            .Where(e => e.CourseId == assessment.CourseId && e.IsActive && e.Status == 1)
            .Select(e => e.StudentId).Distinct().ToListAsync();
        var users = await db.UserMasters.AsNoTracking().Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.Name }).ToListAsync();
        var submissions = await db.InternalAssessmentSubmissions.AsNoTracking()
            .Where(s => s.AssessmentId == assessment.Id && ids.Contains(s.StudentId)).ToListAsync();
        var rows = users.OrderBy(u => u.Name).Select(u =>
        {
            var submission = submissions.Where(s => s.StudentId == u.Id)
                .OrderByDescending(s => s.SubmittedOn).ThenByDescending(s => s.Id).FirstOrDefault();
            return new MarksEntryStudentRow
            {
                StudentId = u.Id, StudentName = u.Name,
                Marks = submission?.Marks,
                Evaluated = submission?.Marks.HasValue == true && submission.EvaluatedOn.HasValue && submission.EvaluatedBy.HasValue,
                HasSubmission = submission != null
            };
        }).ToList();
        return View(rows);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int assessmentId, int studentId, decimal? marks)
    {
        if (!Staff) return Forbid();
        var assessment = await db.InternalAssessments.FirstOrDefaultAsync(a => a.Id == assessmentId && !a.IsDeleted);
        if (assessment == null) return NotFound();
        if (!await CanManage(assessment)) return Forbid();
        if (!Admin && !await db.CourseFacultyMaps.AnyAsync(m =>
            m.FacultyId == GetUserId() && m.CourseId == assessment.CourseId && m.IsActive))
            return Forbid();
        if (assessment.ResultsPublished || assessment.ResultApprovalRequested || assessment.ResultApproved)
            return BadRequest("Results under review, approved or published cannot be changed. An administrator must reject or hide them first.");
        if (!marks.HasValue || marks.Value < 0 || marks.Value > assessment.MaxMarks)
            return BadRequest("Marks must be between 0 and the assessment maximum.");
        if (!await db.CourseEnrollments.AnyAsync(e => e.CourseId == assessment.CourseId && e.StudentId == studentId && e.IsActive && e.Status == 1))
            return BadRequest("Student is not actively enrolled in this course.");
        if (await db.InternalAssessmentQuestions.AnyAsync(q => q.AssessmentId == assessmentId))
            return BadRequest("Question-based assessments must be graded using the existing question-wise grading page.");
        var existing = await db.InternalAssessmentSubmissions
            .Where(s => s.AssessmentId == assessmentId && s.StudentId == studentId)
            .OrderByDescending(s => s.SubmittedOn).ThenByDescending(s => s.Id).Take(2).ToListAsync();
        if (existing.Count > 1)
            return BadRequest("Multiple submissions exist for this student and assessment. Resolve duplicates before editing.");
        var submission = existing.FirstOrDefault();
        if (submission == null)
            return BadRequest("No student submission exists. Published non-submission must remain ABSENT with 0 marks and FAIL.");
        submission.Marks = marks.Value;
        submission.EvaluatedBy = GetUserId();
        submission.EvaluatedOn = DateTime.UtcNow;
        assessment.ResultApproved = false;
        assessment.ResultApprovalRequested = false;
        await db.SaveChangesAsync();
        TempData["MarksEntryMessage"] = "Marks saved. Publish results through Internal Assessment after review.";
        return RedirectToAction(nameof(Index), new { assessmentId });
    }
}

public sealed class MarksEntryStudentRow
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = "";
    public decimal? Marks { get; set; }
    public bool Evaluated { get; set; }
    public bool HasSubmission { get; set; }
}
