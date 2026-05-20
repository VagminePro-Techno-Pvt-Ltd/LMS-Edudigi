using Azure.Storage.Blobs;
using TMS.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace TMS.Utilities
{
    public class AzureOperations
    {

        public static string SaveFileAzure(Stream inputStream, string extension, StorageSettings storageSettings, string? filePrefix = null, string? customFileName = null)
        {
            BlobServiceClient blobServiceClient = new(storageSettings.StorageConnectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(storageSettings.ContainerName);
            containerClient.CreateIfNotExists();
            var blobName = $"{Guid.NewGuid()}{extension}";
            if (!string.IsNullOrWhiteSpace(customFileName))
                blobName = customFileName;
            if (!string.IsNullOrWhiteSpace(filePrefix))
                blobName = $"{filePrefix}-{blobName}";
            containerClient.DeleteBlobIfExists(blobName);
            var blobInfo = containerClient.UploadBlob(blobName, inputStream);
            return blobName;
        }

        public static string SaveFileAzureProductImage(Stream inputStream, string extension, StorageSettings storageSettings, string? filePrefix = null, string? customFileName = null)
        {
            BlobServiceClient blobServiceClient = new(storageSettings.StorageConnectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(storageSettings.PublicContainerName);
            containerClient.CreateIfNotExists(Azure.Storage.Blobs.Models.PublicAccessType.Blob);
            var blobName = $"{Guid.NewGuid()}{extension}";
            if (!string.IsNullOrWhiteSpace(customFileName))
                blobName = customFileName;
            if (!string.IsNullOrWhiteSpace(filePrefix))
                blobName = $"{filePrefix}-{blobName}";
            containerClient.DeleteBlobIfExists(blobName);
            var blobInfo = containerClient.UploadBlob(blobName, inputStream).Value;
            return blobName;
        }

        public static byte[] GetFileAzure(string fileName, StorageSettings storageSettings)
        {
            BlobServiceClient blobServiceClient = new(storageSettings.StorageConnectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(storageSettings.ContainerName);
            var blob = containerClient.GetBlobClient(fileName);
            var item = blob.DownloadContent();
            if (item != null)
            {
                return item.Value.Content.ToArray();
            }
            return default!;
        }
        public static byte[] GetFileAzureProduct(string fileName, StorageSettings storageSettings)
        {
            BlobServiceClient blobServiceClient = new(storageSettings.StorageConnectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(storageSettings.PublicContainerName);
            var blob = containerClient.GetBlobClient(fileName);
            var item = blob.DownloadContent();
            if (item != null)
            {
                return item.Value.Content.ToArray();
            }
            return default!;
        }

        public static Stream GetFileStreamAzure(string fileName, StorageSettings storageSettings, bool isProduct = false)
        {
            BlobServiceClient blobServiceClient = new(storageSettings.StorageConnectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(isProduct ? storageSettings.PublicContainerName : storageSettings.ContainerName);
            var blob = containerClient.GetBlobClient(fileName);
            var item = blob.DownloadContent();
            if (item != null)
            {
                return item.Value.Content.ToStream();
            }
            return default!;
        }

        public static List<string> GetAllFiles(StorageSettings storageSettings, string? startsWith = null)
        {
            BlobServiceClient blobServiceClient = new(storageSettings.StorageConnectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(storageSettings.ContainerName);
            var fileList = containerClient.GetBlobs();
            var files = new List<string>();
            if (fileList != null)
                files = fileList.Select(t => t.Name).ToList();
            if (!string.IsNullOrWhiteSpace(startsWith))
                files = files.Where(t => t.ToLower().StartsWith(startsWith.ToLower())).ToList();

            return files;
        }

        public static void DeleteFileAzure(string fileName, StorageSettings storageSettings)
        {
            BlobServiceClient blobServiceClient = new(storageSettings.StorageConnectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(storageSettings.ContainerName);
            containerClient.DeleteBlobIfExists(fileName);
        }
        public static void DeleteFileAzureProduct(string fileName, StorageSettings storageSettings)
        {
            BlobServiceClient blobServiceClient = new(storageSettings.StorageConnectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(storageSettings.PublicContainerName);
            containerClient.DeleteBlobIfExists(fileName);
        }

        public static string GetFileAzureBase64(string fileName, StorageSettings storageSettings, bool isProduct = true)
        {
            if (isProduct)
                return Convert.ToBase64String(GetFileAzureProduct(fileName, storageSettings));
            else
                return Convert.ToBase64String(GetFileAzure(fileName, storageSettings));
        }
    }
}