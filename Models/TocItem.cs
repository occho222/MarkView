using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MarkView.Models
{
    public class TocItem : INotifyPropertyChanged
    {
        private string _title = "";
        private int _level;
        private ObservableCollection<TocItem>? _children;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public int Level
        {
            get => _level;
            set => SetProperty(ref _level, value);
        }

        public ObservableCollection<TocItem>? Children
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