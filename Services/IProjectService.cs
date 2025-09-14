using System.Collections.ObjectModel;
using MarkView.Models;

namespace MarkView.Services
{
    public interface IProjectService
    {
        Task<ObservableCollection<Project>> LoadProjectsAsync();
        Task SaveProjectsAsync();
        Task<Project> CreateProjectAsync(string name, string folderPath, string description = "");
        Task DeleteProjectAsync(string projectId);
        Task<Project?> GetProjectAsync(string projectId);
        Task<Project?> GetActiveProjectAsync();
        Task SetActiveProjectAsync(string? projectId);
        Task UpdateProjectAsync(Project project);
        Task RefreshProjectFilesAsync(Project project);
        ObservableCollection<Project> Projects { get; }
        Project? ActiveProject { get; }
        event Action<Project?>? ActiveProjectChanged;
    }
}