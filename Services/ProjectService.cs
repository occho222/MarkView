using System.Collections.ObjectModel;
using System.IO;
using MarkView.Models;

namespace MarkView.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IDataPersistenceService _dataPersistence;
        private readonly IFileService _fileService;
        private const string ProjectsFileName = "projects.json";

        private Project? _activeProject;
        private ObservableCollection<Project> _projects = new();

        public ObservableCollection<Project> Projects => _projects;

        public Project? ActiveProject
        {
            get => _activeProject;
            private set
            {
                if (_activeProject != value)
                {
                    if (_activeProject != null)
                    {
                        _activeProject.IsActive = false;
                    }

                    _activeProject = value;

                    if (_activeProject != null)
                    {
                        _activeProject.IsActive = true;
                        _activeProject.LastOpenedAt = DateTime.Now;
                    }

                    ActiveProjectChanged?.Invoke(_activeProject);
                }
            }
        }

        public event Action<Project?>? ActiveProjectChanged;

        public ProjectService(IDataPersistenceService dataPersistence, IFileService fileService)
        {
            _dataPersistence = dataPersistence;
            _fileService = fileService;
        }

        public async Task<ObservableCollection<Project>> LoadProjectsAsync()
        {
            try
            {
                var projects = await _dataPersistence.LoadDataAsync<List<Project>>(ProjectsFileName);

                _projects.Clear();

                if (projects != null)
                {
                    foreach (var project in projects)
                    {
                        _projects.Add(project);

                        if (project.IsActive)
                        {
                            ActiveProject = project;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"プロジェクト読み込み完了: {_projects.Count}件");
                return _projects;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"プロジェクト読み込みエラー: {ex.Message}");
                return _projects;
            }
        }

        public async Task SaveProjectsAsync()
        {
            try
            {
                var projectList = _projects.ToList();
                await _dataPersistence.SaveDataAsync(projectList, ProjectsFileName);
                System.Diagnostics.Debug.WriteLine($"プロジェクト保存完了: {projectList.Count}件");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"プロジェクト保存エラー: {ex.Message}");
                throw;
            }
        }

        public async Task<Project> CreateProjectAsync(string name, string folderPath, string description = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("プロジェクト名を入力してください。", nameof(name));

                if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                    throw new ArgumentException("有効なフォルダパスを指定してください。", nameof(folderPath));

                var project = new Project(name, folderPath, description);

                await RefreshProjectFilesAsync(project);

                _projects.Add(project);
                await SaveProjectsAsync();

                System.Diagnostics.Debug.WriteLine($"プロジェクト作成完了: {project.Name} ({project.MarkdownFiles.Count}ファイル)");
                return project;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"プロジェクト作成エラー: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteProjectAsync(string projectId)
        {
            try
            {
                var project = _projects.FirstOrDefault(p => p.Id == projectId);
                if (project != null)
                {
                    if (ActiveProject?.Id == projectId)
                    {
                        ActiveProject = null;
                    }

                    _projects.Remove(project);
                    await SaveProjectsAsync();

                    System.Diagnostics.Debug.WriteLine($"プロジェクト削除完了: {project.Name}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"プロジェクト削除エラー: {ex.Message}");
                throw;
            }
        }

        public async Task<Project?> GetProjectAsync(string projectId)
        {
            try
            {
                return await Task.FromResult(_projects.FirstOrDefault(p => p.Id == projectId));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"プロジェクト取得エラー: {ex.Message}");
                return null;
            }
        }

        public async Task<Project?> GetActiveProjectAsync()
        {
            return await Task.FromResult(ActiveProject);
        }

        public async Task SetActiveProjectAsync(string? projectId)
        {
            try
            {
                if (string.IsNullOrEmpty(projectId))
                {
                    ActiveProject = null;
                }
                else
                {
                    var project = _projects.FirstOrDefault(p => p.Id == projectId);
                    ActiveProject = project;
                }

                await SaveProjectsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"アクティブプロジェクト設定エラー: {ex.Message}");
                throw;
            }
        }

        public async Task UpdateProjectAsync(Project project)
        {
            try
            {
                var existingProject = _projects.FirstOrDefault(p => p.Id == project.Id);
                if (existingProject != null)
                {
                    var index = _projects.IndexOf(existingProject);
                    _projects[index] = project;

                    if (ActiveProject?.Id == project.Id)
                    {
                        ActiveProject = project;
                    }

                    await SaveProjectsAsync();
                    System.Diagnostics.Debug.WriteLine($"プロジェクト更新完了: {project.Name}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"プロジェクト更新エラー: {ex.Message}");
                throw;
            }
        }

        public async Task RefreshProjectFilesAsync(Project project)
        {
            try
            {
                if (!Directory.Exists(project.FolderPath))
                {
                    System.Diagnostics.Debug.WriteLine($"プロジェクトフォルダが見つかりません: {project.FolderPath}");
                    return;
                }

                project.MarkdownFiles.Clear();

                var folderItems = _fileService.LoadFolderTree(project.FolderPath);

                foreach (var item in folderItems)
                {
                    await CollectMarkdownFilesAsync(item, project.MarkdownFiles);
                }

                System.Diagnostics.Debug.WriteLine($"プロジェクトファイル更新完了: {project.Name} ({project.MarkdownFiles.Count}ファイル)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"プロジェクトファイル更新エラー: {ex.Message}");
            }
        }

        private async Task CollectMarkdownFilesAsync(FileItem item, ObservableCollection<FileItem> markdownFiles)
        {
            await Task.Run(() =>
            {
                if (!item.IsDirectory && _fileService.IsMarkdownFile(item.FilePath))
                {
                    markdownFiles.Add(item);
                }

                if (item.Children != null)
                {
                    foreach (var child in item.Children)
                    {
                        CollectMarkdownFilesAsync(child, markdownFiles).Wait();
                    }
                }
            });
        }
    }
}