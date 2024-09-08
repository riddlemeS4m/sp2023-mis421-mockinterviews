using Google.Apis.Drive.v3;
using Google.Apis.Upload;
using sp2023_mis421_mockinterviews.Interfaces.IServices;
using sp2023_mis421_mockinterviews.Interfaces.IUtilities;

namespace sp2023_mis421_mockinterviews.Services.GoogleDrive
{
    public class GoogleDriveSiteContentService : IGoogleDrive, IConvertFilePathToFormFile
    {
        private readonly string _folderId;
        private readonly DriveService _driveClient;
        private readonly ILogger<GoogleDriveSiteContentService> _logger;
        public GoogleDriveSiteContentService(string folderId, 
            DriveService driveClient, 
            ILogger<GoogleDriveSiteContentService> logger)
        {
            _folderId = folderId;
            _driveClient = driveClient;
            _logger = logger;
        }

        public async Task<(Google.Apis.Drive.v3.Data.File, MemoryStream)> GetOneFile(string fileId, bool download = false)
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
                _logger.LogWarning("Did not find file with id '{query}'", fileId);
                return (null, null);
            }
            else
            {
                _logger.LogInformation("Found file with id '{query}'", fileId);
                return (file, stream);
            }
        }

        public IList<string> GetFileIds(IList<Google.Apis.Drive.v3.Data.File> files)
        {
            var idList = new List<string>();

            if (files == null || files.Count == 0)
            {
                _logger.LogWarning("{name} was empty", nameof(files));
                return idList;
            }
            else
            {
                _logger.LogInformation("File ids parsed");
                foreach (var file in files)
                {
                    idList.Add(file.Id);
                }
                return idList;
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
                _logger.LogWarning("Did not find the required file for '{query}'", query);
                return new List<Google.Apis.Drive.v3.Data.File>();
            }
            else
            {
                _logger.LogInformation("Found the required file for '{query}'", query);
                return result.Files;
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
                throw new Exception("Upload to Google Drive 'Site Content' folder failed.");
            }
            else
            {
                var fileId = request.ResponseBody.Id;
                return fileId;
            }
        }

        public IFormFile ConvertFilePathToFormFile(string filePath)
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);

            var memoryStream = new MemoryStream(fileBytes);

            var fileName = Path.GetFileName(filePath);

            IFormFile formFile = new FormFile(memoryStream, 0, memoryStream.Length, null, fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = GoogleDriveUtility.GetMimeType(filePath) // Optional: set the content type if needed
            };

            return formFile;
        }

        public async Task<string> UploadFileFromFilePath(string filePath)
        {
            var fileId = await UploadFile(ConvertFilePathToFormFile(filePath));
            return fileId;
        }

        public async Task DeleteFile(string fileId)
        {
            if(string.IsNullOrEmpty(fileId))
            {
                throw new ArgumentNullException(nameof(fileId));
            }

            try
            {
                var request = _driveClient.Files.Delete(fileId);
                await request.ExecuteAsync();
                _logger.LogInformation("File with id '{fileId}' was deleted successfully", fileId);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
