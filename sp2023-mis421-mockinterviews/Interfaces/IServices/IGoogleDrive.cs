using Google.Apis.Drive.v3;

namespace sp2023_mis421_mockinterviews.Interfaces.IServices
{
    public interface IGoogleDrive
    {
        public Task<IList<Google.Apis.Drive.v3.Data.File>> QueryForAllFiles(string query);
        public IList<string> GetFileIds(IList<Google.Apis.Drive.v3.Data.File> files);
        //public Task<Google.Apis.Drive.v3.Data.File> QueryForOneFile(string query);
        public Task<IList<string>> GetAllFileIdsFromQuery(string query);
        public Task<(Google.Apis.Drive.v3.Data.File, MemoryStream)> GetOneFile(string fileId, bool download);
        //public Task<(MemoryStream, Google.Apis.Drive.v3.Data.File)> DownloadFile(string fileId);
        public Task<string> UploadFile(IFormFile file);
        public Task DeleteFile(string fileId);
    }
}
