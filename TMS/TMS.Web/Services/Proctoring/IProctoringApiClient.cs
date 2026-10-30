using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMS.ViewModels.Assessments;

namespace TMS.Web.Services.Proctoring
{
    public interface IProctoringApiClient
    {
        Task<List<ProctorExamDto>> GetAllExamsAsync();
        Task<ProctorExamDto?> GetExamByIdAsync(Guid id);
        Task<ProctorExamDto?> CreateExamAsync(CreateProctorExamRequest req);
        Task<bool> UpdateExamAsync(Guid id, UpdateProctorExamDto dto);
        Task<(bool success, bool isActive, string message)> ToggleExamStatusAsync(Guid id);
        Task<bool> DeleteExamAsync(Guid id);

        Task<ExamResultsResponseDto?> GetExamResultsAsync(Guid examId);
        Task<CandidateScriptResponseDto?> GetCandidateScriptAsync(Guid examId, string candidateId);

        Task<List<ExamQuestionItemDto>> GetExamQuestionsAsync(Guid examId);
        Task<bool> AddQuestionAsync(ExamQuestionItemDto question);
        Task<bool> DeleteQuestionAsync(Guid questionId);
        Task<bool> RemoveExamQuestionAsync(Guid examId, Guid questionId);
        Task<string?> UploadQuestionImageAsync(Microsoft.AspNetCore.Http.IFormFile file);

        Task<QuestionBankResultDto> GetQuestionBankAsync(string? searchText = null, string? questionType = null, string? difficultyLevel = null, int page = 1, int pageSize = 50);
        Task<(bool success, int added, string message)> ImportQuestionsBulkAsync(Guid examId, List<AddQuestionToExamDto> questions);
        Task<PdfExtractionResultDto?> ExtractPdfQuestionsAsync(Microsoft.AspNetCore.Http.IFormFile pdfFile);
        Task<(bool success, int saved, string message)> SaveExtractedQuestionsAsync(SaveExtractedQuestionsDto dto);
        Task<List<ExamQuestionItemDto>> GetStudentExamQuestionsAsync(Guid examId);

        Task<List<CandidateItemDto>> GetCandidatesAsync();
        Task<List<ExamAssignmentItemDto>> GetAssignmentsAsync(Guid? examId);
        Task<bool> AssignCandidateAsync(CreateExamAssignmentDto req);
        Task<bool> DeleteAssignmentAsync(Guid assignmentId);

        Task<int> EvaluateExamAsync(Guid examId);
        Task<List<ViolationItemDto>> GetViolationsAsync(Guid examId);
        Task<MasterDataResultDto> GetMasterDataAsync();

        string GetFrontendUrl();
    }
}
