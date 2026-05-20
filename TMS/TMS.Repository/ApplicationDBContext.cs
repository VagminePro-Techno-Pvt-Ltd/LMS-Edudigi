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
        public DbSet<AssignmentSubmission> AssignmentSubmissions { get; set; }
        public DbSet<AssignmentGrade> AssignmentGrades { get; set; }

        // ─── Interactive PPT ───
        public DbSet<InteractivePPT> InteractivePPTs { get; set; }
        public DbSet<PPTSlideInteraction> PPTSlideInteractions { get; set; }






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

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
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

            // ─── LectureQuizAttempt: unique per student+question ───
            builder.Entity<LectureQuizAttempt>()
                .HasIndex(a => new { a.StudentId, a.LectureQuizQuestionId })
                .IsUnique()
                .HasDatabaseName("IX_LectureQuizAttempt_Student_Question");

            base.OnModelCreating(builder);
        }
    }
}
