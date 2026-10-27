using TMS.ViewModels.Academics;

namespace TMS.Web.Services
{
    /// <summary>
    /// Service interface for student-facing learning pages.
    /// Encapsulates all business logic for the Unit-first hierarchy:
    ///   Subject Hub → Unit Details → Quadrant Content
    /// </summary>
    public interface IStudentLearningService
    {
        /// <summary>
        /// Builds the Subject Hub ViewModel: course info + unit grid with progress.
        /// </summary>
        Task<SubjectHubViewModel?> GetSubjectHubAsync(int studentId, int courseId);

        /// <summary>
        /// Builds the Unit Hub ViewModel: unit info + 4 quadrant cards with counts.
        /// </summary>
        Task<UnitHubViewModel?> GetUnitDetailsAsync(int studentId, int courseId, int unitId);

        /// <summary>
        /// Builds the Quadrant Content ViewModel: filtered material list for a unit+quadrant.
        /// </summary>
        Task<QuadrantMaterialListViewModel?> GetQuadrantContentAsync(int studentId, int courseId, int unitId, int quadrantId);

        /// <summary>
        /// Checks if the student is enrolled in the given course.
        /// </summary>
        Task<bool> IsStudentEnrolledAsync(int studentId, int courseId);

        /// <summary>
        /// Checks if the student is enrolled in any course owning or mapped to this lecture material.
        /// </summary>
        Task<bool> IsStudentEnrolledInMaterialAsync(int studentId, int materialId);

        /// <summary>
        /// Checks if the student is enrolled in the parent course of this interactive PPT.
        /// </summary>
        Task<bool> IsStudentEnrolledInPPTAsync(int studentId, int pptId);

        /// <summary>
        /// Checks if the student is enrolled in the parent course of this quiz.
        /// </summary>
        Task<bool> IsStudentEnrolledInQuizAsync(int studentId, int quizId);

        /// <summary>
        /// Checks if the student is enrolled in the parent course/program of this assignment.
        /// </summary>
        Task<bool> IsStudentEnrolledInAssignmentAsync(int studentId, int assignmentId);

        /// <summary>
        /// Returns all course IDs for which the student has an active enrollment.
        /// </summary>
        Task<List<int>> GetEnrolledCourseIdsAsync(int studentId);

        /// <summary>
        /// Computes real course consumption progress percentage for a student in a course.
        /// </summary>
        Task<int> GetCourseProgressPercentAsync(int studentId, int courseId);

        /// <summary>
        /// Computes course consumption progress map for multiple courses in a single batch query.
        /// </summary>
        Task<Dictionary<int, (int Total, int Completed, int Percent)>> GetCoursesProgressMapAsync(int studentId, IEnumerable<int> courseIds);

        /// <summary>
        /// Records that a student viewed/engaged with a lecture document/resource.
        /// </summary>
        Task MarkMaterialViewedAsync(int studentId, int materialId);
    }
}
