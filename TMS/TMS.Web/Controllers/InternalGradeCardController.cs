
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMS.Repository;
using TMS.Web.ViewModels;
using TMS.Web.Services;

namespace TMS.Web.Controllers;

[Authorize]
public class InternalGradeCardController : BaseController
{
    private readonly ApplicationDBContext _db;

    public InternalGradeCardController(ApplicationDBContext db)
    {
        _db = db;
    }

    private bool IsAdmin =>
        User.IsInRole("Admin") ||
        User.IsInRole("Super Admin") ||
        User.IsInRole("super admin") ||
        User.IsInRole("Management");

    private bool IsFaculty =>
        User.IsInRole("Faculty") ||
        User.IsInRole("Exam Coordinator") ||
        IsFacultyRole();

    private bool IsStudent =>
        User.IsInRole("Student") ||
        GetUserRole().Equals(
            "student",
            StringComparison.OrdinalIgnoreCase);

    private bool HasAccess => IsAdmin || IsFaculty || IsStudent;

    private async Task<bool> CanManageAssessment(TMS.Models.Academics.InternalAssessment assessment)
    {
        if (IsAdmin) return true;
        if (!IsFaculty || !await _db.CourseFacultyMaps.AnyAsync(m =>
                m.CourseId == assessment.CourseId && m.FacultyId == GetUserId() && m.IsActive))
            return false;
        if (assessment.CreatedBy == GetUserId()) return true;
        if (await _db.CourseFacultyMaps.AnyAsync(m =>
                m.CourseId == assessment.CourseId && m.FacultyId == assessment.CreatedBy && m.IsActive))
            return false;
        var successorFacultyId = await _db.CourseFacultyMaps
            .Where(m => m.CourseId == assessment.CourseId && m.IsActive)
            .OrderByDescending(m => m.CreatedOn).ThenByDescending(m => m.Id)
            .Select(m => (int?)m.FacultyId).FirstOrDefaultAsync();
        return successorFacultyId == GetUserId();
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!HasAccess)
            return Forbid();

        var userId = GetUserId();

        var assessments = await _db.InternalAssessments
            .AsNoTracking()
            .Where(a =>
                !a.IsDeleted &&
                a.IsPublished &&
                a.ResultsPublished)
            .ToListAsync();

        if (IsStudent && !IsAdmin && !IsFaculty)
        {
            var myCourses = await _db.CourseEnrollments
                .AsNoTracking()
                .Where(e =>
                    e.StudentId == userId && e.Status == 1)
                .Select(e => e.CourseId)
                .Distinct()
                .ToListAsync();

            assessments = assessments
                .Where(a => myCourses.Contains(a.CourseId))
                .ToList();
        }
        else if (!IsAdmin)
        {
            var authorized = new List<TMS.Models.Academics.InternalAssessment>();
            foreach (var assessment in assessments)
                if (await CanManageAssessment(assessment)) authorized.Add(assessment);
            assessments = authorized;
        }

        var courseIds = assessments
            .Select(a => a.CourseId)
            .Distinct()
            .ToList();

        var enrollments = await _db.CourseEnrollments
            .AsNoTracking()
            .Where(e =>
                courseIds.Contains(e.CourseId) && e.Status == 1)
            .Select(e => new
            {
                e.StudentId,
                e.CourseId
            })
            .Distinct()
            .ToListAsync();

        if (IsStudent && !IsAdmin && !IsFaculty)
        {
            enrollments = enrollments
                .Where(e => e.StudentId == userId)
                .ToList();
        }

        var studentIds = enrollments
            .Select(e => e.StudentId)
            .Distinct()
            .ToList();

        var students = await _db.UserMasters
            .AsNoTracking()
            .Where(u => studentIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name);

