using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TMS.Models.Academics;
using TMS.Models.Masters;
using TMS.Repository;
using TMS.ViewModels.Academics;

namespace TMS.Web.Services
{
    public class CertificationExamService : ICertificationExamService
    {
        private readonly ApplicationDBContext _db;
        private readonly IConfiguration? _configuration;
        private readonly IHttpClientFactory? _httpClientFactory;
        private readonly ILogger<CertificationExamService>? _logger;
        private static bool _schemaEnsured = false;
        private static readonly object SchemaLock = new();

        public CertificationExamService(
            ApplicationDBContext db,
            IConfiguration? configuration = null,
            IHttpClientFactory? httpClientFactory = null,
            ILogger<CertificationExamService>? logger = null)
        {
            _db = db;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task EnsureSchemaAsync(CancellationToken ct = default)
        {
            if (_schemaEnsured) return;

            lock (SchemaLock)
            {
                if (_schemaEnsured) return;
            }

            try
            {
                if (_db.Database.IsRelational())
                {
                    const string sql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CertificationExams')
BEGIN
    CREATE TABLE [dbo].[CertificationExams](
        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Title] [nvarchar](250) NOT NULL,
        [CourseId] [int] NOT NULL,
        [SubjectId] [int] NULL,
        [CertificationTopic] [nvarchar](250) NOT NULL,
        [Description] [nvarchar](max) NULL,
        [DurationMinutes] [int] NOT NULL DEFAULT 60,
        [TotalMarks] [decimal](18,2) NOT NULL DEFAULT 100.00,
        [PassingPercentage] [decimal](5,2) NOT NULL DEFAULT 50.00,
        [StartDate] [datetime2](7) NOT NULL,
        [EndDate] [datetime2](7) NOT NULL,
        [MaxAttempts] [int] NOT NULL DEFAULT 1,
        [Status] [nvarchar](50) NOT NULL DEFAULT 'Draft',
        [FacultyId] [int] NOT NULL,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [CreatedBy] [int] NOT NULL DEFAULT 0,
        [CreatedOn] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedOn] [datetime2](7) NULL
    );
    CREATE INDEX IX_CertificationExams_CourseId ON [dbo].[CertificationExams]([CourseId]);
    CREATE INDEX IX_CertificationExams_FacultyId ON [dbo].[CertificationExams]([FacultyId]);
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CertificationExamQuestions')
BEGIN
    CREATE TABLE [dbo].[CertificationExamQuestions](
        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [CertificationExamId] [int] NOT NULL,
        [QuestionText] [nvarchar](max) NOT NULL,
        [QuestionType] [nvarchar](50) NOT NULL DEFAULT 'SingleChoice',
        [Marks] [decimal](18,2) NOT NULL DEFAULT 1.00,
        [Explanation] [nvarchar](max) NULL,
        [SortOrder] [int] NOT NULL DEFAULT 0,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [CreatedBy] [int] NOT NULL DEFAULT 0,
        [CreatedOn] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedOn] [datetime2](7) NULL
    );
    CREATE INDEX IX_CertificationExamQuestions_ExamId ON [dbo].[CertificationExamQuestions]([CertificationExamId]);
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CertificationExamQuestionOptions')
BEGIN
    CREATE TABLE [dbo].[CertificationExamQuestionOptions](
        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [CertificationExamQuestionId] [int] NOT NULL,
        [OptionText] [nvarchar](max) NOT NULL,
        [IsCorrect] [bit] NOT NULL DEFAULT 0,
        [SortOrder] [int] NOT NULL DEFAULT 0,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [CreatedBy] [int] NOT NULL DEFAULT 0,
        [CreatedOn] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedOn] [datetime2](7) NULL
    );
    CREATE INDEX IX_CertificationExamQuestionOptions_QuestionId ON [dbo].[CertificationExamQuestionOptions]([CertificationExamQuestionId]);
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CertificationExamAttempts')
BEGIN
    CREATE TABLE [dbo].[CertificationExamAttempts](
        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [CertificationExamId] [int] NOT NULL,
        [StudentId] [int] NOT NULL,
        [AttemptNumber] [int] NOT NULL DEFAULT 1,
        [StartedAt] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [SubmittedAt] [datetime2](7) NULL,
        [IsSubmitted] [bit] NOT NULL DEFAULT 0,
        [TimeRemainingSeconds] [int] NOT NULL DEFAULT 0,
        [TotalScore] [decimal](18,2) NOT NULL DEFAULT 0.00,
        [MaxScore] [decimal](18,2) NOT NULL DEFAULT 0.00,
        [Percentage] [decimal](5,2) NOT NULL DEFAULT 0.00,
        [Grade] [nvarchar](10) NOT NULL DEFAULT 'F',
        [IsPassed] [bit] NOT NULL DEFAULT 0,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [CreatedBy] [int] NOT NULL DEFAULT 0,
        [CreatedOn] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedOn] [datetime2](7) NULL
    );
    CREATE INDEX IX_CertificationExamAttempts_ExamStudent ON [dbo].[CertificationExamAttempts]([CertificationExamId], [StudentId]);
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CertificationExamStudentAnswers')
BEGIN
    CREATE TABLE [dbo].[CertificationExamStudentAnswers](
        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [CertificationExamAttemptId] [int] NOT NULL,
        [QuestionId] [int] NOT NULL,
        [SelectedOptionIds] [nvarchar](max) NULL,
        [IsCorrect] [bit] NOT NULL DEFAULT 0,
        [MarksAwarded] [decimal](18,2) NOT NULL DEFAULT 0.00,
        [AnsweredAt] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [CreatedBy] [int] NOT NULL DEFAULT 0,
        [CreatedOn] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedOn] [datetime2](7) NULL
    );
    CREATE INDEX IX_CertificationExamStudentAnswers_Attempt ON [dbo].[CertificationExamStudentAnswers]([CertificationExamAttemptId]);
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'IssuedCertificates')
BEGIN
    CREATE TABLE [dbo].[IssuedCertificates](
        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [CertificateNumber] [nvarchar](100) NOT NULL,
        [VerificationToken] [nvarchar](100) NOT NULL,
        [CertificationExamAttemptId] [int] NOT NULL,
        [StudentId] [int] NOT NULL,
        [CertificationExamId] [int] NOT NULL,
        [StudentFullName] [nvarchar](200) NOT NULL,
        [CertificationTopic] [nvarchar](250) NOT NULL,
        [FinalGrade] [nvarchar](20) NOT NULL,
        [IsPassed] [bit] NOT NULL DEFAULT 0,
        [IssueDate] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [PdfFilePath] [nvarchar](500) NOT NULL,
        [Status] [nvarchar](50) NOT NULL DEFAULT 'Valid',
        [RevocationReason] [nvarchar](500) NULL,
        [RevokedBy] [int] NULL,
        [RevokedOn] [datetime2](7) NULL,
        [GeneratedSuccessfully] [bit] NOT NULL DEFAULT 1,
        [GenerationError] [nvarchar](max) NULL,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [CreatedBy] [int] NOT NULL DEFAULT 0,
        [CreatedOn] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedBy] [int] NULL,
        [UpdatedOn] [datetime2](7) NULL
    );
    CREATE UNIQUE INDEX UX_IssuedCertificates_CertNumber ON [dbo].[IssuedCertificates]([CertificateNumber]);
    CREATE UNIQUE INDEX UX_IssuedCertificates_Token ON [dbo].[IssuedCertificates]([VerificationToken]);
    CREATE INDEX IX_IssuedCertificates_AttemptId ON [dbo].[IssuedCertificates]([CertificationExamAttemptId]);
    CREATE INDEX IX_IssuedCertificates_StudentId ON [dbo].[IssuedCertificates]([StudentId]);
