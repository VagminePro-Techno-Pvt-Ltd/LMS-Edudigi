using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TMS.Models.Academics;
using TMS.Repository;
using TMS.ViewModels.Academics;

namespace TMS.Web.Services
{
    public class GradeBoundaryRule
    {
        public string Grade { get; set; } = string.Empty;
        public decimal MinPercentage { get; set; }
        public decimal MaxPercentage { get; set; }
        public decimal GradePoint { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class AutomatedResultService : IAutomatedResultService
    {
        private readonly ApplicationDBContext _db;
        private readonly ILogger<AutomatedResultService> _logger;

        private static readonly List<GradeBoundaryRule> DefaultCbcsBoundaries = new()
        {
            new GradeBoundaryRule { Grade = "O", MinPercentage = 90.0m, MaxPercentage = 100.0m, GradePoint = 10.0m, Description = "Outstanding" },
            new GradeBoundaryRule { Grade = "A+", MinPercentage = 80.0m, MaxPercentage = 89.99m, GradePoint = 9.0m, Description = "Excellent" },
            new GradeBoundaryRule { Grade = "A", MinPercentage = 70.0m, MaxPercentage = 79.99m, GradePoint = 8.0m, Description = "Very Good" },
            new GradeBoundaryRule { Grade = "B+", MinPercentage = 60.0m, MaxPercentage = 69.99m, GradePoint = 7.0m, Description = "Good" },
            new GradeBoundaryRule { Grade = "B", MinPercentage = 50.0m, MaxPercentage = 59.99m, GradePoint = 6.0m, Description = "Above Average" },
            new GradeBoundaryRule { Grade = "C", MinPercentage = 40.0m, MaxPercentage = 49.99m, GradePoint = 5.0m, Description = "Average" },
            new GradeBoundaryRule { Grade = "P", MinPercentage = 33.0m, MaxPercentage = 39.99m, GradePoint = 4.0m, Description = "Pass" },
            new GradeBoundaryRule { Grade = "F", MinPercentage = 0.0m, MaxPercentage = 32.99m, GradePoint = 0.0m, Description = "Fail" }
        };

        public AutomatedResultService(ApplicationDBContext db, ILogger<AutomatedResultService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<InstitutionalGradingPolicyViewModel> GetActiveGradingPolicyAsync(CancellationToken cancellationToken = default)
        {
            var policy = await _db.InstitutionalGradingPolicies.AsNoTracking()
                .FirstOrDefaultAsync(p => p.IsActive, cancellationToken);

            if (policy != null)
            {
                return new InstitutionalGradingPolicyViewModel
                {
                    Id = policy.Id,
                    PolicyCode = policy.Code,
                    PolicyName = policy.Name,
                    PassingPercentage = policy.PassingPercentage,
                    CompartmentThresholdPercentage = policy.CompartmentalMinPercentage,
                    MaxCompartmentSubjects = policy.MaxCompartmentSubjects,
                    MaxGraceMarks = policy.MaxGraceMarks,
                    EnableGraceMarks = policy.EnableGraceMarks,
                    RoundingMethod = policy.RoundingMethod,
                    RoundingDecimals = 2,
                    IsActive = policy.IsActive,
                    GradeBoundariesJson = policy.GradeBoundariesJson
                };
            }

            return new InstitutionalGradingPolicyViewModel
            {
                Id = 0,
                PolicyCode = "CBCS_DEFAULT",
                PolicyName = "UGC Choice Based Credit System (CBCS)",
                PassingPercentage = 33.0m,
                CompartmentThresholdPercentage = 20.0m,
                MaxCompartmentSubjects = 2,
                MaxGraceMarks = 0.0m,
                EnableGraceMarks = false,
                RoundingMethod = "Standard",
                RoundingDecimals = 2,
                IsActive = true,
                GradeBoundariesJson = JsonSerializer.Serialize(DefaultCbcsBoundaries)
            };
        }

        public async Task<bool> SaveGradingPolicyAsync(InstitutionalGradingPolicyViewModel model, int updatedBy, CancellationToken cancellationToken = default)
        {
            if (model == null) return false;

            var entity = model.Id > 0
                ? await _db.InstitutionalGradingPolicies.FirstOrDefaultAsync(p => p.Id == model.Id, cancellationToken)
                : null;

            if (entity == null)
            {
                entity = new InstitutionalGradingPolicy
                {
                    Code = string.IsNullOrWhiteSpace(model.PolicyCode) ? "CBCS_" + DateTime.UtcNow.Year : model.PolicyCode.Trim(),
                    Name = string.IsNullOrWhiteSpace(model.PolicyName) ? "Academic Grading Policy" : model.PolicyName.Trim(),
                    PassingPercentage = model.PassingPercentage > 0 ? model.PassingPercentage : 33m,
                    CompartmentalMinPercentage = model.CompartmentThresholdPercentage >= 0 ? model.CompartmentThresholdPercentage : 20m,
                    MaxCompartmentSubjects = model.MaxCompartmentSubjects > 0 ? model.MaxCompartmentSubjects : 2,
                    MaxGraceMarks = model.MaxGraceMarks >= 0 ? model.MaxGraceMarks : 0m,
                    EnableGraceMarks = model.EnableGraceMarks,
                    RoundingMethod = model.RoundingMethod ?? "Standard",
                    GradeBoundariesJson = string.IsNullOrWhiteSpace(model.GradeBoundariesJson) ? JsonSerializer.Serialize(DefaultCbcsBoundaries) : model.GradeBoundariesJson,
                    IsActive = true,
                    CreatedBy = updatedBy,
                    CreatedOn = DateTime.UtcNow
                };
                await _db.InstitutionalGradingPolicies.AddAsync(entity, cancellationToken);
            }
            else
            {
                entity.Name = model.PolicyName ?? entity.Name;
                entity.PassingPercentage = model.PassingPercentage > 0 ? model.PassingPercentage : entity.PassingPercentage;
                entity.CompartmentalMinPercentage = model.CompartmentThresholdPercentage;
                entity.MaxCompartmentSubjects = model.MaxCompartmentSubjects;
                entity.MaxGraceMarks = model.MaxGraceMarks >= 0 ? model.MaxGraceMarks : entity.MaxGraceMarks;
                entity.EnableGraceMarks = model.EnableGraceMarks;
                entity.RoundingMethod = model.RoundingMethod ?? entity.RoundingMethod;
                if (!string.IsNullOrWhiteSpace(model.GradeBoundariesJson))
                    entity.GradeBoundariesJson = model.GradeBoundariesJson;
                entity.UpdatedBy = updatedBy;
                entity.UpdatedOn = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<ResultCalculationSummaryDto> CalculateAssessmentResultsAsync(
            int assessmentId,
            int calculatedBy,
            int? policyId = null,
            CancellationToken cancellationToken = default)
        {
            var assessment = await _db.InternalAssessments
                .FirstOrDefaultAsync(a => a.Id == assessmentId && !a.IsDeleted, cancellationToken);

            if (assessment == null)
                throw new InvalidOperationException("Assessment not found.");

            var course = await _db.CourseMasters.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == assessment.CourseId, cancellationToken);

            // Fetch Active Policy
            var policy = policyId.HasValue && policyId.Value > 0
                ? await _db.InstitutionalGradingPolicies.FirstOrDefaultAsync(p => p.Id == policyId.Value && p.IsActive, cancellationToken)
                : await _db.InstitutionalGradingPolicies.FirstOrDefaultAsync(p => p.IsActive, cancellationToken);

            var passingPct = policy?.PassingPercentage ?? 33.0m;
            var compartmentThresholdPct = policy?.CompartmentalMinPercentage ?? 20.0m;
            var roundingDecimals = 2;
            var boundaries = ParseBoundaries(policy?.GradeBoundariesJson);

            // Fetch enrolled roster
            var enrolledStudents = await _db.CourseEnrollments.AsNoTracking()
                .Where(e => e.CourseId == assessment.CourseId && e.Status == 1)
                .Select(e => e.StudentId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var studentUsers = await _db.UserMasters.AsNoTracking()
                .Where(u => enrolledStudents.Contains(u.Id))
                .Select(u => new { u.Id, u.Name, u.Email })
                .ToDictionaryAsync(u => u.Id, cancellationToken);

            // Fetch submissions
            var submissions = await _db.InternalAssessmentSubmissions
                .Where(s => s.AssessmentId == assessmentId)
                .ToListAsync(cancellationToken);

            var submissionsByStudent = submissions
                .GroupBy(s => s.StudentId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.SubmittedOn).First());

            int attemptNumber = assessment.AttemptNumber > 0 ? assessment.AttemptNumber : 1;

            // Fetch existing processed results for idempotency
            var existingResults = await _db.ProcessedAssessmentResults
                .Where(r => r.AssessmentId == assessmentId && r.AttemptNumber == attemptNumber)
                .ToListAsync(cancellationToken);

            var existingMap = existingResults.ToDictionary(r => r.StudentId);

            var resultDtos = new List<StudentProcessedResultDto>();
            int passCount = 0;
            int failCount = 0;
            int compartmentCount = 0;
            int absentCount = 0;
            int evaluatedCount = 0;
            int pendingCount = 0;
            decimal totalPctSum = 0m;
            int pctCount = 0;

            foreach (var studentId in enrolledStudents)
            {
                studentUsers.TryGetValue(studentId, out var user);
                var studentName = user?.Name ?? $"Student #{studentId}";
                var email = user?.Email ?? "";

                var hasSubmission = submissionsByStudent.TryGetValue(studentId, out var sub);
                var isEvaluated = hasSubmission && sub!.Marks.HasValue && sub.EvaluatedBy.HasValue && sub.EvaluatedOn.HasValue;

                decimal? obtainedMarks = null;
                decimal percentage = 0m;
                string grade = "Ab";
                decimal gradePoint = 0m;
                string status = "ABSENT";
                DateTime? evalDate = null;
                decimal graceAwarded = 0m;
                bool isGrace = false;
                string? auditNote = null;

                if (!hasSubmission)
                {
                    // Absent
                    obtainedMarks = 0m;
                    percentage = 0m;
                    grade = "Ab";
                    gradePoint = 0m;
                    status = "ABSENT";
                    absentCount++;
                    failCount++;
                }
                else if (!isEvaluated)
                {
                    // Pending evaluation
                    status = "PENDING";
                    pendingCount++;
                }
                else
                {
                    // Evaluated
                    evaluatedCount++;
                    obtainedMarks = sub!.Marks!.Value;
                    evalDate = sub.EvaluatedOn;

                    if (assessment.MaxMarks > 0m)
                    {
                        percentage = Math.Round(obtainedMarks.Value / assessment.MaxMarks * 100m, roundingDecimals, MidpointRounding.AwayFromZero);
                        if (percentage > 100m) percentage = 100m;
                        if (percentage < 0m) percentage = 0m;
                    }

                    var gradeInfo = DetermineGrade(percentage, boundaries);
                    grade = gradeInfo.Grade;
                    gradePoint = gradeInfo.GradePoint;

                    if (percentage >= passingPct)
                    {
                        status = "PASS";
                        passCount++;
                    }
                    else
                    {
                        decimal shortfallMarks = Math.Round((passingPct * assessment.MaxMarks / 100m) - obtainedMarks.Value, roundingDecimals, MidpointRounding.AwayFromZero);
                        if (policy != null && policy.EnableGraceMarks && policy.MaxGraceMarks > 0 && shortfallMarks > 0 && shortfallMarks <= policy.MaxGraceMarks)
                        {
                            graceAwarded = shortfallMarks;
                            isGrace = true;
                            percentage = passingPct;
                            status = "PASS";
                            var passGradeInfo = DetermineGrade(percentage, boundaries);
                            grade = passGradeInfo.Grade;
                            gradePoint = passGradeInfo.GradePoint;
                            auditNote = $"Passed with {Math.Round(shortfallMarks, 2)} grace mark(s) per policy {policy.Code}";
                            passCount++;
                        }
                        else
                        {
                            if (percentage >= compartmentThresholdPct)
                            {
                                status = "COMPARTMENT_ELIGIBLE";
                                compartmentCount++;
                            }
                            else
                            {
                                status = "FAIL";
                            }
                            failCount++;
                        }
                    }

                    totalPctSum += percentage;
                    pctCount++;
                }

                // Upsert record
                if (!existingMap.TryGetValue(studentId, out var resEntity))
                {
                    resEntity = new ProcessedAssessmentResult
                    {
                        AssessmentId = assessmentId,
                        StudentId = studentId,
                        CourseId = assessment.CourseId,
                        AttemptNumber = attemptNumber,
                        MaxMarks = assessment.MaxMarks,
                        ObtainedMarks = obtainedMarks ?? 0m,
                        Percentage = percentage,
                        Grade = grade,
                        GradePoint = gradePoint,
                        ResultStatus = status,
                        IsEvaluated = isEvaluated,
                        IsPublished = assessment.ResultsPublished,
                        CalculatedBy = calculatedBy,
                        CalculatedOn = DateTime.UtcNow,
                        PolicyId = policy?.Id,
                        GraceMarksAwarded = graceAwarded,
                        IsGraceApplied = isGrace,
                        AuditRemark = auditNote != null
                            ? $"Calculated on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC by User {calculatedBy}; {auditNote}"
                            : $"Calculated on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC by User {calculatedBy}"
                    };
                    await _db.ProcessedAssessmentResults.AddAsync(resEntity, cancellationToken);
                }
                else
                {
                    // Never apply grace marks retroactively to published results without an explicit authorized workflow
                    if (!resEntity.IsPublished)
                    {
                        resEntity.MaxMarks = assessment.MaxMarks;
                        resEntity.ObtainedMarks = obtainedMarks ?? 0m;
                        resEntity.Percentage = percentage;
                        resEntity.Grade = grade;
                        resEntity.GradePoint = gradePoint;
                        resEntity.ResultStatus = status;
                        resEntity.IsEvaluated = isEvaluated;
                        resEntity.PolicyId = policy?.Id;
                        resEntity.CalculatedBy = calculatedBy;
                        resEntity.CalculatedOn = DateTime.UtcNow;
                        resEntity.GraceMarksAwarded = graceAwarded;
                        resEntity.IsGraceApplied = isGrace;
                        resEntity.AuditRemark = (resEntity.AuditRemark ?? "") + $"; Recalculated on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC" + (auditNote != null ? $"; {auditNote}" : "");
                    }
                }

                resultDtos.Add(new StudentProcessedResultDto
                {
                    ResultId = resEntity.Id,
                    StudentId = studentId,
                    StudentName = studentName,
                    Email = email,
                    AttemptNumber = attemptNumber,
                    MaximumMarks = assessment.MaxMarks,
                    ObtainedMarks = obtainedMarks,
                    Percentage = percentage,
                    GraceMarksAwarded = resEntity.GraceMarksAwarded,
                    IsGraceApplied = resEntity.IsGraceApplied,
                    AuditRemark = resEntity.AuditRemark,
                    Grade = grade,
                    GradePoint = gradePoint,
                    Status = status,
                    IsPublished = assessment.ResultsPublished,
                    EvaluatedOn = evalDate,
                    SubmissionStatus = !hasSubmission ? "ABSENT" : isEvaluated ? "EVALUATED" : "PENDING"
                });
            }

            await _db.SaveChangesAsync(cancellationToken);

            var avgPct = pctCount > 0 ? Math.Round(totalPctSum / pctCount, 2, MidpointRounding.AwayFromZero) : 0m;

            return new ResultCalculationSummaryDto
            {
                AssessmentId = assessment.Id,
                AssessmentTitle = assessment.Title,
                AssessmentType = assessment.Type,
                CourseId = assessment.CourseId,
                CourseName = course?.Name ?? "Course",
                MaximumMarks = assessment.MaxMarks,
                TotalEnrolled = enrolledStudents.Count,
                SubmittedCount = submissionsByStudent.Count,
                EvaluatedCount = evaluatedCount,
                AbsentCount = absentCount,
                PendingCount = pendingCount,
                PassCount = passCount,
                FailCount = failCount,
                CompartmentEligibleCount = compartmentCount,
                AveragePercentage = avgPct,
                IsCalculated = true,
                IsPublished = assessment.ResultsPublished,
                CalculatedOn = DateTime.UtcNow,
                IsCompartmental = assessment.IsCompartmental,
                AttemptNumber = attemptNumber,
                StudentResults = resultDtos.OrderBy(r => r.StudentName).ToList()
            };
        }

        public async Task<ResultCalculationSummaryDto> GetAssessmentResultsAsync(int assessmentId, CancellationToken cancellationToken = default)
        {
            var assessment = await _db.InternalAssessments.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == assessmentId && !a.IsDeleted, cancellationToken);

            if (assessment == null)
                throw new InvalidOperationException("Assessment not found.");

            var course = await _db.CourseMasters.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == assessment.CourseId, cancellationToken);

            var attemptNumber = assessment.AttemptNumber > 0 ? assessment.AttemptNumber : 1;

            var results = await _db.ProcessedAssessmentResults.AsNoTracking()
                .Include(r => r.Student)
                .Where(r => r.AssessmentId == assessmentId && r.AttemptNumber == attemptNumber)
                .ToListAsync(cancellationToken);

            var studentResults = results.Select(r => new StudentProcessedResultDto
            {
                ResultId = r.Id,
                StudentId = r.StudentId,
                StudentName = r.Student?.Name ?? $"Student #{r.StudentId}",
                Email = r.Student?.Email ?? "",
                AttemptNumber = r.AttemptNumber,
                MaximumMarks = r.MaxMarks,
                ObtainedMarks = r.ObtainedMarks,
                Percentage = r.Percentage,
                Grade = r.Grade,
                GradePoint = r.GradePoint,
                Status = r.ResultStatus,
                IsPublished = r.IsPublished,
                SubmissionStatus = r.ResultStatus == "ABSENT" ? "ABSENT" : r.IsEvaluated ? "EVALUATED" : "PENDING"
            }).OrderBy(x => x.StudentName).ToList();

            int passCount = studentResults.Count(x => x.Status == "PASS");
            int failCount = studentResults.Count(x => x.Status == "FAIL" || x.Status == "ABSENT");
            int compCount = studentResults.Count(x => x.Status == "COMPARTMENT_ELIGIBLE");
            int absentCount = studentResults.Count(x => x.Status == "ABSENT");
            int evaluatedCount = studentResults.Count(x => x.Status != "ABSENT" && x.SubmissionStatus == "EVALUATED");
            var validPcts = studentResults.Where(x => x.Status != "ABSENT" && x.SubmissionStatus == "EVALUATED").Select(x => x.Percentage).ToList();
            var avgPct = validPcts.Any() ? Math.Round(validPcts.Average(), 2) : 0m;

            return new ResultCalculationSummaryDto
            {
                AssessmentId = assessment.Id,
                AssessmentTitle = assessment.Title,
                AssessmentType = assessment.Type,
                CourseId = assessment.CourseId,
                CourseName = course?.Name ?? "Course",
                MaximumMarks = assessment.MaxMarks,
                TotalEnrolled = studentResults.Count,
                SubmittedCount = studentResults.Count(x => x.Status != "ABSENT"),
                EvaluatedCount = evaluatedCount,
                AbsentCount = absentCount,
                PendingCount = studentResults.Count(x => x.Status == "PENDING"),
                PassCount = passCount,
                FailCount = failCount,
                CompartmentEligibleCount = compCount,
                AveragePercentage = avgPct,
                IsCalculated = studentResults.Any(),
                IsPublished = assessment.ResultsPublished,
                CalculatedOn = results.FirstOrDefault()?.CalculatedOn,
                PublishedOn = assessment.ResultApprovedOn,
                IsCompartmental = assessment.IsCompartmental,
                AttemptNumber = attemptNumber,
                StudentResults = studentResults
            };
        }

        public async Task<bool> PublishAssessmentResultsAsync(
            int assessmentId,
            int publishedBy,
            bool publish = true,
            CancellationToken cancellationToken = default)
        {
            var assessment = await _db.InternalAssessments
                .FirstOrDefaultAsync(a => a.Id == assessmentId && !a.IsDeleted, cancellationToken);

            if (assessment == null) return false;

            assessment.ResultsPublished = publish;
            assessment.ResultApprovalRequested = false;
            assessment.ResultApproved = false;
            assessment.ResultApprovedBy = publish ? publishedBy : null;
            assessment.ResultApprovedOn = publish ? DateTime.UtcNow : null;

            var results = await _db.ProcessedAssessmentResults
                .Where(r => r.AssessmentId == assessmentId)
                .ToListAsync(cancellationToken);

            foreach (var r in results)
            {
                r.IsPublished = publish;
                if (publish)
                {
                    r.PublishedBy = publishedBy;
                    r.PublishedOn = DateTime.UtcNow;
                    r.AuditRemark = (r.AuditRemark ?? "") + $"; Published on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC by User {publishedBy}";
                }
                else
                {
                    r.AuditRemark = (r.AuditRemark ?? "") + $"; Unpublished on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC by User {publishedBy}";
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<List<StudentProcessedResultDto>> GetEligibleCompartmentalStudentsAsync(
            int assessmentId,
            CancellationToken cancellationToken = default)
        {
            var assessment = await _db.InternalAssessments.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == assessmentId && !a.IsDeleted, cancellationToken);

            if (assessment == null) return new List<StudentProcessedResultDto>();

            var attemptNumber = assessment.AttemptNumber > 0 ? assessment.AttemptNumber : 1;

            var results = await _db.ProcessedAssessmentResults.AsNoTracking()
                .Include(r => r.Student)
                .Where(r => r.AssessmentId == assessmentId && 
                            r.AttemptNumber == attemptNumber &&
                            (r.ResultStatus == "COMPARTMENT_ELIGIBLE" || r.ResultStatus == "FAIL" || r.ResultStatus == "ABSENT"))
                .ToListAsync(cancellationToken);

            return results.Select(r => new StudentProcessedResultDto
            {
                ResultId = r.Id,
                StudentId = r.StudentId,
                StudentName = r.Student?.Name ?? $"Student #{r.StudentId}",
                Email = r.Student?.Email ?? "",
                AttemptNumber = r.AttemptNumber,
                MaximumMarks = r.MaxMarks,
                ObtainedMarks = r.ObtainedMarks,
                Percentage = r.Percentage,
                Grade = r.Grade,
                GradePoint = r.GradePoint,
                Status = r.ResultStatus,
                IsPublished = r.IsPublished,
                SubmissionStatus = r.ResultStatus == "ABSENT" ? "ABSENT" : "FAILED"
            }).OrderBy(x => x.StudentName).ToList();
        }

        public async Task<int> CreateCompartmentalAssessmentAsync(
            CompartmentalAssessmentCreateDto dto,
            int createdBy,
            CancellationToken cancellationToken = default)
        {
            if (dto == null || dto.OriginalAssessmentId <= 0)
                throw new ArgumentException("Invalid compartmental request parameters.");

            var original = await _db.InternalAssessments
                .FirstOrDefaultAsync(a => a.Id == dto.OriginalAssessmentId && !a.IsDeleted, cancellationToken);

            if (original == null)
                throw new InvalidOperationException("Original assessment not found.");

            var nextAttempt = (original.AttemptNumber > 0 ? original.AttemptNumber : 1) + 1;

            var compAssessment = new InternalAssessment
            {
                CourseId = original.CourseId,
                SubjectId = original.SubjectId,
                WindowId = original.WindowId,
                Title = string.IsNullOrWhiteSpace(dto.Title) ? $"{original.Title} (Compartmental - Attempt {nextAttempt})" : dto.Title.Trim(),
                Type = original.Type,
                MaxMarks = original.MaxMarks,
                DueDate = dto.DueDate > DateTime.UtcNow ? dto.DueDate : original.DueDate.AddDays(7),
                AllowLateSubmission = dto.AllowLateSubmission,
                IsPublished = true,
                ResultsPublished = false,
                IsCompartmental = true,
                AttemptNumber = nextAttempt,
                OriginalAssessmentId = original.Id,
                CreatedBy = createdBy,
                CreatedOn = DateTime.UtcNow
            };

            await _db.InternalAssessments.AddAsync(compAssessment, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            // Copy questions if present
            var originalQuestions = await _db.InternalAssessmentQuestions
                .AsNoTracking()
                .Where(q => q.AssessmentId == original.Id)
                .ToListAsync(cancellationToken);

            if (originalQuestions.Any())
            {
                var copyQuestions = originalQuestions.Select(q => new InternalAssessmentQuestion
                {
                    AssessmentId = compAssessment.Id,
                    Marks = q.Marks,
                    SortOrder = q.SortOrder,
                    Rubric = q.Rubric,
                    RubricVersion = q.RubricVersion,
                    ExpectedAnswer = q.ExpectedAnswer,
                    Type = q.Type,
                    Text = q.Text,
                    OptionA = q.OptionA,
                    OptionB = q.OptionB,
                    OptionC = q.OptionC,
                    OptionD = q.OptionD,
                    CorrectOption = q.CorrectOption
                }).ToList();

                await _db.InternalAssessmentQuestions.AddRangeAsync(copyQuestions, cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);
            }

            return compAssessment.Id;
        }

        public async Task<ResultCalculationSummaryDto> CalculateExamMasterResultsAsync(
            int examId,
            int calculatedBy,
            int? policyId = null,
            CancellationToken cancellationToken = default)
        {
            var exam = await _db.ExamMasters.Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == examId, cancellationToken);

            if (exam == null)
                throw new InvalidOperationException("Examination not found.");

            var policy = policyId.HasValue && policyId.Value > 0
                ? await _db.InstitutionalGradingPolicies.FirstOrDefaultAsync(p => p.Id == policyId.Value && p.IsActive, cancellationToken)
                : await _db.InstitutionalGradingPolicies.FirstOrDefaultAsync(p => p.IsActive, cancellationToken);

            var passingPct = policy?.PassingPercentage ?? 33.0m;
            var compartmentThresholdPct = policy?.CompartmentalMinPercentage ?? 20.0m;
            var roundingDecimals = 2;
            var boundaries = ParseBoundaries(policy?.GradeBoundariesJson);

            var enrolledStudents = await _db.CourseEnrollments.AsNoTracking()
                .Where(e => e.CourseId == exam.CourseId && e.Status == 1)
                .Select(e => e.StudentId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var studentUsers = await _db.UserMasters.AsNoTracking()
                .Where(u => enrolledStudents.Contains(u.Id))
                .Select(u => new { u.Id, u.Name, u.Email })
                .ToDictionaryAsync(u => u.Id, cancellationToken);

            var attempts = await _db.StudentExamAttempts
                .Where(a => a.ExamId == examId && a.IsSubmitted)
                .ToListAsync(cancellationToken);

            var attemptsByStudent = attempts
                .GroupBy(a => a.StudentId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Score).First());

            var existingResults = await _db.ProcessedAssessmentResults
                .Where(r => r.ExamMasterId == examId)
                .ToListAsync(cancellationToken);

            var existingMap = existingResults.ToDictionary(r => r.StudentId);

            var resultDtos = new List<StudentProcessedResultDto>();
            int passCount = 0;
            int failCount = 0;
            int compartmentCount = 0;
            int absentCount = 0;
            int evaluatedCount = 0;
            decimal totalPctSum = 0m;
            int pctCount = 0;

            foreach (var studentId in enrolledStudents)
            {
                studentUsers.TryGetValue(studentId, out var user);
                var studentName = user?.Name ?? $"Student #{studentId}";
                var email = user?.Email ?? "";

                var hasAttempt = attemptsByStudent.TryGetValue(studentId, out var att);

                decimal? obtainedMarks = null;
                decimal percentage = 0m;
                string grade = "Ab";
                decimal gradePoint = 0m;
                string status = "ABSENT";
                DateTime? evalDate = null;
                decimal graceAwarded = 0m;
                bool isGrace = false;
                string? auditNote = null;

                if (!hasAttempt)
                {
                    obtainedMarks = 0m;
                    percentage = 0m;
                    grade = "Ab";
                    gradePoint = 0m;
                    status = "ABSENT";
                    absentCount++;
                    failCount++;
                }
                else
                {
                    evaluatedCount++;
                    obtainedMarks = att!.Score ?? 0m;
                    evalDate = att.SubmittedAt;

                    if (exam.TotalMarks > 0m)
                    {
                        percentage = Math.Round(obtainedMarks.Value / exam.TotalMarks * 100m, roundingDecimals, MidpointRounding.AwayFromZero);
                        if (percentage > 100m) percentage = 100m;
                        if (percentage < 0m) percentage = 0m;
                    }

                    var gradeInfo = DetermineGrade(percentage, boundaries);
                    grade = gradeInfo.Grade;
                    gradePoint = gradeInfo.GradePoint;

                    if (percentage >= passingPct)
                    {
                        status = "PASS";
                        passCount++;
                    }
                    else
                    {
                        decimal shortfallMarks = Math.Round((passingPct * exam.TotalMarks / 100m) - obtainedMarks.Value, roundingDecimals, MidpointRounding.AwayFromZero);
                        if (policy != null && policy.EnableGraceMarks && policy.MaxGraceMarks > 0 && shortfallMarks > 0 && shortfallMarks <= policy.MaxGraceMarks)
                        {
                            graceAwarded = shortfallMarks;
                            isGrace = true;
                            percentage = passingPct;
                            status = "PASS";
                            var passGradeInfo = DetermineGrade(percentage, boundaries);
                            grade = passGradeInfo.Grade;
                            gradePoint = passGradeInfo.GradePoint;
                            auditNote = $"Passed with {Math.Round(shortfallMarks, 2)} grace marks per policy {policy.Code}";
                            passCount++;
                        }
                        else
                        {
                            if (percentage >= compartmentThresholdPct)
                            {
                                status = "COMPARTMENT_ELIGIBLE";
                                compartmentCount++;
                            }
                            else
                            {
                                status = "FAIL";
                            }
                            failCount++;
                        }
                    }

                    totalPctSum += percentage;
                    pctCount++;
                }

                if (!existingMap.TryGetValue(studentId, out var resEntity))
                {
                    resEntity = new ProcessedAssessmentResult
                    {
                        ExamMasterId = examId,
                        StudentExamAttemptId = att?.Id,
                        StudentId = studentId,
                        CourseId = exam.CourseId,
                        AttemptNumber = att?.AttemptNumber ?? 1,
                        MaxMarks = exam.TotalMarks,
                        ObtainedMarks = obtainedMarks ?? 0m,
                        Percentage = percentage,
                        Grade = grade,
                        GradePoint = gradePoint,
                        ResultStatus = status,
                        IsEvaluated = hasAttempt,
                        IsPublished = exam.ShowResultImmediately,
                        CalculatedBy = calculatedBy,
                        CalculatedOn = DateTime.UtcNow,
                        PolicyId = policy?.Id,
                        GraceMarksAwarded = graceAwarded,
                        IsGraceApplied = isGrace,
                        AuditRemark = auditNote != null
                            ? $"Calculated ExamMaster on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC by User {calculatedBy}; {auditNote}"
                            : $"Calculated ExamMaster on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC by User {calculatedBy}"
                    };
                    await _db.ProcessedAssessmentResults.AddAsync(resEntity, cancellationToken);
                }
                else
                {
                    if (!resEntity.IsPublished)
                    {
                        resEntity.MaxMarks = exam.TotalMarks;
                        resEntity.ObtainedMarks = obtainedMarks ?? 0m;
                        resEntity.Percentage = percentage;
                        resEntity.Grade = grade;
                        resEntity.GradePoint = gradePoint;
                        resEntity.ResultStatus = status;
                        resEntity.IsEvaluated = hasAttempt;
                        resEntity.PolicyId = policy?.Id;
                        resEntity.CalculatedBy = calculatedBy;
                        resEntity.CalculatedOn = DateTime.UtcNow;
                        resEntity.GraceMarksAwarded = graceAwarded;
                        resEntity.IsGraceApplied = isGrace;
                        resEntity.AuditRemark = (resEntity.AuditRemark ?? "") + $"; Recalculated on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC" + (auditNote != null ? $"; {auditNote}" : "");
                    }
                }

                resultDtos.Add(new StudentProcessedResultDto
                {
                    ResultId = resEntity.Id,
                    StudentId = studentId,
                    StudentName = studentName,
                    Email = email,
                    AttemptNumber = att?.AttemptNumber ?? 1,
                    MaximumMarks = exam.TotalMarks,
                    ObtainedMarks = obtainedMarks,
                    Percentage = percentage,
                    GraceMarksAwarded = resEntity.GraceMarksAwarded,
                    IsGraceApplied = resEntity.IsGraceApplied,
                    AuditRemark = resEntity.AuditRemark,
                    Grade = grade,
                    GradePoint = gradePoint,
                    Status = status,
                    IsPublished = resEntity.IsPublished,
                    EvaluatedOn = evalDate,
                    SubmissionStatus = !hasAttempt ? "ABSENT" : "EVALUATED"
                });
            }

            await _db.SaveChangesAsync(cancellationToken);

            var avgPct = pctCount > 0 ? Math.Round(totalPctSum / pctCount, 2, MidpointRounding.AwayFromZero) : 0m;

            return new ResultCalculationSummaryDto
            {
                AssessmentId = exam.Id,
                AssessmentTitle = exam.Title,
                AssessmentType = "Examination",
                CourseId = exam.CourseId,
                CourseName = exam.Course?.Name ?? "Course",
                MaximumMarks = exam.TotalMarks,
                TotalEnrolled = enrolledStudents.Count,
                SubmittedCount = attemptsByStudent.Count,
                EvaluatedCount = evaluatedCount,
                AbsentCount = absentCount,
                PendingCount = 0,
                PassCount = passCount,
                FailCount = failCount,
                CompartmentEligibleCount = compartmentCount,
                AveragePercentage = avgPct,
                IsCalculated = true,
                IsPublished = exam.ShowResultImmediately,
                CalculatedOn = DateTime.UtcNow,
                StudentResults = resultDtos
            };
        }

        public async Task<bool> ApplyAuthorizedGraceMarksAsync(
            int resultId,
            decimal graceMarks,
            string approvalReason,
            int authorizedBy,
            CancellationToken cancellationToken = default)
        {
            if (resultId <= 0 || graceMarks <= 0 || string.IsNullOrWhiteSpace(approvalReason))
                return false;

            var res = await _db.ProcessedAssessmentResults.FirstOrDefaultAsync(r => r.Id == resultId, cancellationToken);
            if (res == null) return false;

            var policy = res.PolicyId.HasValue
                ? await _db.InstitutionalGradingPolicies.FirstOrDefaultAsync(p => p.Id == res.PolicyId.Value, cancellationToken)
                : await _db.InstitutionalGradingPolicies.FirstOrDefaultAsync(p => p.IsActive, cancellationToken);

            var passingPct = policy?.PassingPercentage ?? 33.0m;
            var boundaries = ParseBoundaries(policy?.GradeBoundariesJson);

            res.ObtainedMarks += graceMarks;
            res.GraceMarksAwarded += graceMarks;
            res.IsGraceApplied = true;

            if (res.MaxMarks > 0)
            {
                res.Percentage = Math.Round(res.ObtainedMarks / res.MaxMarks * 100m, 2, MidpointRounding.AwayFromZero);
                if (res.Percentage > 100m) res.Percentage = 100m;
            }

            var gradeInfo = DetermineGrade(res.Percentage, boundaries);
            res.Grade = gradeInfo.Grade;
            res.GradePoint = gradeInfo.GradePoint;

            if (res.Percentage >= passingPct)
            {
                res.ResultStatus = "PASS";
            }

            var auditEntry = $"[Authorized Grace Awarded] +{graceMarks} mark(s) granted by Authorized User {authorizedBy}. Reason: {approvalReason.Trim()} on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC";
            res.AuditRemark = string.IsNullOrWhiteSpace(res.AuditRemark) ? auditEntry : res.AuditRemark + "; " + auditEntry;

            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        private static List<GradeBoundaryRule> ParseBoundaries(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return DefaultCbcsBoundaries;
            try
            {
                var list = JsonSerializer.Deserialize<List<GradeBoundaryRule>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return (list != null && list.Any()) ? list : DefaultCbcsBoundaries;
            }
            catch
            {
                return DefaultCbcsBoundaries;
            }
        }

        private static (string Grade, decimal GradePoint) DetermineGrade(decimal percentage, List<GradeBoundaryRule> boundaries)
        {
            var match = boundaries.FirstOrDefault(b => percentage >= b.MinPercentage && percentage <= b.MaxPercentage);
            if (match != null)
                return (match.Grade, match.GradePoint);

            if (percentage >= 90.0m) return ("O", 10.0m);
            if (percentage >= 80.0m) return ("A+", 9.0m);
            if (percentage >= 70.0m) return ("A", 8.0m);
            if (percentage >= 60.0m) return ("B+", 7.0m);
            if (percentage >= 50.0m) return ("B", 6.0m);
            if (percentage >= 40.0m) return ("C", 5.0m);
            if (percentage >= 33.0m) return ("P", 4.0m);
            return ("F", 0.0m);
        }
    }
}
