using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMS.Models.Academics;
using TMS.ViewModels.Academics;

namespace TMS.Web.Services
{
    public interface ICertificationExamService
    {
        Task EnsureSchemaAsync(CancellationToken ct = default);

        // Faculty operations
        Task<FacultyCertificationDashboardViewModel> GetFacultyDashboardAsync(int facultyId, CancellationToken ct = default);
        Task<CertificationExam?> GetExamByIdAsync(int examId, CancellationToken ct = default);
        Task<int> CreateExamAsync(CertificationExamFormModel model, int facultyId, CancellationToken ct = default);
        Task<bool> UpdateExamAsync(CertificationExamFormModel model, int facultyId, CancellationToken ct = default);
        Task<bool> DeleteExamAsync(int examId, int facultyId, CancellationToken ct = default);
        Task<(bool Success, string Message)> PublishExamAsync(int examId, int facultyId, CancellationToken ct = default);
        Task<FacultyAttemptsViewModel> GetFacultyAttemptsAsync(int examId, int facultyId, CancellationToken ct = default);

        // Question operations
        Task<List<CertificationExamQuestion>> GetQuestionsAsync(int examId, CancellationToken ct = default);
        Task<CertificationExamQuestion?> GetQuestionByIdAsync(int questionId, CancellationToken ct = default);
        Task<bool> SaveQuestionAsync(CertificationQuestionFormModel model, int facultyId, CancellationToken ct = default);
        Task<bool> DeleteQuestionAsync(int questionId, int facultyId, CancellationToken ct = default);
        Task<AiQuestionGenerateResponseModel> GenerateAiQuestionsAsync(AiQuestionGenerateRequestModel request, int facultyId, CancellationToken ct = default);
        Task<(bool Success, string Message, int SavedCount, decimal TotalMarks)> SaveBatchQuestionsAsync(int examId, List<CertificationQuestionFormModel> questions, int facultyId, CancellationToken ct = default);

        // Student operations
        Task<List<StudentCertificationExamCardViewModel>> GetStudentExamsAsync(int studentId, CancellationToken ct = default);
        Task<StudentExamTakingViewModel?> StartOrResumeAttemptAsync(int examId, int studentId, CancellationToken ct = default);
        Task<bool> SaveAnswerAsync(int attemptId, int questionId, string selectedOptionIds, int remainingSeconds, int studentId, CancellationToken ct = default);
        Task<(bool Success, string Message, int AttemptId)> SubmitAttemptAsync(int attemptId, int studentId, string wwwrootPath, CancellationToken ct = default);
        Task<CertificationExamResultViewModel?> GetAttemptResultAsync(int attemptId, int studentId, CancellationToken ct = default);

        // Certificate operations
        Task<IssuedCertificate?> GenerateCertificateForAttemptAsync(int attemptId, string wwwrootPath, CancellationToken ct = default);
        Task<byte[]?> GetCertificatePdfBytesAsync(int certificateId, CancellationToken ct = default);
        Task<IssuedCertificate?> GetCertificateByIdAsync(int certificateId, CancellationToken ct = default);
        Task<IssuedCertificate?> GetCertificateByTokenAsync(string token, CancellationToken ct = default);

        // Admin & Faculty Certificate Management
        Task<List<IssuedCertificate>> GetAllCertificatesAsync(string? searchTerm, string? statusFilter, int? examId, CancellationToken ct = default);
        Task<(bool Success, string Message)> RevokeCertificateAsync(int certificateId, string reason, int adminId, CancellationToken ct = default);
        Task<(bool Success, string Message)> RetryCertificateGenerationAsync(int certificateId, string wwwrootPath, CancellationToken ct = default);
    }
}
