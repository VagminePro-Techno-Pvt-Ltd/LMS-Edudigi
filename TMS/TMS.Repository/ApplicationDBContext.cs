using TMS.Models.Audit;
using TMS.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using TMS.Models;
using TMS.Models.Academics;
using TMS.Models.Account;
using TMS.Models.Masters;
using TMS.Models.Chat;
using TMS.Models.Training;
using TMS.Repository.SeedData;
using TMS.ViewModels.Masters;
using TMS.Models.Assessments;
using TMS.Models.LearningAssistant;

namespace TMS.Repository
{
    public class ApplicationDBContext : DbContext

    {
        public DbSet<UserMaster> UserMasters { get; set; }
        public DbSet<UserType> UserType { get; set; }
        public DbSet<RoleMaster> RoleMaster { get; set; }
        public DbSet<MenuMaster> MenuMaster { get; set; }
        public DbSet<FormMaster> FormMaster { get; set; }
        public DbSet<FormControlMaster> FormControlMaster { get; set; }
        public DbSet<FormControlRoleMaster> FormControlRoleMaster { get; set; }
        public DbSet<RolePermissions> RolePermissions { get; set; }
        #region Masters
        public DbSet<BadgeMaster> BadgeMasters { get; set; }
        public DbSet<CertificateMaster> CertificateMasters { get; set; }
        public DbSet<CompanyMaster> CompanyMasters { get; set; }
        public DbSet<CourseCategoryMaster> CourseCategoryMasters { get; set; }
        public DbSet<CourseEmploymentTypeMapping> CourseEmploymentTypeMappings { get; set; }
        public DbSet<CourseMaster> CourseMasters { get; set; }
        public DbSet<DepartmentMaster> DepartmentMasters { get; set; }
        public DbSet<DesignationMaster> DesignationMasters { get; set; }
        public DbSet<DivisionMaster> DivisionMasters { get; set; }
        public DbSet<EmploymentTypeMaster> EmploymentTypeMasters { get; set; }
        public DbSet<LocationMaster> LocationMasters { get; set; }
        public DbSet<PhotoGalleryMaster> PhotoGalleryMasters { get; set; }
        public DbSet<SignatureMaster> SignatureMasters { get; set; }
        public DbSet<VenueMaster> VenueMasters { get; set; }
        public DbSet<TrainingMatrix> TrainingMatrices { get; set; }
        public DbSet<QuestionMaster> QuestionMaster { get; set; }
        public DbSet<QuestionOptionMaster> QuestionOptionMaster { get; set; }
        public DbSet<QuizMaster> QuizMaster { get; set; }
        public DbSet<LanguageMaster> LanguageMaster { get; set; }
        public DbSet<TrainingScheduleStatusMaster> TrainingScheduleStatusMaster { get; set; }
        public DbSet<RankingMaster> RankingMasters { get; set; }
        public DbSet<QueryMaster> QueryMaster { get; set; }


        public DbSet<CourseEnrollment> CourseEnrollments { get; set; }
        public DbSet<DiscussionThread> DiscussionThreads { get; set; }
        public DbSet<DiscussionReply> DiscussionReplies { get; set; }
        public DbSet<DiscussionAttachment> DiscussionAttachments { get; set; }
        public DbSet<QuizMaster> QuizMasters { get; set; }
        public DbSet<QuizQuestion> QuizQuestions { get; set; }
        public DbSet<QuizOption> QuizOptions { get; set; }
        public DbSet<LectureMaterial> LectureMaterials { get; set; }
        public DbSet<CourseMaterialMapping> CourseMaterialMappings { get; set; }
        public DbSet<DocumentChunk> DocumentChunks { get; set; }
        public DbSet<DocumentIndexStatus> DocumentIndexStatuses { get; set; }

        public DbSet<CourseQuadrantMaster> CourseQuadrantMasters { get; set; }
        public DbSet<SemesterMaster> SemesterMasters { get; set; }
        public DbSet<UnitMaster> UnitMasters { get; set; }
        public DbSet<SubjectMaster> SubjectMasters { get; set; }

        public DbSet<CourseFacultyMapMaster> CourseFacultyMaps { get; set; }
        public DbSet<StudentQuizAnswer> StudentQuizAnswer { get; set; }
        public DbSet<StudentQuizAttempt> StudentQuizAttempt { get; set; }
        public DbSet<CourseMeeting> CourseMeeting { get; set; }
        public DbSet<FacultyQuizAssessment> FacultyQuizAssessment { get; set; }
        public DbSet<FacultyQuizQuestionAssessment> FacultyQuizQuestionAssessment { get; set; }

        // ─── Classroom Calendar ───
        public DbSet<ClassSeries> ClassSeries { get; set; }
        public DbSet<ClassInstance> ClassInstances { get; set; }
        public DbSet<ClassAttendance> ClassAttendances { get; set; }
        public DbSet<DailyAttendance> DailyAttendances { get; set; }

