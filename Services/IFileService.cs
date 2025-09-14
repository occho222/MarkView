using System.Collections.ObjectModel;
using System.IO;
using MarkView.Models;

namespace MarkView.Services
{
    public interface IFileService
    {
        Task<string> ReadFileAsync(string filePath);
        bool IsMarkdownFile(string filePath);
        ObservableCollection<FileItem> LoadFolderTree(string folderPath);
        FileItem CreateFileItem(DirectoryInfo dirInfo);
        string FormatFileSize(long bytes);
    }
}