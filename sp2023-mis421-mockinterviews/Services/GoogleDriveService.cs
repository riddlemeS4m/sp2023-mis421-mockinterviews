using Google.Apis.Drive.v3;
using Google.Apis.Upload;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace sp2023_mis421_mockinterviews.Services
{
    public class GoogleDriveService
    {
        private const string Manual = "Manual";
        private const string ParkingPass = "Parking";
        private const string ParkingPassFilePath = "wwwroot/lib/GuestParking_Spring2024.pdf";
        private const string ManualFilePath = "wwwroot/lib/MockInterviewManual_Spring2024.docx";
        private readonly DriveService _driveService;
        public GoogleDriveService(DriveService driveService)
        {
            _driveService = driveService;
        }

        private bool CheckForNullDriveClient()
        {
            if (_driveService == null)
            {
                throw new Exception("Google Drive service is null.");
            }
            return false;
        }

        private static readonly Dictionary<string, string> MimeTypes = new Dictionary<string, string>
        {
            { ".pdf", "application/pdf" },
            { ".doc", "application/msword" },
            { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            { ".jpg", "image/jpeg" },
            { ".png", "image/png" },
            // Add other MIME types as needed
        };

        public static string GetMimeType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return MimeTypes.ContainsKey(extension) ? MimeTypes[extension] : "application/octet-stream";
        }

        public static string SerializeCredentials(IConfigurationSection googleCredentialSection)
        {

            var googleCredentialJson = new
            {
                type = googleCredentialSection["type"],
                project_id = googleCredentialSection["project_id"],
                private_key_id = googleCredentialSection["private_key_id"],
                private_key = googleCredentialSection["private_key"],
                client_email = googleCredentialSection["client_email"],
                client_id = googleCredentialSection["client_id"],
                auth_uri = googleCredentialSection["auth_uri"],
                token_uri = googleCredentialSection["token_uri"],
                auth_provider_x509_cert_url = googleCredentialSection["auth_provider_x509_cert_url"],
                client_x509_cert_url = googleCredentialSection["client_x509_cert_url"]
            };

            return JsonSerializer.Serialize(googleCredentialJson);
        }

        public async Task Test(string folderId)
        {
            if (string.IsNullOrEmpty(folderId))
            {
                throw new Exception("Test failed. Provided folder ID was null or empty.");
            }

            var manualExists = await QueryForBoolean(Manual, folderId);

            if (!manualExists)
            {
                await Upload(ManualFilePath, folderId);
            }

            var parkingPassExists = await QueryForBoolean(ParkingPass, folderId);

            if (!parkingPassExists)
            {
                await Upload(ParkingPassFilePath, folderId);
            }
        }

        public async Task<IList<Google.Apis.Drive.v3.Data.File>> QueryForFiles(string query, string folderId)
        {
            if (CheckForNullDriveClient() || string.IsNullOrEmpty(folderId) || string.IsNullOrEmpty(query))
            {
                throw new Exception("Provided folder ID was null or empty.");
            }

            var request = _driveService.Files.List();
            request.Q = $"name contains '{query}' and '{folderId}' in parents and trashed = false";
            request.Fields = "files(id, name, mimeType, parents)";

            var result = await request.ExecuteAsync();

            if (result.Files == null || result.Files.Count == 0)
            {
                Console.WriteLine($"Did not find query '{query}'");
                return new List<Google.Apis.Drive.v3.Data.File>();
            }
            else
            {
                Console.WriteLine($"Found query '{query}'");
                return result.Files;
            }
        }

        public async Task<IList<string>> QueryForFileIds(string query, string folderId)
        {
            if (CheckForNullDriveClient() || string.IsNullOrEmpty(folderId) || string.IsNullOrEmpty(query))
            {
                throw new Exception("Provided folder ID was null or empty.");
            }

            var request = _driveService.Files.List();
            request.Q = $"name contains '{query}' and '{folderId}' in parents and trashed = false";
            request.Fields = "files(id, name, mimeType, parents)";

            var result = await request.ExecuteAsync();

            var idList = new List<string>();

            if (result.Files == null || result.Files.Count == 0)
            {
                Console.WriteLine($"Did not find query '{query}'");
                return idList;
            }
            else
            {
                Console.WriteLine($"Found query '{query}'");
                foreach (var file in result.Files)
                {
                    idList.Add(file.Id);
                }
                return idList;
            }
        }

        public async Task<bool> QueryForBoolean(string query, string folderId)
        {
            if (CheckForNullDriveClient() || string.IsNullOrEmpty(folderId) || string.IsNullOrEmpty(query))
            {
                throw new Exception("Provided folder ID was null or empty.");
            }

            var request = _driveService.Files.List();
            request.Q = $"name contains '{query}' and '{folderId}' in parents and trashed = false";
            request.Fields = "files(id, name, mimeType, parents)";

            var result = await request.ExecuteAsync();

            if (result.Files == null || result.Files.Count == 0)
            {
                Console.WriteLine($"Did not find query '{query}'");
                return false;
            }
            else
            {
                Console.WriteLine($"Found query '{query}'");
                return true;
            }
        }

        public async Task<(MemoryStream, Google.Apis.Drive.v3.Data.File)> GetFile(string fileId, string folderId)
        {
            if (CheckForNullDriveClient() || string.IsNullOrEmpty(fileId) || string.IsNullOrEmpty(folderId))
            {
                throw new Exception("Provided file ID or folder ID was null or empty.");
            }

            var request = _driveService.Files.Get(fileId);
            var stream = new MemoryStream();

            try
            {
                // Download the file content
                await request.DownloadAsync(stream);

                // Verify that the file is in the specified folder
                var file = await _driveService.Files.Get(fileId).ExecuteAsync();
                if (file.Parents == null || !file.Parents.Contains(folderId))
                {
                    throw new FileNotFoundException("File not found in the specified folder.");
                }

                stream.Position = 0;
                return (stream, file);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> CheckFileExists(string fileId, string folderId)
        {
            if (CheckForNullDriveClient() || string.IsNullOrEmpty(fileId) || string.IsNullOrEmpty(folderId))
            {
                throw new Exception("Provided file ID or folder ID was null or empty.");
            }

            try
            {
                var request = _driveService.Files.List();
                request.Q = $"'{folderId}' in parents and id = '{fileId}' and trashed = false";
                request.Fields = "files(id, name, mimeType, parents)";

                var result = await request.ExecuteAsync();
                var file = result.Files.FirstOrDefault();

                if (file != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> Upload(string filePath, string folderId)
        {
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + "_" + Path.GetFileName(filePath),
                Parents = new List<string> { folderId },
                MimeType = GetMimeType(Path.GetFileName(filePath))
            };

            // FilesResource.CreateMediaUpload request;
            // using (var streamFile = new FileStream(filePath, FileMode.Open))
            // {
            //     request = _driveService.Files.Create(fileMetadata, streamFile, "");
            //     request.Fields = "id";
            //     await request.UploadAsync();
            // }

            using var fileStream = new FileStream(filePath, FileMode.Open);

            FilesResource.CreateMediaUpload request = _driveService.Files.Create(fileMetadata, fileStream, fileMetadata.MimeType);

            request.ProgressChanged += Upload_ProgressChanged;

            var result = await request.UploadAsync();

            if (result.Status != UploadStatus.Completed)
            {
                return false;
                //throw new Exception("Upload to Google Drive failed.")
            }
            else
            {
                var fileId = request.ResponseBody.Id;
                return true;
            }
        }

        public async Task<bool> Upload(IFormFile file, string folderId)
        {
            var fileMetaData = new Google.Apis.Drive.v3.Data.File()
            {
                Name = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + "_" + file.FileName,
                Parents = new List<string> { folderId },
                MimeType = file.ContentType
            };

            // FilesResource.CreateMediaUpload request;
            // await using (var stream = file.OpenReadStream())
            // {
            //     request = _driveService.Files.Create(fileMetaData, stream, file.ContentType);
            //     request.Fields = "id";
            //     await request.UploadAsync();
            // }

            using var fileStream = file.OpenReadStream();

            FilesResource.CreateMediaUpload request = _driveService.Files.Create(fileMetaData, fileStream, fileMetaData.MimeType);

            request.ProgressChanged += Upload_ProgressChanged;

            var result = await request.UploadAsync();

            if (result.Status != UploadStatus.Completed)
            {
                return false;
                //throw new Exception("Upload to Google Drive failed.")
            }
            else
            {
                var fileId = request.ResponseBody.Id;
                return true;
            }
        }

        private static void Upload_ProgressChanged(IUploadProgress progress)
        {
            switch (progress.Status)
            {
                case UploadStatus.Uploading:
                    // Optionally, log the progress of the upload
                    Console.WriteLine($"Uploading: {progress.BytesSent} bytes sent.");
                    break;
                case UploadStatus.Completed:
                    // Optionally, log the completion of the upload
                    Console.WriteLine("Upload completed successfully.");
                    break;
                case UploadStatus.Failed:
                    // Optionally, log the failure of the upload
                    Console.WriteLine($"Upload failed: {progress.Exception.Message}");
                    break;
            }
        }
    }
}