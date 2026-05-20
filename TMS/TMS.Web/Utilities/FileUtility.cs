using TMS.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Text;
using System.Web;

namespace TMS.Utilities
{
    public class FileUtility
    {
        private readonly StorageSettings _storageSettings;
        public FileUtility(StorageSettings storageSettings)
        {
            _storageSettings = storageSettings;
        }
        public AttachmentModel UploadFile(IFormFile file, bool isProductImage = false, string? oldFile = null)
        {
            try
            {
                AttachmentModel objAttachment = new();

                if (file == null || file.Length == 0)
                    return objAttachment;
                var filePath = string.Empty;
                if (isProductImage)
                    filePath = AzureOperations.SaveFileAzureProductImage(file.OpenReadStream(), Path.GetExtension(file.FileName), _storageSettings);
                else
                    filePath = AzureOperations.SaveFileAzure(file.OpenReadStream(), Path.GetExtension(file.FileName), _storageSettings);
                objAttachment = new AttachmentModel()
                {
                    FileName = Path.GetFileName(file.FileName),
                    FilePath = filePath,
                };
                try
                {
                    if (!string.IsNullOrWhiteSpace(oldFile))
                    {
                        if (isProductImage)
                            AzureOperations.DeleteFileAzureProduct(oldFile, _storageSettings);
                        else
                            AzureOperations.DeleteFileAzure(oldFile, _storageSettings);
                    }
                }
                catch (Exception)
                {

                }
                return objAttachment;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public AttachmentModel UploadFile(Stream inputStream, string fileName, bool isProductImage = false, string? oldFile = null, string? customFileName = null)
        {
            try
            {
                AttachmentModel objAttachment = new();

                var filePath = string.Empty;
                if (isProductImage)
                    filePath = AzureOperations.SaveFileAzureProductImage(inputStream, Path.GetExtension(fileName), _storageSettings, null, customFileName);
                else
                    filePath = AzureOperations.SaveFileAzure(inputStream, Path.GetExtension(fileName), _storageSettings, null, customFileName);
                objAttachment = new AttachmentModel()
                {
                    FileName = Path.GetFileName(fileName),
                    FilePath = filePath,
                };
                try
                {
                    if (!string.IsNullOrWhiteSpace(oldFile))
                    {
                        if (isProductImage)
                            AzureOperations.DeleteFileAzureProduct(oldFile, _storageSettings);
                        else
                            AzureOperations.DeleteFileAzure(oldFile, _storageSettings);
                    }
                }
                catch (Exception)
                {

                }
                return objAttachment;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public Stream GetFileStream(string fileName)
        {
            return AzureOperations.GetFileStreamAzure(fileName, _storageSettings);
        }

        public void DeleteFile(string fileName)
        {
            AzureOperations.DeleteFileAzure(fileName, _storageSettings);
        }
    }
}