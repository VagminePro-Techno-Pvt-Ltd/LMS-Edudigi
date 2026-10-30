using System.Collections.Generic;
using System.Threading.Tasks;
using TMS.ViewModels.LearningAssistant;

namespace TMS.Web.Services
{
    /// <summary>
    /// Service contract for querying student academic data directly from LMS database records
    /// without requiring an external LLM / AI provider.
    /// </summary>
    public interface IAcademicRecordsService
    {
        /// <summary>
        /// Retrieves verified student attendance metrics across daily platform logins and class sessions.
        /// </summary>
        Task<StudentAttendanceSummaryDto> GetAttendanceSummaryAsync(int studentId, int? courseId = null);

        /// <summary>
        /// Retrieves official student grade cards, evaluated assessment marks, and exam outcomes.
        /// </summary>
        Task<StudentGradesSummaryDto> GetGradesSummaryAsync(int studentId, int? courseId = null);

        /// <summary>
        /// Retrieves student's active enrolled courses.
        /// </summary>
        Task<List<EnrolledCourseSummaryDto>> GetEnrolledCoursesSummaryAsync(int studentId);

        /// <summary>
        /// Retrieves student assignments, submission states, and upcoming deadlines.
        /// </summary>
        Task<StudentAssignmentsSummaryDto> GetAssignmentsSummaryAsync(int studentId, int? courseId = null);

        /// <summary>
        /// Retrieves student's upcoming timetable, live lecture sessions, and meetings.
        /// </summary>
        Task<StudentScheduleSummaryDto> GetUpcomingScheduleAsync(int studentId, int? courseId = null);

        /// <summary>
        /// Retrieves student identity, enrolled program, and overall learning progression.
        /// </summary>
        Task<StudentProfileSummaryDto> GetProfileSummaryAsync(int studentId, int? courseId = null);

        /// <summary>
        /// Retrieves all pending and upcoming academic deadlines across assignments, internal assessments, quizzes, and exams.
        /// </summary>
        Task<StudentDeadlinesSummaryDto> GetDeadlinesSummaryAsync(int studentId, int? courseId = null);

        /// <summary>
        /// Retrieves comprehensive course details including syllabus units, assigned faculty instructors, credit weight, and learning materials.
        /// </summary>
        Task<StudentCourseDetailSummaryDto?> GetCourseDetailsSummaryAsync(int studentId, int? courseId = null, string? courseCodeOrTitle = null);
    }
}
