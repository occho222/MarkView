using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MarkView.Services
{
    public class DataPersistenceService : IDataPersistenceService
    {
        private readonly string _dataDirectory;
        private readonly JsonSerializerOptions _jsonOptions;

        public DataPersistenceService()
        {
            _dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MarkView"
            );

            if (!Directory.Exists(_dataDirectory))
            {
                Directory.CreateDirectory(_dataDirectory);
            }

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters =
                {
                    new JsonStringEnumConverter()
                }
            };
        }

        public async Task<T?> LoadDataAsync<T>(string fileName) where T : class
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, fileName);

                if (!File.Exists(filePath))
                {
                    return null;
                }

                var jsonString = await File.ReadAllTextAsync(filePath);

                if (string.IsNullOrWhiteSpace(jsonString))
                {
                    return null;
                }

                return JsonSerializer.Deserialize<T>(jsonString, _jsonOptions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"データ読み込みエラー ({fileName}): {ex.Message}");
                return null;
            }
        }

        public async Task SaveDataAsync<T>(T data, string fileName) where T : class
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, fileName);
                var jsonString = JsonSerializer.Serialize(data, _jsonOptions);

                await File.WriteAllTextAsync(filePath, jsonString);

                System.Diagnostics.Debug.WriteLine($"データ保存完了: {filePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"データ保存エラー ({fileName}): {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DataExistsAsync(string fileName)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, fileName);
                return await Task.FromResult(File.Exists(filePath));
            }
            catch
            {
                return false;
            }
        }

        public async Task DeleteDataAsync(string fileName)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, fileName);

                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath));
                    System.Diagnostics.Debug.WriteLine($"データ削除完了: {filePath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"データ削除エラー ({fileName}): {ex.Message}");
                throw;
            }
        }

        public string GetDataDirectory()
        {
            return _dataDirectory;
        }
    }
}