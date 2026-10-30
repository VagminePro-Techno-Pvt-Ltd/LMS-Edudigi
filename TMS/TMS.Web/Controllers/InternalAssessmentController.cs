using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using TMS.Models.Academics;
using TMS.Repository;
using TMS.Web.Services;
using TMS.ViewModels.Academics;

namespace TMS.Web.Controllers;

[Authorize]
public class InternalAssessmentController : BaseController
{
    private readonly ApplicationDBContext db;
    private readonly IWebHostEnvironment environment;
    private readonly IAiEvaluationService aiService;
    private readonly IAutomatedResultService _resultService;
    private readonly TMS.Web.Services.Email.IEmailOutboxService _outboxService;
    private static readonly string[] Types = { "Assignment", "Written", "Practical", "Project", "Presentation", "Viva", "MCQ" };
    public InternalAssessmentController(
        ApplicationDBContext db, 
        IWebHostEnvironment environment, 
        IAiEvaluationService aiService, 
        IAutomatedResultService resultService,
        TMS.Web.Services.Email.IEmailOutboxService outboxService)
    { 
        this.db = db; 
        this.environment = environment; 
        this.aiService = aiService; 
        this._resultService = resultService;
        this._outboxService = outboxService;
    }

    private IActionResult IaNotice(string action, string message, string kind = "warning", object? route = null)
    {
        TempData["IA_ModalMessage"] = message;
        TempData["IA_ModalKind"] = kind;
        return RedirectToAction(action, route);
    }

    private bool Admin => User.IsInRole("Admin") || User.IsInRole("Super Admin") || User.IsInRole("super admin") || User.IsInRole("Management");
    private bool Faculty => User.IsInRole("Faculty") || User.IsInRole("Exam Coordinator") || IsFacultyRole();
    private bool Student => User.IsInRole("Student") || GetUserRole().Equals("student", StringComparison.OrdinalIgnoreCase);
    private bool Staff => Admin || Faculty;
    private async Task<bool> CanManage(InternalAssessment a)
    {
        if (Admin) return true;
        if (!Faculty || !await CanTeach(a.CourseId)) return false;
        if (a.CreatedBy == GetUserId()) return true;

        // Preserve CreatedBy as audit history. A currently mapped faculty member
        // inherits management only after the original creator is no longer mapped.
        if (await db.CourseFacultyMaps.AnyAsync(m =>
            m.CourseId == a.CourseId &&
            m.FacultyId == a.CreatedBy &&
            m.IsActive))
            return false;

        var successorFacultyId = await db.CourseFacultyMaps
            .Where(m => m.CourseId == a.CourseId && m.IsActive)
            .OrderByDescending(m => m.CreatedOn)
            .ThenByDescending(m => m.Id)
            .Select(m => (int?)m.FacultyId)
            .FirstOrDefaultAsync();
        return successorFacultyId == GetUserId();
    }
    private async Task<bool> CanTeach(int courseId) => Admin || (Faculty && await db.CourseFacultyMaps.AnyAsync(m => m.CourseId == courseId && m.FacultyId == GetUserId() && m.IsActive));
    private async Task<bool> Enrolled(int courseId) => await db.CourseEnrollments.AnyAsync(e => e.CourseId == courseId && e.StudentId == GetUserId() && e.IsActive && e.Status == 1);

    public IActionResult Index() => RedirectToAction(Admin ? nameof(AdminDashboard) : Faculty ? nameof(FacultyDashboard) : nameof(StudentDashboard));

    public async Task<IActionResult> StudentDashboard()
    {
        if (!Student) return Forbid();
        var enrollments = await db.CourseEnrollments.AsNoTracking()
            .Where(e => e.StudentId == GetUserId() && e.Status == 1)
            .Select(e => new { e.CourseId, Active = e.IsActive })
            .ToListAsync();
        var ids = enrollments.Select(e => e.CourseId).Distinct().ToList();
        var activeIds = enrollments.Where(e => e.Active).Select(e => e.CourseId).Distinct().ToList();
        var assessments = await db.InternalAssessments
            .Where(a => !a.IsDeleted && a.IsPublished && ids.Contains(a.CourseId) &&
                        (activeIds.Contains(a.CourseId) || a.ResultsPublished))
            .OrderBy(a => a.DueDate).ToListAsync();
        var aid = assessments.Select(a => a.Id).ToList();
        ViewBag.Submissions = await db.InternalAssessmentSubmissions.Where(s => s.StudentId == GetUserId() && aid.Contains(s.AssessmentId)).ToListAsync();
        ViewBag.Courses = await db.CourseMasters.Where(c => ids.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Name);
        ViewBag.QuestionCounts = await db.InternalAssessmentQuestions.Where(q=>aid.Contains(q.AssessmentId)).GroupBy(q=>q.AssessmentId).Select(g=>new {Id=g.Key,Count=g.Count()}).ToDictionaryAsync(x=>x.Id,x=>x.Count);
        ViewBag.ActiveCourseIds = activeIds.ToHashSet();
        return View(assessments);
    }

    public async Task<IActionResult> FacultyDashboard()
    {
        if (!Staff) return Forbid();
        var courseIds = Admin ? await db.CourseMasters.Where(c=>c.IsActive).Select(c=>c.Id).ToListAsync() : await db.CourseFacultyMaps.Where(m=>m.FacultyId==GetUserId() && m.IsActive).Select(m=>m.CourseId).Distinct().ToListAsync();
        ViewBag.Courses = await db.CourseMasters.Where(c=>courseIds.Contains(c.Id)).OrderBy(c=>c.Name).ToListAsync();
        ViewBag.Subjects = await db.SubjectMasters.Where(s=>s.IsActive).OrderBy(s=>s.Name).ToListAsync();
        ViewBag.Windows = await db.InternalAssessmentWindows.Where(w=>w.IsActive).OrderBy(w=>w.StartsOn).ToListAsync();
        ViewBag.Policies = await db.InternalAssessmentPolicies.Where(p=>p.IsActive).ToListAsync();
        List<InternalAssessment> assessments;
        if (Admin)
        {
            assessments = await db.InternalAssessments.Where(a => !a.IsDeleted)
                .OrderByDescending(a => a.CreatedOn).ToListAsync();
        }
        else
        {
            var userId = GetUserId();
            var candidates = await db.InternalAssessments
                .Where(a => !a.IsDeleted && courseIds.Contains(a.CourseId))
                .OrderByDescending(a => a.CreatedOn).ToListAsync();
            assessments = new List<InternalAssessment>();
            foreach (var assessment in candidates)
                if (await CanManage(assessment)) assessments.Add(assessment);
        }
        var ids = assessments.Select(a=>a.Id).ToList();
        ViewBag.SubmissionCounts = await db.InternalAssessmentSubmissions.Where(s=>ids.Contains(s.AssessmentId)).GroupBy(s=>s.AssessmentId).Select(g=>new { Id=g.Key, Count=g.Count() }).ToDictionaryAsync(x=>x.Id,x=>x.Count);
        ViewBag.IsAdmin = Admin;
        return View(assessments);
    }

