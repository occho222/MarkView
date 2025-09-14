using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using MarkView.Models;

namespace MarkView.Services
{
    public class FileService : IFileService
    {
        public async Task<string> ReadFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"ファイルが見つかりません: {filePath}");

            return await File.ReadAllTextAsync(filePath, Encoding.UTF8);
        }

        public bool IsMarkdownFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLower();
            return extension == ".md" || extension == ".markdown" || extension == ".txt";
        }

        public ObservableCollection<FileItem> LoadFolderTree(string folderPath)
        {
            var items = new ObservableCollection<FileItem>();

            try
            {
                if (!Directory.Exists(folderPath))
                {
                    return items;
                }

                var rootItem = CreateFileItem(new DirectoryInfo(folderPath), 0, 2); // 最大2階層まで
                items.Add(rootItem);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"フォルダ読み込みエラー: {ex.Message}");
            }

            return items;
        }

        public FileItem CreateFileItem(DirectoryInfo dirInfo)
        {
            return CreateFileItem(dirInfo, 0, 1); // デフォルトは1階層のみ
        }

        private FileItem CreateFileItem(DirectoryInfo dirInfo, int currentDepth, int maxDepth)
        {
            var item = new FileItem
            {
                Name = dirInfo.Name,
                FilePath = dirInfo.FullName,
                IsDirectory = true,
                Children = new ObservableCollection<FileItem>()
            };

            try
            {
                // 深度制限チェック
                if (currentDepth >= maxDepth)
                {
                    return item;
                }

                // アクセス権限チェック
                var directories = dirInfo.GetDirectories();
                var files = dirInfo.GetFiles();

                // サブディレクトリを追加（制限付き）
                var dirCount = 0;
                foreach (var dir in directories)
                {
                    // システムフォルダやhiddenフォルダをスキップ
                    if (dir.Name.StartsWith(".") ||
                        dir.Attributes.HasFlag(FileAttributes.Hidden) ||
                        dir.Attributes.HasFlag(FileAttributes.System))
                    {
                        continue;
                    }

                    // ディレクトリ数制限（パフォーマンス対策）
                    if (dirCount >= 100)
                    {
                        break;
                    }

                    try
                    {
                        var childItem = CreateFileItem(dir, currentDepth + 1, maxDepth);
                        item.Children.Add(childItem);
                        dirCount++;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // アクセス権限なしのフォルダはスキップ
                        continue;
                    }
                }

                // Markdownファイルを追加（制限付き）
                var fileCount = 0;
                foreach (var file in files)
                {
                    if (IsMarkdownFile(file.FullName))
                    {
                        // ファイル数制限（パフォーマンス対策）
                        if (fileCount >= 500)
                        {
                            break;
                        }

                        item.Children.Add(new FileItem
                        {
                            Name = file.Name,
                            FilePath = file.FullName,
                            IsDirectory = false,
                            LastOpened = file.LastWriteTime
                        });
                        fileCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ディレクトリアクセスエラー ({dirInfo.FullName}): {ex.Message}");
            }

            return item;
        }

        public string FormatFileSize(long bytes)
        {
            if (bytes >= 1024 * 1024)
                return $"{bytes / (1024.0 * 1024.0):F1} MB";
            if (bytes >= 1024)
                return $"{bytes / 1024.0:F1} KB";
            return $"{bytes} B";
        }
    }
}