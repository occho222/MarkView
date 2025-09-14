namespace MarkView.Services
{
    public interface IDataPersistenceService
    {
        Task<T?> LoadDataAsync<T>(string fileName) where T : class;
        Task SaveDataAsync<T>(T data, string fileName) where T : class;
        Task<bool> DataExistsAsync(string fileName);
        Task DeleteDataAsync(string fileName);
        string GetDataDirectory();
    }
}