    public async Task<IActionResult> AdminDashboard()
    {
        if (!Admin) return Forbid();
        var assessments = await db.InternalAssessments.Where(a=>!a.IsDeleted).OrderByDescending(a=>a.CreatedOn).ToListAsync();
        var ids = assessments.Select(a=>a.Id).ToList();
        ViewBag.Submissions = await db.InternalAssessmentSubmissions.Where(s=>ids.Contains(s.AssessmentId)).ToListAsync();
        ViewBag.Courses = await db.CourseMasters.ToDictionaryAsync(c=>c.Id,c=>c.Name);
        ViewBag.SemesterIds = await db.CourseMasters.Where(c => c.SemesterId.HasValue).Select(c => c.SemesterId.Value).Distinct().OrderBy(id => id).ToListAsync();
        ViewBag.ScheduleCourses = await db.CourseMasters.Where(c => c.IsActive && c.SemesterId.HasValue).OrderBy(c => c.Name).ToListAsync();
        ViewBag.Policies = await db.InternalAssessmentPolicies.OrderByDescending(x=>x.Id).ToListAsync();
        ViewBag.Windows = await db.InternalAssessmentWindows.OrderByDescending(x=>x.StartsOn).ToListAsync();
        ViewBag.FacultyNames = await db.UserMasters.Where(u=>assessments.Select(a=>a.CreatedBy).Contains(u.Id)).ToDictionaryAsync(u=>u.Id,u=>u.Name);
        return View(assessments);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string title, string? instructions, string[] types, int courseId, int? subjectId, DateTime dueDate, decimal maxMarks, bool allowLateSubmission, int windowId)
    {
        if (!Staff) return Forbid();
        if (!await CanTeach(courseId)) return Forbid();
        var course = await db.CourseMasters.FirstOrDefaultAsync(c=>c.Id==courseId && c.IsActive);
        if (course==null || string.IsNullOrWhiteSpace(title) || title.Length>200 || (instructions?.Length??0)>4000 || types == null || types.Length == 0 || types.Any(t => !Types.Contains(t)) || maxMarks<=0 || maxMarks>10000 || string.Join(",", types ?? Array.Empty<string>()).Length > 100 || dueDate<=DateTime.Now)
            return IaNotice(nameof(FacultyDashboard), "Enter valid assessment details, choose at least one type and select a future due date.");
        if (subjectId.HasValue && !await db.SubjectMasters.AnyAsync(s=>s.Id==subjectId && s.IsActive && course.SemesterId.HasValue && s.SemesterId==course.SemesterId.Value))
            return IaNotice(nameof(FacultyDashboard), "The selected subject does not match the course semester.");
        var window = await (from w in db.InternalAssessmentWindows join p in db.InternalAssessmentPolicies on w.PolicyId equals p.Id where w.Id == windowId && w.IsActive && p.IsActive select new { w.StartsOn, w.EndsOn, w.CourseId, p.SemesterId, p.MaxMarks, p.AllowLateSubmission }).FirstOrDefaultAsync();
        if (window == null || !course.SemesterId.HasValue || course.SemesterId.Value != window.SemesterId || (window.CourseId.HasValue && window.CourseId.Value != courseId) || DateTime.Now > window.EndsOn || dueDate < window.StartsOn || dueDate > window.EndsOn || maxMarks > window.MaxMarks || (allowLateSubmission && !window.AllowLateSubmission))
            return IaNotice(nameof(FacultyDashboard), "Assessment must comply with the selected schedule, maximum marks and late-submission policy.");
        db.InternalAssessments.Add(new InternalAssessment { WindowId=windowId, Title=title.Trim(), Instructions=instructions, Type=string.Join(",",types.Distinct()), CourseId=courseId, SubjectId=subjectId, DueDate=dueDate, MaxMarks=maxMarks, AllowLateSubmission=allowLateSubmission, CreatedBy=GetUserId(), CreatedOn=DateTime.UtcNow });
        await db.SaveChangesAsync();
        TempData["IA_ModalMessage"]="Assessment created as draft."; TempData["IA_ModalKind"]="success";
        return RedirectToAction(nameof(FacultyDashboard));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAssessment(int id, string title, string? instructions, string[] types, int? subjectId, DateTime dueDate, decimal maxMarks, bool allowLateSubmission, int windowId)
    {
        if (!Staff) return Forbid();
        var assessment = await db.InternalAssessments.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
        if (assessment == null) return NotFound();
        if (!await CanManage(assessment)) return Forbid();
        if (assessment.IsPublished || assessment.ResultsPublished ||
            await db.InternalAssessmentSubmissions.AnyAsync(s => s.AssessmentId == id))
            return IaNotice(nameof(FacultyDashboard), "Unpublish before editing. Assessments with submissions cannot be changed.");

        var course = await db.CourseMasters.FirstOrDefaultAsync(c => c.Id == assessment.CourseId && c.IsActive);
        if (course == null || string.IsNullOrWhiteSpace(title) || title.Length > 200 ||
            (instructions?.Length ?? 0) > 4000 || types == null || types.Length == 0 ||
            types.Any(t => !Types.Contains(t)) || maxMarks <= 0 || maxMarks > 10000 ||
            string.Join(",", types).Length > 100 || dueDate <= DateTime.Now)
            return IaNotice(nameof(FacultyDashboard), "Enter valid assessment details, choose at least one type and select a future due date.");

        if (subjectId.HasValue && !await db.SubjectMasters.AnyAsync(s => s.Id == subjectId && s.IsActive &&
            course.SemesterId.HasValue && s.SemesterId == course.SemesterId.Value))
            return IaNotice(nameof(FacultyDashboard), "The selected subject does not match the course semester.");

        var window = await (from w in db.InternalAssessmentWindows
                            join p in db.InternalAssessmentPolicies on w.PolicyId equals p.Id
                            where w.Id == windowId && w.IsActive && p.IsActive
                            select new { w.StartsOn, w.EndsOn, w.CourseId, p.SemesterId, p.MaxMarks, p.AllowLateSubmission })
            .FirstOrDefaultAsync();
        if (window == null || !course.SemesterId.HasValue || course.SemesterId.Value != window.SemesterId ||
            (window.CourseId.HasValue && window.CourseId.Value != assessment.CourseId) ||
            DateTime.Now > window.EndsOn || dueDate < window.StartsOn || dueDate > window.EndsOn ||
            maxMarks > window.MaxMarks || (allowLateSubmission && !window.AllowLateSubmission))
            return IaNotice(nameof(FacultyDashboard), "Assessment must comply with the selected schedule, maximum marks and late-submission policy.");

        assessment.Title = title.Trim();
        assessment.Instructions = instructions;
        assessment.Type = string.Join(",", types.Distinct());
        assessment.SubjectId = subjectId;
        assessment.DueDate = dueDate;
        assessment.MaxMarks = maxMarks;
        assessment.AllowLateSubmission = allowLateSubmission;
        assessment.WindowId = windowId;
        await db.SaveChangesAsync();
        return IaNotice(nameof(FacultyDashboard), "Assessment updated successfully.", "success");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id, bool publish)
    {
        if (!Staff) return Forbid();
        var a=await db.InternalAssessments.FindAsync(id);
        if (a==null || a.IsDeleted) return NotFound();
        if (!await CanManage(a)) return Forbid();
        // No questions means text/file submission mode; question-based assessments must match their total marks.
        if (publish && await db.InternalAssessmentQuestions.AnyAsync(q => q.AssessmentId == id) && await db.InternalAssessmentQuestions.Where(q => q.AssessmentId == id).SumAsync(q => (decimal?)q.Marks) != a.MaxMarks) return IaNotice(nameof(FacultyDashboard), "Question marks must equal assessment maximum marks.");
        if (publish && a.WindowId.HasValue)
        {
            var window = await db.InternalAssessmentWindows.FindAsync(a.WindowId.Value);
            if (window == null || !window.IsActive || (window.CourseId.HasValue && window.CourseId.Value != a.CourseId) || DateTime.Now < window.StartsOn || DateTime.Now > window.EndsOn || a.DueDate > window.EndsOn)
                return IaNotice(nameof(FacultyDashboard), "The selected assessment schedule is not currently active.");
        }
        else if (publish) return IaNotice(nameof(FacultyDashboard), "A scheduled window is required before publishing.");
        a.IsPublished=publish;
        if (!publish)
        {
            a.ResultsPublished = false;
            a.ResultApprovalRequested = false;
            a.ResultApproved = false;
            a.ResultApprovedBy = null;
            a.ResultApprovedOn = null;
        }
        await db.SaveChangesAsync();

        if (publish)
        {
            await QueueIaPublicationAndRemindersAsync(a);
        }
        else
        {
            await _outboxService.CancelScheduledJobsAsync($"remind-ia-*-{id}-*");
        }

        TempData["IA_ModalMessage"] = publish ? "Assessment published successfully." : "Assessment unpublished; results are hidden.";
        TempData["IA_ModalKind"] = "success";
        return RedirectToAction(nameof(FacultyDashboard));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishResults(int id, bool publish)
    {
        if (!Staff) return Forbid();
        var a = await db.InternalAssessments.FindAsync(id);
        if (a == null || a.IsDeleted) return NotFound();
        if (!await CanManage(a) || !await CanTeach(a.CourseId)) return Forbid();
        if (publish)
        {
            var validation = await ValidateResultsReady(a);
            if (validation != null) return IaNotice(nameof(Submissions), validation, route: new { id });
        }
        a.ResultsPublished = publish;
        // Faculty publishes directly; no administrator approval is required.
        a.ResultApprovalRequested = false;
        a.ResultApproved = false;
        a.ResultApprovedBy = publish ? GetUserId() : null;
        await db.SaveChangesAsync();

        // Sync with ProcessedAssessmentResults table
        try
        {
            await _resultService.PublishAssessmentResultsAsync(id, GetUserId(), publish);
        }
        catch { /* Processed results may not have been calculated yet; preserve standard flow */ }

        if (publish)
        {
            await QueueIaResultsPublishedAsync(a);
        }

        return IaNotice(nameof(Submissions), publish ? "Results published. Students can now view their marks." : "Results hidden from students.", "success", new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CalculateResults([FromBody] CalculateResultsRequestDto? dto, [FromForm] int? assessmentId, [FromForm] int? policyId)
    {
        if (!Staff) return Forbid();
        var targetId = dto?.AssessmentId ?? assessmentId ?? 0;
        var targetPolicyId = dto?.PolicyId ?? policyId;
        if (targetId <= 0)
            return Json(new { success = false, message = "Invalid assessment." });

        var a = await db.InternalAssessments.FindAsync(targetId);
        if (a == null || a.IsDeleted) return Json(new { success = false, message = "Assessment not found." });
        if (!await CanManage(a) || !await CanTeach(a.CourseId)) return Json(new { success = false, message = "Unauthorized access." });

        try
        {
            var summary = await _resultService.CalculateAssessmentResultsAsync(targetId, GetUserId(), targetPolicyId);
            return Json(new { success = true, summary, message = "Automated assessment results calculated successfully according to institutional grading policy." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetCalculatedResults(int assessmentId)
    {
        if (!Staff) return Forbid();
        var a = await db.InternalAssessments.FindAsync(assessmentId);
        if (a == null || a.IsDeleted) return Json(new { success = false, message = "Assessment not found." });
        if (!await CanManage(a) || !await CanTeach(a.CourseId)) return Json(new { success = false, message = "Unauthorized access." });

        try
        {
            var summary = await _resultService.GetAssessmentResultsAsync(assessmentId);
            return Json(new { success = true, summary });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PublishProcessedResults([FromBody] PublishProcessedResultsRequestDto? dto, [FromForm] int? assessmentId, [FromForm] bool? publish)
    {
        if (!Staff) return Forbid();
        var targetId = dto?.AssessmentId ?? assessmentId ?? 0;
        var targetPublish = dto?.Publish ?? publish ?? true;
        if (targetId <= 0)
            return Json(new { success = false, message = "Invalid assessment parameter." });

        var a = await db.InternalAssessments.FindAsync(targetId);
        if (a == null || a.IsDeleted) return Json(new { success = false, message = "Assessment not found." });
        if (!await CanManage(a) || !await CanTeach(a.CourseId)) return Json(new { success = false, message = "Unauthorized access." });

        if (targetPublish)
        {
            var validation = await ValidateResultsReady(a);
            if (validation != null)
                return Json(new { success = false, message = validation });
        }

        a.ResultsPublished = targetPublish;
        a.ResultApprovalRequested = false;
        a.ResultApproved = false;
        a.ResultApprovedBy = targetPublish ? GetUserId() : null;
        a.ResultApprovedOn = targetPublish ? DateTime.UtcNow : null;
        await db.SaveChangesAsync();

        await _resultService.PublishAssessmentResultsAsync(targetId, GetUserId(), targetPublish);

        return Json(new
        {
            success = true,
            isPublished = targetPublish,
            message = targetPublish ? "Results published successfully to student grade cards." : "Results hidden from students."
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyAuthorizedGraceMarks([FromBody] ApplyGraceMarksRequestDto? dto)
    {
        if (!Staff) return Forbid();
        if (dto == null || dto.ResultId <= 0 || dto.GraceMarks <= 0 || string.IsNullOrWhiteSpace(dto.ApprovalReason))
            return Json(new { success = false, message = "Valid Result ID, positive grace marks, and approval reason are required." });

        var res = await db.ProcessedAssessmentResults.FindAsync(dto.ResultId);
        if (res == null) return Json(new { success = false, message = "Assessment result record not found." });

        if (res.AssessmentId.HasValue)
        {
            var a = await db.InternalAssessments.FindAsync(res.AssessmentId.Value);
            if (a == null || !await CanManage(a) || !await CanTeach(a.CourseId))
                return Json(new { success = false, message = "Unauthorized access to award grace marks for this assessment." });
        }

        var success = await _resultService.ApplyAuthorizedGraceMarksAsync(dto.ResultId, dto.GraceMarks, dto.ApprovalReason, GetUserId());
        return Json(new
        {
            success,
            message = success ? "Grace marks awarded and audited successfully." : "Failed to apply grace marks."
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CalculateExamResults([FromBody] CalculateExamResultsRequestDto? dto)
    {
        if (!Staff) return Forbid();
        if (dto == null || dto.ExamId <= 0)
            return Json(new { success = false, message = "Valid Exam ID is required." });

        var exam = await db.ExamMasters.FindAsync(dto.ExamId);
        if (exam == null) return Json(new { success = false, message = "Exam not found." });
        if (!await CanTeach(exam.CourseId)) return Json(new { success = false, message = "Unauthorized access." });

        try
        {
            var summary = await _resultService.CalculateExamMasterResultsAsync(dto.ExamId, GetUserId(), dto.PolicyId);
            return Json(new
            {
                success = true,
                summary,
                message = "Formal examination results successfully processed and mapped to UGC CBCS institutional grade ledger."
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetCompartmentEligibleStudents(int assessmentId)
    {
        if (!Staff) return Forbid();
        var a = await db.InternalAssessments.FindAsync(assessmentId);
        if (a == null || a.IsDeleted) return Json(new { success = false, message = "Assessment not found." });
        if (!await CanManage(a) || !await CanTeach(a.CourseId)) return Json(new { success = false, message = "Unauthorized access." });

        var list = await _resultService.GetEligibleCompartmentalStudentsAsync(assessmentId);
        return Json(new { success = true, students = list });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCompartmentalAssessment([FromBody] CompartmentalAssessmentCreateDto? dto, [FromForm] int? originalAssessmentId, [FromForm] string? title, [FromForm] DateTime? dueDate, [FromForm] bool? allowLateSubmission)
    {
        if (!Staff) return Forbid();
        var targetOriginalId = dto?.OriginalAssessmentId ?? originalAssessmentId ?? 0;
        if (targetOriginalId <= 0)
            return Json(new { success = false, message = "Invalid compartmental assessment parameters." });

        var a = await db.InternalAssessments.FindAsync(targetOriginalId);
        if (a == null || a.IsDeleted) return Json(new { success = false, message = "Original assessment not found." });
        if (!await CanManage(a) || !await CanTeach(a.CourseId)) return Json(new { success = false, message = "Unauthorized access." });

        try
        {
            var createDto = dto ?? new CompartmentalAssessmentCreateDto
            {
                OriginalAssessmentId = targetOriginalId,
                Title = title ?? $"{a.Title} (Compartmental)",
                DueDate = dueDate ?? DateTime.Now.AddDays(7),
                AllowLateSubmission = allowLateSubmission ?? false
            };
            var compId = await _resultService.CreateCompartmentalAssessmentAsync(createDto, GetUserId());
            return Json(new { success = true, assessmentId = compId, message = "Compartmental assessment created successfully." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestResultApproval(int id)
    {
        if (!Faculty || Admin) return Forbid();
        var assessment = await db.InternalAssessments.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
        if (assessment == null) return NotFound();
        if (!await CanManage(assessment)) return Forbid();
        if (assessment.ResultsPublished)
            return IaNotice(nameof(Submissions), "Published results must be hidden by an administrator before another approval request.", route: new { id });

        var validation = await ValidateResultsReady(assessment);
        if (validation != null) return IaNotice(nameof(Submissions), validation, route: new { id });

        assessment.ResultApprovalRequested = true;
        assessment.ResultApproved = false;
        assessment.ResultApprovedBy = null;
        assessment.ResultApprovedOn = null;
        await db.SaveChangesAsync();
        return IaNotice(nameof(Submissions), "Results submitted to an administrator for approval.", "success", new { id });
    }

    private async Task<string?> ValidateResultsReady(InternalAssessment assessment)
    {
        if (!assessment.IsPublished) return "Publish the assessment before publishing results.";
        if (!assessment.WindowId.HasValue || !await db.InternalAssessmentWindows.AnyAsync(w => w.Id == assessment.WindowId.Value))
            return "Assessment schedule is missing.";

        // Publish only completed evaluations. Unsubmitted students remain pending;
        // an assessment-wide ResultsPublished flag never creates ABSENT records.
        var submissions = await db.InternalAssessmentSubmissions
            .Where(x => x.AssessmentId == assessment.Id).ToListAsync();
        var evaluated = submissions.Where(x => x.Marks.HasValue && x.EvaluatedBy.HasValue && x.EvaluatedOn.HasValue).ToList();
        if (evaluated.Count == 0) return "Evaluate at least one student before publishing results.";
        if (evaluated.Any(x => x.Marks!.Value < 0 || x.Marks.Value > assessment.MaxMarks))
            return "Some evaluated marks are outside the permitted range.";

        var questions = await db.InternalAssessmentQuestions.Where(q => q.AssessmentId == assessment.Id).ToListAsync();
        if (questions.Count == 0) return null;
        if (questions.Sum(q => q.Marks) != assessment.MaxMarks)
            return "Question marks do not match assessment maximum marks.";

        var submissionIds = evaluated.Select(x => x.Id).ToList();
        var answers = await db.InternalAssessmentQuestionAnswers
            .Where(x => submissionIds.Contains(x.SubmissionId)).ToListAsync();
        var questionIds = questions.Select(q => q.Id).ToHashSet();
        foreach (var submission in evaluated)
        {
            var rows = answers.Where(x => x.SubmissionId == submission.Id).ToList();
            if (rows.Count != questions.Count || rows.Select(x => x.QuestionId).Distinct().Count() != questions.Count ||
                rows.Any(x => !questionIds.Contains(x.QuestionId) || !x.Marks.HasValue) ||
                rows.Sum(x => x.Marks ?? 0) != submission.Marks)
                return "Some evaluated question answers are incomplete or inconsistent.";
        }
        return null;
    }

    public async Task<IActionResult> Submissions(int id)
    {
        if (!Staff) return Forbid();
        var a=await db.InternalAssessments.FindAsync(id);
        if (a==null || a.IsDeleted) return NotFound();
        if (!await CanManage(a) || !await CanTeach(a.CourseId)) return Forbid();
        ViewBag.Assessment=a;
        ViewBag.HasQuestions = await db.InternalAssessmentQuestions.AnyAsync(q => q.AssessmentId == id);
        var items=await db.InternalAssessmentSubmissions.Where(s=>s.AssessmentId==id).OrderByDescending(s=>s.SubmittedOn).ToListAsync();
        var userIds=items.Select(s=>s.StudentId).Distinct().ToList();
        // Include every valid enrollment record so historical published absences remain visible.
        // Submission endpoints still require an active enrollment.
        var enrolledIds = await db.CourseEnrollments.AsNoTracking()
            .Where(e => e.CourseId == a.CourseId && e.Status == 1)
            .Select(e => e.StudentId).Distinct().ToListAsync();
        var roster = await db.UserMasters.AsNoTracking()
            .Where(u => enrolledIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Name }).OrderBy(u => u.Name).ToListAsync();
        ViewBag.EnrolledStudents = roster.ToDictionary(u => u.Id, u => u.Name);
        ViewBag.StudentNames=await db.UserMasters.Where(u=>userIds.Contains(u.Id)).ToDictionaryAsync(u=>u.Id,u=>u.Name);
        ViewBag.IsAdmin = Admin;

        try
        {
            ViewBag.ResultSummary = await _resultService.GetAssessmentResultsAsync(id);
            ViewBag.ActivePolicy = await _resultService.GetActiveGradingPolicyAsync();
        }
        catch { }

        return View(items);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Evaluate(int submissionId, decimal marks, string? feedback)
    {
        if (!Staff) return Forbid();
        var s=await db.InternalAssessmentSubmissions.FindAsync(submissionId);
        if (s==null) return NotFound();
        var a=await db.InternalAssessments.FindAsync(s.AssessmentId);
        if (a==null || a.IsDeleted) return NotFound();
        if (!await CanManage(a)) return Forbid();
        if (await db.InternalAssessmentQuestions.AnyAsync(q=>q.AssessmentId==a.Id)) return BadRequest("Use question-wise grading.");
        if (marks<0 || marks>a.MaxMarks || (feedback?.Length??0)>2000) return BadRequest("Invalid marks or feedback.");
        if ((a.ResultsPublished && s.Marks.HasValue) || a.ResultApprovalRequested || a.ResultApproved)
            return BadRequest("This result is already published or under review. Hide results before changing an existing grade.");
        a.ResultApproved = false;
        a.ResultApprovalRequested = false;
        s.Marks=marks; s.Feedback=feedback; s.EvaluatedBy=GetUserId(); s.EvaluatedOn=DateTime.UtcNow;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Submissions),new { id=s.AssessmentId });
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(10485760)]
    public async Task<IActionResult> Submit(int id, string? answerText, IFormFile? attachment)
    {
        if (!Student) return Forbid();
        if (await db.InternalAssessmentQuestions.AnyAsync(q=>q.AssessmentId==id)) return BadRequest("Use the question-wise attempt page.");
        var a=await db.InternalAssessments.FindAsync(id);
        if (a==null || a.IsDeleted || !a.IsPublished) return NotFound();
        if (!await Enrolled(a.CourseId)) return Forbid();
        if (!a.WindowId.HasValue) return BadRequest("Assessment schedule is missing.");
        var activeWindow = await db.InternalAssessmentWindows.FindAsync(a.WindowId.Value);
        if (activeWindow == null || !activeWindow.IsActive || DateTime.Now < activeWindow.StartsOn || DateTime.Now > activeWindow.EndsOn)
            return BadRequest("Assessment is outside the scheduled window.");
        if (DateTime.Now>a.DueDate && !a.AllowLateSubmission) return BadRequest("Submission deadline has passed.");
        if ((answerText?.Length??0)>10000 || (string.IsNullOrWhiteSpace(answerText) && (attachment==null || attachment.Length==0))) return BadRequest("Answer required (max 10000 characters).");
        if (await db.InternalAssessmentSubmissions.AnyAsync(s=>s.AssessmentId==id && s.StudentId==GetUserId())) return Conflict("Already submitted.");

        string? fileName = null;
        string? temporaryPath = null;
        string? finalPath = null;
        if (attachment!=null && attachment.Length>0)
        {
            var ext=Path.GetExtension(attachment.FileName).ToLowerInvariant();
            if (!new[]{".pdf",".docx",".pptx",".jpg",".jpeg",".png"}.Contains(ext) ||
                attachment.Length > InternalAssessmentUploadValidator.MaximumBytes)
                return BadRequest("Unsupported file or file exceeds 8MB.");
            await using (var input = attachment.OpenReadStream())
            {
                if (!await InternalAssessmentUploadValidator.IsValidAsync(input, attachment.Length, ext, HttpContext.RequestAborted))
                    return BadRequest("File content does not match the allowed format.");
            }
            var folder=Path.Combine(environment.ContentRootPath,"InternalAssessmentUploads");
            Directory.CreateDirectory(folder);
            fileName=Guid.NewGuid().ToString("N")+ext;
            temporaryPath=Path.Combine(folder,fileName+".tmp");
            finalPath=Path.Combine(folder,fileName);
        }

        try
        {
            if (temporaryPath != null && attachment != null)
            {
                await using var stream = System.IO.File.Create(temporaryPath);
                await attachment.CopyToAsync(stream, HttpContext.RequestAborted);
            }
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            if (await db.InternalAssessmentSubmissions.AnyAsync(s=>s.AssessmentId==id && s.StudentId==GetUserId()))
            {
                DeleteUploadIfPresent(temporaryPath);
                return Conflict("Already submitted.");
            }

            db.InternalAssessmentSubmissions.Add(new InternalAssessmentSubmission { AssessmentId=id, StudentId=GetUserId(), AnswerText=answerText, FilePath=fileName, SubmittedOn=DateTime.UtcNow, IsLate=DateTime.Now>a.DueDate });
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            await QueueIaSubmissionReceiptAsync(a, GetUserId());

            TempData["IA_Message"]="Assessment submitted successfully.";
            return RedirectToAction(nameof(StudentDashboard));
        }
        catch (Exception ex) when (IsConcurrentSubmissionException(ex))
        {
            DeleteUploadIfPresent(temporaryPath);
            DeleteUploadIfPresent(finalPath);
            return Conflict("A submission already exists or another submission is being processed.");
        }
        catch
        {
            DeleteUploadIfPresent(temporaryPath);
            DeleteUploadIfPresent(finalPath);
            throw;
        }
    }

    private static bool IsConcurrentSubmissionException(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException!)
        {
            if (current is SqlException sql && sql.Number is 1205 or 2601 or 2627)
                return true;
        }
        return false;
    }

    private static void DeleteUploadIfPresent(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        try
        {
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
        catch (IOException)
        {
            // Preserve the original request failure; operational cleanup can retry.
        }
        catch (UnauthorizedAccessException)
        {
            // Preserve the original request failure; operational cleanup can retry.
        }
    }

    public async Task<IActionResult> Download(int submissionId)
    {
        var s=await db.InternalAssessmentSubmissions.FindAsync(submissionId);
        if (s==null || string.IsNullOrEmpty(s.FilePath)) return NotFound();
        var a=await db.InternalAssessments.FindAsync(s.AssessmentId);
        if (a==null || a.IsDeleted) return NotFound();
        if (s.StudentId == GetUserId())
        {
            if (!Student || !await db.CourseEnrollments.AnyAsync(e => e.StudentId == GetUserId() && e.CourseId == a.CourseId && e.Status == 1)) return Forbid();
        }
        else if (!Staff || !await CanManage(a)) return Forbid();
        var path=Path.Combine(environment.ContentRootPath,"InternalAssessmentUploads",Path.GetFileName(s.FilePath));
        if (!System.IO.File.Exists(path)) return NotFound();
        return PhysicalFile(path,"application/octet-stream","submission"+Path.GetExtension(path));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePolicy(int semesterId, string name, decimal maxMarks, bool allowLateSubmission)
    {
        if (!Admin) return Forbid();
        if (semesterId <= 0 || string.IsNullOrWhiteSpace(name) || name.Length > 100 || maxMarks <= 0 || maxMarks > 10000 || !await db.CourseMasters.AnyAsync(c => c.SemesterId == semesterId))
            return IaNotice(nameof(AdminDashboard), "Enter a valid semester, policy name and maximum marks.");
        var policy = new InternalAssessmentPolicy { SemesterId=semesterId, Name=name.Trim(), MaxMarks=maxMarks, AllowLateSubmission=allowLateSubmission, RequireResultApproval=false };
        db.InternalAssessmentPolicies.Add(policy);
        await db.SaveChangesAsync();
        TempData["IA_ModalMessage"] = "Semester policy created."; TempData["IA_ModalKind"] = "success";
        return RedirectToAction(nameof(AdminDashboard));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPolicy(int policyId, string name, decimal maxMarks, bool allowLateSubmission)
    {
        if (!Admin) return Forbid();
        var policy = await db.InternalAssessmentPolicies.FirstOrDefaultAsync(p => p.Id == policyId);
        if (policy == null) return NotFound();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100 || maxMarks <= 0 || maxMarks > 10000)
            return IaNotice(nameof(AdminDashboard), "Enter a valid policy name and maximum marks.");

        var windowIds = await db.InternalAssessmentWindows.Where(w => w.PolicyId == policyId).Select(w => w.Id).ToListAsync();
        var linkedAssessments = await db.InternalAssessments
            .Where(a => !a.IsDeleted && a.WindowId.HasValue && windowIds.Contains(a.WindowId.Value))
            .ToListAsync();
        var linkedIds = linkedAssessments.Select(a => a.Id).ToList();
        var locked = linkedAssessments.Any(a => a.IsPublished || a.ResultsPublished) ||
                     await db.InternalAssessmentSubmissions.AnyAsync(s => linkedIds.Contains(s.AssessmentId));
        if (locked && (policy.MaxMarks != maxMarks || policy.AllowLateSubmission != allowLateSubmission))
            return IaNotice(nameof(AdminDashboard), "Maximum marks and late-submission rules are locked after publication or submission. The policy name may still be corrected.");
        if (linkedAssessments.Any(a => a.MaxMarks > maxMarks))
            return IaNotice(nameof(AdminDashboard), "Policy maximum cannot be lower than an existing linked assessment maximum.");
        if (!allowLateSubmission && linkedAssessments.Any(a => a.AllowLateSubmission))
            return IaNotice(nameof(AdminDashboard), "Disable late submission on linked draft assessments before removing it from the policy.");

        policy.Name = name.Trim();
        policy.MaxMarks = maxMarks;
        policy.AllowLateSubmission = allowLateSubmission;
        await db.SaveChangesAsync();
        return IaNotice(nameof(AdminDashboard), "Policy updated successfully.", "success");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveWindow(int policyId, int courseId, string name, DateTime startsOn, DateTime endsOn)
    {
        if (!Admin) return Forbid();
        var policy = await db.InternalAssessmentPolicies.FirstOrDefaultAsync(p => p.Id == policyId && p.IsActive);
        var course = await db.CourseMasters.FirstOrDefaultAsync(c => c.Id == courseId && c.IsActive);
        if (policy == null || course == null || !course.SemesterId.HasValue || course.SemesterId.Value != policy.SemesterId || string.IsNullOrWhiteSpace(name) || name.Length > 120 || startsOn >= endsOn || endsOn <= DateTime.Now)
            return IaNotice(nameof(AdminDashboard), "Choose a course matching the policy semester and a valid future schedule.");
        db.InternalAssessmentWindows.Add(new InternalAssessmentWindow { PolicyId=policyId, CourseId=courseId, Name=name.Trim(), StartsOn=startsOn, EndsOn=endsOn });
        await db.SaveChangesAsync();
        TempData["IA_ModalMessage"] = "Assessment schedule created."; TempData["IA_ModalKind"] = "success";
        return RedirectToAction(nameof(AdminDashboard));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RescheduleWindow(int windowId, DateTime startsOn, DateTime endsOn, bool updateAssessmentDueDates = false)
    {
        if (!Admin) return Forbid();
        var window = await db.InternalAssessmentWindows.FirstOrDefaultAsync(w => w.Id == windowId);
        if (window == null) return NotFound();
        if (!window.IsActive) return IaNotice(nameof(AdminDashboard), "Inactive schedules cannot be rescheduled.");
        if (startsOn >= endsOn || endsOn <= DateTime.Now)
            return IaNotice(nameof(AdminDashboard), "Choose a valid schedule with an end time in the future and after its start.");

        var linked = await db.InternalAssessments
            .Where(a => !a.IsDeleted && a.WindowId == windowId).ToListAsync();
        if (linked.Any(a => a.ResultsPublished))
            return IaNotice(nameof(AdminDashboard), "Hide published results before rescheduling this exam.");

        if (!updateAssessmentDueDates && linked.Any(a => a.DueDate < startsOn || a.DueDate > endsOn))
            return IaNotice(nameof(AdminDashboard), "Some assessment due dates fall outside the new schedule. Select 'Update linked assessment deadlines' to set them to the new end time, or choose a compatible date range.");

        // No submissions, grades, or question records are removed or modified.
        window.StartsOn = startsOn;
        window.EndsOn = endsOn;
        if (updateAssessmentDueDates)
        {
            foreach (var assessment in linked) assessment.DueDate = endsOn;
        }
        await db.SaveChangesAsync();
        TempData["IA_ModalMessage"] = updateAssessmentDueDates
            ? "Schedule and linked assessment deadlines updated successfully."
            : "Assessment schedule updated successfully. Existing deadlines were preserved.";
        TempData["IA_ModalKind"] = "success";
        return RedirectToAction(nameof(AdminDashboard));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveResults(int id, bool approve)
    {
        if (!Admin) return Forbid();
        var assessment = await db.InternalAssessments.FirstOrDefaultAsync(a=>a.Id==id && !a.IsDeleted);
        if (assessment==null) return NotFound();
        if (!assessment.ResultApprovalRequested) return IaNotice(nameof(AdminDashboard), "No approval request is pending for this assessment.");
        if (approve)
        {
            var validation = await ValidateResultsReady(assessment);
            if (validation != null) return IaNotice(nameof(AdminDashboard), validation);
        }
        assessment.ResultApproved=approve;
        assessment.ResultApprovedBy=approve ? GetUserId() : null;
        assessment.ResultApprovedOn=approve ? DateTime.UtcNow : null;
        assessment.ResultApprovalRequested=false;
        if (!approve) assessment.ResultsPublished=false;
        await db.SaveChangesAsync();
        return IaNotice(nameof(AdminDashboard), approve ? "Results approved. An administrator may now publish them." : "Approval rejected. Faculty may correct grades and request review again.", approve ? "success" : "warning");
    }


    public async Task<IActionResult> Questions(int id)
    {
        if (!Staff) return Forbid();
        var a=await db.InternalAssessments.FindAsync(id);
        if (a==null || a.IsDeleted) return NotFound();
        if (!await CanManage(a) || !await CanTeach(a.CourseId)) return Forbid();
        ViewBag.Assessment=a;
        return View(await db.InternalAssessmentQuestions.Where(q=>q.AssessmentId==id).OrderBy(q=>q.SortOrder).ThenBy(q=>q.Id).ToListAsync());
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveQuestion(int assessmentId,int? questionId,string type,string text,decimal marks,string? optionA,string? optionB,string? optionC,string? optionD,string? correctOption,string? expectedAnswer = null,string? rubric = null)
    {
        if (!Staff) return Forbid();
        var a=await db.InternalAssessments.FindAsync(assessmentId);
        if (a==null || a.IsDeleted) return NotFound();
        if (!await CanManage(a)) return Forbid();
        if (a.IsPublished || await db.InternalAssessmentSubmissions.AnyAsync(x=>x.AssessmentId==assessmentId)) return BadRequest("Unpublish before editing questions; submitted assessments cannot be changed.");
        if (!a.Type.Split(',').Contains(type) || string.IsNullOrWhiteSpace(text) || text.Length>4000 || marks<=0 || marks>a.MaxMarks) return BadRequest("Invalid question.");
        if (type=="MCQ" && (new[]{optionA,optionB,optionC,optionD}.Any(x=>string.IsNullOrWhiteSpace(x) || x.Length>1000) || !new[]{"A","B","C","D"}.Contains(correctOption))) return BadRequest("MCQ needs four options and a correct answer.");
        var q=questionId.HasValue ? await db.InternalAssessmentQuestions.FirstOrDefaultAsync(x=>x.Id==questionId && x.AssessmentId==assessmentId) : null;
        if (questionId.HasValue && q==null) return NotFound();
        var other=await db.InternalAssessmentQuestions.Where(x=>x.AssessmentId==assessmentId && (!questionId.HasValue || x.Id!=questionId.Value)).SumAsync(x=>(decimal?)x.Marks) ?? 0;
        if (other+marks>a.MaxMarks) return BadRequest("Total question marks exceed assessment maximum.");
        if (q==null) { q=new InternalAssessmentQuestion { AssessmentId=assessmentId,SortOrder=await db.InternalAssessmentQuestions.CountAsync(x=>x.AssessmentId==assessmentId)+1 };db.InternalAssessmentQuestions.Add(q); }
        else if (q.ExpectedAnswer != expectedAnswer?.Trim() || q.Rubric != rubric?.Trim()) { q.RubricVersion += 1; }
        q.Type=type;q.Text=text.Trim();q.Marks=marks;q.OptionA=type=="MCQ"?optionA:null;q.OptionB=type=="MCQ"?optionB:null;q.OptionC=type=="MCQ"?optionC:null;q.OptionD=type=="MCQ"?optionD:null;q.CorrectOption=type=="MCQ"?correctOption:null;
        q.ExpectedAnswer=type=="MCQ"?null:expectedAnswer?.Trim();q.Rubric=type=="MCQ"?null:rubric?.Trim();
        await db.SaveChangesAsync();return RedirectToAction(nameof(Questions),new{id=assessmentId});
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuestion(int id)
    {
        if (!Staff) return Forbid();
        var q=await db.InternalAssessmentQuestions.FindAsync(id);if(q==null)return NotFound();
        var a=await db.InternalAssessments.FindAsync(q.AssessmentId);if(a==null)return NotFound();if(!await CanManage(a))return Forbid();
        if(a.IsPublished || await db.InternalAssessmentSubmissions.AnyAsync(x=>x.AssessmentId==a.Id))return BadRequest("Cannot delete questions from published/submitted assessment.");
        db.InternalAssessmentQuestions.Remove(q);await db.SaveChangesAsync();return RedirectToAction(nameof(Questions),new{id=a.Id});
    }
    public async Task<IActionResult> Attempt(int id)
    {
        if (!Student) return Forbid();
        var a=await db.InternalAssessments.FindAsync(id);if(a==null||a.IsDeleted||!a.IsPublished)return NotFound();
        if(!await Enrolled(a.CourseId))return Forbid();
        if(await db.InternalAssessmentSubmissions.AnyAsync(x=>x.AssessmentId==id&&x.StudentId==GetUserId()))return BadRequest("Already submitted.");
        if(!a.WindowId.HasValue)return BadRequest("No schedule.");
        var w=await db.InternalAssessmentWindows.FindAsync(a.WindowId.Value);
        if(w==null||!w.IsActive||DateTime.Now<w.StartsOn||DateTime.Now>w.EndsOn||(DateTime.Now>a.DueDate&&!a.AllowLateSubmission))return BadRequest("Assessment is not open.");
        ViewBag.Assessment=a;
        return View(await db.InternalAssessmentQuestions.Where(q=>q.AssessmentId==id).OrderBy(q=>q.SortOrder).ThenBy(q=>q.Id).ToListAsync());
    }
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(33554432)]
    public async Task<IActionResult> SubmitAnswers(int id, Dictionary<int, string>? answers)
    {
        if (!Student) return Forbid();
        var a = await db.InternalAssessments.FindAsync(id);
        if (a == null || a.IsDeleted || !a.IsPublished) return NotFound();
        if (!await Enrolled(a.CourseId)) return Forbid();
        if (!a.WindowId.HasValue) return BadRequest("No schedule.");
        var w = await db.InternalAssessmentWindows.FindAsync(a.WindowId.Value);
        if (w == null || !w.IsActive || DateTime.Now < w.StartsOn || DateTime.Now > w.EndsOn || (DateTime.Now > a.DueDate && !a.AllowLateSubmission))
            return BadRequest("Assessment is not open.");

        var qs = await db.InternalAssessmentQuestions.Where(q => q.AssessmentId == id).OrderBy(q => q.SortOrder).ThenBy(q => q.Id).ToListAsync();
        if (qs.Count == 0) return BadRequest("No questions.");

        if (await db.InternalAssessmentSubmissions.AnyAsync(x => x.AssessmentId == id && x.StudentId == GetUserId()))
            return Conflict("Already submitted.");

        var temporaryPaths = new List<string>();
        var finalPaths = new List<string>();
        var folder = Path.Combine(environment.ContentRootPath, "InternalAssessmentUploads");
        Directory.CreateDirectory(folder);

        try
        {
            var questionFiles = new Dictionary<int, string>();
            string? firstUploadedFile = null;

            // Optional overall attachment
            var generalFile = Request.Form.Files["generalAttachment"] ?? Request.Form.Files["attachment"];
            if (generalFile != null && generalFile.Length > 0)
            {
                var ext = Path.GetExtension(generalFile.FileName).ToLowerInvariant();
                if (!new[] { ".pdf", ".docx", ".pptx", ".jpg", ".jpeg", ".png" }.Contains(ext) || generalFile.Length > InternalAssessmentUploadValidator.MaximumBytes)
                    return BadRequest("Overall attachment exceeds 8MB or is not an allowed format (.pdf, .docx, .pptx, .jpg, .png).");

                await using (var input = generalFile.OpenReadStream())
                {
                    if (!await InternalAssessmentUploadValidator.IsValidAsync(input, generalFile.Length, ext, HttpContext.RequestAborted))
                        return BadRequest("Overall attachment content is invalid or corrupted.");
                }

                var genName = Guid.NewGuid().ToString("N") + ext;
                var tmpPath = Path.Combine(folder, genName + ".tmp");
                var finPath = Path.Combine(folder, genName);
                await using (var stream = System.IO.File.Create(tmpPath))
                {
                    await generalFile.CopyToAsync(stream, HttpContext.RequestAborted);
                }
                temporaryPaths.Add(tmpPath);
                finalPaths.Add(finPath);
                firstUploadedFile = genName;
            }

            // Validate answers and process per-question document uploads
            answers ??= new Dictionary<int, string>();
            foreach (var q in qs)
            {
                answers.TryGetValue(q.Id, out var textVal);
                textVal = textVal?.Trim();

                var qFile = Request.Form.Files[$"attachment_{q.Id}"]
                         ?? Request.Form.Files[$"attachments[{q.Id}]"];

                if (qFile != null && qFile.Length > 0)
                {
                    var ext = Path.GetExtension(qFile.FileName).ToLowerInvariant();
                    if (!new[] { ".pdf", ".docx", ".pptx", ".jpg", ".jpeg", ".png" }.Contains(ext) || qFile.Length > InternalAssessmentUploadValidator.MaximumBytes)
                        return BadRequest($"Uploaded file for Question #{q.SortOrder} exceeds 8MB or is not an allowed format (.pdf, .docx, .pptx, .jpg, .png).");

                    await using (var input = qFile.OpenReadStream())
                    {
                        if (!await InternalAssessmentUploadValidator.IsValidAsync(input, qFile.Length, ext, HttpContext.RequestAborted))
                            return BadRequest($"File content for Question #{q.SortOrder} is invalid or corrupted.");
                    }

                    var safeName = Guid.NewGuid().ToString("N") + ext;
                    var tmpPath = Path.Combine(folder, safeName + ".tmp");
                    var finPath = Path.Combine(folder, safeName);
                    await using (var stream = System.IO.File.Create(tmpPath))
                    {
                        await qFile.CopyToAsync(stream, HttpContext.RequestAborted);
                    }
                    temporaryPaths.Add(tmpPath);
                    finalPaths.Add(finPath);
                    questionFiles[q.Id] = safeName;
                    firstUploadedFile ??= safeName;
                }

                if (q.Type == "MCQ")
                {
                    if (string.IsNullOrWhiteSpace(textVal) || !new[] { "A", "B", "C", "D" }.Contains(textVal))
                        return BadRequest($"Please select an answer (A, B, C, or D) for MCQ Question #{q.SortOrder}.");
                }
                else if (string.Equals(q.Type, "Project", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(q.Type, "Presentation", StringComparison.OrdinalIgnoreCase))
                {
                    var hasFile = questionFiles.ContainsKey(q.Id) || !string.IsNullOrEmpty(firstUploadedFile);
                    var hasText = !string.IsNullOrWhiteSpace(textVal);

                    if (!hasFile && !hasText)
                    {
                        return BadRequest($"Please upload a project/presentation document or enter notes for Question #{q.SortOrder}.");
                    }

                    if (string.IsNullOrWhiteSpace(textVal) && hasFile)
                    {
                        textVal = $"[Document Uploaded: {qFile?.FileName ?? "Attachment"}]";
                        answers[q.Id] = textVal;
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(textVal) && !questionFiles.ContainsKey(q.Id))
                        return BadRequest($"Please provide an answer for Question #{q.SortOrder} (max 10000 characters).");

                    if ((textVal?.Length ?? 0) > 10000)
                        return BadRequest($"Answer for Question #{q.SortOrder} exceeds 10000 characters.");
                }
            }

            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            if (await db.InternalAssessmentSubmissions.AnyAsync(x => x.AssessmentId == id && x.StudentId == GetUserId()))
            {
                foreach (var p in temporaryPaths) DeleteUploadIfPresent(p);
                return Conflict("Already submitted.");
            }

            var submission = new InternalAssessmentSubmission
            {
                AssessmentId = id,
                StudentId = GetUserId(),
                SubmittedOn = DateTime.UtcNow,
                IsLate = DateTime.Now > a.DueDate,
                FilePath = firstUploadedFile
            };
            db.InternalAssessmentSubmissions.Add(submission);
            await db.SaveChangesAsync();

            foreach (var q in qs)
            {
                answers.TryGetValue(q.Id, out var ansText);
                questionFiles.TryGetValue(q.Id, out var qFilePath);

                db.InternalAssessmentQuestionAnswers.Add(new InternalAssessmentQuestionAnswer
                {
                    SubmissionId = submission.Id,
                    QuestionId = q.Id,
                    AnswerText = ansText,
                    FilePath = qFilePath,
                    Marks = q.Type == "MCQ" ? (ansText == q.CorrectOption ? q.Marks : 0) : null
                });
            }

            await db.SaveChangesAsync();

            for (var i = 0; i < temporaryPaths.Count; i++)
            {
                if (System.IO.File.Exists(temporaryPaths[i]))
                {
                    System.IO.File.Move(temporaryPaths[i], finalPaths[i]);
                }
            }

            await transaction.CommitAsync();

            await QueueIaSubmissionReceiptAsync(a, GetUserId());

            TempData["IA_Message"] = "Assessment submitted successfully.";
            return RedirectToAction(nameof(StudentDashboard));
        }
        catch (Exception ex) when (IsConcurrentSubmissionException(ex))
        {
            foreach (var p in temporaryPaths) DeleteUploadIfPresent(p);
            foreach (var p in finalPaths) DeleteUploadIfPresent(p);
            return Conflict("A submission already exists or another submission is being processed.");
        }
        catch
        {
            foreach (var p in temporaryPaths) DeleteUploadIfPresent(p);
            foreach (var p in finalPaths) DeleteUploadIfPresent(p);
            throw;
        }
    }

    public async Task<IActionResult> DownloadAnswerFile(int answerId)
    {
        var ans = await db.InternalAssessmentQuestionAnswers.FindAsync(answerId);
        if (ans == null || string.IsNullOrEmpty(ans.FilePath)) return NotFound();
        var s = await db.InternalAssessmentSubmissions.FindAsync(ans.SubmissionId);
        if (s == null) return NotFound();
        var a = await db.InternalAssessments.FindAsync(s.AssessmentId);
        if (a == null || a.IsDeleted) return NotFound();

        if (s.StudentId == GetUserId())
        {
            if (!Student || !await db.CourseEnrollments.AnyAsync(e => e.StudentId == GetUserId() && e.CourseId == a.CourseId && e.Status == 1))
                return Forbid();
        }
        else if (!Staff || !await CanManage(a))
        {
            return Forbid();
        }

        var path = Path.Combine(environment.ContentRootPath, "InternalAssessmentUploads", Path.GetFileName(ans.FilePath));
        if (!System.IO.File.Exists(path)) return NotFound();

        var q = await db.InternalAssessmentQuestions.FindAsync(ans.QuestionId);
        var qType = q?.Type ?? "Question";
        var downloadName = $"{qType}_Q{ans.QuestionId}_{s.StudentId}{Path.GetExtension(path)}";
        return PhysicalFile(path, "application/octet-stream", downloadName);
    }
    public async Task<IActionResult> GradeQuestions(int submissionId)
    {
        if(!Staff)return Forbid();var s=await db.InternalAssessmentSubmissions.FindAsync(submissionId);if(s==null)return NotFound();
        var a=await db.InternalAssessments.FindAsync(s.AssessmentId);if(a==null||a.IsDeleted)return NotFound();if(!await CanManage(a))return Forbid();
        ViewBag.Assessment=a;ViewBag.Submission=s;
        ViewBag.Questions=await db.InternalAssessmentQuestions.Where(q=>q.AssessmentId==a.Id).OrderBy(q=>q.SortOrder).ThenBy(q=>q.Id).ToListAsync();
        return View(await db.InternalAssessmentQuestionAnswers.Where(x=>x.SubmissionId==submissionId).ToListAsync());
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveGrades(int submissionId,Dictionary<int,decimal> marks,Dictionary<int,string>? feedback)
    {
        if(!Staff)return Forbid();var s=await db.InternalAssessmentSubmissions.FindAsync(submissionId);if(s==null)return NotFound();
        var a=await db.InternalAssessments.FindAsync(s.AssessmentId);if(a==null||a.IsDeleted)return NotFound();if(!await CanManage(a))return Forbid();
        if ((a.ResultsPublished && s.Marks.HasValue) || a.ResultApprovalRequested || a.ResultApproved)
            return BadRequest("This result is already published or under review. Hide results before changing an existing grade.");
        var qs=await db.InternalAssessmentQuestions.Where(q=>q.AssessmentId==a.Id).ToDictionaryAsync(q=>q.Id);
        var rows=await db.InternalAssessmentQuestionAnswers.Where(x=>x.SubmissionId==submissionId).ToListAsync();
        if(rows.Count!=qs.Count)return BadRequest("Missing question answers.");
        foreach(var r in rows)
        {
            var q=qs[r.QuestionId];
            if(q.Type=="MCQ") {r.Marks=r.AnswerText==q.CorrectOption?q.Marks:0;continue;}
            if(marks==null||!marks.TryGetValue(q.Id,out var value)||value<0||value>q.Marks)return BadRequest("Invalid question-wise marks.");
            r.Marks=value;r.Feedback=feedback!=null&&feedback.TryGetValue(q.Id,out var f)?f:null;
            if((r.Feedback?.Length??0)>1000)return BadRequest("Feedback too long.");
        }
        s.Marks=rows.Sum(x=>x.Marks??0);s.EvaluatedBy=GetUserId();s.EvaluatedOn=DateTime.UtcNow;
        a.ResultApproved=false;a.ResultApprovalRequested=false;a.ResultApprovedBy=null;a.ResultApprovedOn=null;
        await db.SaveChangesAsync();return RedirectToAction(nameof(Submissions),new{id=a.Id});
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EvaluateQuestionAi(int submissionId, int questionId, System.Threading.CancellationToken cancellationToken)
    {
        if (!Staff) return Forbid();
        var s = await db.InternalAssessmentSubmissions.FindAsync(submissionId);
        if (s == null) return NotFound();
        var a = await db.InternalAssessments.FindAsync(s.AssessmentId);
        if (a == null || a.IsDeleted) return NotFound();
        if (!await CanManage(a)) return Forbid();

        var q = await db.InternalAssessmentQuestions.FirstOrDefaultAsync(x => x.Id == questionId && x.AssessmentId == a.Id);
        if (q == null) return NotFound();

        var r = await db.InternalAssessmentQuestionAnswers.FirstOrDefaultAsync(x => x.SubmissionId == submissionId && x.QuestionId == questionId);
        if (r == null) return NotFound();

        var req = new AiEvaluationRequest
        {
            QuestionId = q.Id,
            QuestionType = q.Type,
            QuestionText = q.Text,
            MaxMarks = q.Marks,
            ExpectedAnswer = q.ExpectedAnswer,
            Rubric = q.Rubric,
            RubricVersion = q.RubricVersion,
            StudentAnswer = r.AnswerText
        };

        var eval = await aiService.EvaluateQuestionAsync(req, cancellationToken);

        if (eval.Success)
        {
            r.AiConceptualMatchPercentage = eval.ConceptualMatchPercentage;
            r.AiSuggestedMarks = eval.SuggestedMarks;
            r.AiThresholdMet = eval.ThresholdMet;
            r.AiFeedback = eval.Feedback;
            r.AiStrengths = eval.Strengths;
            r.AiMissingConcepts = eval.MissingConcepts;
            r.AiConfidence = eval.Confidence;
            r.AiModel = eval.Model;
            r.AiEvaluatedOn = eval.EvaluatedOn;
            r.AiRubricVersion = q.RubricVersion;
            r.AiEvaluationMetadata = eval.RawResponseJson;
            await db.SaveChangesAsync();
        }

        return Json(new
        {
            success = eval.Success,
            rubricRequired = eval.RubricRequired,
            errorMessage = eval.ErrorMessage,
            questionId = q.Id,
            evaluation = eval
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EvaluateAllQuestionsAi(int submissionId, System.Threading.CancellationToken cancellationToken)
    {
        if (!Staff) return Forbid();
        var s = await db.InternalAssessmentSubmissions.FindAsync(submissionId);
        if (s == null) return NotFound();
        var a = await db.InternalAssessments.FindAsync(s.AssessmentId);
        if (a == null || a.IsDeleted) return NotFound();
        if (!await CanManage(a)) return Forbid();

        var questions = await db.InternalAssessmentQuestions
            .Where(q => q.AssessmentId == a.Id)
            .OrderBy(q => q.SortOrder)
            .ThenBy(q => q.Id)
            .ToListAsync();

        var answers = await db.InternalAssessmentQuestionAnswers
            .Where(x => x.SubmissionId == submissionId)
            .ToDictionaryAsync(x => x.QuestionId);

        var results = new List<object>();
        var totalEvaluated = 0;
        var totalRubricRequired = 0;
        var totalErrors = 0;

        foreach (var q in questions)
        {
            if (q.Type == "MCQ")
                continue;

            if (!answers.TryGetValue(q.Id, out var r))
                continue;

            var req = new AiEvaluationRequest
            {
                QuestionId = q.Id,
                QuestionType = q.Type,
                QuestionText = q.Text,
                MaxMarks = q.Marks,
                ExpectedAnswer = q.ExpectedAnswer,
                Rubric = q.Rubric,
                RubricVersion = q.RubricVersion,
                StudentAnswer = r.AnswerText
            };

            var eval = await aiService.EvaluateQuestionAsync(req, cancellationToken);
            if (eval.Success)
            {
                r.AiConceptualMatchPercentage = eval.ConceptualMatchPercentage;
                r.AiSuggestedMarks = eval.SuggestedMarks;
                r.AiThresholdMet = eval.ThresholdMet;
                r.AiFeedback = eval.Feedback;
                r.AiStrengths = eval.Strengths;
                r.AiMissingConcepts = eval.MissingConcepts;
                r.AiConfidence = eval.Confidence;
                r.AiModel = eval.Model;
                r.AiEvaluatedOn = eval.EvaluatedOn;
                r.AiRubricVersion = q.RubricVersion;
                r.AiEvaluationMetadata = eval.RawResponseJson;
                totalEvaluated++;
            }
            else if (eval.RubricRequired)
            {
                totalRubricRequired++;
            }
            else
            {
                totalErrors++;
            }

            results.Add(new
            {
                questionId = q.Id,
                success = eval.Success,
                rubricRequired = eval.RubricRequired,
                errorMessage = eval.ErrorMessage,
                evaluation = eval
            });
        }

        await db.SaveChangesAsync();

        return Json(new
        {
            success = true,
            totalQuestions = questions.Count(q => q.Type != "MCQ"),
            totalEvaluated,
            totalRubricRequired,
            totalErrors,
            results
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveQuestionRubric(int submissionId, int questionId, string? expectedAnswer, string? rubric)
    {
        if (!Staff) return Forbid();
        var s = await db.InternalAssessmentSubmissions.FindAsync(submissionId);
        if (s == null) return NotFound();
        var a = await db.InternalAssessments.FindAsync(s.AssessmentId);
        if (a == null || a.IsDeleted) return NotFound();
        if (!await CanManage(a)) return Forbid();

        var q = await db.InternalAssessmentQuestions.FirstOrDefaultAsync(x => x.Id == questionId && x.AssessmentId == a.Id);
        if (q == null) return NotFound();

        q.ExpectedAnswer = expectedAnswer?.Trim();
        q.Rubric = rubric?.Trim();
        q.RubricVersion += 1;

        await db.SaveChangesAsync();

        return Json(new
        {
            success = true,
            questionId = q.Id,
            rubricVersion = q.RubricVersion,
            message = "Rubric saved successfully."
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptAiMarks(int submissionId, int questionId)
    {
        if (!Staff) return Forbid();
        var s = await db.InternalAssessmentSubmissions.FindAsync(submissionId);
        if (s == null) return NotFound();
        var a = await db.InternalAssessments.FindAsync(s.AssessmentId);
        if (a == null || a.IsDeleted) return NotFound();
        if (!await CanManage(a)) return Forbid();

        if ((a.ResultsPublished && s.Marks.HasValue) || a.ResultApprovalRequested || a.ResultApproved)
            return BadRequest("This result is already published or under review. Hide results before changing an existing grade.");

        var r = await db.InternalAssessmentQuestionAnswers.FirstOrDefaultAsync(x => x.SubmissionId == submissionId && x.QuestionId == questionId);
        if (r == null) return NotFound();

        if (!r.AiSuggestedMarks.HasValue)
            return BadRequest("No evaluated marks available to apply.");

        var q = await db.InternalAssessmentQuestions.FirstOrDefaultAsync(x => x.Id == questionId && x.AssessmentId == a.Id);
        if (q == null) return NotFound();

        var marks = Math.Clamp(r.AiSuggestedMarks.Value, 0m, q.Marks);
        r.Marks = marks;
        if (!string.IsNullOrWhiteSpace(r.AiFeedback))
        {
            r.Feedback = r.AiFeedback.Length > 1000 ? r.AiFeedback.Substring(0, 997) + "..." : r.AiFeedback;
        }
        r.AiAccepted = true;

        var allAnswers = await db.InternalAssessmentQuestionAnswers.Where(x => x.SubmissionId == submissionId).ToListAsync();
        s.Marks = allAnswers.Sum(x => x.Marks ?? 0m);
        s.EvaluatedBy = GetUserId();
        s.EvaluatedOn = DateTime.UtcNow;

        a.ResultApproved = false;
        a.ResultApprovalRequested = false;
        a.ResultApprovedBy = null;
        a.ResultApprovedOn = null;

        await db.SaveChangesAsync();

        return Json(new
        {
            success = true,
            questionId = q.Id,
            marks = r.Marks,
            feedback = r.Feedback,
            submissionTotal = s.Marks
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptAllAiMarks(int submissionId)
    {
        if (!Staff) return Forbid();
        var s = await db.InternalAssessmentSubmissions.FindAsync(submissionId);
        if (s == null) return NotFound();
        var a = await db.InternalAssessments.FindAsync(s.AssessmentId);
        if (a == null || a.IsDeleted) return NotFound();
        if (!await CanManage(a)) return Forbid();

        if ((a.ResultsPublished && s.Marks.HasValue) || a.ResultApprovalRequested || a.ResultApproved)
            return BadRequest("This result is already published or under review. Hide results before changing an existing grade.");

        var qs = await db.InternalAssessmentQuestions.Where(q => q.AssessmentId == a.Id).ToDictionaryAsync(q => q.Id);
        var rows = await db.InternalAssessmentQuestionAnswers.Where(x => x.SubmissionId == submissionId).ToListAsync();

        var acceptedCount = 0;
        foreach (var r in rows)
        {
            if (qs.TryGetValue(r.QuestionId, out var q))
            {
                if (q.Type == "MCQ")
                {
                    r.Marks = r.AnswerText == q.CorrectOption ? q.Marks : 0;
                    continue;
                }

                if (r.AiSuggestedMarks.HasValue)
                {
                    r.Marks = Math.Clamp(r.AiSuggestedMarks.Value, 0m, q.Marks);
                    if (!string.IsNullOrWhiteSpace(r.AiFeedback))
                    {
                        r.Feedback = r.AiFeedback.Length > 1000 ? r.AiFeedback.Substring(0, 997) + "..." : r.AiFeedback;
                    }
                    r.AiAccepted = true;
                    acceptedCount++;
                }
            }
        }

        s.Marks = rows.Sum(x => x.Marks ?? 0m);
        s.EvaluatedBy = GetUserId();
        s.EvaluatedOn = DateTime.UtcNow;

        a.ResultApproved = false;
        a.ResultApprovalRequested = false;
        a.ResultApprovedBy = null;
        a.ResultApprovedOn = null;

        await db.SaveChangesAsync();

        return Json(new
        {
            success = true,
            acceptedCount,
            submissionTotal = s.Marks
        });
    }

    #region Private Email Queue Helpers

    private static string FormatIaAcademicDate(DateTime utcDateTime)
    {
        try
        {
            var istZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
            var istTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc), istZone);
            return istTime.ToString("dd MMM yyyy, hh:mm tt") + " IST";
        }
        catch
        {
            return utcDateTime.ToString("dd MMM yyyy, hh:mm tt") + " UTC";
        }
    }

    private async Task QueueIaPublicationAndRemindersAsync(InternalAssessment a)
    {
        try
        {
            var course = await db.CourseMasters.FindAsync(a.CourseId);
            string courseName = course?.Name ?? "Course";

            var enrollments = await db.CourseEnrollments
                .Where(e => e.CourseId == a.CourseId && e.IsActive && (e.Status == 0 || e.Status == 1))
                .Select(e => e.StudentId)
                .Distinct()
                .ToListAsync();

            if (!enrollments.Any()) return;

            var students = await db.UserMasters
                .Where(u => enrollments.Contains(u.Id) && u.IsActive && !string.IsNullOrEmpty(u.Email))
                .ToListAsync();

            var batch = new List<TMS.ViewModels.Email.EmailEnqueueRequest>();
            var nowUtc = DateTime.UtcNow;
            string scheduleDisplay = FormatIaAcademicDate(a.DueDate);

            foreach (var s in students)
            {
                // Publication Notice
                batch.Add(new TMS.ViewModels.Email.EmailEnqueueRequest
                {
                    RecipientEmail = s.Email.Trim(),
                    RecipientName = s.Name,
                    Subject = $"Assessment Notice: {a.Title} ({courseName})",
                    TemplateName = "ExamPublication",
                    TemplateData = new Dictionary<string, string>
                    {
                        ["RecipientName"] = s.Name ?? "Student",
                        ["ExamTitle"] = a.Title,
                        ["CourseName"] = courseName,
                        ["ExamDate"] = scheduleDisplay,
                        ["DurationMinutes"] = "Take-home / Term Deadline",
                        ["RequiresSeb"] = "false"
                    },
                    EventType = "AssessmentPublication",
                    IdempotencyKey = $"ia-pub-{a.Id}-{s.Id}"
                });

                // 24h Reminder
                var remind24 = a.DueDate.AddHours(-24);
                if (remind24 > nowUtc)
                {
                    batch.Add(new TMS.ViewModels.Email.EmailEnqueueRequest
                    {
                        RecipientEmail = s.Email.Trim(),
                        RecipientName = s.Name,
                        Subject = $"Assessment Reminder: {a.Title} deadline in 24 hours",
                        TemplateName = "ExamReminder",
                        TemplateData = new Dictionary<string, string>
                        {
                            ["RecipientName"] = s.Name ?? "Student",
                            ["ExamTitle"] = a.Title,
                            ["CourseName"] = courseName,
                            ["StartTime"] = scheduleDisplay,
                            ["HoursLeft"] = "24",
                            ["RequiresSeb"] = "false"
                        },
                        ScheduledForUtc = remind24,
                        EventType = "AssessmentReminder24h",
                        IdempotencyKey = $"remind-ia-24h-{a.Id}-{s.Id}"
                    });
                }

                // 2h Reminder
                var remind2 = a.DueDate.AddHours(-2);
                if (remind2 > nowUtc)
                {
                    batch.Add(new TMS.ViewModels.Email.EmailEnqueueRequest
                    {
                        RecipientEmail = s.Email.Trim(),
                        RecipientName = s.Name,
                        Subject = $"Assessment Reminder: {a.Title} deadline in 2 hours",
                        TemplateName = "ExamReminder",
                        TemplateData = new Dictionary<string, string>
                        {
                            ["RecipientName"] = s.Name ?? "Student",
                            ["ExamTitle"] = a.Title,
                            ["CourseName"] = courseName,
                            ["StartTime"] = scheduleDisplay,
                            ["HoursLeft"] = "2",
                            ["RequiresSeb"] = "false"
                        },
                        ScheduledForUtc = remind2,
                        EventType = "AssessmentReminder2h",
                        IdempotencyKey = $"remind-ia-2h-{a.Id}-{s.Id}"
                    });
                }
            }

            if (batch.Any())
            {
                await _outboxService.EnqueueBatchAsync(batch);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[InternalAssessmentController] Failed to queue publication emails: {ex.Message}");
        }
    }

    private async Task QueueIaSubmissionReceiptAsync(InternalAssessment a, int studentId)
    {
        try
        {
            await _outboxService.CancelScheduledJobsAsync($"remind-ia-*-{a.Id}-{studentId}");

            var student = await db.UserMasters.FindAsync(studentId);
            if (student == null || string.IsNullOrWhiteSpace(student.Email)) return;

            var course = await db.CourseMasters.FindAsync(a.CourseId);
            string courseName = course?.Name ?? "Course";

            await _outboxService.EnqueueAsync(new TMS.ViewModels.Email.EmailEnqueueRequest
            {
                RecipientEmail = student.Email.Trim(),
                RecipientName = student.Name,
                Subject = $"Assessment Submission Receipt: {a.Title} ({courseName})",
                TemplateName = "ExamSubmissionReceipt",
                TemplateData = new Dictionary<string, string>
                {
                    ["RecipientName"] = student.Name ?? "Student",
                    ["ExamTitle"] = a.Title,
                    ["CourseName"] = courseName,
                    ["SubmittedAt"] = DateTime.UtcNow.ToString("dd MMM yyyy, hh:mm tt") + " UTC",
                    ["AttemptRef"] = $"IA-SUB-{a.Id}-{studentId}"
                },
                EventType = "AssessmentSubmissionReceipt",
                IdempotencyKey = $"ia-sub-{a.Id}-{studentId}"
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[InternalAssessmentController] Failed to queue submission receipt: {ex.Message}");
        }
    }

    private async Task QueueIaResultsPublishedAsync(InternalAssessment a)
    {
        try
        {
            var course = await db.CourseMasters.FindAsync(a.CourseId);
            string courseName = course?.Name ?? "Course";

            var submissions = await db.InternalAssessmentSubmissions
                .Where(s => s.AssessmentId == a.Id)
                .ToListAsync();

            if (!submissions.Any()) return;

            var studentIds = submissions.Select(s => s.StudentId).Distinct().ToList();
            var studentsMap = await db.UserMasters
                .Where(u => studentIds.Contains(u.Id) && u.IsActive && !string.IsNullOrEmpty(u.Email))
                .ToDictionaryAsync(u => u.Id);

            decimal passingMarks = a.MaxMarks * 0.40m; // 40% standard passing cutoff

            var batch = new List<TMS.ViewModels.Email.EmailEnqueueRequest>();
            foreach (var sub in submissions)
            {
                if (!studentsMap.TryGetValue(sub.StudentId, out var student)) continue;

                batch.Add(new TMS.ViewModels.Email.EmailEnqueueRequest
                {
                    RecipientEmail = student.Email.Trim(),
                    RecipientName = student.Name,
                    Subject = $"Official Result Published: {a.Title} ({courseName})",
                    TemplateName = "ResultsPublished",
                    TemplateData = new Dictionary<string, string>
                    {
                        ["RecipientName"] = student.Name ?? "Student",
                        ["AssessmentTitle"] = a.Title,
                        ["CourseName"] = courseName,
                        ["MarksObtained"] = sub.Marks?.ToString("0.##") ?? "Evaluated",
                        ["MaxMarks"] = a.MaxMarks.ToString(),
                        ["Grade"] = (sub.Marks ?? 0) >= passingMarks ? "Pass" : "Needs Improvement"
                    },
                    EventType = "ResultsPublished",
                    IdempotencyKey = $"result-ia-{a.Id}-{sub.StudentId}"
                });
            }

            if (batch.Any())
            {
                await _outboxService.EnqueueBatchAsync(batch);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[InternalAssessmentController] Failed to queue IA results emails: {ex.Message}");
        }
    }

    #endregion
}

