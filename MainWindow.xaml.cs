using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using Markdig;
using System.Diagnostics;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;

namespace MarkView
{
    public partial class MainWindow : Window
    {
        private bool _isDarkTheme = false;
        private double _currentZoom = 1.0;
        private string? _currentFilePath = null;
        private string? _currentFolderPath = null;
        private readonly ObservableCollection<FileItem> _recentFiles = new();
        private readonly ObservableCollection<FileItem> _fileTreeItems = new();
        private readonly ObservableCollection<TocItem> _tocItems = new();

        public MainWindow()
        {
            InitializeComponent();
            SetApplicationTitle();
            InitializeAsync();
        }

        private void SetApplicationTitle()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
            Title = $"MarkView v{version} - 軽量マークダウンビューア";
        }

        private async void InitializeAsync()
        {
            try
            {
                await MarkdownWebView.EnsureCoreWebView2Async();

                MarkdownWebView.CoreWebView2.Settings.IsSwipeNavigationEnabled = false;
                MarkdownWebView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
                MarkdownWebView.CoreWebView2.Settings.AreHostObjectsAllowed = false;
                MarkdownWebView.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;

                RecentFilesListBox.ItemsSource = _recentFiles;
                FileTreeView.ItemsSource = _fileTreeItems;
                TocTreeView.ItemsSource = _tocItems;

                LoadSettings();
                LoadRecentFiles();

                UpdateStatusBar("準備完了");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初期化エラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSettings()
        {
        }

        private void LoadRecentFiles()
        {
        }

        private void SaveSettings()
        {
        }

        private void Window_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    var filePath = files[0];
                    if (IsMarkdownFile(filePath))
                    {
                        LoadMarkdownFile(filePath);
                    }
                    else
                    {
                        MessageBox.Show("マークダウンファイル (.md, .markdown, .txt) を選択してください。",
                                      "無効なファイル", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        private bool IsMarkdownFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLower();
            return extension == ".md" || extension == ".markdown" || extension == ".txt";
        }

        private async void LoadMarkdownFile(string filePath)
        {
            try
            {
                UpdateStatusBar($"読み込み中: {Path.GetFileName(filePath)}");

                var markdown = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
                var html = ConvertMarkdownToHtml(markdown);

                MarkdownWebView.NavigateToString(html);

                _currentFilePath = filePath;
                WelcomeScreen.Visibility = Visibility.Collapsed;
                MarkdownWebView.Visibility = Visibility.Visible;

                AddToRecentFiles(filePath);
                GenerateTableOfContents(markdown);
                UpdateFileInfo(filePath);

                var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
                Title = $"MarkView v{version} - {Path.GetFileName(filePath)}";
                UpdateStatusBar($"ファイルを開きました: {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ファイルの読み込みに失敗しました: {ex.Message}",
                              "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatusBar("エラー: ファイルの読み込みに失敗しました");
            }
        }

        private string ConvertMarkdownToHtml(string markdown)
        {
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseEmojiAndSmiley()
                .Build();

            var htmlContent = Markdown.ToHtml(markdown, pipeline);

            return CreateHtmlTemplate(htmlContent);
        }

        private string CreateHtmlTemplate(string content)
        {
            var theme = _isDarkTheme ? "dark" : "light";
            var fontSize = GetCurrentFontSize();

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <title>Markdown Preview</title>
    <link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/styles/{(_isDarkTheme ? "github-dark" : "github")}.min.css"">
    <script src=""https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/highlight.min.js""></script>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Noto Sans', Helvetica, Arial, sans-serif;
            font-size: {fontSize}px;
            line-height: 1.6;
            color: {(_isDarkTheme ? "#e6edf3" : "#1f2328")};
            background-color: {(_isDarkTheme ? "#0d1117" : "#ffffff")};
            max-width: none;
            margin: 0;
            padding: 20px;
        }}

        h1, h2, h3, h4, h5, h6 {{
            margin-top: 24px;
            margin-bottom: 16px;
            font-weight: 600;
            line-height: 1.25;
            border-bottom: {(_isDarkTheme ? "1px solid #30363d" : "1px solid #d0d7de")};
            padding-bottom: 0.3em;
        }}

        h1 {{ font-size: 2em; }}
        h2 {{ font-size: 1.5em; }}
        h3 {{ font-size: 1.25em; }}
        h4 {{ font-size: 1em; }}
        h5 {{ font-size: 0.875em; }}
        h6 {{ font-size: 0.85em; color: {(_isDarkTheme ? "#7d8590" : "#656d76")}; }}

        p {{ margin-bottom: 16px; }}

        code {{
            background-color: {(_isDarkTheme ? "#161b22" : "#f6f8fa")};
            color: {(_isDarkTheme ? "#f0f6fc" : "#1f2328")};
            padding: 0.2em 0.4em;
            border-radius: 6px;
            font-size: 85%;
            font-family: 'SFMono-Regular', Consolas, 'Liberation Mono', Menlo, monospace;
        }}

        pre {{
            background-color: {(_isDarkTheme ? "#161b22" : "#f6f8fa")};
            border-radius: 6px;
            padding: 16px;
            overflow: auto;
            margin-bottom: 16px;
        }}

        pre code {{
            background-color: transparent;
            padding: 0;
            border-radius: 0;
            font-size: inherit;
        }}

        blockquote {{
            margin: 0 0 16px 0;
            padding: 0 1em;
            color: {(_isDarkTheme ? "#7d8590" : "#656d76")};
            border-left: 0.25em solid {(_isDarkTheme ? "#30363d" : "#d0d7de")};
        }}

        table {{
            border-collapse: collapse;
            border-spacing: 0;
            margin-bottom: 16px;
            width: 100%;
        }}

        table th, table td {{
            padding: 6px 13px;
            border: 1px solid {(_isDarkTheme ? "#30363d" : "#d0d7de")};
        }}

        table th {{
            background-color: {(_isDarkTheme ? "#161b22" : "#f6f8fa")};
            font-weight: 600;
        }}

        ul, ol {{ margin-bottom: 16px; }}

        img {{
            max-width: 100%;
            height: auto;
        }}

        a {{
            color: {(_isDarkTheme ? "#58a6ff" : "#0969da")};
            text-decoration: none;
        }}

        a:hover {{
            text-decoration: underline;
        }}

        hr {{
            height: 0.25em;
            padding: 0;
            margin: 24px 0;
            background-color: {(_isDarkTheme ? "#30363d" : "#d0d7de")};
            border: 0;
        }}
    </style>
</head>
<body>
    {content}
    <script>
        hljs.highlightAll();

        document.addEventListener('click', function(e) {{
            if (e.target.tagName === 'A') {{
                e.preventDefault();
                window.external.OpenLink(e.target.href);
            }}
        }});
    </script>
</body>
</html>";
        }

        private int GetCurrentFontSize()
        {
            if (FontSizeComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                return int.Parse(item.Tag.ToString()!);
            }
            return 14;
        }

        private void GenerateTableOfContents(string markdown)
        {
            _tocItems.Clear();

            var lines = markdown.Split('\n');
            var tocStack = new Stack<TocItem>();

            foreach (var line in lines)
            {
                var match = Regex.Match(line, @"^(#{1,6})\s+(.+)$");
                if (match.Success)
                {
                    var level = match.Groups[1].Value.Length;
                    var title = match.Groups[2].Value;
                    var tocItem = new TocItem { Title = title, Level = level, Children = new ObservableCollection<TocItem>() };

                    while (tocStack.Count > 0 && tocStack.Peek().Level >= level)
                    {
                        tocStack.Pop();
                    }

                    if (tocStack.Count == 0)
                    {
                        _tocItems.Add(tocItem);
                    }
                    else
                    {
                        tocStack.Peek().Children?.Add(tocItem);
                    }

                    tocStack.Push(tocItem);
                }
            }
        }

        private void AddToRecentFiles(string filePath)
        {
            var existing = _recentFiles.FirstOrDefault(f => f.FilePath == filePath);
            if (existing != null)
            {
                _recentFiles.Remove(existing);
            }

            _recentFiles.Insert(0, new FileItem
            {
                Name = Path.GetFileName(filePath),
                FilePath = filePath,
                LastOpened = DateTime.Now
            });

            if (_recentFiles.Count > 10)
            {
                _recentFiles.RemoveAt(_recentFiles.Count - 1);
            }
        }

        private void UpdateFileInfo(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            FileInfoText.Text = $"サイズ: {FormatFileSize(fileInfo.Length)} | 更新: {fileInfo.LastWriteTime:yyyy/MM/dd HH:mm}";
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes >= 1024 * 1024)
                return $"{bytes / (1024.0 * 1024.0):F1} MB";
            if (bytes >= 1024)
                return $"{bytes / 1024.0:F1} KB";
            return $"{bytes} B";
        }

        private void UpdateStatusBar(string message)
        {
            StatusText.Text = message;
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "マークダウンファイルを開く",
                Filter = "マークダウンファイル (*.md;*.markdown;*.txt)|*.md;*.markdown;*.txt|すべてのファイル (*.*)|*.*",
                FilterIndex = 1
            };

            if (dialog.ShowDialog() == true)
            {
                LoadMarkdownFile(dialog.FileName);
            }
        }

        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "マークダウンファイルが含まれるフォルダを選択してください";

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                LoadFolder(dialog.SelectedPath);
            }
        }

        private void LoadFolder(string folderPath)
        {
            try
            {
                _currentFolderPath = folderPath;
                CurrentFolderText.Text = Path.GetFileName(folderPath);

                LoadFolderTree(folderPath);

                UpdateStatusBar($"フォルダを選択しました: {Path.GetFileName(folderPath)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"フォルダの読み込みに失敗しました: {ex.Message}",
                              "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadFolderTree(string folderPath)
        {
            _fileTreeItems.Clear();

            try
            {
                var rootItem = CreateFileItem(new DirectoryInfo(folderPath));
                _fileTreeItems.Add(rootItem);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ファイル一覧の読み込みに失敗しました: {ex.Message}",
                              "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FileItem CreateFileItem(DirectoryInfo dirInfo)
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
                foreach (var dir in dirInfo.GetDirectories())
                {
                    if (!dir.Name.StartsWith("."))
                    {
                        item.Children.Add(CreateFileItem(dir));
                    }
                }

                foreach (var file in dirInfo.GetFiles())
                {
                    if (IsMarkdownFile(file.FullName))
                    {
                        item.Children.Add(new FileItem
                        {
                            Name = file.Name,
                            FilePath = file.FullName,
                            IsDirectory = false,
                            LastOpened = file.LastWriteTime
                        });
                    }
                }
            }
            catch
            {
            }

            return item;
        }

        private void FileTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is FileItem fileItem && !fileItem.IsDirectory && IsMarkdownFile(fileItem.FilePath))
            {
                LoadMarkdownFile(fileItem.FilePath);
            }
        }

        private void TocTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TocItem tocItem)
            {
                var script = $"document.querySelector('h{tocItem.Level}')?.scrollIntoView({{ behavior: 'smooth' }});";
                _ = MarkdownWebView.ExecuteScriptAsync(script);
            }
        }

        private void RecentFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is FileItem fileItem)
            {
                if (File.Exists(fileItem.FilePath))
                {
                    LoadMarkdownFile(fileItem.FilePath);
                }
                else
                {
                    MessageBox.Show("ファイルが見つかりません。", "エラー",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    _recentFiles.Remove(fileItem);
                }
            }
        }

        private async void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await MarkdownWebView.CoreWebView2.PrintAsync(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"印刷に失敗しました: {ex.Message}", "エラー",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToPdf_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("PDF エクスポート機能は今後のバージョンで実装予定です。", "情報",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void LightTheme_Click(object sender, RoutedEventArgs e)
        {
            _isDarkTheme = false;
            LightThemeMenuItem.IsChecked = true;
            DarkThemeMenuItem.IsChecked = false;

            if (_currentFilePath != null)
            {
                await RefreshCurrentFile();
            }
        }

        private async void DarkTheme_Click(object sender, RoutedEventArgs e)
        {
            _isDarkTheme = true;
            LightThemeMenuItem.IsChecked = false;
            DarkThemeMenuItem.IsChecked = true;

            if (_currentFilePath != null)
            {
                await RefreshCurrentFile();
            }
        }

        private async Task RefreshCurrentFile()
        {
            if (_currentFilePath != null && File.Exists(_currentFilePath))
            {
                var markdown = await File.ReadAllTextAsync(_currentFilePath, Encoding.UTF8);
                var html = ConvertMarkdownToHtml(markdown);
                MarkdownWebView.NavigateToString(html);
                UpdateZoomDisplay();
            }
        }

        private async void FontSize_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_currentFilePath != null)
            {
                await RefreshCurrentFile();
            }
        }

        private void IncreaseFontSize_Click(object sender, RoutedEventArgs e)
        {
            var currentIndex = FontSizeComboBox.SelectedIndex;
            if (currentIndex < FontSizeComboBox.Items.Count - 1)
            {
                FontSizeComboBox.SelectedIndex = currentIndex + 1;
            }
        }

        private void DecreaseFontSize_Click(object sender, RoutedEventArgs e)
        {
            var currentIndex = FontSizeComboBox.SelectedIndex;
            if (currentIndex > 0)
            {
                FontSizeComboBox.SelectedIndex = currentIndex - 1;
            }
        }

        private void ResetFontSize_Click(object sender, RoutedEventArgs e)
        {
            FontSizeComboBox.SelectedIndex = 2;
        }

        private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            var isVisible = SidebarGrid.Visibility == Visibility.Visible;
            SidebarGrid.Visibility = isVisible ? Visibility.Collapsed : Visibility.Visible;
            ShowSidebarMenuItem.IsChecked = !isVisible;

            if (isVisible)
            {
                SidebarColumn.Width = new GridLength(0);
            }
            else
            {
                SidebarColumn.Width = new GridLength(250);
            }
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            SearchPanel.Visibility = SearchPanel.Visibility == Visibility.Collapsed ?
                Visibility.Visible : Visibility.Collapsed;

            if (SearchPanel.Visibility == Visibility.Visible)
            {
                SearchTextBox.Focus();
            }
        }

        private void SearchTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PerformSearch();
            }
            else if (e.Key == Key.Escape)
            {
                CloseSearch_Click(sender, new RoutedEventArgs());
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch();
        }

        private void PerformSearch()
        {
            var searchText = SearchTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(searchText))
            {
                var script = $@"
                    window.find('{searchText.Replace("'", "\\'")}', false, false, true, false, true, false);
                ";
                _ = MarkdownWebView.ExecuteScriptAsync(script);
            }
        }

        private void CloseSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchPanel.Visibility = Visibility.Collapsed;
            SearchTextBox.Text = "";
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
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

        private void UpdateZoomDisplay()
        {
            ZoomLevelText.Text = $"ズーム: {(_currentZoom * 100):F0}%";
        }

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.O && Keyboard.Modifiers == ModifierKeys.Control)
            {
                OpenFile_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Search_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.P && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Print_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.Add && Keyboard.Modifiers == ModifierKeys.Control)
            {
                IncreaseFontSize_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.Subtract && Keyboard.Modifiers == ModifierKeys.Control)
            {
                DecreaseFontSize_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.D0 && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ResetFontSize_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }
    }

    public class FileItem
    {
        public string Name { get; set; } = "";
        public string FilePath { get; set; } = "";
        public bool IsDirectory { get; set; }
        public DateTime LastOpened { get; set; }
        public string FileName => Path.GetFileName(FilePath);
        public ObservableCollection<FileItem>? Children { get; set; }
    }

    public class TocItem
    {
        public string Title { get; set; } = "";
        public int Level { get; set; }
        public ObservableCollection<TocItem>? Children { get; set; }
    }
}