using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Upload;
using sp2023_mis421_mockinterviews.Interfaces;

namespace sp2023_mis421_mockinterviews.Services.GoogleDrive
{
    public class GoogleDriveResumeService : IGoogleDrive
    {
        private readonly string _folderId;
        private readonly DriveService _driveClient;
        public GoogleDriveResumeService(string folderId, DriveService driveClient)
        {
            _folderId = folderId;
            _driveClient = driveClient;
        }

        public async Task<(Google.Apis.Drive.v3.Data.File, MemoryStream)> GetOneFile(string fileId, bool download)
        {
            if (string.IsNullOrEmpty(fileId))
            {
                throw new ArgumentNullException(nameof(fileId));
            }

            var request = _driveClient.Files.Get(fileId);
            var stream = new MemoryStream();

            var file = await request.ExecuteAsync();

            if (download)
            {
                request.MediaDownloader.ProgressChanged += GoogleDriveUtility.Download_ProgressChanged;

                await request.DownloadAsync(stream);
            }

            if (file == null)
            {
                Console.WriteLine($"Did not find file with id '{fileId}'");
                return (null, null);
            }
            else
            {
                Console.WriteLine($"File with id '{fileId}' found.");
                return (file, stream);
            }
        }

        public async Task<IList<Google.Apis.Drive.v3.Data.File>> QueryForAllFiles(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentNullException(nameof(query));
            }

            var request = _driveClient.Files.List();
            request.Q = $"name contains '{query}' and '{_folderId}' in parents and trashed = false";
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

        public IList<string> GetFileIds(IList<Google.Apis.Drive.v3.Data.File> files)
        {
            var idList = new List<string>();

            if (files == null || files.Count == 0)
            {
                Console.WriteLine($"{nameof(files)} was empty.");
                return idList;
            }
            else
            {
                Console.WriteLine($"Resume ids parsed.");
                foreach (var file in files)
                {
                    idList.Add(file.Id);
                }
                return idList;
            }
        }

        public async Task<IList<string>> GetAllFileIdsFromQuery(string query)
        {
            var files = await QueryForAllFiles(query);
            return GetFileIds(files);
        }

        public async Task<string> UploadFile(IFormFile file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            var fileMetaData = new Google.Apis.Drive.v3.Data.File()
            {
                Name = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + "_" + file.FileName,
                Parents = new List<string> { _folderId },
                MimeType = file.ContentType
            };

            using var fileStream = file.OpenReadStream();

            FilesResource.CreateMediaUpload request = _driveClient.Files.Create(fileMetaData, fileStream, fileMetaData.MimeType);

            request.ProgressChanged += GoogleDriveUtility.Upload_ProgressChanged;

            var result = await request.UploadAsync();

            if (result.Status != UploadStatus.Completed)
            {
                throw new Exception("Upload to Google Drive 'Resume' folder failed.");
            }
            else
            {
                var fileId = request.ResponseBody.Id;
                Console.WriteLine("Upload to Google Drive 'Resume' folder succeeded.");
                return fileId;
            }
        }

        public async Task DeleteFile(string fileId)
        {
            if (string.IsNullOrEmpty(fileId))
            {
                throw new ArgumentNullException(nameof(fileId));
            }

            try
            {
                var request = _driveClient.Files.Delete(fileId);
                await request.ExecuteAsync();
                Console.WriteLine($"File with id {fileId} was deleted successfully.");
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
