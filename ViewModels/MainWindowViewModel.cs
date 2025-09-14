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
        private readonly IProjectService _projectService;
        private readonly IFavoriteService _favoriteService;

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

        public MainWindowViewModel(IMarkdownService markdownService, IFileService fileService, IProjectService projectService, IFavoriteService favoriteService)
        {
            _markdownService = markdownService;
            _fileService = fileService;
            _projectService = projectService;
            _favoriteService = favoriteService;

            InitializeCollections();
            InitializeCommands();
            SetApplicationTitle();
            InitializeAsync();
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
        public ObservableCollection<Project> Projects => _projectService.Projects;
        public ObservableCollection<FavoriteItem> Favorites => _favoriteService.Favorites;
        public Project? ActiveProject => _projectService.ActiveProject;

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

        public ICommand? CreateProjectCommand { get; private set; }
        public ICommand? DeleteProjectCommand { get; private set; }
        public ICommand? SetActiveProjectCommand { get; private set; }
        public ICommand? RefreshProjectCommand { get; private set; }
        public ICommand? AddToFavoritesCommand { get; private set; }
        public ICommand? RemoveFromFavoritesCommand { get; private set; }
        public ICommand? OpenFavoriteCommand { get; private set; }

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

            CreateProjectCommand = new RelayCommand(CreateProject);
            DeleteProjectCommand = new RelayCommand<string>(DeleteProject);
            SetActiveProjectCommand = new RelayCommand<string>(SetActiveProject);
            RefreshProjectCommand = new RelayCommand<string>(RefreshProject);
            AddToFavoritesCommand = new RelayCommand(AddToFavorites);
            RemoveFromFavoritesCommand = new RelayCommand<string>(RemoveFromFavorites);
            OpenFavoriteCommand = new RelayCommand<FavoriteItem>(OpenFavorite);
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

        #region Project and Favorite Methods

        private async void InitializeAsync()
        {
            try
            {
                await _projectService.LoadProjectsAsync();
                await _favoriteService.LoadFavoritesAsync();

                _projectService.ActiveProjectChanged += OnActiveProjectChanged;

                StatusText = "プロジェクトとお気に入りを読み込み完了";
            }
            catch (Exception ex)
            {
                StatusText = "初期化エラーが発生しました";
                System.Diagnostics.Debug.WriteLine($"初期化エラー: {ex.Message}");
            }
        }

        private void OnActiveProjectChanged(Project? activeProject)
        {
            OnPropertyChanged(nameof(ActiveProject));
            if (activeProject != null)
            {
                StatusText = $"アクティブプロジェクト: {activeProject.Name} ({activeProject.MarkdownFiles.Count}ファイル)";
            }
            else
            {
                StatusText = "アクティブプロジェクト: 未選択";
            }
        }

        private async void CreateProject()
        {
            try
            {
                using var dialog = new System.Windows.Forms.FolderBrowserDialog();
                dialog.Description = "プロジェクト用フォルダを選択してください";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var folderPath = dialog.SelectedPath;
                    var projectName = System.IO.Path.GetFileName(folderPath);

                    var project = await _projectService.CreateProjectAsync(projectName, folderPath);
                    await _projectService.SetActiveProjectAsync(project.Id);

                    StatusText = $"プロジェクト作成完了: {project.Name}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"プロジェクト作成に失敗しました: {ex.Message}", "エラー",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteProject(string? projectId)
        {
            if (string.IsNullOrEmpty(projectId)) return;

            try
            {
                var project = await _projectService.GetProjectAsync(projectId);
                if (project == null) return;

                var result = MessageBox.Show($"プロジェクト '{project.Name}' を削除しますか？", "確認",
                                           MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await _projectService.DeleteProjectAsync(projectId);
                    StatusText = $"プロジェクト削除完了: {project.Name}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"プロジェクト削除に失敗しました: {ex.Message}", "エラー",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SetActiveProject(string? projectId)
        {
            try
            {
                await _projectService.SetActiveProjectAsync(projectId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"プロジェクト切り替えに失敗しました: {ex.Message}", "エラー",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RefreshProject(string? projectId)
        {
            if (string.IsNullOrEmpty(projectId)) return;

            try
            {
                var project = await _projectService.GetProjectAsync(projectId);
                if (project != null)
                {
                    await _projectService.RefreshProjectFilesAsync(project);
                    StatusText = $"プロジェクト更新完了: {project.Name}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"プロジェクト更新に失敗しました: {ex.Message}", "エラー",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AddToFavorites()
        {
            try
            {
                if (string.IsNullOrEmpty(CurrentFilePath)) return;

                var isAlreadyFavorite = await _favoriteService.IsFavoriteAsync(CurrentFilePath);
                if (isAlreadyFavorite)
                {
                    MessageBox.Show("このファイルは既にお気に入りに登録されています。", "情報",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var fileName = System.IO.Path.GetFileNameWithoutExtension(CurrentFilePath);
                var favorite = await _favoriteService.AddFavoriteAsync(fileName, CurrentFilePath);

                StatusText = $"お気に入りに追加: {favorite.Title}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"お気に入り追加に失敗しました: {ex.Message}", "エラー",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RemoveFromFavorites(string? favoriteId)
        {
            if (string.IsNullOrEmpty(favoriteId)) return;

            try
            {
                var favorite = await _favoriteService.GetFavoriteAsync(favoriteId);
                if (favorite == null) return;

                var result = MessageBox.Show($"お気に入り '{favorite.Title}' を削除しますか？", "確認",
                                           MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await _favoriteService.RemoveFavoriteAsync(favoriteId);
                    StatusText = $"お気に入り削除完了: {favorite.Title}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"お気に入り削除に失敗しました: {ex.Message}", "エラー",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OpenFavorite(FavoriteItem? favorite)
        {
            if (favorite == null) return;

            try
            {
                if (File.Exists(favorite.FilePath))
                {
                    await LoadMarkdownFileAsync(favorite.FilePath);
                    await _favoriteService.MarkFavoriteAccessedAsync(favorite.Id);
                }
                else
                {
                    MessageBox.Show("ファイルが見つかりません。", "エラー",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ファイル読み込みに失敗しました: {ex.Message}", "エラー",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}