        // ─── Video Learning ───
        public DbSet<TopicMaster> TopicMasters { get; set; }
        public DbSet<TopicLookup> TopicLookups { get; set; }
        public DbSet<VideoMaster> VideoMasters { get; set; }
        public DbSet<VideoProgress> VideoProgresses { get; set; }
        public DbSet<LectureVideoProgress> LectureVideoProgresses { get; set; }
        public DbSet<VideoQuizQuestion> VideoQuizQuestions { get; set; }
        public DbSet<VideoQuizAttempt> VideoQuizAttempts { get; set; }

        // ─── Lecture Video Quiz (LectureMaterial-based) ───
        public DbSet<LectureQuizQuestion> LectureQuizQuestions { get; set; }
        public DbSet<LectureQuizAttempt> LectureQuizAttempts { get; set; }

        // ─── Announcements ───
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<AssignmentMaster> AssignmentMasters { get; set; }
        public DbSet<InternalAssessment> InternalAssessments { get; set; }
        public DbSet<InternalAssessmentSubmission> InternalAssessmentSubmissions { get; set; }
        public DbSet<InternalAssessmentPolicy> InternalAssessmentPolicies { get; set; }
        public DbSet<InternalAssessmentWindow> InternalAssessmentWindows { get; set; }
        public DbSet<InternalAssessmentQuestion> InternalAssessmentQuestions { get; set; }
        public DbSet<InternalAssessmentQuestionAnswer> InternalAssessmentQuestionAnswers { get; set; }
        public DbSet<AssignmentSubmission> AssignmentSubmissions { get; set; }
        public DbSet<AssignmentGrade> AssignmentGrades { get; set; }

        // ─── Interactive PPT ───
        public DbSet<InteractivePPT> InteractivePPTs { get; set; }
        public DbSet<PPTSlideInteraction> PPTSlideInteractions { get; set; }

        // ─── Feature 77: Student At-Risk Alerts & Moderation ───
        public DbSet<CourseActivityDeadline> CourseActivityDeadlines { get; set; }
        public DbSet<StudentRiskAlert> StudentRiskAlerts { get; set; }
        public DbSet<ContentModerationFlag> ContentModerationFlags { get; set; }

        // ─── Feature 68: Certification Exams & Automatic Digital Certificate Generation ───
        public DbSet<CertificationExam> CertificationExams { get; set; }
        public DbSet<CertificationExamQuestion> CertificationExamQuestions { get; set; }
        public DbSet<CertificationExamQuestionOption> CertificationExamQuestionOptions { get; set; }
        public DbSet<CertificationExamAttempt> CertificationExamAttempts { get; set; }
        public DbSet<CertificationExamStudentAnswer> CertificationExamStudentAnswers { get; set; }
        public DbSet<IssuedCertificate> IssuedCertificates { get; set; }

        #endregion
        #region Training schedule
        public DbSet<TrainingSchedule> TrainingSchedules { get; set; }
        public DbSet<TrainingScheduleLanguage> TrainingScheduleLanguages { get; set; }
        public DbSet<TrainingScheduleAttachment> TrainingScheduleAttachments { get; set; }
        public DbSet<TrainingScheduleApprovalHistory> TrainingScheduleApprovalHistory { get; set; }
        public DbSet<NominationRequest> NominationRequest { get; set; }
		public DbSet<NominationRequestRemarks> NominationRequestRemarks { get; set; }

        #endregion
        public DbSet<StudentQuizResponse> StudentQuizResponses { get; set; }
        public DbSet<StudentQuizAnswer> StudentQuizAnswers { get; set; }
        public DbSet<ForgotPassword> ForgotPassword { get; set; }

        #region Chat
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<ChatAttachment> ChatAttachments { get; set; }
        #endregion

        // ─── AI Learning Companion Caching ───
        public DbSet<LearningAssistantAnswerCache> LearningAssistantAnswerCaches { get; set; }

        // ─── Enterprise Assessment Module ───
        public DbSet<ExamMaster> ExamMasters { get; set; }
        public DbSet<ExamSection> ExamSections { get; set; }
        public DbSet<QuestionBank> QuestionBanks { get; set; }
        public DbSet<QuestionOption> QuestionOptions { get; set; }
        public DbSet<ExamQuestionMap> ExamQuestionMaps { get; set; }
        public DbSet<ExamAssignment> ExamAssignments { get; set; }
        public DbSet<StudentExamAttempt> StudentExamAttempts { get; set; }
        public DbSet<StudentExamAnswer> StudentExamAnswers { get; set; }
        public DbSet<FacultyEvaluation> FacultyEvaluations { get; set; }
        public DbSet<ExamResult> ExamResults { get; set; }
        public DbSet<QuestionPaper> QuestionPapers { get; set; }
        public DbSet<QuestionPaperQuestion> QuestionPaperQuestions { get; set; }
        public DbSet<InstitutionalGradingPolicy> InstitutionalGradingPolicies { get; set; }
        public DbSet<ProcessedAssessmentResult> ProcessedAssessmentResults { get; set; }
        public DbSet<ProctoringSession> ProctoringSessions { get; set; }
        public DbSet<ProctoringIncident> ProctoringIncidents { get; set; }