        var courses = await _db.CourseMasters
            .AsNoTracking()
            .Where(c => courseIds.Contains(c.Id))
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.SemesterId
            })
            .ToDictionaryAsync(c => c.Id);

        var assessmentIds = assessments
            .Select(a => a.Id)
            .ToList();

        var submissions = await _db.InternalAssessmentSubmissions
            .AsNoTracking()
            .Where(s =>
                assessmentIds.Contains(s.AssessmentId) &&
                studentIds.Contains(s.StudentId))
            .Select(s => new
            {
                s.AssessmentId,
                s.StudentId,
                s.Marks,
                s.EvaluatedOn,
                s.EvaluatedBy
            })
            .ToListAsync();

        var items = new List<InternalGradeCardListItem>();

        foreach (var enrollment in enrollments)
        {
            if (!students.TryGetValue(
                enrollment.StudentId,
                out var studentName))
                continue;

            if (!courses.TryGetValue(
                enrollment.CourseId,
                out var course))
                continue;

            var courseAssessments = assessments
                .Where(a => a.CourseId == enrollment.CourseId)
                .ToList();

            if (courseAssessments.Count == 0)
                continue;

            var evaluatedCount = 0;
            var absentCount = 0;
            var pendingCount = 0;
            var hasFailedAssessment = false;
            foreach (var assessment in courseAssessments)
            {
                var submission = submissions.FirstOrDefault(s =>
                    s.AssessmentId == assessment.Id && s.StudentId == enrollment.StudentId);
                if (submission == null)
                {
                    absentCount++;
                    hasFailedAssessment = true;
                    continue;
                }

                var evaluated = submission.Marks.HasValue && submission.EvaluatedBy.HasValue && submission.EvaluatedOn.HasValue;
                if (!evaluated)
                {
                    pendingCount++;
                    continue;
                }

                evaluatedCount++;
                if (!InternalAssessmentResultPolicy.IsPass(submission.Marks!.Value, assessment.MaxMarks))
                    hasFailedAssessment = true;
            }

            items.Add(new InternalGradeCardListItem
            {
                StudentId = enrollment.StudentId,
                StudentName = studentName,
                CourseId = enrollment.CourseId,
                CourseName = course.Name,
                SemesterId = course.SemesterId,
                PublishedAssessments = courseAssessments.Count,
                EvaluatedAssessments = evaluatedCount,
                AbsentAssessments = absentCount,
                PendingAssessments = pendingCount,
                ResultStatus = hasFailedAssessment ? "FAIL" : pendingCount > 0 ? "PENDING" : "PASS"
            });
        }

        return View(items
            .OrderBy(x => x.CourseName)
            .ThenBy(x => x.StudentName)
            .ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Preview(
        int studentId,
        int courseId)
    {
        if (!HasAccess)
            return Forbid();

        var currentUserId = GetUserId();

        // A student can access only their own grade card.
        if (!IsAdmin && !IsFaculty &&
            studentId != currentUserId)
        {
            return Forbid();
        }

        if (!IsAdmin && IsFaculty && !await _db.CourseFacultyMaps.AsNoTracking()
            .AnyAsync(m => m.FacultyId == currentUserId && m.CourseId == courseId && m.IsActive)) return Forbid();
        var enrolled = await _db.CourseEnrollments
            .AsNoTracking()
            .AnyAsync(e =>
                e.StudentId == studentId &&
                e.CourseId == courseId &&
                e.Status == 1);

        if (!enrolled)
            return NotFound();

        var course = await _db.CourseMasters
            .AsNoTracking()
            .Where(c => c.Id == courseId)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.SemesterId
            })
            .FirstOrDefaultAsync();

        if (course == null)
            return NotFound();

        var student = await _db.UserMasters
            .AsNoTracking()
            .Where(u => u.Id == studentId)
            .Select(u => new
            {
                u.Id,
                u.Name
            })
            .FirstOrDefaultAsync();

        if (student == null)
            return NotFound();

        var query = _db.InternalAssessments
            .AsNoTracking()
            .Where(a =>
                a.CourseId == courseId &&
                !a.IsDeleted &&
                a.IsPublished &&
                a.ResultsPublished);

        var assessments = await query
            .OrderBy(a => a.DueDate)
            .ThenBy(a => a.Id)
            .ToListAsync();

        if (!IsAdmin && IsFaculty)
        {
            var authorized = new List<TMS.Models.Academics.InternalAssessment>();
            foreach (var assessment in assessments)
                if (await CanManageAssessment(assessment)) authorized.Add(assessment);
            assessments = authorized;
        }

        if (assessments.Count == 0)
            return NotFound();

        var assessmentIds = assessments
            .Select(a => a.Id)
            .ToList();

        var submissions = await _db.InternalAssessmentSubmissions
            .AsNoTracking()
            .Where(s =>
                s.StudentId == studentId &&
                assessmentIds.Contains(s.AssessmentId))
            .ToListAsync();

        var processedResults = await _db.ProcessedAssessmentResults.AsNoTracking()
            .Where(r => r.StudentId == studentId && r.AssessmentId.HasValue && assessmentIds.Contains(r.AssessmentId.Value) && r.IsPublished)
            .ToListAsync();

        var model = new InternalGradeCardViewModel
        {
            StudentId = student.Id,
            StudentName = student.Name,
            CourseId = course.Id,
            CourseName = course.Name,
            SemesterId = course.SemesterId
        };

        foreach (var assessment in assessments)
        {
            var submission = submissions
                .Where(s => s.AssessmentId == assessment.Id)
                .OrderByDescending(s => s.SubmittedOn)
                .FirstOrDefault();

            var proc = processedResults.FirstOrDefault(r => r.AssessmentId == assessment.Id);

            var evaluated =
                submission != null &&
                submission.Marks.HasValue &&
                submission.EvaluatedBy.HasValue &&
                submission.EvaluatedOn.HasValue;

            var hasSubmission = submission != null;
            var marks = evaluated ? submission!.Marks : !hasSubmission ? 0m : (decimal?)null;

            model.Rows.Add(new InternalGradeCardRow
            {
                AssessmentId = assessment.Id,
                AssessmentTitle = assessment.Title,
                AssessmentType = assessment.Type,
                CourseName = course.Name,
                SemesterId = course.SemesterId,
                MaximumMarks = assessment.MaxMarks,
                ObtainedMarks = marks,
                DueDate = assessment.DueDate,
                EvaluatedOn = evaluated
                    ? submission!.EvaluatedOn
                    : null,
                SubmissionStatus = InternalAssessmentResultPolicy.SubmissionStatus(hasSubmission, evaluated),
                Outcome = InternalAssessmentResultPolicy.AssessmentOutcome(
                    true, hasSubmission, evaluated, marks, assessment.MaxMarks),
                Grade = proc?.Grade,
                GradePoint = proc?.GradePoint,
                IsCompartmental = proc != null ? proc.AttemptNumber > 1 || assessment.IsCompartmental : assessment.IsCompartmental,
                AttemptNumber = proc != null ? proc.AttemptNumber : assessment.AttemptNumber
            });
        }

        model.PendingCount = model.Rows.Count(
            r => r.Outcome == "PENDING");

        // Totals are only final when every included
        // published assessment has evaluated marks.
        if (model.PendingCount == 0 &&
            model.Rows.Count > 0)
        {
            model.TotalMaximumMarks = model.Rows
                .Sum(r => r.MaximumMarks);

            model.TotalObtainedMarks = model.Rows
                .Sum(r => r.ObtainedMarks ?? 0m);

            if (model.TotalMaximumMarks > 0)
            {
                model.Percentage = Math.Round(
                    model.TotalObtainedMarks /
                    model.TotalMaximumMarks * 100m,
                    2,
                    MidpointRounding.AwayFromZero);

                var p = model.Percentage.Value;
                model.OverallGrade = p >= 90m ? "O" : p >= 80m ? "A+" : p >= 70m ? "A" : p >= 60m ? "B+" : p >= 50m ? "B" : p >= 40m ? "C" : p >= 33m ? "P" : "F";
                model.OverallGradePoint = p >= 90m ? 10m : p >= 80m ? 9m : p >= 70m ? 8m : p >= 60m ? 7m : p >= 50m ? 6m : p >= 40m ? 5m : p >= 33m ? 4m : 0m;
            }
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> SemesterPreview(int studentId, int semesterId)
    {
        if (!HasAccess) return Forbid();
        var currentUserId = GetUserId();
        if (IsStudent && !IsAdmin && !IsFaculty && studentId != currentUserId)
            return Forbid();

        // Use valid historical enrollments and published results. Never disclose another
        // faculty member's assessments to a faculty user.
        var enrolledCourseIds = await _db.CourseEnrollments.AsNoTracking()
            .Where(e => e.StudentId == studentId && e.Status == 1)
            .Select(e => e.CourseId).Distinct().ToListAsync();
        var courses = await _db.CourseMasters.AsNoTracking()
            .Where(c => enrolledCourseIds.Contains(c.Id) && c.SemesterId == semesterId)
            .Select(c => new { c.Id, c.Name }).ToListAsync();
        if (courses.Count == 0) return NotFound();

        var student = await _db.UserMasters.AsNoTracking()
            .Where(u => u.Id == studentId)
            .Select(u => new { u.Id, u.Name }).FirstOrDefaultAsync();
        if (student == null) return NotFound();

        var ids = courses.Select(c => c.Id).ToList();
        var assessmentQuery = _db.InternalAssessments.AsNoTracking()
            .Where(a => ids.Contains(a.CourseId) && !a.IsDeleted &&
                        a.IsPublished && a.ResultsPublished);
        var assessments = await assessmentQuery.ToListAsync();
        if (!IsAdmin && IsFaculty)
        {
            var authorized = new List<TMS.Models.Academics.InternalAssessment>();
            foreach (var assessment in assessments)
                if (await CanManageAssessment(assessment)) authorized.Add(assessment);
            assessments = authorized;
        }
        if (assessments.Count == 0) return NotFound();

        var assessmentIds = assessments.Select(a => a.Id).ToList();
        var submissions = await _db.InternalAssessmentSubmissions.AsNoTracking()
            .Where(s => s.StudentId == studentId && assessmentIds.Contains(s.AssessmentId))
            .Select(s => new { s.AssessmentId, s.Marks, s.EvaluatedBy,
                               s.EvaluatedOn, s.SubmittedOn })
            .ToListAsync();
        var model = new InternalSemesterGradeCardViewModel
        {
            StudentId = student.Id,
            StudentName = student.Name,
            SemesterId = semesterId
        };

        var subjectIds = assessments.Where(a => a.SubjectId.HasValue).Select(a => a.SubjectId!.Value).Distinct().ToList();
        var subjectInfo = await _db.SubjectMasters.AsNoTracking()
            .Where(s => subjectIds.Contains(s.Id))
            .Select(s => new { s.Id, s.Name, s.SubjectCode }).ToDictionaryAsync(s => s.Id);
        foreach (var group in assessments.GroupBy(a => a.SubjectId)
                     .OrderBy(g => g.Key.HasValue && subjectInfo.TryGetValue(g.Key.Value, out var info) ? info.Name : "~"))
        {
            var list = group.ToList();
            decimal obtained = 0m, maximum = 0m;
            int evaluated = 0, absent = 0, pending = 0;
            bool failed = false;
            foreach (var assessment in list)
            {
                var submission = submissions.Where(s => s.AssessmentId == assessment.Id)
                    .OrderByDescending(s => s.SubmittedOn).FirstOrDefault();
                maximum += assessment.MaxMarks;
                if (submission == null)
                {
                    absent++;
                    failed = true;
                    continue;
                }

                if (submission.Marks.HasValue && submission.EvaluatedBy.HasValue && submission.EvaluatedOn.HasValue)
                {
                    evaluated++;
                    obtained += submission.Marks.Value;
                    if (!InternalAssessmentResultPolicy.IsPass(submission.Marks.Value, assessment.MaxMarks)) failed = true;
                }
                else
                {
                    pending++;
                }
            }

            var complete = pending == 0;
            var coursePct = complete && maximum > 0m ? Math.Round(obtained / maximum * 100m, 2, MidpointRounding.AwayFromZero) : (decimal?)null;
            string? courseGrade = null;
            decimal? courseGradePoint = null;
            if (coursePct.HasValue)
            {
                var cp = coursePct.Value;
                courseGrade = cp >= 90m ? "O" : cp >= 80m ? "A+" : cp >= 70m ? "A" : cp >= 60m ? "B+" : cp >= 50m ? "B" : cp >= 40m ? "C" : cp >= 33m ? "P" : "F";
                courseGradePoint = cp >= 90m ? 10m : cp >= 80m ? 9m : cp >= 70m ? 8m : cp >= 60m ? 7m : cp >= 50m ? 6m : cp >= 40m ? 5m : cp >= 33m ? 4m : 0m;
            }

            var subject = group.Key.HasValue && subjectInfo.TryGetValue(group.Key.Value, out var sInfo) ? sInfo : null;
            model.Courses.Add(new InternalSemesterGradeCardCourse
            {
                CourseId = list.Select(a => a.CourseId).First(),
                CourseName = subject?.Name ?? "Subject unassigned",
                SubjectCode = subject?.SubjectCode ?? "UNASSIGNED",
                PublishedAssessments = list.Count,
                EvaluatedAssessments = evaluated,
                AbsentAssessments = absent,
                PendingAssessments = pending,
                MaximumMarks = complete ? maximum : null,
                ObtainedMarks = complete ? obtained : null,
                Percentage = coursePct,
                Grade = courseGrade,
                GradePoint = courseGradePoint,
                HasFailedAssessment = failed
            });
        }
        if (model.Complete)
        {
            model.TotalMaximumMarks = model.Courses.Sum(c => c.MaximumMarks ?? 0m);
            model.TotalObtainedMarks = model.Courses.Sum(c => c.ObtainedMarks ?? 0m);
            if (model.TotalMaximumMarks > 0m)
                model.Percentage = Math.Round(
                    model.TotalObtainedMarks.Value / model.TotalMaximumMarks.Value * 100m,
                    2, MidpointRounding.AwayFromZero);
        }
        return View(model);
    }

}
