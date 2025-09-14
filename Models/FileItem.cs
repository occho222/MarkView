using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace MarkView.Models
{
    public class FileItem : INotifyPropertyChanged
    {
        private string _name = "";
        private string _filePath = "";
        private bool _isDirectory;
        private DateTime _lastOpened;
        private ObservableCollection<FileItem>? _children;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
        }

        public bool IsDirectory
        {
            get => _isDirectory;
            set => SetProperty(ref _isDirectory, value);
        }

        public DateTime LastOpened
        {
            get => _lastOpened;
            set => SetProperty(ref _lastOpened, value);
        }

        public string FileName => Path.GetFileName(FilePath);

        public ObservableCollection<FileItem>? Children
        {
            get => _children;
            set => SetProperty(ref _children, value);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}