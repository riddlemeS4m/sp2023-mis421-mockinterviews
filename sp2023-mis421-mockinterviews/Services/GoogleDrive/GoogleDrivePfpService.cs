using Google.Apis.Drive.v3;
using sp2023_mis421_mockinterviews.Interfaces;

namespace sp2023_mis421_mockinterviews.Services.GoogleDrive
{
    public class GoogleDrivePfpService : IGoogleDrive
    {
        private readonly string _folderId;
        private readonly DriveService _driveClient;

        public GoogleDrivePfpService(string folderId, DriveService driveClient)
        {
            _folderId = folderId;
            _driveClient = driveClient;
        }

        public IList<string> GetFileIds(IList<Google.Apis.Drive.v3.Data.File> files)
        {
            throw new NotImplementedException();
        }

        public Task<IList<Google.Apis.Drive.v3.Data.File>> QueryForAllFiles(string query)
        {
            throw new NotImplementedException();
        }

        public Task<string> UploadFile(IFormFile file)
        {
            throw new NotImplementedException();
        }

        public Task<IList<string>> GetAllFileIdsFromQuery(string query)
        {
            throw new NotImplementedException();
        }

        public Task<(Google.Apis.Drive.v3.Data.File, MemoryStream)> GetOneFile(string fileId, bool download)
        {
            throw new NotImplementedException();
        }

        public Task DeleteFile(string fileId)
        {
            throw new NotImplementedException();
        }
    }
}
