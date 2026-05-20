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
    }
}
