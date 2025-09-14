using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using System.Windows.Forms;
using MarkView.Models;
using MarkView.ViewModels;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;

namespace MarkView.Views
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();
            InitializeAsync();
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

                if (ViewModel != null)
                {
                    ViewModel.OnMarkdownContentChanged += OnMarkdownContentChanged;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初期化エラー: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnMarkdownContentChanged(string html)
        {
            MarkdownWebView.NavigateToString(html);
            WelcomeScreen.Visibility = Visibility.Collapsed;
            MarkdownWebView.Visibility = Visibility.Visible;
        }

        #region Event Handlers - UI Interactions Only

        private async void Window_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (ViewModel != null && e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    var filePath = files[0];
                    if (Path.GetExtension(filePath).ToLower() is ".md" or ".markdown" or ".txt")
                    {
                        await ViewModel.LoadMarkdownFileAsync(filePath);
                    }
                    else
                    {
                        MessageBox.Show("マークダウンファイル (.md, .markdown, .txt) を選択してください。",
                                      "無効なファイル", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        private async void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;

            var dialog = new OpenFileDialog
            {
                Title = "マークダウンファイルを開く",
                Filter = "マークダウンファイル (*.md;*.markdown;*.txt)|*.md;*.markdown;*.txt|すべてのファイル (*.*)|*.*",
                FilterIndex = 1
            };

            if (dialog.ShowDialog() == true)
            {
                await ViewModel.LoadMarkdownFileAsync(dialog.FileName);
            }
        }

        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;

            using var dialog = new FolderBrowserDialog();
            dialog.Description = "マークダウンファイルが含まれるフォルダを選択してください";

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ViewModel.LoadFolder(dialog.SelectedPath);
            }
        }

        private async void FileTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is FileItem fileItem && !fileItem.IsDirectory)
            {
                await ViewModel.LoadMarkdownFileAsync(fileItem.FilePath);
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

        private async void RecentFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is FileItem fileItem)
            {
                if (File.Exists(fileItem.FilePath))
                {
                    await ViewModel.LoadMarkdownFileAsync(fileItem.FilePath);
                }
                else
                {
                    MessageBox.Show("ファイルが見つかりません。", "エラー",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    ViewModel.RecentFiles.Remove(fileItem);
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
            ViewModel.ExportToPdfCommand?.Execute(null);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ExitCommand?.Execute(null);
        }

        private async void LightTheme_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsDarkTheme = false;
            LightThemeMenuItem.IsChecked = true;
            DarkThemeMenuItem.IsChecked = false;
            await ViewModel.RefreshCurrentFileAsync();
        }

        private async void DarkTheme_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsDarkTheme = true;
            LightThemeMenuItem.IsChecked = false;
            DarkThemeMenuItem.IsChecked = true;
            await ViewModel.RefreshCurrentFileAsync();
        }

        private async void FontSize_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel != null && sender is System.Windows.Controls.ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                ViewModel.SelectedFontSize = int.Parse(item.Tag.ToString()!);
                await ViewModel.RefreshCurrentFileAsync();
            }
        }

        private void IncreaseFontSize_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IncreaseFontSizeCommand?.Execute(null);
        }

        private void DecreaseFontSize_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.DecreaseFontSizeCommand?.Execute(null);
        }

        private void ResetFontSize_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ResetFontSizeCommand?.Execute(null);
        }

        private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ToggleSidebarCommand?.Execute(null);
            ShowSidebarMenuItem.IsChecked = ViewModel.IsSidebarVisible;

            if (ViewModel.IsSidebarVisible)
            {
                SidebarColumn.Width = new GridLength(250);
                SidebarGrid.Visibility = Visibility.Visible;
            }
            else
            {
                SidebarColumn.Width = new GridLength(0);
                SidebarGrid.Visibility = Visibility.Collapsed;
            }
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ToggleSearchCommand?.Execute(null);
            SearchPanel.Visibility = ViewModel.IsSearchVisible ? Visibility.Visible : Visibility.Collapsed;

            if (ViewModel.IsSearchVisible)
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
            var searchText = ViewModel.SearchText?.Trim();
            if (!string.IsNullOrEmpty(searchText))
            {
                var script = $@"window.find('{searchText.Replace("'", "\\'")}', false, false, true, false, true, false);";
                _ = MarkdownWebView.ExecuteScriptAsync(script);
            }
        }

        private void CloseSearch_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CloseSearchCommand?.Execute(null);
            SearchPanel.Visibility = Visibility.Collapsed;
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AboutCommand?.Execute(null);
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

        private void ProjectItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item && item.DataContext is Project project)
            {
                ViewModel?.SetActiveProjectCommand?.Execute(project.Id);
            }
        }

        private void FavoriteItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item && item.DataContext is FavoriteItem favorite)
            {
                ViewModel?.OpenFavoriteCommand?.Execute(favorite);
            }
        }

        private async void ProjectFile_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item && item.DataContext is FileItem fileItem && ViewModel != null)
            {
                await ViewModel.LoadMarkdownFileAsync(fileItem.FilePath);
            }
        }

        #endregion
    }
}