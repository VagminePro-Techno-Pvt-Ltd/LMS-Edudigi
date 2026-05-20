using AutoMapper;
using TMS.Models;
using TMS.Models.Academics;
using TMS.Models.Account;
using TMS.Models.Masters;
using TMS.Models.Training;
using TMS.ViewModels;
using TMS.ViewModels.Academics;
using TMS.ViewModels.Account;
using TMS.ViewModels.Masters;
using TMS.ViewModels.Training;

namespace TMS.Repository
{
    internal class AutoMapperProfileConfiguration : Profile
    {
        public AutoMapperProfileConfiguration()
        {
            CreateMap<BaseModel, BaseViewModel>().ReverseMap();
            CreateMap<BaseMasterModel, BaseMasterViewModel>().ReverseMap();
            #region Account
            CreateMap<UserMaster, UserViewModel>().ReverseMap();
            CreateMap<UserRoles, UserRolesViewModel>().ReverseMap();
            CreateMap<UserType, UserTypeViewModel>().ReverseMap();
            CreateMap<RoleMaster, RoleMasterViewModel>().ReverseMap();
            #endregion
            #region Master
            CreateMap<BadgeMaster, BadgeMasterViewModel>().ReverseMap();
            CreateMap<CertificateMaster, CertificateMasterViewModel>().ReverseMap();
            CreateMap<CompanyMaster, CompanyMasterViewModel>().ReverseMap();
            CreateMap<CourseCategoryMaster, CourseCategoryMasterViewModel>().ReverseMap();
            CreateMap<CourseEmploymentTypeMapping, CourseEmploymentTypeMappingViewModel>().ReverseMap();
            CreateMap<CourseMaster, CourseMasterViewModel>().ReverseMap();
            CreateMap<DepartmentMaster, DepartmentMasterViewModel>().ReverseMap();
            CreateMap<DesignationMaster, DesignationMasterViewModel>().ReverseMap();
            CreateMap<DivisionMaster, DivisionMasterViewModel>().ReverseMap();
            CreateMap<EmploymentTypeMaster, EmploymentTypeMasterViewModel>().ReverseMap();
            CreateMap<LocationMaster, LocationMasterViewModel>().ReverseMap();
            CreateMap<PhotoGalleryMaster, PhotoGalleryMasterViewModel>().ReverseMap();
            CreateMap<SignatureMaster, SignatureMasterViewModel>().ReverseMap();
            CreateMap<VenueMaster, VenueMasterViewModel>().ReverseMap();
            CreateMap<RankingMaster, RankingMasterViewModel>().ReverseMap();
            CreateMap<QueryMaster, QueryMasterViewModel>().ReverseMap();




            CreateMap<TrainingMatrix, TrainingMatrixViewModel>().ReverseMap();
            CreateMap<QuestionMaster, QuestionMasterViewModel>().ReverseMap();
            CreateMap<QuestionOptionMaster, QuestionOptionMasterViewModel>().ReverseMap();
            CreateMap<QuizMaster, QuizMasterViewModel>().ReverseMap();
            CreateMap<LanguageMaster, LanguageMasterViewModel>().ReverseMap();
            CreateMap<TrainingScheduleStatusMaster, TrainingScheduleStatusMasterViewModel>().ReverseMap();



            CreateMap<SemesterMaster, SemesterMasterViewModel>().ReverseMap();
            CreateMap<UnitMaster, UnitMasterViewModel>().ReverseMap();
            CreateMap<SubjectMaster, SubjectMasterViewModel>().ReverseMap();
            CreateMap<CourseQuadrantMaster, CourseQuadrantViewModel>().ReverseMap();
            CreateMap<LectureMaterial, LectureMaterialViewModel>().ReverseMap();
            CreateMap<CourseMaterialMapping, CourseMaterialMappingViewModel>().ReverseMap();
            CreateMap<QuizMaster, CourseQuizViewModel>().ReverseMap();
             
            CreateMap<DiscussionThread, DiscussionThreadViewModel>().ReverseMap();
            CreateMap<DiscussionReply, DiscussionReplyViewModel>().ReverseMap();
            CreateMap<CourseEnrollment, CourseEnrollmentViewModel>().ReverseMap();
            CreateMap<CourseFacultyMapMaster, CourseFacultyMapViewModel>().ReverseMap();
            CreateMap<QuizMaster, QuizMasterViewModel>().ReverseMap();
            CreateMap<QuizQuestion, QuizQuestionViewModel>().ReverseMap();
            CreateMap<QuizOption, QuizOptionViewModel>().ReverseMap();
            CreateMap<StudentQuizAnswer, StudentQuizAnswerViewModel>().ReverseMap();
            CreateMap<StudentQuizAttempt, StudentQuizAttemptViewModel>().ReverseMap();
            CreateMap<CourseMeeting, CourseMeetingViewModel>().ReverseMap();
            CreateMap<ClassSeries, ClassSeriesViewModel>().ReverseMap();
            CreateMap<ClassInstance, ClassInstanceViewModel>().ReverseMap();
            CreateMap<ClassAttendance, ClassAttendanceViewModel>().ReverseMap();
            CreateMap<DailyAttendance, DailyAttendanceViewModel>().ReverseMap();
            CreateMap<Announcement, AnnouncementViewModel>().ReverseMap();

            CreateMap<FacultyQuizAssessment, FacultyQuizAssessmentViewModel>().ReverseMap();
            CreateMap<FacultyQuizQuestionAssessment, FacultyQuizQuestionAssessmentViewModel>().ReverseMap();
            CreateMap<ForgotPassword, ForgotPasswordViewModel>().ReverseMap();

            CreateMap<AssignmentMaster, AssignmentMasterViewModel>().ReverseMap();
            CreateMap<AssignmentSubmission, AssignmentSubmissionViewModel>().ReverseMap();
            CreateMap<AssignmentGrade, AssignmentGradeViewModel>().ReverseMap();

            #endregion


            #region Training Schedule
            CreateMap<TrainingScheduleAttachment, TrainingScheduleAttachmentViewModel>().ReverseMap();
            CreateMap<TrainingScheduleLanguage, TrainingScheduleLanguageViewModel>().ReverseMap();
            CreateMap<TrainingSchedule, TrainingScheduleViewModel>().ReverseMap();
            CreateMap<TrainingScheduleApprovalHistory, TrainingScheduleApprovalHistoryViewModel>().ReverseMap();
			CreateMap<NominationRequest, NominationRequestViewModel>().ReverseMap();
			CreateMap<NominationRequestRemarks, NominationRequestRemarksViewModel>().ReverseMap();



			#endregion
		}
	}
}
