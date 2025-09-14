using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MarkView.Models
{
    public class FavoriteItem : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _title = string.Empty;
        private string _filePath = string.Empty;
        private string _description = string.Empty;
        private string _category = string.Empty;
        private DateTime _addedAt = DateTime.Now;
        private DateTime _lastAccessedAt = DateTime.Now;
        private int _accessCount = 0;
        private bool _isPinned = false;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public string Category
        {
            get => _category;
            set => SetProperty(ref _category, value);
        }

        public DateTime AddedAt
        {
            get => _addedAt;
            set => SetProperty(ref _addedAt, value);
        }

        public DateTime LastAccessedAt
        {
            get => _lastAccessedAt;
            set => SetProperty(ref _lastAccessedAt, value);
        }

        public int AccessCount
        {
            get => _accessCount;
            set => SetProperty(ref _accessCount, value);
        }

        public bool IsPinned
        {
            get => _isPinned;
            set => SetProperty(ref _isPinned, value);
        }

        public string DisplayText => $"{Title} ({System.IO.Path.GetFileName(FilePath)})";
        public string FormattedAddedDate => AddedAt.ToString("yyyy/MM/dd HH:mm");
        public string FormattedLastAccessed => LastAccessedAt.ToString("yyyy/MM/dd HH:mm");

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

        public FavoriteItem()
        {
            Id = Guid.NewGuid().ToString();
        }

        public FavoriteItem(string title, string filePath, string description = "", string category = "")
        {
            Id = Guid.NewGuid().ToString();
            Title = title;
            FilePath = filePath;
            Description = description;
            Category = category;
        }

        public void MarkAsAccessed()
        {
            LastAccessedAt = DateTime.Now;
            AccessCount++;
        }
    }
}