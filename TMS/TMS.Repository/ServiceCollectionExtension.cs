using TMS.Models;
using TMS.Models.Account;
using TMS.Models.Masters;
using TMS.Repository.Helpers;
using TMS.Repository.Managers;
using TMS.Repository.Managers.Implementations;
using TMS.Repository.Managers.Implementations.Masters;
using TMS.Repository.Repositories;
using TMS.Repository.Repositories.Implementations;
using TMS.ViewModels;
using TMS.ViewModels.Masters;
using Microsoft.Extensions.DependencyInjection;
using TMS.Models.Training;
using TMS.ViewModels.Training;
using TMS.ViewModels.Account;
using TMS.Models.Academics;
using TMS.ViewModels.Academics;

namespace TMS.Repository
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddRepositoryDependencies(this IServiceCollection services)
        {
            services.AddTransient(provider =>
            {
                var factory = new ContextFactory();
                return factory.CreateDbContext(Array.Empty<string>());
            });

            var config = new AutoMapper.MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new AutoMapperProfileConfiguration());
            });
            var mapper = config.CreateMapper();
            services.AddSingleton(mapper);

            #region Helpers
            services.AddScoped<IDBHelper, DBHelper>();
            #endregion

            #region Repositories
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped(typeof(IBaseModelRepository<>), typeof(BaseModelRepository<>));
            services.AddScoped(typeof(IBaseIdModelRepository<>), typeof(BaseIdModelRepository<>));
            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<IApprovalRepository, ApprovalRepository>(); 
            services.AddScoped<ITrainingScheduleRepository, TrainingScheduleRepository>();
            #endregion

            #region Managers    
            #region Masters
            services.AddScoped<IMasterManager<RoleMasterViewModel>, MasterManager<RoleMasterViewModel, RoleMaster>>();
            services.AddScoped<IMasterManager<UserViewModel>, MasterManager<UserViewModel, UserMaster>>();
            services.AddScoped<IMasterManager<CompanyMasterViewModel>, MasterManager<CompanyMasterViewModel, CompanyMaster>>();
            services.AddScoped<IMasterManager<BadgeMasterViewModel>, MasterManager<BadgeMasterViewModel, BadgeMaster>>();
            services.AddScoped<IMasterManager<CertificateMasterViewModel>, MasterManager<CertificateMasterViewModel, CertificateMaster>>();
            services.AddScoped<IMasterManager<CourseCategoryMasterViewModel>, MasterManager<CourseCategoryMasterViewModel, CourseCategoryMaster>>();
            services.AddScoped<IMasterManager<CourseMasterViewModel>, CourseMasterManager>();
            services.AddScoped<IMasterManager<DepartmentMasterViewModel>, MasterManager<DepartmentMasterViewModel, DepartmentMaster>>();
            services.AddScoped<IMasterManager<DesignationMasterViewModel>, MasterManager<DesignationMasterViewModel, DesignationMaster>>();
            services.AddScoped<IMasterManager<DivisionMasterViewModel>, MasterManager<DivisionMasterViewModel, DivisionMaster>>();
            services.AddScoped<IMasterManager<EmploymentTypeMasterViewModel>, MasterManager<EmploymentTypeMasterViewModel, EmploymentTypeMaster>>();
            services.AddScoped<IMasterManager<LocationMasterViewModel>, MasterManager<LocationMasterViewModel, LocationMaster>>();
            services.AddScoped<IMasterBaseManager<PhotoGalleryMasterViewModel>, MasterBaseManager<PhotoGalleryMasterViewModel, PhotoGalleryMaster>>();
            services.AddScoped<IMasterManager<SignatureMasterViewModel>, MasterManager<SignatureMasterViewModel, SignatureMaster>>();
            services.AddScoped<IMasterManager<VenueMasterViewModel>, MasterManager<VenueMasterViewModel, VenueMaster>>();
             
            
            services.AddScoped<IMasterManager<LanguageMasterViewModel>, MasterManager<LanguageMasterViewModel, LanguageMaster>>();
            services.AddScoped<IMasterManager<TrainingScheduleStatusMasterViewModel>, MasterManager<TrainingScheduleStatusMasterViewModel, TrainingScheduleStatusMaster>>();
          
			services.AddScoped<IMasterBaseManager<NominationRequestViewModel>, MasterBaseManager<NominationRequestViewModel,NominationRequest>>();
			services.AddScoped<IMasterBaseManager<NominationRequestRemarksViewModel>, MasterBaseManager<NominationRequestRemarksViewModel, NominationRequestRemarks>>();
			 

            services.AddScoped<IMasterManager<CourseMasterViewModel>, CourseMasterManager>();

            services.AddScoped<IMasterManager<SemesterMasterViewModel>, MasterManager<SemesterMasterViewModel, SemesterMaster>>();
            services.AddScoped<IMasterManager<UnitMasterViewModel>, MasterManager<UnitMasterViewModel, UnitMaster>>();
            services.AddScoped<IMasterManager<SubjectMasterViewModel>, MasterManager<SubjectMasterViewModel, SubjectMaster>>();
            services.AddScoped<IMasterBaseManager<CourseQuadrantViewModel>, MasterBaseManager<CourseQuadrantViewModel, CourseQuadrantMaster>>();


            services.AddScoped<IMasterBaseManager<CourseQuadrantViewModel>, MasterBaseManager<CourseQuadrantViewModel, CourseQuadrantMaster>>();
            services.AddScoped<IMasterBaseManager<LectureMaterialViewModel>, MasterBaseManager<LectureMaterialViewModel, LectureMaterial>>();
            services.AddScoped<IMasterBaseManager<CourseMaterialMappingViewModel>, MasterBaseManager<CourseMaterialMappingViewModel, CourseMaterialMapping>>();
            services.AddScoped<IMasterBaseManager<CourseQuizViewModel>, MasterBaseManager<CourseQuizViewModel, QuizMaster>>();
            services.AddScoped<IMasterBaseManager<CourseQuizQuestionViewModel>, MasterBaseManager<CourseQuizQuestionViewModel, QuizQuestion>>();
            services.AddScoped<IMasterBaseManager<DiscussionThreadViewModel>, MasterBaseManager<DiscussionThreadViewModel, DiscussionThread>>();
            services.AddScoped<IDiscussionManager, DiscussionManager>();
            services.AddScoped<IEnrollmentManager, EnrollmentManager>();
            services.AddScoped<IMasterBaseManager<CourseEnrollmentViewModel>>(sp => sp.GetRequiredService<IEnrollmentManager>());
            services.AddScoped<IFacultyMappingManager, FacultyMappingManager>();
            services.AddScoped<IMasterBaseManager<CourseFacultyMapViewModel>>(sp => sp.GetRequiredService<IFacultyMappingManager>());
            services.AddScoped<IMasterBaseManager<QuizMasterViewModel>, MasterBaseManager<QuizMasterViewModel, QuizMaster>>();
            services.AddScoped<IMasterBaseManager<StudentQuizAttemptViewModel>, MasterBaseManager<StudentQuizAttemptViewModel, StudentQuizAttempt>>();
            services.AddScoped<IMasterBaseManager<CourseMeetingViewModel>, MasterBaseManager<CourseMeetingViewModel, CourseMeeting>>();
            services.AddScoped<IMasterBaseManager<StudentQuizAnswerViewModel>, MasterBaseManager<StudentQuizAnswerViewModel, StudentQuizAnswer>>();

            services.AddScoped<IMasterBaseManager<FacultyQuizQuestionAssessmentViewModel>, MasterBaseManager<FacultyQuizQuestionAssessmentViewModel, FacultyQuizQuestionAssessment>>();
            services.AddScoped<IMasterBaseManager<FacultyQuizAssessmentViewModel>, MasterBaseManager<FacultyQuizAssessmentViewModel, FacultyQuizAssessment>>();
            services.AddScoped<IMasterBaseManager<ForgotPasswordViewModel>, MasterBaseManager<ForgotPasswordViewModel, ForgotPassword>>();
            services.AddScoped<IMasterBaseManager<AssignmentMasterViewModel>, MasterBaseManager<AssignmentMasterViewModel, AssignmentMaster>>();
            services.AddScoped<IMasterBaseManager<AssignmentSubmissionViewModel>, MasterBaseManager<AssignmentSubmissionViewModel, AssignmentSubmission>>();
            services.AddScoped<IMasterBaseManager<AssignmentGradeViewModel>, MasterBaseManager<AssignmentGradeViewModel, AssignmentGrade>>();


            // ─── Classroom Calendar ───
            services.AddScoped<IMasterBaseManager<ClassSeriesViewModel>, MasterBaseManager<ClassSeriesViewModel, ClassSeries>>();
            services.AddScoped<IMasterBaseManager<ClassInstanceViewModel>, MasterBaseManager<ClassInstanceViewModel, ClassInstance>>();
            services.AddScoped<IMasterBaseManager<ClassAttendanceViewModel>, MasterBaseManager<ClassAttendanceViewModel, ClassAttendance>>();
            services.AddScoped<IDailyAttendanceManager, DailyAttendanceManager>();

            // ─── Announcements ───
            services.AddScoped<IAnnouncementManager, AnnouncementManager>();


            #endregion

            services.AddScoped<IAccountManager, AccountManager>();
            services.AddScoped<IApprovalManager, ApprovalManager>();
            services.AddScoped<IChatManager, ChatManager>();
            services.AddScoped<IAssessmentManager, AssessmentManager>();
            services.AddScoped<IBulkUserManager, BulkUserManager>();   
         
            #endregion

            return services;
        }
    }
}
