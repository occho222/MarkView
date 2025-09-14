using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MarkView.Models
{
    public class Project : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _name = string.Empty;
        private string _folderPath = string.Empty;
        private string _description = string.Empty;
        private DateTime _createdAt = DateTime.Now;
        private DateTime _lastOpenedAt = DateTime.Now;
        private bool _isActive = false;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string FolderPath
        {
            get => _folderPath;
            set => SetProperty(ref _folderPath, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        public DateTime LastOpenedAt
        {
            get => _lastOpenedAt;
            set => SetProperty(ref _lastOpenedAt, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public ObservableCollection<FileItem> MarkdownFiles { get; set; } = new();
        public ObservableCollection<FavoriteItem> Favorites { get; set; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public Project()
        {
            Id = Guid.NewGuid().ToString();
        }

        public Project(string name, string folderPath, string description = "")
        {
            Id = Guid.NewGuid().ToString();
            Name = name;
            FolderPath = folderPath;
            Description = description;
        }
    }
}