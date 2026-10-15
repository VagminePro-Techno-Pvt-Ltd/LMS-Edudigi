using System.Collections.Generic;
using System.Threading.Tasks;
using TMS.ViewModels.Masters;

namespace TMS.Repository.Managers
{
    public interface IFacultyMappingManager : IMasterBaseManager<CourseFacultyMapViewModel>
    {
        /// <summary>
        /// Smart sync for faculty mapping. 
        /// - New faculties -> INSERT
        /// - Removed faculties -> Soft delete (IsActive = false)
        /// - Already mapped -> Reactivate if inactive
        /// </summary>
        Task<BulkEnrollmentResult> SaveMappingAsync(int courseId, List<int> facultyIds, int userId);

        /// <summary>
        /// Direct unassign of a single instructor from a course.
        /// </summary>
        Task<bool> UnassignFacultyAsync(int courseId, int facultyId, int userId);

        /// <summary>
        /// Direct unassign of all instructors from a course.
        /// </summary>
        Task<int> UnassignAllAsync(int courseId, int userId);
    }
}