END
";
                    await _db.Database.ExecuteSqlRawAsync(sql, ct);
                    _schemaEnsured = true;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing idempotent schema initialization for Feature 68 Certification Exams.");
            }
        }

        #region Faculty Operations

        public async Task<FacultyCertificationDashboardViewModel> GetFacultyDashboardAsync(int facultyId, CancellationToken ct = default)
        {
            await EnsureSchemaAsync(ct);

            var mappedCourseIds = await _db.CourseFacultyMaps.AsNoTracking()
                .Where(m => m.FacultyId == facultyId && m.IsActive)
                .Select(m => m.CourseId)
                .Distinct()
                .ToListAsync(ct);

            List<CourseMaster> courses;
            if (mappedCourseIds.Any())
            {
                courses = await _db.CourseMasters.AsNoTracking()
                    .Where(c => c.IsActive && mappedCourseIds.Contains(c.Id))
                    .OrderBy(c => c.Name)
                    .ToListAsync(ct);
            }
            else
            {
                courses = await _db.CourseMasters.AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync(ct);
            }

            var courseIds = courses.Select(c => c.Id).ToList();
            var subjects = await _db.SubjectMasters.AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync(ct);

            var examsQuery = _db.CertificationExams.AsNoTracking()
                .Include(e => e.Course)
                .Include(e => e.Subject)
                .Where(e => e.IsActive && (e.FacultyId == facultyId || courseIds.Contains(e.CourseId)))
                .OrderByDescending(e => e.CreatedOn);

            var exams = await examsQuery.ToListAsync(ct);
            var examIds = exams.Select(e => e.Id).ToList();

            var questionCounts = await _db.CertificationExamQuestions.AsNoTracking()
                .Where(q => q.IsActive && examIds.Contains(q.CertificationExamId))
                .GroupBy(q => q.CertificationExamId)
                .Select(g => new { ExamId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ExamId, x => x.Count, ct);

            var attemptCounts = await _db.CertificationExamAttempts.AsNoTracking()
                .Where(a => a.IsActive && examIds.Contains(a.CertificationExamId))
                .GroupBy(a => a.CertificationExamId)
                .Select(g => new { ExamId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ExamId, x => x.Count, ct);

            var certCounts = await _db.IssuedCertificates.AsNoTracking()
                .Where(c => c.IsActive && examIds.Contains(c.CertificationExamId) && c.Status == "Valid")
                .GroupBy(c => c.CertificationExamId)
                .Select(g => new { ExamId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ExamId, x => x.Count, ct);

            return new FacultyCertificationDashboardViewModel
            {
                Exams = exams,
                Courses = courses,
                Subjects = subjects,
                TotalExams = exams.Count,
                PublishedExams = exams.Count(e => e.Status == "Published"),
                TotalAttempts = attemptCounts.Values.Sum(),
                TotalCertificates = certCounts.Values.Sum(),
                QuestionCounts = questionCounts,
                AttemptCounts = attemptCounts,
                CertificateCounts = certCounts
            };
        }

        public async Task<CertificationExam?> GetExamByIdAsync(int examId, CancellationToken ct = default)
        {
            await EnsureSchemaAsync(ct);
            return await _db.CertificationExams
                .Include(e => e.Course)
                .Include(e => e.Subject)
                .Include(e => e.Questions.Where(q => q.IsActive))
                    .ThenInclude(q => q.Options.Where(o => o.IsActive))
                .FirstOrDefaultAsync(e => e.Id == examId && e.IsActive, ct);
        }

        public async Task<int> CreateExamAsync(CertificationExamFormModel model, int facultyId, CancellationToken ct = default)
        {
            await EnsureSchemaAsync(ct);

            var exam = new CertificationExam
            {
                Title = model.Title.Trim(),
                CourseId = model.CourseId,
                SubjectId = model.SubjectId,
                CertificationTopic = model.CertificationTopic.Trim(),
                Description = model.Description?.Trim(),
                DurationMinutes = model.DurationMinutes,
                TotalMarks = model.TotalMarks,
                PassingPercentage = model.PassingPercentage,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                MaxAttempts = model.MaxAttempts,
                Status = "Draft",
                FacultyId = facultyId,
                CreatedBy = facultyId,
                CreatedOn = DateTime.UtcNow,
                IsActive = true
            };

            _db.CertificationExams.Add(exam);
            await _db.SaveChangesAsync(ct);
            return exam.Id;
        }

        public async Task<bool> UpdateExamAsync(CertificationExamFormModel model, int facultyId, CancellationToken ct = default)
        {
            await EnsureSchemaAsync(ct);
            var exam = await _db.CertificationExams.FirstOrDefaultAsync(e => e.Id == model.Id && e.IsActive, ct);
            if (exam == null) return false;

            exam.Title = model.Title.Trim();
            exam.CourseId = model.CourseId;
            exam.SubjectId = model.SubjectId;
            exam.CertificationTopic = model.CertificationTopic.Trim();
            exam.Description = model.Description?.Trim();
            exam.DurationMinutes = model.DurationMinutes;
            exam.TotalMarks = model.TotalMarks;
            exam.PassingPercentage = model.PassingPercentage;
            exam.StartDate = model.StartDate;
            exam.EndDate = model.EndDate;
            exam.MaxAttempts = model.MaxAttempts;
            exam.UpdatedBy = facultyId;
            exam.UpdatedOn = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteExamAsync(int examId, int facultyId, CancellationToken ct = default)
        {
            await EnsureSchemaAsync(ct);
            var exam = await _db.CertificationExams.FirstOrDefaultAsync(e => e.Id == examId && e.IsActive, ct);
            if (exam == null) return false;

            exam.IsActive = false;
            exam.UpdatedBy = facultyId;
            exam.UpdatedOn = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<(bool Success, string Message)> PublishExamAsync(int examId, int facultyId, CancellationToken ct = default)
        {
            await EnsureSchemaAsync(ct);
            var exam = await _db.CertificationExams
                .Include(e => e.Questions.Where(q => q.IsActive))
                    .ThenInclude(q => q.Options.Where(o => o.IsActive))
                .FirstOrDefaultAsync(e => e.Id == examId && e.IsActive, ct);

            if (exam == null)
                return (false, "Exam not found.");

            if (!exam.Questions.Any())
                return (false, "Cannot publish an exam without questions. Please add questions first.");

            foreach (var q in exam.Questions)
            {
                if (q.QuestionType == "SingleChoice" || q.QuestionType == "TrueFalse")
                {
                    if (!q.Options.Any(o => o.IsCorrect))
                        return (false, $"Question '{q.QuestionText[..Math.Min(30, q.QuestionText.Length)]}...' does not have a correct answer marked.");
                }
                else if (q.QuestionType == "MultipleChoice")
                {
                    if (!q.Options.Any(o => o.IsCorrect))
                        return (false, $"Multiple choice question '{q.QuestionText[..Math.Min(30, q.QuestionText.Length)]}...' must have at least one correct option.");
                }
            }

            var sumMarks = exam.Questions.Sum(q => q.Marks);
            if (sumMarks > 0)
            {
                exam.TotalMarks = sumMarks;
            }

            exam.Status = "Published";
            exam.UpdatedBy = facultyId;
            exam.UpdatedOn = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return (true, "Certification examination published successfully! Students can now access and attempt it.");
        }

        public async Task<FacultyAttemptsViewModel> GetFacultyAttemptsAsync(int examId, int facultyId, CancellationToken ct = default)
        {
            await EnsureSchemaAsync(ct);
            var exam = await _db.CertificationExams
                .Include(e => e.Course)
                .Include(e => e.Subject)
                .FirstOrDefaultAsync(e => e.Id == examId && e.IsActive, ct);

            if (exam == null)
            {
                return new FacultyAttemptsViewModel();
            }

            var attempts = await _db.CertificationExamAttempts
                .Include(a => a.Student)
                .Where(a => a.CertificationExamId == examId && a.IsActive)
                .OrderByDescending(a => a.SubmittedAt ?? a.StartedAt)
                .ToListAsync(ct);

            var attemptIds = attempts.Select(a => a.Id).ToList();
            var certificates = await _db.IssuedCertificates
                .Where(c => c.IsActive && attemptIds.Contains(c.CertificationExamAttemptId))
                .ToDictionaryAsync(c => c.CertificationExamAttemptId, c => c, ct);

            return new FacultyAttemptsViewModel
            {
                Exam = exam,
                Attempts = attempts,
                CertificatesByAttemptId = certificates
            };
        }

        #endregion

        #region Question Operations

        public async Task<List<CertificationExamQuestion>> GetQuestionsAsync(int examId, CancellationToken ct = default)
        {
            await EnsureSchemaAsync(ct);
            return await _db.CertificationExamQuestions
                .Include(q => q.Options.Where(o => o.IsActive).OrderBy(o => o.SortOrder))
                .Where(q => q.CertificationExamId == examId && q.IsActive)
                .OrderBy(q => q.SortOrder)
                .ThenBy(q => q.Id)
                .ToListAsync(ct);
        }

        public async Task<CertificationExamQuestion?> GetQuestionByIdAsync(int questionId, CancellationToken ct = default)
        {
            await EnsureSchemaAsync(ct);
            return await _db.CertificationExamQuestions
                .Include(q => q.Options.Where(o => o.IsActive).OrderBy(o => o.SortOrder))
                .FirstOrDefaultAsync(q => q.Id == questionId && q.IsActive, ct);
        }

        public async Task<bool> SaveQuestionAsync(CertificationQuestionFormModel model, int facultyId, CancellationToken ct = default)
        {
            await EnsureSchemaAsync(ct);

            if (model.Id == 0)
            {
                var maxOrder = await _db.CertificationExamQuestions
                    .Where(q => q.CertificationExamId == model.CertificationExamId && q.IsActive)
                    .Select(q => (int?)q.SortOrder)
                    .MaxAsync(ct) ?? 0;

                var q = new CertificationExamQuestion
                {
                    CertificationExamId = model.CertificationExamId,
                    QuestionText = model.QuestionText.Trim(),
                    QuestionType = model.QuestionType,
                    Marks = model.Marks,
                    Explanation = model.Explanation?.Trim(),
                    SortOrder = maxOrder + 1,
                    CreatedBy = facultyId,
                    CreatedOn = DateTime.UtcNow,
                    IsActive = true
                };

                int optOrder = 1;
                foreach (var opt in model.Options)
                {
                    if (string.IsNullOrWhiteSpace(opt.OptionText)) continue;
                    q.Options.Add(new CertificationExamQuestionOption
                    {
                        OptionText = opt.OptionText.Trim(),
                        IsCorrect = opt.IsCorrect,
                        SortOrder = optOrder++,
                        CreatedBy = facultyId,
                        CreatedOn = DateTime.UtcNow,
                        IsActive = true
                    });
                }

                _db.CertificationExamQuestions.Add(q);
                await _db.SaveChangesAsync(ct);
                return true;
            }
            else
            {
                var q = await _db.CertificationExamQuestions
                    .Include(x => x.Options)
                    .FirstOrDefaultAsync(x => x.Id == model.Id && x.IsActive, ct);

                if (q == null) return false;

                q.QuestionText = model.QuestionText.Trim();
                q.QuestionType = model.QuestionType;
                q.Marks = model.Marks;
                q.Explanation = model.Explanation?.Trim();
                q.UpdatedBy = facultyId;
                q.UpdatedOn = DateTime.UtcNow;

                // Deactivate old options
                foreach (var old in q.Options)
                {
                    old.IsActive = false;
                    old.UpdatedBy = facultyId;
                    old.UpdatedOn = DateTime.UtcNow;
                }

                int optOrder = 1;
                foreach (var opt in model.Options)
                {
                    if (string.IsNullOrWhiteSpace(opt.OptionText)) continue;
                    q.Options.Add(new CertificationExamQuestionOption
                    {
                        OptionText = opt.OptionText.Trim(),
                        IsCorrect = opt.IsCorrect,
                        SortOrder = optOrder++,
                        CreatedBy = facultyId,
                        CreatedOn = DateTime.UtcNow,
                        IsActive = true
                    });
                }

                await _db.SaveChangesAsync(ct);
                return true;
            }
        }

        public async Task<bool> DeleteQuestionAsync(int questionId, int facultyId, CancellationToken ct = default)
        {
            await EnsureSchemaAsync(ct);
            var q = await _db.CertificationExamQuestions
                .Include(x => x.Options)
                .FirstOrDefaultAsync(x => x.Id == questionId && x.IsActive, ct);

            if (q == null) return false;

            q.IsActive = false;
            q.UpdatedBy = facultyId;
            q.UpdatedOn = DateTime.UtcNow;

            foreach (var opt in q.Options)
            {
                opt.IsActive = false;
                opt.UpdatedBy = facultyId;
                opt.UpdatedOn = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);
            return true;
        }

        #region AI Question Generation

        public async Task<AiQuestionGenerateResponseModel> GenerateAiQuestionsAsync(
            AiQuestionGenerateRequestModel request,
            int facultyId,
            CancellationToken ct = default)
        {
            if (request == null)
            {
                return new AiQuestionGenerateResponseModel { Success = false, ErrorMessage = "Invalid generation request parameters." };
            }

            await EnsureSchemaAsync(ct);

            // 1. Verify exam existence & faculty authorization
            var exam = await _db.CertificationExams
                .Include(e => e.Course)
                .Include(e => e.Subject)
                .FirstOrDefaultAsync(e => e.Id == request.ExamId && e.IsActive, ct);

            if (exam == null)
            {
                return new AiQuestionGenerateResponseModel { Success = false, ErrorMessage = "Certification exam not found." };
            }

            if (facultyId > 0 && exam.FacultyId != facultyId)
            {
                return new AiQuestionGenerateResponseModel { Success = false, ErrorMessage = "Unauthorized: You do not own this certification exam." };
            }

            if (exam.Status != "Draft")
            {
                return new AiQuestionGenerateResponseModel { Success = false, ErrorMessage = "Questions can only be added to exams in Draft status." };
            }

            // 2. Validate parameters
            int count = Math.Clamp(request.NumberOfQuestions, 1, 30);
            decimal marks = request.MarksPerQuestion > 0 ? request.MarksPerQuestion : 1.0m;
            string subject = string.IsNullOrWhiteSpace(request.Subject)
                ? (exam.Subject?.Name ?? exam.Course?.Name ?? "Academic Subject")
                : request.Subject.Trim();
            string topic = string.IsNullOrWhiteSpace(request.CertificationTopic)
                ? exam.CertificationTopic
                : request.CertificationTopic.Trim();
            string difficulty = string.IsNullOrWhiteSpace(request.Difficulty) ? "Mixed" : request.Difficulty.Trim();
            string questionType = string.IsNullOrWhiteSpace(request.QuestionType) ? "SingleChoice" : request.QuestionType.Trim();

            // 3. Resolve Gemini API Key & Model
            var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? _configuration?["Gemini:ApiKey"]
                ?? _configuration?["GEMINI_API_KEY"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return new AiQuestionGenerateResponseModel
                {
                    Success = false,
                    ErrorMessage = "AI service API key is not configured. Please ensure GEMINI_API_KEY environment variable is set or configured in appsettings.json."
                };
            }

            var model = _configuration?["Gemini:Model"] ?? "gemini-3.6-flash";

            // 4. Build Strict Prompt
            var prompt = BuildAiQuestionPrompt(subject, topic, difficulty, questionType, count, marks, request.AdditionalInstructions);

            // 5. Call Gemini API with transient retry
            using var client = _httpClientFactory?.CreateClient() ?? new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(_configuration?.GetValue<int>("Gemini:TimeoutSeconds", 45) ?? 45);

            string? rawJson = await CallGeminiForQuestionsAsync(client, apiKey, model, prompt, ct);

            if (string.IsNullOrWhiteSpace(rawJson))
            {
                return new AiQuestionGenerateResponseModel
                {
                    Success = false,
                    ErrorMessage = "Unable to generate questions at this time. Please check your network connection or retry in a few moments."
                };
            }

            // 6. Parse and Validate Response
            var generatedQuestions = ParseAndValidateAiQuestions(rawJson, marks, defaultType: questionType, defaultDifficulty: difficulty);

            if (!generatedQuestions.Any())
            {
                return new AiQuestionGenerateResponseModel
                {
                    Success = false,
                    ErrorMessage = "The AI service returned an unexpected response format. Please retry generation."
                };
            }

            return new AiQuestionGenerateResponseModel
            {
                Success = true,
                Questions = generatedQuestions
            };
        }

        public async Task<(bool Success, string Message, int SavedCount, decimal TotalMarks)> SaveBatchQuestionsAsync(
            int examId,
            List<CertificationQuestionFormModel> questions,
            int facultyId,
            CancellationToken ct = default)
        {
            if (questions == null || !questions.Any())
            {
                return (false, "No questions provided to save.", 0, 0);
            }

            await EnsureSchemaAsync(ct);

            var exam = await _db.CertificationExams
                .FirstOrDefaultAsync(e => e.Id == examId && e.IsActive, ct);

            if (exam == null)
            {
                return (false, "Certification exam not found.", 0, 0);
            }

            if (facultyId > 0 && exam.FacultyId != facultyId)
            {
                return (false, "Unauthorized: You do not own this certification exam.", 0, 0);
            }

            if (exam.Status != "Draft")
            {
                return (false, "Questions can only be added to exams in Draft status.", 0, 0);
            }

            var strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _db.Database.BeginTransactionAsync(ct);
                try
                {
                    var maxOrder = await _db.CertificationExamQuestions
                        .Where(q => q.CertificationExamId == examId && q.IsActive)
                        .Select(q => (int?)q.SortOrder)
                        .MaxAsync(ct) ?? 0;

                    int savedCount = 0;

                    foreach (var qModel in questions)
                    {
                        if (string.IsNullOrWhiteSpace(qModel.QuestionText)) continue;
                        if (qModel.Options == null || !qModel.Options.Any(o => o.IsCorrect)) continue;

                        maxOrder++;
                        var qEntity = new CertificationExamQuestion
                        {
                            CertificationExamId = examId,
                            QuestionText = qModel.QuestionText.Trim(),
                            QuestionType = string.IsNullOrWhiteSpace(qModel.QuestionType) ? "SingleChoice" : qModel.QuestionType.Trim(),
                            Marks = qModel.Marks > 0 ? qModel.Marks : 1.0m,
                            Explanation = qModel.Explanation?.Trim(),
                            SortOrder = maxOrder,
                            CreatedBy = facultyId,
                            CreatedOn = DateTime.UtcNow,
                            IsActive = true
                        };

                        int optOrder = 1;
                        foreach (var opt in qModel.Options)
                        {
                            if (string.IsNullOrWhiteSpace(opt.OptionText)) continue;
                            qEntity.Options.Add(new CertificationExamQuestionOption
                            {
                                OptionText = opt.OptionText.Trim(),
                                IsCorrect = opt.IsCorrect,
                                SortOrder = optOrder++,
                                CreatedBy = facultyId,
                                CreatedOn = DateTime.UtcNow,
                                IsActive = true
                            });
                        }

                        _db.CertificationExamQuestions.Add(qEntity);
                        savedCount++;
                    }

                    await _db.SaveChangesAsync(ct);

                    // Recalculate Exam Total Marks
                    var newTotalMarks = await _db.CertificationExamQuestions
                        .Where(q => q.CertificationExamId == examId && q.IsActive)
                        .SumAsync(q => q.Marks, ct);

                    exam.TotalMarks = newTotalMarks;
                    exam.UpdatedBy = facultyId;
                    exam.UpdatedOn = DateTime.UtcNow;
                    await _db.SaveChangesAsync(ct);

                    await tx.CommitAsync(ct);
                    return (true, $"Successfully added {savedCount} questions to the exam repository.", savedCount, newTotalMarks);
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync(ct);
                    _logger?.LogError(ex, "Failed to save batch AI questions for exam {ExamId}", examId);
                    return (false, "Failed to persist questions to database: " + ex.Message, 0, 0);
                }
            });
        }

        private string BuildAiQuestionPrompt(
            string subject,
            string topic,
            string difficulty,
            string questionType,
            int count,
            decimal marks,
            string? additionalInstructions)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are an expert university professor and certification examination author.");
            sb.AppendLine($"Generate exactly {count} high-quality, professional certification examination questions.");
            sb.AppendLine();
            sb.AppendLine("Parameters:");
            sb.AppendLine($"- Subject: {subject}");
            sb.AppendLine($"- Certification Topic: {topic}");
            sb.AppendLine($"- Difficulty Level: {difficulty}");
            sb.AppendLine($"- Target Question Type: {questionType}");
            sb.AppendLine($"- Marks per Question: {marks}");
            if (!string.IsNullOrWhiteSpace(additionalInstructions))
            {
                sb.AppendLine($"- Specific Instructions: {additionalInstructions.Trim()}");
            }
            sb.AppendLine();
            sb.AppendLine("Strict Question Requirements:");
            sb.AppendLine("1. SingleChoice questions: provide exactly 4 clear options. Exactly ONE option must have IsCorrect: true.");
            sb.AppendLine("2. MultipleChoice questions: provide exactly 4 clear options. At least TWO options must have IsCorrect: true.");
            sb.AppendLine("3. TrueFalse questions: provide exactly 2 options: 'True' and 'False'. Exactly ONE option must have IsCorrect: true.");
            sb.AppendLine("4. If Target Question Type is 'Mixed', distribute questions across SingleChoice, MultipleChoice, and TrueFalse.");
            sb.AppendLine("5. If Difficulty Level is 'Mixed', distribute questions across Easy, Medium, and Hard.");
            sb.AppendLine("6. All questions must test genuine conceptual depth, real-world application, or analytical reasoning.");
            sb.AppendLine("7. Include an educational Explanation for each question clarifying why the answer is correct.");
            sb.AppendLine("8. No empty or duplicate questions. Return ONLY a valid JSON array.");
            sb.AppendLine();
            sb.AppendLine("JSON Array Schema:");
            sb.AppendLine("[");
            sb.AppendLine("  {");
            sb.AppendLine("    \"QuestionText\": \"Question statement...\",");
            sb.AppendLine("    \"QuestionType\": \"SingleChoice\",");
            sb.AppendLine("    \"Difficulty\": \"Medium\",");
            sb.AppendLine($"    \"Marks\": {marks},");
            sb.AppendLine("    \"Explanation\": \"Reason why the answer is correct...\",");
            sb.AppendLine("    \"Options\": [");
            sb.AppendLine("      { \"OptionText\": \"Option A statement\", \"IsCorrect\": true },");
            sb.AppendLine("      { \"OptionText\": \"Option B statement\", \"IsCorrect\": false },");
            sb.AppendLine("      { \"OptionText\": \"Option C statement\", \"IsCorrect\": false },");
            sb.AppendLine("      { \"OptionText\": \"Option D statement\", \"IsCorrect\": false }");
            sb.AppendLine("    ]");
            sb.AppendLine("  }");
            sb.AppendLine("]");
            return sb.ToString();
        }

        private async Task<string?> CallGeminiForQuestionsAsync(
            HttpClient client,
            string apiKey,
            string primaryModel,
            string prompt,
            CancellationToken ct)
        {
            var modelsToTry = new[] { primaryModel, "gemini-3.6-flash", "gemini-3.8-flash" }
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var model in modelsToTry)
            {
                for (int attempt = 1; attempt <= 2; attempt++)
                {
                    try
                    {
                        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
                        var payload = new
                        {
                            contents = new[]
                            {
                                new
                                {
                                    role = "user",
                                    parts = new[] { new { text = prompt } }
                                }
                            },
                            generationConfig = new
                            {
                                response_mime_type = "application/json",
                                temperature = 0.35
                            }
                        };

                        var jsonPayload = JsonSerializer.Serialize(payload);
                        using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                        using var response = await client.PostAsync(url, content, ct);

                        if (response.IsSuccessStatusCode)
                        {
                            var responseBody = await response.Content.ReadAsStringAsync(ct);
                            using var doc = JsonDocument.Parse(responseBody);
                            if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                                candidates.GetArrayLength() > 0)
                            {
                                var cand = candidates[0];
                                if (cand.TryGetProperty("content", out var candContent) &&
                                    candContent.TryGetProperty("parts", out var parts) &&
                                    parts.GetArrayLength() > 0)
                                {
                                    return parts[0].GetProperty("text").GetString();
                                }
                            }
                        }

                        var statusCode = (int)response.StatusCode;
                        if (statusCode == 429 || statusCode >= 500)
                        {
                            await Task.Delay(attempt * 1500, ct);
                            continue;
                        }

                        // If 404 on model, break attempt loop and try next model
                        if (statusCode == 404) break;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger?.LogWarning(ex, "Gemini call attempt {Attempt} for model {Model} failed", attempt, model);
                        if (attempt < 2) await Task.Delay(1000, ct);
                    }
                }
            }

            return null;
        }

        private List<AiGeneratedQuestionModel> ParseAndValidateAiQuestions(
            string rawJson,
            decimal defaultMarks,
            string defaultType = "SingleChoice",
            string defaultDifficulty = "Medium")
        {
            var results = new List<AiGeneratedQuestionModel>();
            if (string.IsNullOrWhiteSpace(rawJson)) return results;

            try
            {
                // Remove potential markdown code fences ```json ... ```
                string cleaned = rawJson.Trim();
                if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                {
                    cleaned = cleaned.Substring(7);
                }
                else if (cleaned.StartsWith("```"))
                {
                    cleaned = cleaned.Substring(3);
                }
                if (cleaned.EndsWith("```"))
                {
                    cleaned = cleaned.Substring(0, cleaned.Length - 3);
                }
                cleaned = cleaned.Trim();

                using var doc = JsonDocument.Parse(cleaned);
                JsonElement arrayElement;

                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    arrayElement = doc.RootElement;
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (doc.RootElement.TryGetProperty("questions", out var qArray) && qArray.ValueKind == JsonValueKind.Array)
                        arrayElement = qArray;
                    else if (doc.RootElement.TryGetProperty("data", out var dArray) && dArray.ValueKind == JsonValueKind.Array)
                        arrayElement = dArray;
                    else
                        return results;
                }
                else
                {
                    return results;
                }

                var seenTexts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var el in arrayElement.EnumerateArray())
                {
                    string? qText = el.TryGetProperty("QuestionText", out var qt) ? qt.GetString() : null;
                    if (string.IsNullOrWhiteSpace(qText) && el.TryGetProperty("question", out var altQ))
                        qText = altQ.GetString();

                    if (string.IsNullOrWhiteSpace(qText)) continue;
                    qText = qText.Trim();

                    // Deduplicate
                    if (!seenTexts.Add(qText)) continue;

                    string qType = el.TryGetProperty("QuestionType", out var typeEl) ? (typeEl.GetString() ?? defaultType) : defaultType;
                    // Normalize type
                    if (qType.Contains("multi", StringComparison.OrdinalIgnoreCase)) qType = "MultipleChoice";
                    else if (qType.Contains("true", StringComparison.OrdinalIgnoreCase) || qType.Contains("false", StringComparison.OrdinalIgnoreCase)) qType = "TrueFalse";
                    else qType = "SingleChoice";

                    string diff = el.TryGetProperty("Difficulty", out var diffEl) ? (diffEl.GetString() ?? defaultDifficulty) : defaultDifficulty;
                    if (diff.Equals("Mixed", StringComparison.OrdinalIgnoreCase)) diff = "Medium";

                    decimal qMarks = defaultMarks;
                    if (el.TryGetProperty("Marks", out var marksEl) && marksEl.TryGetDecimal(out var mVal) && mVal > 0)
                        qMarks = mVal;

                    string? explanation = el.TryGetProperty("Explanation", out var expEl) ? expEl.GetString() : null;

                    var options = new List<CertificationOptionModel>();
                    if (el.TryGetProperty("Options", out var optArray) && optArray.ValueKind == JsonValueKind.Array)
                    {
                        int optOrder = 1;
                        foreach (var optEl in optArray.EnumerateArray())
                        {
                            string? optText = optEl.TryGetProperty("OptionText", out var ot) ? ot.GetString() : null;
                            if (string.IsNullOrWhiteSpace(optText) && optEl.TryGetProperty("text", out var altOt))
                                optText = altOt.GetString();

                            if (string.IsNullOrWhiteSpace(optText)) continue;

                            bool isCorrect = false;
                            if (optEl.TryGetProperty("IsCorrect", out var icEl))
                            {
                                if (icEl.ValueKind == JsonValueKind.True || icEl.ValueKind == JsonValueKind.False)
                                    isCorrect = icEl.GetBoolean();
                            }

                            options.Add(new CertificationOptionModel
                            {
                                OptionText = optText.Trim(),
                                IsCorrect = isCorrect,
                                SortOrder = optOrder++
                            });
                        }
                    }

                    // Strict Option Normalization
                    if (qType == "TrueFalse")
                    {
                        if (options.Count < 2)
                        {
                            options = new List<CertificationOptionModel>
                            {
                                new() { OptionText = "True", IsCorrect = true, SortOrder = 1 },
                                new() { OptionText = "False", IsCorrect = false, SortOrder = 2 }
                            };
                        }
                        else
                        {
                            // Ensure only 1 is correct
                            if (!options.Any(o => o.IsCorrect)) options[0].IsCorrect = true;
                            else if (options.Count(o => o.IsCorrect) > 1)
                            {
                                bool first = true;
                                foreach (var opt in options)
                                {
                                    if (opt.IsCorrect)
                                    {
                                        if (first) first = false;
                                        else opt.IsCorrect = false;
                                    }
                                }
                            }
                        }
                    }
                    else if (qType == "MultipleChoice")
                    {
                        if (options.Count < 2) continue;
                        if (!options.Any(o => o.IsCorrect))
                        {
                            options[0].IsCorrect = true;
                            if (options.Count > 1) options[1].IsCorrect = true;
                        }
                        else if (options.Count(o => o.IsCorrect) < 2 && options.Count > 1)
                        {
                            // Mark a second option correct for multiple choice
                            var second = options.FirstOrDefault(o => !o.IsCorrect);
                            if (second != null) second.IsCorrect = true;
                        }
                    }
                    else // SingleChoice
                    {
                        if (options.Count < 2) continue;
                        if (!options.Any(o => o.IsCorrect)) options[0].IsCorrect = true;
                        else if (options.Count(o => o.IsCorrect) > 1)
                        {
                            bool first = true;
                            foreach (var opt in options)
                            {
                                if (opt.IsCorrect)
                                {
                                    if (first) first = false;
                                    else opt.IsCorrect = false;
                                }
                            }
                        }
                    }

                    results.Add(new AiGeneratedQuestionModel
                    {
                        TempId = Guid.NewGuid().ToString("N"),
                        QuestionText = qText,
                        QuestionType = qType,
                        Difficulty = diff,
                        Marks = qMarks,
                        Explanation = explanation?.Trim(),
                        Options = options,
                        IsSelected = true
                    });
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to parse AI question generation output.");
            }

            return results;
        }

        #endregion

        #endregion

        #region Student Operations

        public async Task<List<StudentCertificationExamCardViewModel>> GetStudentExamsAsync(int studentId, CancellationToken ct = default)
        {
            await EnsureSchemaAsync(ct);

            // Real enrollments for student
            var enrolledCourseIds = await _db.CourseEnrollments.AsNoTracking()
                .Where(e => e.StudentId == studentId && e.IsActive && (e.Status == 0 || e.Status == 1))
                .Select(e => e.CourseId)
                .Distinct()
                .ToListAsync(ct);

            var now = DateTime.UtcNow;

            var exams = await _db.CertificationExams.AsNoTracking()
                .Include(e => e.Course)
                .Include(e => e.Subject)
                .Include(e => e.Questions.Where(q => q.IsActive))
                .Where(e => e.IsActive && e.Status == "Published" &&
                           (enrolledCourseIds.Contains(e.CourseId) || !enrolledCourseIds.Any()))
                .OrderByDescending(e => e.StartDate)
                .ToListAsync(ct);

            var examIds = exams.Select(e => e.Id).ToList();

            var studentAttempts = await _db.CertificationExamAttempts.AsNoTracking()
                .Where(a => a.StudentId == studentId && a.IsActive && examIds.Contains(a.CertificationExamId))
                .OrderByDescending(a => a.StartedAt)
                .ToListAsync(ct);

            var attemptIds = studentAttempts.Select(a => a.Id).ToList();
            var certificates = await _db.IssuedCertificates.AsNoTracking()
                .Where(c => c.StudentId == studentId && c.IsActive && attemptIds.Contains(c.CertificationExamAttemptId))
                .ToDictionaryAsync(c => c.CertificationExamAttemptId, c => c, ct);

            var resultList = new List<StudentCertificationExamCardViewModel>();

            foreach (var exam in exams)
            {
                var attemptsForExam = studentAttempts.Where(a => a.CertificationExamId == exam.Id).ToList();
                var attemptsUsed = attemptsForExam.Count(a => a.IsSubmitted);
                var activeAttempt = attemptsForExam.FirstOrDefault(a => !a.IsSubmitted);
                var latestSubmitted = attemptsForExam.FirstOrDefault(a => a.IsSubmitted);

                string examStatus;
                if (now < exam.StartDate)
                    examStatus = "Upcoming";
                else if (now > exam.EndDate)
                    examStatus = "Closed";
                else
                    examStatus = "Active";

                string attemptStatus;
                if (latestSubmitted != null)
                    attemptStatus = "Completed";
                else if (activeAttempt != null)
                    attemptStatus = "In Progress";
                else
                    attemptStatus = "Not Attempted";

                bool canAttempt = (examStatus == "Active") && (activeAttempt != null || attemptsUsed < exam.MaxAttempts);

                IssuedCertificate? cert = null;
                if (latestSubmitted != null && certificates.TryGetValue(latestSubmitted.Id, out var foundCert))
                {
                    cert = foundCert;
                }

                resultList.Add(new StudentCertificationExamCardViewModel
                {
                    Id = exam.Id,
                    Title = exam.Title,
                    CourseName = exam.Course?.Name ?? "General Course",
                    SubjectName = exam.Subject?.Name,
                    CertificationTopic = exam.CertificationTopic,
                    Description = exam.Description,
                    TotalQuestions = exam.Questions.Count,
                    DurationMinutes = exam.DurationMinutes,
                    PassingPercentage = exam.PassingPercentage,
                    TotalMarks = exam.TotalMarks,
                    StartDate = exam.StartDate,
                    EndDate = exam.EndDate,
                    ExamStatus = examStatus,
                    AttemptStatus = attemptStatus,
                    AttemptsUsed = attemptsUsed,
                    MaxAttempts = exam.MaxAttempts,
                    CanAttempt = canAttempt,
                    AttemptId = activeAttempt?.Id ?? latestSubmitted?.Id,
                    IsPassed = latestSubmitted?.IsPassed,
                    Grade = latestSubmitted?.Grade,
                    Score = latestSubmitted?.TotalScore,
                    Percentage = latestSubmitted?.Percentage,
                    CertificateId = cert?.Id,
                    CertificateNumber = cert?.CertificateNumber
                });
            }

            return resultList;
        }

        public async Task<StudentExamTakingViewModel?> StartOrResumeAttemptAsync(int examId, int studentId, CancellationToken ct = default)
        {
            await EnsureSchemaAsync(ct);

            var exam = await _db.CertificationExams
                .Include(e => e.Questions.Where(q => q.IsActive))
                    .ThenInclude(q => q.Options.Where(o => o.IsActive))
                .FirstOrDefaultAsync(e => e.Id == examId && e.IsActive && e.Status == "Published", ct);

            if (exam == null) return null;

            var existingAttempts = await _db.CertificationExamAttempts
                .Where(a => a.CertificationExamId == examId && a.StudentId == studentId && a.IsActive)
                .OrderByDescending(a => a.StartedAt)
                .ToListAsync(ct);

            var activeAttempt = existingAttempts.FirstOrDefault(a => !a.IsSubmitted);

            if (activeAttempt == null)
            {
                var submittedCount = existingAttempts.Count(a => a.IsSubmitted);
                if (submittedCount >= exam.MaxAttempts)
                {
                    return null; // Maximum attempts reached
                }

                // Check dates
                var now = DateTime.UtcNow;
                if (now < exam.StartDate || now > exam.EndDate)
                {
                    return null; // Outside valid window
                }

                activeAttempt = new CertificationExamAttempt
                {
                    CertificationExamId = examId,
                    StudentId = studentId,
                    AttemptNumber = submittedCount + 1,
                    StartedAt = now,
                    IsSubmitted = false,
                    TimeRemainingSeconds = exam.DurationMinutes * 60,
                    CreatedBy = studentId,
                    CreatedOn = now,
                    IsActive = true
                };

                _db.CertificationExamAttempts.Add(activeAttempt);
                await _db.SaveChangesAsync(ct);
            }

            // Server-side time enforcement
            var elapsedSeconds = (int)(DateTime.UtcNow - activeAttempt.StartedAt).TotalSeconds;
            var totalDurationSeconds = exam.DurationMinutes * 60;
            var remainingSeconds = Math.Max(0, totalDurationSeconds - elapsedSeconds);

            var existingAnswers = await _db.CertificationExamStudentAnswers
                .Where(ans => ans.CertificationExamAttemptId == activeAttempt.Id && ans.IsActive)
                .ToDictionaryAsync(ans => ans.QuestionId, ans => ans.SelectedOptionIds, ct);

            var questionList = new List<ExamTakingQuestionViewModel>();
            int idx = 1;
            foreach (var q in exam.Questions.OrderBy(q => q.SortOrder))
            {
                existingAnswers.TryGetValue(q.Id, out var selIdsStr);
                var selIds = new List<int>();
                if (!string.IsNullOrEmpty(selIdsStr))
                {
                    foreach (var s in selIdsStr.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (int.TryParse(s, out var sid)) selIds.Add(sid);
                    }
                }

                questionList.Add(new ExamTakingQuestionViewModel
                {
                    QuestionId = q.Id,
                    QuestionIndex = idx++,
                    QuestionText = q.QuestionText,
                    QuestionType = q.QuestionType,
                    Marks = q.Marks,
                    Options = q.Options.OrderBy(o => o.SortOrder).Select(o => new ExamTakingOptionViewModel
                    {
                        OptionId = o.Id,
                        OptionText = o.OptionText,
                        SortOrder = o.SortOrder
                    }).ToList(),
                    SelectedOptionIds = selIds
                });
            }

            return new StudentExamTakingViewModel
            {
                AttemptId = activeAttempt.Id,
                ExamId = exam.Id,
                ExamTitle = exam.Title,
                CertificationTopic = exam.CertificationTopic,
                DurationMinutes = exam.DurationMinutes,
                RemainingSeconds = remainingSeconds,
                StartedAt = activeAttempt.StartedAt,
                DueAt = activeAttempt.StartedAt.AddSeconds(totalDurationSeconds),
                Questions = questionList
            };
        }

        public async Task<bool> SaveAnswerAsync(int attemptId, int questionId, string selectedOptionIds, int remainingSeconds, int studentId, CancellationToken ct = default)
        {
            await EnsureSchemaAsync(ct);

            var attempt = await _db.CertificationExamAttempts
                .FirstOrDefaultAsync(a => a.Id == attemptId && a.StudentId == studentId && a.IsActive, ct);

            if (attempt == null || attempt.IsSubmitted) return false;

            attempt.TimeRemainingSeconds = remainingSeconds;
            attempt.UpdatedBy = studentId;
            attempt.UpdatedOn = DateTime.UtcNow;

            var existingAnswer = await _db.CertificationExamStudentAnswers
                .FirstOrDefaultAsync(ans => ans.CertificationExamAttemptId == attemptId && ans.QuestionId == questionId && ans.IsActive, ct);

            if (existingAnswer == null)
            {
                existingAnswer = new CertificationExamStudentAnswer
                {
                    CertificationExamAttemptId = attemptId,
                    QuestionId = questionId,
                    SelectedOptionIds = selectedOptionIds?.Trim(),
                    AnsweredAt = DateTime.UtcNow,
                    CreatedBy = studentId,
                    CreatedOn = DateTime.UtcNow,
                    IsActive = true
                };
                _db.CertificationExamStudentAnswers.Add(existingAnswer);
            }
            else
            {
                existingAnswer.SelectedOptionIds = selectedOptionIds?.Trim();
                existingAnswer.AnsweredAt = DateTime.UtcNow;
                existingAnswer.UpdatedBy = studentId;
                existingAnswer.UpdatedOn = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<(bool Success, string Message, int AttemptId)> SubmitAttemptAsync(int attemptId, int studentId, string wwwrootPath, CancellationToken ct = default)
        {
            await EnsureSchemaAsync(ct);

            var attempt = await _db.CertificationExamAttempts
                .Include(a => a.CertificationExam)
                    .ThenInclude(e => e!.Questions.Where(q => q.IsActive))
                        .ThenInclude(q => q.Options.Where(o => o.IsActive))
                .FirstOrDefaultAsync(a => a.Id == attemptId && a.StudentId == studentId && a.IsActive, ct);

            if (attempt == null)
                return (false, "Exam attempt not found.", 0);

            if (attempt.IsSubmitted)
                return (true, "Exam attempt already submitted.", attemptId);

            var answers = await _db.CertificationExamStudentAnswers
                .Where(ans => ans.CertificationExamAttemptId == attemptId && ans.IsActive)
                .ToListAsync(ct);

            var answersByQuestion = answers.ToDictionary(a => a.QuestionId);

            decimal totalScore = 0.00m;
            decimal maxScore = 0.00m;

            foreach (var q in attempt.CertificationExam!.Questions)
            {
                maxScore += q.Marks;
                answersByQuestion.TryGetValue(q.Id, out var ans);

                var correctOptionIds = q.Options.Where(o => o.IsCorrect).Select(o => o.Id).OrderBy(x => x).ToList();

                var selectedIds = new List<int>();
                if (ans != null && !string.IsNullOrEmpty(ans.SelectedOptionIds))
                {
                    foreach (var s in ans.SelectedOptionIds.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (int.TryParse(s, out var sid)) selectedIds.Add(sid);
                    }
                }
                selectedIds = selectedIds.OrderBy(x => x).ToList();

                bool isCorrect = false;
                decimal marksAwarded = 0.00m;

                if (q.QuestionType == "SingleChoice" || q.QuestionType == "TrueFalse")
                {
                    if (selectedIds.Count == 1 && correctOptionIds.Count == 1 && selectedIds[0] == correctOptionIds[0])
                    {
                        isCorrect = true;
                        marksAwarded = q.Marks;
                    }
                }
                else if (q.QuestionType == "MultipleChoice")
                {
                    if (selectedIds.Count == correctOptionIds.Count && selectedIds.SequenceEqual(correctOptionIds))
                    {
                        isCorrect = true;
                        marksAwarded = q.Marks;
                    }
                }

                if (ans != null)
                {
                    ans.IsCorrect = isCorrect;
                    ans.MarksAwarded = marksAwarded;
                    ans.UpdatedBy = studentId;
                    ans.UpdatedOn = DateTime.UtcNow;
                }

                totalScore += marksAwarded;
            }

            decimal percentage = maxScore > 0 ? Math.Round((totalScore / maxScore) * 100m, 2) : 0.00m;
            bool isPassed = percentage >= attempt.CertificationExam.PassingPercentage;

            // UGC 10-point Scale letter grade
            string grade;
            if (percentage >= 90m) grade = "O";
            else if (percentage >= 80m) grade = "A+";
            else if (percentage >= 70m) grade = "A";
            else if (percentage >= 60m) grade = "B+";
            else if (percentage >= 55m) grade = "B";
            else if (percentage >= 50m) grade = "C";
            else if (percentage >= attempt.CertificationExam.PassingPercentage) grade = "P";
            else grade = "F";

            attempt.IsSubmitted = true;
            attempt.SubmittedAt = DateTime.UtcNow;
            attempt.TimeRemainingSeconds = 0;
            attempt.TotalScore = totalScore;
            attempt.MaxScore = maxScore;
            attempt.Percentage = percentage;
            attempt.Grade = grade;
            attempt.IsPassed = isPassed;
            attempt.UpdatedBy = studentId;
            attempt.UpdatedOn = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            // Automatic Digital Certificate Generation (No Admin approval required!)
            try
            {
                await GenerateCertificateForAttemptAsync(attempt.Id, wwwrootPath, ct);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Automatic certificate generation encountered an issue for attempt {AttemptId}", attempt.Id);
            }

            return (true, "Certification examination submitted and evaluated successfully!", attempt.Id);
        }

        public async Task<CertificationExamResultViewModel?> GetAttemptResultAsync(int attemptId, int studentId, CancellationToken ct = default)
        {
            await EnsureSchemaAsync(ct);

            var attempt = await _db.CertificationExamAttempts
                .Include(a => a.Student)
                .Include(a => a.CertificationExam)
                    .ThenInclude(e => e!.Course)
                .Include(a => a.CertificationExam)
                    .ThenInclude(e => e!.Subject)
                .FirstOrDefaultAsync(a => a.Id == attemptId && a.IsActive &&
                    (a.StudentId == studentId || studentId == 0), ct);

            if (attempt == null || !attempt.IsSubmitted) return null;

            var cert = await _db.IssuedCertificates
                .FirstOrDefaultAsync(c => c.CertificationExamAttemptId == attemptId && c.IsActive, ct);

            return new CertificationExamResultViewModel
            {
                AttemptId = attempt.Id,
                ExamId = attempt.CertificationExamId,
                ExamTitle = attempt.CertificationExam?.Title ?? "Certification Exam",
                CertificationTopic = attempt.CertificationExam?.CertificationTopic ?? "Certified Academic Achievement",
                CourseName = attempt.CertificationExam?.Course?.Name ?? "Course",
                SubjectName = attempt.CertificationExam?.Subject?.Name,
                StudentName = attempt.Student?.Name ?? "Student",
                SubmittedAt = attempt.SubmittedAt ?? attempt.CreatedOn,
                TotalScore = attempt.TotalScore,
                MaxScore = attempt.MaxScore,
                Percentage = attempt.Percentage,
                PassingPercentage = attempt.CertificationExam?.PassingPercentage ?? 50m,
                Grade = attempt.Grade,
                IsPassed = attempt.IsPassed,
                CertificateId = cert?.Id,
                CertificateNumber = cert?.CertificateNumber,
                VerificationToken = cert?.VerificationToken
            };
        }

        #endregion

        #region Certificate Generation & Management

        public async Task<IssuedCertificate?> GenerateCertificateForAttemptAsync(int attemptId, string wwwrootPath, CancellationToken ct = default)
        {
            await EnsureSchemaAsync(ct);

            // 1. Idempotency Check: if certificate already generated successfully and file exists, return it
            var existingCert = await _db.IssuedCertificates
                .FirstOrDefaultAsync(c => c.CertificationExamAttemptId == attemptId && c.IsActive, ct);

            if (existingCert != null && existingCert.GeneratedSuccessfully)
            {
                var fullPath = Path.Combine(wwwrootPath, existingCert.PdfFilePath.TrimStart('/', '\\'));
                if (File.Exists(fullPath))
                {
                    return existingCert;
                }
            }

            // 2. Fetch full attempt and student info
            var attempt = await _db.CertificationExamAttempts
                .Include(a => a.Student)
                .Include(a => a.CertificationExam)
                .FirstOrDefaultAsync(a => a.Id == attemptId && a.IsActive, ct);

            if (attempt == null || !attempt.IsSubmitted) return null;

            string studentName = attempt.Student?.Name ?? "Student";
            string topic = attempt.CertificationExam?.CertificationTopic ?? "Professional Certification";
            string grade = attempt.Grade;
            bool isPassed = attempt.IsPassed;

            string certNumber = existingCert?.CertificateNumber ??
                $"EDC-CERT-{DateTime.UtcNow:yyyy}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
            string verifyToken = existingCert?.VerificationToken ?? Guid.NewGuid().ToString("N");
            DateTime issueDate = existingCert?.IssueDate ?? DateTime.UtcNow;

            // 3. Render clean certificate background image with dynamic text and QR code
            string templatePath = Path.Combine(wwwrootPath, "images", "certificates", "certificate_clean_template.png");
            if (!File.Exists(templatePath))
            {
                // Fallback to reference image if clean template missing
                templatePath = Path.Combine(wwwrootPath, "images", "certificates", "certificate_template.png");
            }

            string uploadsDir = Path.Combine(wwwrootPath, "uploads", "certificates");
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }

            string pdfFileName = $"{certNumber}.pdf";
            string pdfFilePath = Path.Combine(uploadsDir, pdfFileName);
            string relativePath = $"/uploads/certificates/{pdfFileName}";

            try
            {
                byte[] renderedImageBytes;

                using (var baseImg = System.Drawing.Image.FromFile(templatePath))
                using (var bmp = new Bitmap(baseImg.Width, baseImg.Height, PixelFormat.Format32bppArgb))
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                    // Draw template background
                    g.DrawImage(baseImg, 0, 0, baseImg.Width, baseImg.Height);

                    // Colors
                    var navyColor = System.Drawing.Color.FromArgb(20, 42, 76);
                    var darkBlueColor = System.Drawing.Color.FromArgb(30, 58, 138);
                    var slateColor = System.Drawing.Color.FromArgb(71, 85, 105);
                    var mutedColor = System.Drawing.Color.FromArgb(100, 116, 139);
                    var greenColor = System.Drawing.Color.FromArgb(22, 101, 52);

                    // 1. Student Name (Dynamic Font Size)
                    float nameFontSize = 74f;
                    if (studentName.Length > 30) nameFontSize = 52f;
                    else if (studentName.Length > 24) nameFontSize = 62f;

                    using (var fontName = new Font("Georgia", nameFontSize, FontStyle.Regular, GraphicsUnit.Pixel))
                    using (var brushNavy = new SolidBrush(navyColor))
                    {
                        var nameSize = g.MeasureString(studentName, fontName);
                        float xName = (bmp.Width - nameSize.Width) / 2f;
                        g.DrawString(studentName, fontName, brushNavy, xName, 665);
                    }

                    // 2. Certification Text
                    using (var fontText = new Font("Segoe UI", 28f, FontStyle.Regular, GraphicsUnit.Pixel))
                    using (var fontTopic = new Font("Segoe UI", 32f, FontStyle.Bold, GraphicsUnit.Pixel))
                    using (var fontGrade = new Font("Segoe UI", 34f, FontStyle.Bold, GraphicsUnit.Pixel))
                    using (var brushSlate = new SolidBrush(slateColor))
                    using (var brushNavyDark = new SolidBrush(navyColor))
                    using (var brushGrade = new SolidBrush(isPassed ? darkBlueColor : slateColor))
                    {
                        string line1 = isPassed
                            ? "has successfully completed the comprehensive certification examination in"
                            : "has completed and participated in the certification examination in";

                        var sLine1 = g.MeasureString(line1, fontText);
                        g.DrawString(line1, fontText, brushSlate, (bmp.Width - sLine1.Width) / 2f, 795);

                        var sTopic = g.MeasureString(topic, fontTopic);
                        g.DrawString(topic, fontTopic, brushNavyDark, (bmp.Width - sTopic.Width) / 2f, 845);

                        string gradeDesc = grade switch
                        {
                            "O" => "Outstanding",
                            "A+" => "Excellent",
                            "A" => "Very Good",
                            "B+" => "Good",
                            "B" => "Above Average",
                            "C" => "Average",
                            "P" => "Pass",
                            _ => isPassed ? "Qualified" : "Participation"
                        };

                        string line3 = isPassed
                            ? $"and is hereby awarded Grade: {grade} ({gradeDesc})"
                            : $"with recorded evaluation Grade: {grade} (Participation)";

                        var sLine3 = g.MeasureString(line3, fontGrade);
                        g.DrawString(line3, fontGrade, brushGrade, (bmp.Width - sLine3.Width) / 2f, 895);
                    }

                    // 3. Footer Metadata & Institutional Signatory
                    using (var fontMetaLbl = new Font("Segoe UI", 19f, FontStyle.Bold, GraphicsUnit.Pixel))
                    using (var fontMetaVal = new Font("Segoe UI", 19f, FontStyle.Regular, GraphicsUnit.Pixel))
                    using (var fontSigLbl = new Font("Segoe UI", 20f, FontStyle.Bold, GraphicsUnit.Pixel))
                    using (var fontSigSub = new Font("Segoe UI", 16f, FontStyle.Regular, GraphicsUnit.Pixel))
                    using (var brushMuted = new SolidBrush(mutedColor))
                    using (var brushVal = new SolidBrush(navyColor))
                    using (var brushInst = new SolidBrush(slateColor))
                    {
                        // Left side (under left signature line)
                        g.DrawString("Certificate ID:", fontMetaLbl, brushMuted, 370, 1170);
                        g.DrawString(certNumber, fontMetaVal, brushVal, 505, 1170);

                        g.DrawString("Issue Date:", fontMetaLbl, brushMuted, 370, 1205);
                        g.DrawString(issueDate.ToString("dd MMMM yyyy"), fontMetaVal, brushVal, 505, 1205);

                        g.DrawString("Authorized Authority: EduDigiClass University", fontMetaVal, brushInst, 370, 1240);

                        // Right side (under right signature line)
                        float sigCenterX = (1293f + 1608f) / 2f;
                        var sSig = g.MeasureString("Authorized Signatory", fontSigLbl);
                        g.DrawString("Authorized Signatory", fontSigLbl, brushVal, sigCenterX - (sSig.Width / 2f), 1172);

                        var sInst = g.MeasureString("EduDigiClass University", fontSigSub);
                        g.DrawString("EduDigiClass University", fontSigSub, brushMuted, sigCenterX - (sInst.Width / 2f), 1202);
                    }

                    // 4. Dedicated Bottom-Right QR Code & Verification (Zero Overlap with Signature Line)
                    string verificationUrl = $"https://edudigi.org/CertificationExam/Verify?token={verifyToken}";
                    using (var qrGenerator = new QRCodeGenerator())
                    {
                        var qrCodeData = qrGenerator.CreateQrCode(verificationUrl, QRCodeGenerator.ECCLevel.Q);
                        var qrCode = new PngByteQRCode(qrCodeData);
                        byte[] qrBytes = qrCode.GetGraphic(5);

                        int qrX = 1655;
                        int qrY = 1065;
                        int qrSize = 115;
                        int pad = 10;
                        int cardX = qrX - pad;
                        int cardY = qrY - pad;
                        int cardW = qrSize + (2 * pad);
                        int cardH = qrSize + (2 * pad);

                        // White card with quiet zone and subtle rounded border
                        using (var path = new GraphicsPath())
                        {
                            int radius = 8;
                            int diameter = radius * 2;
                            var rect = new Rectangle(cardX, cardY, cardW, cardH);
                            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
                            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
                            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
                            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
                            path.CloseFigure();

                            using (var brushWhite = new SolidBrush(System.Drawing.Color.White))
                            using (var penBorder = new Pen(System.Drawing.Color.FromArgb(226, 232, 240), 1.5f))
                            {
                                g.FillPath(brushWhite, path);
                                g.DrawPath(penBorder, path);
                            }
                        }

                        using (var msQr = new MemoryStream(qrBytes))
                        using (var qrImg = System.Drawing.Image.FromStream(msQr))
                        {
                            g.DrawImage(qrImg, qrX, qrY, qrSize, qrSize);
                        }

                        // Neat verification labels centered beneath the dedicated QR card
                        using (var fontQrHead = new Font("Segoe UI", 18f, FontStyle.Bold, GraphicsUnit.Pixel))
                        using (var fontQrSub = new Font("Segoe UI", 15f, FontStyle.Regular, GraphicsUnit.Pixel))
                        using (var brushHead = new SolidBrush(navyColor))
                        using (var brushSub = new SolidBrush(mutedColor))
                        {
                            float qrCenterX = cardX + (cardW / 2f);

                            var sHead = g.MeasureString("Scan to Verify", fontQrHead);
                            g.DrawString("Scan to Verify", fontQrHead, brushHead, qrCenterX - (sHead.Width / 2f), 1200);

                            var sSub = g.MeasureString("Official Digital Record", fontQrSub);
                            g.DrawString("Official Digital Record", fontQrSub, brushSub, qrCenterX - (sSub.Width / 2f), 1224);
                        }
                    }

                    using (var msBmp = new MemoryStream())
                    {
                        bmp.Save(msBmp, System.Drawing.Imaging.ImageFormat.Png);
                        renderedImageBytes = msBmp.ToArray();
                    }
                }

                // 5. Wrap in QuestPDF Landscape Document
                var pdfDoc = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4.Landscape());
                        page.Margin(0);
                        page.Content().Image(renderedImageBytes).FitArea();
                    });
                });

                byte[] pdfBytes = pdfDoc.GeneratePdf();
                await File.WriteAllBytesAsync(pdfFilePath, pdfBytes, ct);

                // 6. Save or update IssuedCertificate record in DB
                if (existingCert == null)
                {
                    existingCert = new IssuedCertificate
                    {
                        CertificateNumber = certNumber,
                        VerificationToken = verifyToken,
                        CertificationExamAttemptId = attemptId,
                        StudentId = attempt.StudentId,
                        CertificationExamId = attempt.CertificationExamId,
                        StudentFullName = studentName,
                        CertificationTopic = topic,
                        FinalGrade = grade,
                        IsPassed = isPassed,
                        IssueDate = issueDate,
                        PdfFilePath = relativePath,
                        Status = "Valid",
                        GeneratedSuccessfully = true,
                        CreatedBy = attempt.StudentId,
                        CreatedOn = DateTime.UtcNow,
                        IsActive = true
                    };
                    _db.IssuedCertificates.Add(existingCert);
                }
                else
                {
                    existingCert.PdfFilePath = relativePath;
                    existingCert.GeneratedSuccessfully = true;
                    existingCert.GenerationError = null;
                    existingCert.UpdatedOn = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync(ct);
                return existingCert;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "System.Drawing GDI+ pipeline unavailable. Falling back to native QuestPDF cross-platform rendering for attempt {AttemptId}", attemptId);

                try
                {
                    // Generate QR code bytes via PngByteQRCode (100% managed C#, zero GDI+ dependency)
                    string verificationUrl = $"https://edudigi.org/CertificationExam/Verify?token={verifyToken}";
                    byte[]? qrBytes = null;
                    try
                    {
                        using var qrGen = new QRCodeGenerator();
                        var qrData = qrGen.CreateQrCode(verificationUrl, QRCodeGenerator.ECCLevel.Q);
                        var pngQr = new PngByteQRCode(qrData);
                        qrBytes = pngQr.GetGraphic(4);
                    }
                    catch
                    {
                        // QR generation non-fatal for fallback
                    }

                    var fallbackDoc = Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4.Landscape());
                            page.Margin(28);
                            page.PageColor(Colors.Grey.Lighten5);

                            page.Content().Border(3).BorderColor(Colors.Blue.Darken3).Padding(24).Column(col =>
                            {
                                col.Item().AlignCenter().Text("EDUDIGICLASS UNIVERSITY")
                                    .FontSize(24).Bold().FontColor(Colors.Blue.Darken4);
                                col.Item().AlignCenter().Text("OFFICIAL CERTIFICATE OF COMPLETION")
                                    .FontSize(14).LetterSpacing(0.08f).FontColor(Colors.Grey.Darken1);

                                col.Item().PaddingTop(22).AlignCenter().Text("This is proudly presented to")
                                    .FontSize(12).Italic().FontColor(Colors.Grey.Darken2);

                                col.Item().PaddingTop(6).AlignCenter().Text(studentName)
                                    .FontSize(28).Bold().FontColor(Colors.Grey.Darken4);

                                col.Item().PaddingTop(12).AlignCenter().Text(
                                    isPassed
                                        ? $"for successfully completing the comprehensive examination in {topic} with Grade {grade}."
                                        : $"for completing and participating in the certification examination in {topic}.")
                                    .FontSize(13).FontColor(Colors.Grey.Darken3);

                                col.Item().PaddingTop(30).Row(row =>
                                {
                                    row.RelativeItem().Column(c =>
                                    {
                                        c.Item().Text($"Certificate ID: {certNumber}").FontSize(10).FontColor(Colors.Grey.Darken2);
                                        c.Item().Text($"Issue Date: {issueDate:dd MMMM yyyy}").FontSize(10).FontColor(Colors.Grey.Darken2);
                                        c.Item().Text($"Authorized Authority: EduDigiClass University").FontSize(9).FontColor(Colors.Grey.Darken1);
                                    });

                                    if (qrBytes != null && qrBytes.Length > 0)
                                    {
                                        row.ConstantItem(80).Column(qc =>
                                        {
                                            qc.Item().Width(70).Height(70).Image(qrBytes).FitArea();
                                            qc.Item().AlignCenter().Text("Scan to Verify").FontSize(7).FontColor(Colors.Grey.Darken2);
                                        });
                                    }

                                    row.RelativeItem().AlignRight().Column(c =>
                                    {
                                        c.Item().Text("Authorized Signatory").FontSize(10).Bold().FontColor(Colors.Grey.Darken4);
                                        c.Item().Text("EduDigiClass Academic Board").FontSize(9).FontColor(Colors.Grey.Darken1);
                                    });
                                });
                            });
                        });
                    });

                    byte[] fallbackPdfBytes = fallbackDoc.GeneratePdf();
                    await File.WriteAllBytesAsync(pdfFilePath, fallbackPdfBytes, ct);

                    if (existingCert == null)
                    {
                        existingCert = new IssuedCertificate
                        {
                            CertificateNumber = certNumber,
                            VerificationToken = verifyToken,
                            CertificationExamAttemptId = attemptId,
                            StudentId = attempt.StudentId,
                            CertificationExamId = attempt.CertificationExamId,
                            StudentFullName = studentName,
                            CertificationTopic = topic,
                            FinalGrade = grade,
                            IsPassed = isPassed,
                            IssueDate = issueDate,
                            PdfFilePath = relativePath,
                            Status = "Valid",
                            GeneratedSuccessfully = true,
                            CreatedBy = attempt.StudentId,
                            CreatedOn = DateTime.UtcNow,
                            IsActive = true
                        };
                        _db.IssuedCertificates.Add(existingCert);
                    }
                    else
                    {
                        existingCert.PdfFilePath = relativePath;
                        existingCert.GeneratedSuccessfully = true;
                        existingCert.GenerationError = null;
                        existingCert.UpdatedOn = DateTime.UtcNow;
                    }

                    await _db.SaveChangesAsync(ct);
                    return existingCert;
                }
                catch (Exception fallbackEx)
                {
                    _logger?.LogError(fallbackEx, "Both GDI+ and QuestPDF fallback failed for attempt {AttemptId}", attemptId);

                    if (existingCert == null)
                    {
                        existingCert = new IssuedCertificate
                        {
                            CertificateNumber = certNumber,
                            VerificationToken = verifyToken,
                            CertificationExamAttemptId = attemptId,
                            StudentId = attempt.StudentId,
                            CertificationExamId = attempt.CertificationExamId,
                            StudentFullName = studentName,
                            CertificationTopic = topic,
                            FinalGrade = grade,
                            IsPassed = isPassed,
                            IssueDate = issueDate,
                            PdfFilePath = relativePath,
                            Status = "Valid",
                            GeneratedSuccessfully = false,
                            GenerationError = ex.Message,
                            CreatedBy = attempt.StudentId,
                            CreatedOn = DateTime.UtcNow,
                            IsActive = true
                        };
                        _db.IssuedCertificates.Add(existingCert);
                    }
                    else
                    {
                        existingCert.GeneratedSuccessfully = false;
                        existingCert.GenerationError = ex.Message;
                        existingCert.UpdatedOn = DateTime.UtcNow;
                    }

                    await _db.SaveChangesAsync(ct);
                    return existingCert;
                }
            }
        }

        public async Task<byte[]?> GetCertificatePdfBytesAsync(int certificateId, CancellationToken ct = default)
        {
            await EnsureSchemaAsync(ct);
            var cert = await _db.IssuedCertificates.FirstOrDefaultAsync(c => c.Id == certificateId && c.IsActive, ct);
            if (cert == null) return null;

            if (File.Exists(cert.PdfFilePath))
            {
                return await File.ReadAllBytesAsync(cert.PdfFilePath, ct);
            }

            var candidates = new[]
            {
                Path.Combine("..", "TMS.Web", "wwwroot", cert.PdfFilePath.TrimStart('/', '\\')),
                Path.Combine("TMS", "TMS.Web", "wwwroot", cert.PdfFilePath.TrimStart('/', '\\')),
                Path.Combine("wwwroot", cert.PdfFilePath.TrimStart('/', '\\'))
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return await File.ReadAllBytesAsync(candidate, ct);
                }
            }

            return null;
        }

        public async Task<IssuedCertificate?> GetCertificateByIdAsync(int certificateId, CancellationToken ct = default)
        {
            await EnsureSchemaAsync(ct);
            return await _db.IssuedCertificates
                .Include(c => c.Student)
                .Include(c => c.CertificationExam)
                .FirstOrDefaultAsync(c => c.Id == certificateId && c.IsActive, ct);
        }

        public async Task<IssuedCertificate?> GetCertificateByTokenAsync(string token, CancellationToken ct = default)
        {
            await EnsureSchemaAsync(ct);
            return await _db.IssuedCertificates
                .Include(c => c.Student)
                .Include(c => c.CertificationExam)
                .FirstOrDefaultAsync(c => c.VerificationToken == token && c.IsActive, ct);
        }

        public async Task<List<IssuedCertificate>> GetAllCertificatesAsync(string? searchTerm, string? statusFilter, int? examId, CancellationToken ct = default)
        {
            await EnsureSchemaAsync(ct);
            var q = _db.IssuedCertificates.AsNoTracking()
                .Include(c => c.Student)
                .Include(c => c.CertificationExam)
                .Include(c => c.RevokedByUser)
                .Where(c => c.IsActive);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string s = searchTerm.Trim().ToLower();
                q = q.Where(c => c.StudentFullName.ToLower().Contains(s) ||
                                 c.CertificateNumber.ToLower().Contains(s) ||
                                 c.CertificationTopic.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "All")
            {
                q = q.Where(c => c.Status == statusFilter);
            }

            if (examId.HasValue && examId.Value > 0)
            {
                q = q.Where(c => c.CertificationExamId == examId.Value);
            }

            return await q.OrderByDescending(c => c.IssueDate).ToListAsync(ct);
        }

        public async Task<(bool Success, string Message)> RevokeCertificateAsync(int certificateId, string reason, int adminId, CancellationToken ct = default)
        {
            await EnsureSchemaAsync(ct);
            var cert = await _db.IssuedCertificates.FirstOrDefaultAsync(c => c.Id == certificateId && c.IsActive, ct);
            if (cert == null) return (false, "Certificate record not found.");

            cert.Status = "Revoked";
            cert.RevocationReason = reason?.Trim();
            cert.RevokedBy = adminId;
            cert.RevokedOn = DateTime.UtcNow;
            cert.UpdatedBy = adminId;
            cert.UpdatedOn = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return (true, $"Certificate {cert.CertificateNumber} has been successfully revoked.");
        }

        public async Task<(bool Success, string Message)> RetryCertificateGenerationAsync(int certificateId, string wwwrootPath, CancellationToken ct = default)
        {
            await EnsureSchemaAsync(ct);
            var cert = await _db.IssuedCertificates.FirstOrDefaultAsync(c => c.Id == certificateId && c.IsActive, ct);
            if (cert == null) return (false, "Certificate record not found.");

            var regenerated = await GenerateCertificateForAttemptAsync(cert.CertificationExamAttemptId, wwwrootPath, ct);
            if (regenerated != null && regenerated.GeneratedSuccessfully)
            {
                return (true, $"Certificate {cert.CertificateNumber} generated and saved successfully.");
            }
            return (false, $"Certificate regeneration failed: {regenerated?.GenerationError ?? "Unknown error"}");
        }

        #endregion
    }
}
