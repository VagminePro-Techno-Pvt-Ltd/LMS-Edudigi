using System.IO;
using TMS.ViewModels.LearningAssistant;

namespace TMS.Web.Services
{
    public interface IDocumentProcessingService
    {
        ExtractedDocumentResult Extract(string physicalFilePath, string? title = null, int materialId = 0);
        ExtractedDocumentResult Extract(Stream stream, string fileExtension, string? title = null, int materialId = 0);
    }
}
