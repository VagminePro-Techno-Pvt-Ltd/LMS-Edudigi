using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMS.ViewModels.LearningAssistant;

namespace TMS.Web.Services
{
    public interface IDocumentIndexService
    {
        Task<DocumentIndexResult> IndexMaterialAsync(int lectureMaterialId, bool force = false, CancellationToken ct = default);

        Task<int> EnsureCourseMaterialsIndexedAsync(int courseId, CancellationToken ct = default);

        Task<bool> RemoveIndexForMaterialAsync(int lectureMaterialId, CancellationToken ct = default);

        Task<List<DocumentChunkSearchResult>> SearchPassagesAsync(
            int userId,
            string userRole,
            int? courseId,
            int? materialId,
            string query,
            int maxResults = 5,
            CancellationToken ct = default);

        Task EnsureSchemaAsync(CancellationToken ct = default);
    }
}
