using System.Collections.Generic;
using System.Threading.Tasks;
using TMS.ViewModels.Masters;

namespace TMS.Repository.Managers
{
    /// <summary>
    /// Specialized enrollment manager with status-based lifecycle, 
    /// duplicate prevention, and bulk CSV enrollment support.
    /// </summary>
    public interface IEnrollmentManager : IMasterBaseManager<CourseEnrollmentViewModel>
    {
        /// <summary>
        /// Smart enroll: if student already enrolled (any status), reactivates.
        /// Otherwise inserts new enrollment. Prevents duplicates.
        /// </summary>
        Task<EnrollmentResult> EnrollStudentAsync(int courseId, int studentId, int userId, string source = "Admin");

        /// <summary>
        /// Soft-drop a student from a course (status = Dropped, sets DroppedOn).
        /// Does NOT delete the record — preserves history.
        /// </summary>
        Task<bool> DropStudentAsync(int courseId, int studentId, int userId);

        /// <summary>
        /// Mark enrollment as completed (status = Completed, sets CompletedOn).
        /// </summary>
        Task<bool> CompleteEnrollmentAsync(int courseId, int studentId, int userId);

        /// <summary>
        /// Smart save: syncs the selected student list for a course.
        /// - New selections → enrolled (or re-activated)
        /// - Removed selections → dropped (soft delete)
        /// </summary>
        Task<BulkEnrollmentResult> SaveMappingAsync(int courseId, List<int> selectedStudentIds, int userId, string source = "Admin");

        /// <summary>
        /// Bulk enroll from CSV data. Each row = (StudentId, CourseId).
        /// </summary>
        Task<BulkEnrollmentResult> BulkEnrollFromCsvAsync(List<CsvEnrollmentRow> rows, int userId);

        /// <summary>
        /// Smart Bulk Enroll: Creates student if doesn't exist (by email) and then enrolls them.
        /// </summary>
        Task<BulkEnrollmentResult> BulkEnrollWithAutoCreateAsync(int courseId, List<StudentUploadRow> rows, int userId);

        /// <summary>
        /// Get active enrollment count for a course.
        /// </summary>
        Task<int> GetActiveCountAsync(int courseId);
    }

    // ─── Result Models ───

    public class StudentUploadRow
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? ContactNo { get; set; }
        public string? CourseCode { get; set; } // Added for multi-course support
    }

    public class EnrollmentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool WasReactivated { get; set; }
    }

    public class BulkEnrollmentResult
    {
        public int TotalProcessed { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public int ReactivatedCount { get; set; }
        public int DroppedCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class CsvEnrollmentRow
    {
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public string? StudentName { get; set; }
        public string? CourseName { get; set; }
    }
}
