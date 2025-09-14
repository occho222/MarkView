using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using MarkView.Models;
using MarkView.Services;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;

namespace MarkView.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly IMarkdownService _markdownService;
        private readonly IFileService _fileService;

        private bool _isDarkTheme = false;
        private double _currentZoom = 1.0;
        private string? _currentFilePath = null;
        private string? _currentFolderPath = null;
        private int _selectedFontSize = 14;
        private bool _isSidebarVisible = true;
        private bool _isSearchVisible = false;
        private string _searchText = "";
        private string _statusText = "準備完了";
        private string _fileInfoText = "";
        private string _zoomLevelText = "ズーム: 100%";
        private string _windowTitle = "MarkView v1.0.0 - 軽量マークダウンビューア";

        public MainWindowViewModel(IMarkdownService markdownService, IFileService fileService)
        {
            _markdownService = markdownService;
            _fileService = fileService;

            InitializeCollections();
            InitializeCommands();
            SetApplicationTitle();
        }

        #region Properties

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set => SetProperty(ref _isDarkTheme, value);
        }

        public double CurrentZoom
        {
            get => _currentZoom;
            set
            {
                if (SetProperty(ref _currentZoom, value))
                {
                    ZoomLevelText = $"ズーム: {(value * 100):F0}%";
                }
            }
        }

        public string? CurrentFilePath
        {
            get => _currentFilePath;
            set => SetProperty(ref _currentFilePath, value);
        }

        public string? CurrentFolderPath
        {
            get => _currentFolderPath;
            set => SetProperty(ref _currentFolderPath, value);
        }

        public int SelectedFontSize
        {
            get => _selectedFontSize;
            set => SetProperty(ref _selectedFontSize, value);
        }

        public bool IsSidebarVisible
        {
            get => _isSidebarVisible;
            set => SetProperty(ref _isSidebarVisible, value);
        }

        public bool IsSearchVisible
        {
            get => _isSearchVisible;
            set => SetProperty(ref _isSearchVisible, value);
        }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public string FileInfoText
        {
            get => _fileInfoText;
            set => SetProperty(ref _fileInfoText, value);
        }

        public string ZoomLevelText
        {
            get => _zoomLevelText;
            set => SetProperty(ref _zoomLevelText, value);
        }

        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        public ObservableCollection<FileItem> RecentFiles { get; private set; } = new();
        public ObservableCollection<FileItem> FileTreeItems { get; private set; } = new();
        public ObservableCollection<TocItem> TocItems { get; private set; } = new();

        #endregion

        #region Commands

        public ICommand? OpenFileCommand { get; private set; }
        public ICommand? SelectFolderCommand { get; private set; }
        public ICommand? ToggleSidebarCommand { get; private set; }
        public ICommand? ToggleSearchCommand { get; private set; }
        public ICommand? SearchCommand { get; private set; }
        public ICommand? CloseSearchCommand { get; private set; }
        public ICommand? IncreaseFontSizeCommand { get; private set; }
        public ICommand? DecreaseFontSizeCommand { get; private set; }
        public ICommand? ResetFontSizeCommand { get; private set; }
        public ICommand? ToggleDarkThemeCommand { get; private set; }
        public ICommand? PrintCommand { get; private set; }
        public ICommand? ExportToPdfCommand { get; private set; }
        public ICommand? AboutCommand { get; private set; }
        public ICommand? ExitCommand { get; private set; }

        #endregion

        #region Public Methods

        public async Task LoadMarkdownFileAsync(string filePath)
        {
            try
            {
                StatusText = $"読み込み中: {Path.GetFileName(filePath)}";

                var markdown = await _fileService.ReadFileAsync(filePath);
                var html = _markdownService.ConvertToHtml(markdown, IsDarkTheme, SelectedFontSize);

                // HTMLを生成してWebViewに渡す処理は、ViewでWebViewに直接アクセスする必要があるため、
                // イベントまたはメッセンジャーパターンを使用する
                OnMarkdownContentChanged?.Invoke(html);

                CurrentFilePath = filePath;
                AddToRecentFiles(filePath);
                GenerateTableOfContents(markdown);
                UpdateFileInfo(filePath);

                var version = GetApplicationVersion();
                WindowTitle = $"MarkView v{version} - {Path.GetFileName(filePath)}";
                StatusText = $"ファイルを開きました: {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                StatusText = "エラー: ファイルの読み込みに失敗しました";
                MessageBox.Show($"ファイルの読み込みに失敗しました: {ex.Message}",
                              "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadFolder(string folderPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    StatusText = "エラー: フォルダパスが無効です";
                    return;
                }

                if (!Directory.Exists(folderPath))
                {
                    StatusText = "エラー: フォルダが見つかりません";
                    MessageBox.Show($"指定されたフォルダが見つかりません: {folderPath}",
                                  "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                StatusText = $"フォルダを読み込み中: {Path.GetFileName(folderPath)}";
                CurrentFolderPath = folderPath;
                FileTreeItems.Clear();

                var items = _fileService.LoadFolderTree(folderPath);

                if (items?.Count > 0)
                {
                    foreach (var item in items)
                    {
                        FileTreeItems.Add(item);
                    }
                    StatusText = $"フォルダを選択しました: {Path.GetFileName(folderPath)} ({items.Count}個のアイテム)";
                }
                else
                {
                    StatusText = $"フォルダを選択しました: {Path.GetFileName(folderPath)} (マークダウンファイルが見つかりません)";
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                var errorMsg = $"フォルダへのアクセス権限がありません: {folderPath}";
                StatusText = "エラー: アクセス権限不足";
                System.Diagnostics.Debug.WriteLine($"アクセス権限エラー: {ex.Message}");
                MessageBox.Show(errorMsg, "アクセス権限エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (DirectoryNotFoundException ex)
            {
                var errorMsg = $"フォルダが見つかりません: {folderPath}";
                StatusText = "エラー: フォルダが見つかりません";
                System.Diagnostics.Debug.WriteLine($"フォルダ不存在エラー: {ex.Message}");
                MessageBox.Show(errorMsg, "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (IOException ex)
            {
                var errorMsg = $"フォルダの読み込み中にI/Oエラーが発生しました: {ex.Message}";
                StatusText = "エラー: I/Oエラー";
                System.Diagnostics.Debug.WriteLine($"I/Oエラー: {ex.Message}");
                MessageBox.Show(errorMsg, "I/Oエラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                var errorMsg = $"フォルダの読み込みに失敗しました: {ex.Message}";
                StatusText = "エラー: フォルダ読み込み失敗";
                System.Diagnostics.Debug.WriteLine($"予期しないエラー ({ex.GetType().Name}): {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"スタックトレース: {ex.StackTrace}");
                MessageBox.Show(errorMsg, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task RefreshCurrentFileAsync()
        {
            if (CurrentFilePath != null && File.Exists(CurrentFilePath))
            {
                await LoadMarkdownFileAsync(CurrentFilePath);
            }
        }

        #endregion

        #region Events

        public event Action<string>? OnMarkdownContentChanged;

        #endregion

        #region Private Methods

        private void InitializeCollections()
        {
            RecentFiles = new ObservableCollection<FileItem>();
            FileTreeItems = new ObservableCollection<FileItem>();
            TocItems = new ObservableCollection<TocItem>();
        }

        private void InitializeCommands()
        {
            ToggleSidebarCommand = new RelayCommand(() => IsSidebarVisible = !IsSidebarVisible);
            ToggleSearchCommand = new RelayCommand(() => IsSearchVisible = !IsSearchVisible);
            CloseSearchCommand = new RelayCommand(() => { IsSearchVisible = false; SearchText = ""; });
            IncreaseFontSizeCommand = new RelayCommand(IncreaseFontSize);
            DecreaseFontSizeCommand = new RelayCommand(DecreaseFontSize);
            ResetFontSizeCommand = new RelayCommand(() => SelectedFontSize = 14);
            ToggleDarkThemeCommand = new RelayCommand(async () => { IsDarkTheme = !IsDarkTheme; await RefreshCurrentFileAsync(); });
            AboutCommand = new RelayCommand(ShowAboutDialog);
            ExitCommand = new RelayCommand(() => Application.Current.Shutdown());
            ExportToPdfCommand = new RelayCommand(() => MessageBox.Show("PDF エクスポート機能は今後のバージョンで実装予定です。", "情報",
                                      MessageBoxButton.OK, MessageBoxImage.Information));
        }

        private void SetApplicationTitle()
        {
            var version = GetApplicationVersion();
            WindowTitle = $"MarkView v{version} - 軽量マークダウンビューア";
        }

        private static string GetApplicationVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
        }

        private void GenerateTableOfContents(string markdown)
        {
            TocItems.Clear();
            var tocItems = _markdownService.GenerateTableOfContents(markdown);
            foreach (var item in tocItems)
            {
                TocItems.Add(item);
            }
        }

        private void AddToRecentFiles(string filePath)
        {
            var existing = RecentFiles.FirstOrDefault(f => f.FilePath == filePath);
            if (existing != null)
            {
                RecentFiles.Remove(existing);
            }

            RecentFiles.Insert(0, new FileItem
            {
                Name = Path.GetFileName(filePath),
                FilePath = filePath,
                LastOpened = DateTime.Now
            });

            if (RecentFiles.Count > 10)
            {
                RecentFiles.RemoveAt(RecentFiles.Count - 1);
            }
        }

        private void UpdateFileInfo(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            FileInfoText = $"サイズ: {_fileService.FormatFileSize(fileInfo.Length)} | 更新: {fileInfo.LastWriteTime:yyyy/MM/dd HH:mm}";
        }

        private void IncreaseFontSize()
        {
            var fontSizes = new[] { 10, 12, 14, 16, 18, 20, 24 };
            var currentIndex = Array.IndexOf(fontSizes, SelectedFontSize);
            if (currentIndex < fontSizes.Length - 1)
            {
                SelectedFontSize = fontSizes[currentIndex + 1];
            }
        }

        private void DecreaseFontSize()
        {
            var fontSizes = new[] { 10, 12, 14, 16, 18, 20, 24 };
            var currentIndex = Array.IndexOf(fontSizes, SelectedFontSize);
            if (currentIndex > 0)
            {
                SelectedFontSize = fontSizes[currentIndex - 1];
            }
        }

        private void ShowAboutDialog()
        {
            var version = GetApplicationVersion();
            MessageBox.Show(
                "MarkView - 軽量マークダウンビューア\n\n" +
                $"バージョン: {version}\n" +
                "Obsidianのような使いやすさを目指したマークダウンビューアです。\n\n" +
                "使用ライブラリ:\n" +
                "- Markdig (マークダウン変換)\n" +
                "- WebView2 (HTML レンダリング)\n" +
                "- CommunityToolkit.Mvvm (MVVM サポート)",
                "MarkView について",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        #endregion
    }
}