using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMS.Models.Assessments;

namespace TMS.Repository.Managers
{
    public interface IAssessmentManager
    {
        // --- Exam Master Management (Faculty/Admin) ---
        Task<List<ExamMaster>> GetExamsAsync(int facultyId = 0);
        Task<ExamMaster?> GetExamDetailsAsync(int examId);
        Task<int> CreateOrUpdateExamAsync(ExamMaster exam, int userId);
        Task<bool> UpdateExamStatusAsync(int examId, string status);
        Task<bool> AddExamSectionAsync(ExamSection section);
        Task<bool> DeleteExamSectionAsync(int sectionId);
        Task<bool> MapQuestionsToExamAsync(int examId, int sectionId, List<int> questionIds);
        Task<List<ExamSection>> GetExamSectionsAsync(int examId);

        // --- Question Bank (Faculty) ---
        Task<List<QuestionBank>> GetQuestionBankAsync(int courseId = 0, int facultyId = 0);
        Task<QuestionBank?> GetQuestionDetailsAsync(int questionId);
        Task<int> CreateOrUpdateQuestionAsync(QuestionBank question, List<QuestionOption> options, int userId);
        Task<bool> DeleteQuestionAsync(int questionId);

        // --- Exam Assignment (Faculty/Admin) ---
        Task<bool> AssignExamToStudentsAsync(int examId, List<int> studentIds, int assignedByUserId);
        Task<bool> AssignExamToCourseEnrollmentsAsync(int examId, int courseId, int assignedByUserId);
        Task<List<ExamAssignment>> GetAssignmentsForExamAsync(int examId);

        // --- Student Exam Engine (Student) ---
        Task<List<ExamMaster>> GetAssignedExamsForStudentAsync(int studentId);
        Task<StudentExamAttempt?> StartExamAttemptAsync(int examId, int studentId);
        Task<bool> SaveStudentAnswerAsync(int attemptId, int questionId, int? selectedOptionId, string? subjectiveAnswer, int remainingSeconds);
        Task<ExamResult?> SubmitExamAttemptAsync(int attemptId, bool forceSubmit = false);
        Task<StudentExamAttempt?> GetAttemptDetailsAsync(int attemptId);
        Task<ExamResult?> GetExamResultAsync(int attemptId);

        // --- Evaluation System (Faculty) ---
        Task<List<StudentExamAttempt>> GetAttemptsForEvaluationAsync(int facultyId);
        Task<bool> SaveFacultyEvaluationAsync(int attemptId, int facultyId, string? feedback, decimal totalMarksAwarded, Dictionary<int, decimal> questionMarks);

        // --- Analytics Dashboard (Admin/Faculty) ---
        Task<ExamAnalyticsViewModel> GetExamAnalyticsAsync(int examId);
        Task<List<StudentExamAttempt>> GetLiveMonitoringAttemptsAsync(int examId);
    }

    // ViewModels for Dashboard & Analytics
    public class ExamAnalyticsViewModel
    {
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = default!;
        public int TotalAssigned { get; set; }
        public int TotalAttempts { get; set; }
        public double AttendancePercentage { get; set; }
        public decimal AverageScore { get; set; }
        public decimal HighestScore { get; set; }
        public int PassedCount { get; set; }
        public int FailedCount { get; set; }
        public double PassPercentage { get; set; }
        public List<StudentAttemptSummary> TopPerformers { get; set; } = new List<StudentAttemptSummary>();
        public List<QuestionDifficultySummary> QuestionAnalysis { get; set; } = new List<QuestionDifficultySummary>();
    }

    public class StudentAttemptSummary
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public decimal Score { get; set; }
        public decimal Percentage { get; set; }
        public string ResultStatus { get; set; } = default!;
    }

    public class QuestionDifficultySummary
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = default!;
        public string QuestionType { get; set; } = default!;
        public double CorrectResponsePercentage { get; set; }
        public string DeducedDifficulty { get; set; } = default!; // Easy, Medium, Hard based on actual responses
    }
}