        public DbSet<AuditLog> AuditLogs { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private readonly IAuditUserProvider? _auditUserProvider;

        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options, IAuditUserProvider? auditUserProvider = null) : base(options)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            _auditUserProvider = auditUserProvider;
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Internal Assessment relational contract. Keep this model definition
            // authoritative; deployment remains an additive, separately reviewed migration.
            builder.Entity<InternalAssessment>(entity =>
            {
                entity.HasIndex(x => x.CourseId);
                entity.HasIndex(x => x.SubjectId);
                entity.HasIndex(x => x.WindowId);
                entity.HasIndex(x => x.CreatedBy);
                entity.HasOne<CourseMaster>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne<SubjectMaster>().WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne<InternalAssessmentWindow>().WithMany().HasForeignKey(x => x.WindowId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne<UserMaster>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne<UserMaster>().WithMany().HasForeignKey(x => x.ResultApprovedBy).OnDelete(DeleteBehavior.NoAction);
            });
            builder.Entity<InternalAssessmentSubmission>(entity =>
            {
                entity.HasIndex(x => new { x.AssessmentId, x.StudentId }).IsUnique();
                entity.HasIndex(x => x.StudentId);
                entity.HasIndex(x => x.EvaluatedBy);
                entity.HasOne<InternalAssessment>().WithMany().HasForeignKey(x => x.AssessmentId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne<UserMaster>().WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne<UserMaster>().WithMany().HasForeignKey(x => x.EvaluatedBy).OnDelete(DeleteBehavior.NoAction);
            });
            builder.Entity<InternalAssessmentPolicy>(entity =>
            {
                entity.HasIndex(x => x.SemesterId);
                entity.HasOne<SemesterMaster>().WithMany().HasForeignKey(x => x.SemesterId).OnDelete(DeleteBehavior.NoAction);
            });
            builder.Entity<InternalAssessmentWindow>(entity =>
            {
                entity.HasIndex(x => x.PolicyId);
                entity.HasIndex(x => x.CourseId);
                entity.HasOne<InternalAssessmentPolicy>().WithMany().HasForeignKey(x => x.PolicyId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne<CourseMaster>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.NoAction);
            });
            builder.Entity<InternalAssessmentQuestion>(entity =>
            {
                entity.HasIndex(x => new { x.AssessmentId, x.SortOrder });
                entity.HasOne<InternalAssessment>().WithMany().HasForeignKey(x => x.AssessmentId).OnDelete(DeleteBehavior.NoAction);
            });
            builder.Entity<InternalAssessmentQuestionAnswer>(entity =>
            {
                entity.HasIndex(x => new { x.SubmissionId, x.QuestionId }).IsUnique();
                entity.HasIndex(x => x.QuestionId);
                entity.HasOne<InternalAssessmentSubmission>().WithMany().HasForeignKey(x => x.SubmissionId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne<InternalAssessmentQuestion>().WithMany().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.NoAction);
            });

            foreach (var relations in builder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relations.DeleteBehavior = DeleteBehavior.NoAction;
            }

            foreach (var entity in builder.Model.GetEntityTypes())
            {
                int iIndex = 3;
                var properties = entity.ClrType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).OrderBy(t => t.MetadataToken);
                foreach (var property in properties)
                {
                    var prop = entity.FindProperty(property.Name);
                    if (prop != null)
                    {
                        var ano = prop.FindAnnotation("Relational:ColumnOrder");
                        if (ano == null)
                        {
                            prop.AddAnnotation("Relational:ColumnOrder", iIndex);
                            iIndex += 1;
                        }
                    }
                }
            }


            //Seed Data start
            //Comment once migration completed
            builder.SeedCommonMasterData();

            builder.SeedMenuMasterData();

            builder.SeedFormMasterData();
            builder.SeedFormControlMasterData();
            //Seed Data end

            builder.Entity<UserMaster>().HasOne(t => t.Role).WithMany().HasForeignKey(k => k.RoleId);
       
            builder.Entity<UserRoles>().HasKey(t => new { t.UserId, t.RoleId });
            builder.Entity<FormControlMaster>().HasKey(t => new { t.FormId, t.ControlId });
            builder.Entity<RolePermissions>().HasKey(t => new { t.RoleId, t.FormId });
            builder.Entity<TrainingScheduleLanguage>().HasKey(t => new { t.TraningScheduleId, t.LanguageId });

            builder.Entity<QuizQuestion>()
        .HasOne(q => q.Quiz)
        .WithMany(m => m.Questions)
        .HasForeignKey(q => q.QuizId)
        .OnDelete(DeleteBehavior.Cascade);

            // QuizQuestion → QuizOption (1-many)
            builder.Entity<QuizOption>()
                .HasOne(o => o.QuizQuestion)
                .WithMany(q => q.Options)
                .HasForeignKey(o => o.QuizQuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            // QuizQuestion → CorrectOption (self-reference)
            builder.Entity<QuizQuestion>()
                .HasOne(q => q.CorrectOption)
                .WithMany()
                .HasForeignKey(q => q.CorrectOptionId)
                .OnDelete(DeleteBehavior.NoAction);
            builder.Entity<FacultyQuizQuestionAssessment>()
                .HasOne(q => q.FacultyQuizAssessment)
                .WithMany(a => a.Questions)
                .HasForeignKey(q => q.FacultyQuizAssessmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<StudentQuizAnswer>()
                .HasOne(a => a.StudentQuizAttempt)
                .WithMany(t => t.Answers)
                .HasForeignKey(a => a.StudentQuizAttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            // ─── CourseEnrollment: Unique Constraint (CourseId + StudentId) ───
            builder.Entity<CourseEnrollment>()
                .HasIndex(e => new { e.CourseId, e.StudentId })
                .IsUnique()
                .HasDatabaseName("IX_CourseEnrollment_CourseId_StudentId");

            // ─── VideoProgress: unique per student+video ───
            builder.Entity<VideoProgress>()
                .HasIndex(vp => new { vp.StudentId, vp.VideoId })
                .IsUnique()
                .HasDatabaseName("IX_VideoProgress_Student_Video");

            // ─── LectureVideoProgress: unique per student+material ───
            builder.Entity<LectureVideoProgress>()
                .HasIndex(vp => new { vp.StudentId, vp.LectureMaterialId })
                .IsUnique()
                .HasDatabaseName("IX_LectureVideoProgress_Student_Material");

            // ─── VideoQuizAttempt: unique per student+question (no duplicate attempts) ───
            builder.Entity<VideoQuizAttempt>()
                .HasIndex(va => new { va.StudentId, va.VideoQuizQuestionId })
                .IsUnique()
                .HasDatabaseName("IX_VideoQuizAttempt_Student_Question");

            // ─── QuestionPaper & QuestionPaperQuestion ───
            builder.Entity<QuestionPaper>(entity =>
            {
                entity.HasIndex(x => x.CourseId);
                entity.HasIndex(x => x.FacultyId);
                entity.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(x => x.Semester).WithMany().HasForeignKey(x => x.SemesterId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(x => x.Faculty).WithMany().HasForeignKey(x => x.FacultyId).OnDelete(DeleteBehavior.NoAction);
            });

            builder.Entity<QuestionPaperQuestion>(entity =>
            {
                entity.HasIndex(x => x.QuestionPaperId);
                entity.HasIndex(x => x.QuestionBankId);
                entity.HasOne(x => x.QuestionPaper).WithMany(p => p.Questions).HasForeignKey(x => x.QuestionPaperId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.QuestionBank).WithMany().HasForeignKey(x => x.QuestionBankId).OnDelete(DeleteBehavior.NoAction);
            });

            // ─── ProcessedAssessmentResult ───
            builder.Entity<ProcessedAssessmentResult>(entity =>
            {
                entity.HasIndex(x => new { x.AssessmentId, x.StudentId, x.AttemptNumber }).IsUnique();
                entity.HasIndex(x => new { x.CourseId, x.StudentId });
                entity.HasOne(x => x.Assessment).WithMany().HasForeignKey(x => x.AssessmentId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(x => x.Semester).WithMany().HasForeignKey(x => x.SemesterId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(x => x.Policy).WithMany().HasForeignKey(x => x.PolicyId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(x => x.OriginalResult).WithMany().HasForeignKey(x => x.OriginalResultId).OnDelete(DeleteBehavior.NoAction);
            });

            // ─── QuestionBank Navigation Relations ───
            builder.Entity<QuestionBank>(entity =>
            {
                entity.HasOne(x => x.Unit).WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(x => x.Topic).WithMany().HasForeignKey(x => x.TopicId).OnDelete(DeleteBehavior.NoAction);
            });

            builder.Entity<TopicMaster>(entity =>
            {
                entity.Ignore(x => x.Description);
            });

            // ─── Document-Aware AI Assistant: Searchable Chunks & Index Status ───
            builder.Entity<DocumentChunk>(b =>
            {
                b.ToTable("DocumentChunks");
                b.HasKey(c => c.Id);
                b.HasIndex(c => c.LectureMaterialId)
                    .HasDatabaseName("IX_DocumentChunks_LectureMaterialId");
                b.Property(c => c.Heading).HasMaxLength(300);
            });

            builder.Entity<DocumentIndexStatus>(b =>
            {
                b.ToTable("DocumentIndexStatuses");
                b.HasKey(s => s.Id);
                b.HasIndex(s => s.LectureMaterialId)
                    .IsUnique()
                    .HasDatabaseName("IX_DocumentIndexStatuses_LectureMaterialId");
                b.Property(s => s.Status).HasMaxLength(50);
                b.Property(s => s.FileHash).HasMaxLength(100);
            });

            builder.Entity<LearningAssistantAnswerCache>(b =>
            {
                b.ToTable("LearningAssistantAnswerCaches");
                b.HasKey(c => c.Id);
                b.HasIndex(c => c.CacheKey)
                    .IsUnique()
                    .HasDatabaseName("IX_LearningAssistantAnswerCaches_CacheKey");
                b.HasIndex(c => c.LectureMaterialId)
                    .HasDatabaseName("IX_LearningAssistantAnswerCaches_LectureMaterialId");
                b.HasIndex(c => c.CourseId)
                    .HasDatabaseName("IX_LearningAssistantAnswerCaches_CourseId");
                b.HasIndex(c => c.ExpiresOn)
                    .HasDatabaseName("IX_LearningAssistantAnswerCaches_ExpiresOn");
                b.Property(c => c.CacheKey).HasMaxLength(128);
                b.Property(c => c.NormalizedQuery).HasMaxLength(500);
                b.Property(c => c.ModelName).HasMaxLength(50);
                b.Property(c => c.Language).HasMaxLength(20);
            });

            // ─── Feature 77: Student At-Risk & Moderation ───
            builder.Entity<CourseActivityDeadline>(entity =>
            {
                entity.HasIndex(x => new { x.CourseId, x.ActivityType, x.ActivityId }).IsUnique();
                entity.HasIndex(x => x.CourseId);
                entity.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.NoAction);
            });

            builder.Entity<StudentRiskAlert>(entity =>
            {
                entity.HasIndex(x => new { x.StudentId, x.IsResolved });
                entity.HasIndex(x => new { x.CourseId, x.StudentId });
                entity.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.NoAction);
            });

            builder.Entity<ContentModerationFlag>(entity =>
            {
                entity.HasIndex(x => x.Status);
                entity.HasIndex(x => x.AuthorUserId);
                entity.HasOne(x => x.AuthorUser).WithMany().HasForeignKey(x => x.AuthorUserId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(x => x.ReviewedByUser).WithMany().HasForeignKey(x => x.ReviewedByUserId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(x => x.Program).WithMany().HasForeignKey(x => x.ProgramId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.NoAction);
            });

            
            builder.Entity<AuditLog>(b =>
            {
                b.ToTable("AuditLogs");
                b.HasKey(x => x.Id);
                b.Property(x => x.ActionType).IsRequired().HasMaxLength(100);
                b.Property(x => x.ModuleName).IsRequired().HasMaxLength(100);
                b.Property(x => x.Status).IsRequired().HasMaxLength(50);
                b.Property(x => x.Username).HasMaxLength(150);
                b.Property(x => x.UserRole).HasMaxLength(50);
                b.Property(x => x.EntityType).HasMaxLength(100);
                b.Property(x => x.EntityId).HasMaxLength(100);
                b.Property(x => x.IpAddress).HasMaxLength(50);
                b.Property(x => x.CorrelationId).HasMaxLength(100);
                b.HasIndex(x => x.TimestampUtc).HasDatabaseName("IX_AuditLogs_TimestampUtc");
                b.HasIndex(x => x.UserId).HasDatabaseName("IX_AuditLogs_UserId");
                b.HasIndex(x => x.ModuleName).HasDatabaseName("IX_AuditLogs_ModuleName");
                b.HasIndex(x => x.ActionType).HasDatabaseName("IX_AuditLogs_ActionType");
                b.HasIndex(x => x.Status).HasDatabaseName("IX_AuditLogs_Status");
            });


            base.OnModelCreating(builder);
        }

        #region Audit Trail Interceptor
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (_auditUserProvider?.IsTrackingSuppressed() == true)
            {
                return await base.SaveChangesAsync(cancellationToken);
            }

            List<AuditEntry>? auditEntries = null;
            try
            {
                auditEntries = OnBeforeSaveChanges();
            }
            catch
            {
                // Never block save operations if audit gathering fails
            }

            var result = await base.SaveChangesAsync(cancellationToken);

            if (auditEntries != null && auditEntries.Count > 0)
            {
                try
                {
                    await OnAfterSaveChangesAsync(auditEntries, cancellationToken);
                }
                catch
                {
                    // Never block save operations if audit persistence fails
                }
            }

            return result;
        }

        public override int SaveChanges()
        {
            if (_auditUserProvider?.IsTrackingSuppressed() == true)
            {
                return base.SaveChanges();
            }

            List<AuditEntry>? auditEntries = null;
            try
            {
                auditEntries = OnBeforeSaveChanges();
            }
            catch
            {
                // Never block save operations if audit gathering fails
            }

            var result = base.SaveChanges();

            if (auditEntries != null && auditEntries.Count > 0)
            {
                try
                {
                    OnAfterSaveChanges(auditEntries);
                }
                catch
                {
                    // Never block save operations if audit persistence fails
                }
            }

            return result;
        }

        private List<AuditEntry> OnBeforeSaveChanges()
        {
            ChangeTracker.DetectChanges();
            var auditEntries = new List<AuditEntry>();

            var currentUserId = _auditUserProvider?.GetUserId();
            var currentUsername = _auditUserProvider?.GetUsername();
            var currentUserRole = _auditUserProvider?.GetUserRole();
            var currentIp = _auditUserProvider?.GetIpAddress();
            var currentCorrelationId = _auditUserProvider?.GetCorrelationId();

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is AuditLog || entry.State is EntityState.Detached or EntityState.Unchanged)
                    continue;

                var entityType = entry.Entity.GetType().Name;
                int proxyIdx = entityType.IndexOf('_');
                if (proxyIdx > 0) entityType = entityType.Substring(0, proxyIdx);

                // Exclude high-frequency background/cache/indexing entities
                if (entityType is "DocumentChunk" or "DocumentIndexStatus" or "LearningAssistantAnswerCache" or "LearningAssistantQueryLog" or "LearningAssistantCitationLog")
                    continue;

                var auditEntry = new AuditEntry(entry)
                {
                    EntityType = entityType,
                    UserId = currentUserId,
                    Username = currentUsername,
                    UserRole = currentUserRole,
                    IpAddress = currentIp,
                    CorrelationId = currentCorrelationId
                };

                // Fall back to entity properties if user not in context
                if (!auditEntry.UserId.HasValue)
                {
                    var updatedByProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedBy");
                    if (updatedByProp?.CurrentValue is int uId && uId > 0)
                        auditEntry.UserId = uId;
                    else
                    {
                        var createdByProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedBy");
                        if (createdByProp?.CurrentValue is int cId && cId > 0)
                            auditEntry.UserId = cId;
                    }
                }

                if (string.IsNullOrWhiteSpace(auditEntry.Username))
                {
                    auditEntry.Username = auditEntry.UserId.HasValue ? $"User #{auditEntry.UserId}" : "System";
                }
                if (string.IsNullOrWhiteSpace(auditEntry.UserRole))
                {
                    auditEntry.UserRole = auditEntry.UserId.HasValue ? "User" : "System";
                }

                if (entry.State != EntityState.Added)
                {
                    auditEntry.EntityId = GetPrimaryKeyValue(entry);
                }

                foreach (var property in entry.Properties)
                {
                    var propName = property.Metadata.Name;
                    if (IsSensitiveProperty(propName)) continue;
                    if (propName is "UpdatedOn" or "UpdatedBy") continue;

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            auditEntry.NewValues[propName] = property.CurrentValue;
                            auditEntry.ChangedFields.Add(propName);
                            break;

                        case EntityState.Deleted:
                            auditEntry.OldValues[propName] = property.OriginalValue;
                            auditEntry.ChangedFields.Add(propName);
                            break;

                        case EntityState.Modified:
                            if (property.IsModified)
                            {
                                var orig = property.OriginalValue;
                                var curr = property.CurrentValue;
                                if (!Equals(orig, curr))
                                {
                                    auditEntry.OldValues[propName] = orig;
                                    auditEntry.NewValues[propName] = curr;
                                    auditEntry.ChangedFields.Add(propName);
                                }
                            }
                            break;
                    }
                }

                // If modified but no auditable fields changed, skip
                if (entry.State == EntityState.Modified && auditEntry.ChangedFields.Count == 0)
                    continue;

                var (module, action) = DetermineModuleAndAction(entry, auditEntry.ChangedFields);
                auditEntry.ModuleName = module;
                auditEntry.ActionType = action;

                auditEntries.Add(auditEntry);
            }

            return auditEntries;
        }

        private async Task OnAfterSaveChangesAsync(List<AuditEntry> auditEntries, CancellationToken ct)
        {
            foreach (var auditEntry in auditEntries)
            {
                if (auditEntry.TargetEntry.State == EntityState.Added || string.IsNullOrEmpty(auditEntry.EntityId))
                {
                    auditEntry.EntityId = GetPrimaryKeyValue(auditEntry.TargetEntry);
                }

                AuditLogs.Add(auditEntry.ToAuditLog());
            }

            await base.SaveChangesAsync(ct);
        }

        private void OnAfterSaveChanges(List<AuditEntry> auditEntries)
        {
            foreach (var auditEntry in auditEntries)
            {
                if (auditEntry.TargetEntry.State == EntityState.Added || string.IsNullOrEmpty(auditEntry.EntityId))
                {
                    auditEntry.EntityId = GetPrimaryKeyValue(auditEntry.TargetEntry);
                }

                AuditLogs.Add(auditEntry.ToAuditLog());
            }

            base.SaveChanges();
        }

        private static string? GetPrimaryKeyValue(EntityEntry entry)
        {
            var key = entry.Metadata.FindPrimaryKey();
            if (key == null) return null;
            var values = key.Properties
                .Select(p => entry.Property(p.Name).CurrentValue?.ToString())
                .Where(v => !string.IsNullOrEmpty(v));
            return string.Join(",", values);
        }

        private static bool IsSensitiveProperty(string propName)
        {
            return propName.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
                   propName.Contains("Token", StringComparison.OrdinalIgnoreCase) ||
                   propName.Contains("Secret", StringComparison.OrdinalIgnoreCase) ||
                   propName.Contains("ApiKey", StringComparison.OrdinalIgnoreCase) ||
                   propName.Contains("SecurityStamp", StringComparison.OrdinalIgnoreCase) ||
                   propName.Contains("ConcurrencyStamp", StringComparison.OrdinalIgnoreCase) ||
                   propName.Contains("Pin", StringComparison.OrdinalIgnoreCase);
        }

        private static (string Module, string Action) DetermineModuleAndAction(EntityEntry entry, List<string> changedFields)
        {
            var entityName = entry.Entity.GetType().Name;
            int proxyIdx = entityName.IndexOf('_');
            if (proxyIdx > 0) entityName = entityName.Substring(0, proxyIdx);

            var state = entry.State;

            switch (entityName)
            {
                case "UserMaster":
                    if (state == EntityState.Added) return ("UserManagement", "CREATE_USER");
                    if (state == EntityState.Deleted) return ("UserManagement", "DELETE_USER");
                    if (changedFields.Contains("IsActive")) return ("UserManagement", "USER_STATUS_CHANGE");
                    if (changedFields.Contains("RoleId")) return ("UserManagement", "USER_ROLE_CHANGE");
                    return ("UserManagement", "UPDATE_USER");

                case "RoleMaster":
                    return ("RolesAndPermissions", state == EntityState.Added ? "CREATE_ROLE" : state == EntityState.Deleted ? "DELETE_ROLE" : "UPDATE_ROLE");

                case "RolePermissions":
                    return ("RolesAndPermissions", state == EntityState.Added ? "GRANT_PERMISSION" : state == EntityState.Deleted ? "REVOKE_PERMISSION" : "UPDATE_PERMISSION");

                case "CourseMaster":
                    return ("Courses", state == EntityState.Added ? "CREATE_COURSE" : state == EntityState.Deleted ? "DELETE_COURSE" : "UPDATE_COURSE");

                case "CourseCategoryMaster":
                    return ("Academics", state == EntityState.Added ? "CREATE_PROGRAM" : state == EntityState.Deleted ? "DELETE_PROGRAM" : "UPDATE_PROGRAM");

                case "SemesterMaster":
                    return ("Academics", state == EntityState.Added ? "CREATE_SEMESTER" : state == EntityState.Deleted ? "DELETE_SEMESTER" : "UPDATE_SEMESTER");

                case "SubjectMaster":
                    return ("Academics", state == EntityState.Added ? "CREATE_SUBJECT" : state == EntityState.Deleted ? "DELETE_SUBJECT" : "UPDATE_SUBJECT");

                case "UnitMaster":
                    return ("Academics", state == EntityState.Added ? "CREATE_UNIT" : state == EntityState.Deleted ? "DELETE_UNIT" : "UPDATE_UNIT");

                case "CourseFacultyMapMaster":
                    return ("FacultyMapping", state == EntityState.Added ? "ASSIGN_FACULTY" : state == EntityState.Deleted ? "REMOVE_FACULTY_MAPPING" : "UPDATE_FACULTY_MAPPING");

                case "CourseEnrollment":
                    return ("Enrollment", state == EntityState.Added ? "ENROLL_STUDENT" : state == EntityState.Deleted ? "DROP_ENROLLMENT" : "UPDATE_ENROLLMENT_STATUS");

                case "AssignmentMaster":
                    return ("Assignments", state == EntityState.Added ? "CREATE_ASSIGNMENT" : state == EntityState.Deleted ? "DELETE_ASSIGNMENT" : "UPDATE_ASSIGNMENT");

                case "AssignmentSubmission":
                    return ("Assignments", state == EntityState.Added ? "SUBMIT_ASSIGNMENT" : "UPDATE_ASSIGNMENT_SUBMISSION");

                case "AssignmentGrade":
                    return ("Assignments", "EVALUATE_ASSIGNMENT");

                case "InternalAssessment":
                    return ("InternalAssessment", state == EntityState.Added ? "CREATE_ASSESSMENT" : state == EntityState.Deleted ? "DELETE_ASSESSMENT" : "UPDATE_ASSESSMENT");

                case "InternalAssessmentPolicy":
                    return ("InternalAssessment", state == EntityState.Added ? "CREATE_ASSESSMENT_POLICY" : "UPDATE_ASSESSMENT_POLICY");

                case "InternalAssessmentWindow":
                    return ("InternalAssessment", state == EntityState.Added ? "SCHEDULE_ASSESSMENT_WINDOW" : "UPDATE_ASSESSMENT_WINDOW");

                case "InternalAssessmentQuestion":
                    return ("InternalAssessment", state == EntityState.Added ? "ADD_ASSESSMENT_QUESTION" : state == EntityState.Deleted ? "DELETE_ASSESSMENT_QUESTION" : "UPDATE_ASSESSMENT_QUESTION");

                case "InternalAssessmentSubmission":
                    if (changedFields.Contains("Marks") || changedFields.Contains("Grade") || changedFields.Contains("EvaluatedBy"))
                        return ("InternalAssessment", "UPDATE_STUDENT_MARKS");
                    return ("InternalAssessment", state == EntityState.Added ? "SUBMIT_ASSESSMENT" : "UPDATE_ASSESSMENT_SUBMISSION");

                case "InternalAssessmentQuestionAnswer":
                    if (changedFields.Contains("AwardedMarks") || changedFields.Contains("EvaluatedMarks"))
                        return ("InternalAssessment", "GRADE_ASSESSMENT_ANSWER");
                    return ("InternalAssessment", state == EntityState.Added ? "SUBMIT_ASSESSMENT_ANSWER" : "UPDATE_ASSESSMENT_ANSWER");

                case "QuestionPaper":
                    return ("Examination", state == EntityState.Added ? "CREATE_QUESTION_PAPER" : state == EntityState.Deleted ? "DELETE_QUESTION_PAPER" : "UPDATE_QUESTION_PAPER");

                case "QuestionPaperQuestion":
                    return ("Examination", state == EntityState.Added ? "ADD_PAPER_QUESTION" : state == EntityState.Deleted ? "REMOVE_PAPER_QUESTION" : "UPDATE_PAPER_QUESTION");

                case "QuestionBank":
                    return ("QuestionBank", state == EntityState.Added ? "ADD_QUESTION_BANK_ITEM" : state == EntityState.Deleted ? "DELETE_QUESTION_BANK_ITEM" : "UPDATE_QUESTION_BANK_ITEM");

                case "ProcessedAssessmentResult":
                    if (changedFields.Contains("GraceMarksAwarded"))
                        return ("InternalAssessment", "APPLY_GRACE_MARKS");
                    return ("InternalAssessment", "UPDATE_STUDENT_RESULT");

                case "InstitutionalGradingPolicy":
                    return ("InternalAssessment", "UPDATE_GRADING_POLICY");

                case "ExamMaster":
                    return ("Examination", state == EntityState.Added ? "CREATE_EXAM" : state == EntityState.Deleted ? "DELETE_EXAM" : "UPDATE_EXAM");

                case "ExamResult":
                    return ("Examination", "UPDATE_EXAM_RESULT");

                case "CertificationExam":
                    return ("DigitalCertificates", state == EntityState.Added ? "CREATE_CERTIFICATION_EXAM" : state == EntityState.Deleted ? "DELETE_CERTIFICATION_EXAM" : "UPDATE_CERTIFICATION_EXAM");

                case "CertificationExamQuestion":
                    return ("DigitalCertificates", state == EntityState.Added ? "ADD_CERT_QUESTION" : "UPDATE_CERT_QUESTION");

                case "IssuedCertificate":
                    if (changedFields.Contains("RevocationReason") || changedFields.Contains("Status"))
                        return ("DigitalCertificates", "REVOKE_CERTIFICATE");
                    return ("DigitalCertificates", state == EntityState.Added ? "ISSUE_CERTIFICATE" : "UPDATE_CERTIFICATE");

                case "CertificationExamAttempt":
                    return ("DigitalCertificates", state == EntityState.Added ? "START_CERTIFICATION_EXAM" : "SUBMIT_CERTIFICATION_EXAM");

                case "LectureMaterial":
                    return ("ContentLibrary", state == EntityState.Added ? "ADD_LECTURE_MATERIAL" : state == EntityState.Deleted ? "DELETE_LECTURE_MATERIAL" : "UPDATE_LECTURE_MATERIAL");

                case "ClassSeries":
                case "ClassInstance":
                case "ClassAttendance":
                    return ("ClassesAndAttendance", state == EntityState.Added ? $"CREATE_{entityName.ToUpperInvariant()}" : $"UPDATE_{entityName.ToUpperInvariant()}");

                default:
                    return (entityName, $"{state}_{entityName}".ToUpperInvariant());
            }
        }

        private sealed class AuditEntry
        {
            public EntityEntry TargetEntry { get; }
            public string ModuleName { get; set; } = string.Empty;
            public string ActionType { get; set; } = string.Empty;
            public string EntityType { get; set; } = string.Empty;
            public string? EntityId { get; set; }
            public Dictionary<string, object?> OldValues { get; } = new();
            public Dictionary<string, object?> NewValues { get; } = new();
            public List<string> ChangedFields { get; } = new();
            public int? UserId { get; set; }
            public string? Username { get; set; }
            public string? UserRole { get; set; }
            public string? IpAddress { get; set; }
            public string? CorrelationId { get; set; }
            public string? Details { get; set; }

            public AuditEntry(EntityEntry entry)
            {
                TargetEntry = entry;
            }

            public AuditLog ToAuditLog()
            {
                return new AuditLog
                {
                    UserId = UserId,
                    Username = Username,
                    UserRole = UserRole,
                    ActionType = ActionType,
                    ModuleName = ModuleName,
                    EntityType = EntityType,
                    EntityId = EntityId,
                    OldValuesJson = OldValues.Count > 0 ? JsonSerializer.Serialize(OldValues) : null,
                    NewValuesJson = NewValues.Count > 0 ? JsonSerializer.Serialize(NewValues) : null,
                    ChangedFieldsJson = ChangedFields.Count > 0 ? string.Join(", ", ChangedFields) : null,
                    TimestampUtc = DateTime.UtcNow,
                    IpAddress = IpAddress,
                    Status = "SUCCESS",
                    CorrelationId = CorrelationId,
                    Details = Details
                };
            }
        }
        #endregion


    }

}

