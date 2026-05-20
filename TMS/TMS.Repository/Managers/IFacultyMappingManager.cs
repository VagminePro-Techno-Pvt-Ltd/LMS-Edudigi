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
    }
}
