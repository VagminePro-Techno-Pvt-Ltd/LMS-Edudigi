using System.Collections.Generic;
using System.Threading.Tasks;
using TMS.ViewModels;

namespace TMS.Repository.Managers
{
    /// <summary>
    /// Manager for bulk user creation, independent of course enrollment.
    /// Supports creating Admin, Faculty, and Student users via file upload.
    /// </summary>
    public interface IBulkUserManager
    {
        /// <summary>
        /// Validate rows and return preview (without saving anything to DB).
        /// Each row gets Status = "Valid", "Duplicate", "Error", etc.
        /// </summary>
        Task<List<BulkUserUploadRow>> ValidateRowsAsync(List<BulkUserUploadRow> rows);

        /// <summary>
        /// Process bulk user upload rows with mode support:
        /// - CreateOnly: Create new, fail on existing
        /// - SkipExisting: Create new, skip existing
        /// - UpdateExisting: Create new, update existing
        /// </summary>
        Task<BulkUserUploadResult> ProcessBulkUploadAsync(List<BulkUserUploadRow> rows, int createdByUserId, BulkUploadMode mode = BulkUploadMode.SkipExisting);
    }
}
