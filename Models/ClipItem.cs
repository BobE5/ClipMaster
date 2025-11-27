using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ClipMaster.Models
{
    public class ClipItem : INotifyPropertyChanged
    {
        private int _id;
        private string _text = string.Empty;
        private DateTime _timestamp;
        private string _sourceApp = string.Empty;
        private bool _isFavorite;
        private string _category = string.Empty;

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Text
        {
            get => _text;
            set { _text = value; OnPropertyChanged(); OnPropertyChanged(nameof(Preview)); }
        }

        public DateTime Timestamp
        {
            get => _timestamp;
            set { _timestamp = value; OnPropertyChanged(); OnPropertyChanged(nameof(TimeDisplay)); }
        }

        public string SourceApp
        {
            get => _sourceApp;
            set { _sourceApp = value; OnPropertyChanged(); }
        }

        public bool IsFavorite
        {
            get => _isFavorite;
            set { _isFavorite = value; OnPropertyChanged(); }
        }

        public string Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(); }
        }

        // Computed properties for display
        public string Preview
        {
            get
            {
                if (string.IsNullOrEmpty(Text)) return string.Empty;
                var preview = Text.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
                return preview.Length > 60 ? preview.Substring(0, 57) + "..." : preview;
            }
        }

        public string TimeDisplay
        {
            get
            {
                var today = DateTime.Today;
                if (Timestamp.Date == today)
                    return Timestamp.ToString("h:mm tt");
                if (Timestamp.Date == today.AddDays(-1))
                    return "Yesterday " + Timestamp.ToString("h:mm tt");
                return Timestamp.ToString("MMM d, h:mm tt");
            }
        }

        public int CharCount => Text?.Length ?? 0;
        public int WordCount => string.IsNullOrWhiteSpace(Text) ? 0 : 
            Text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        public int LineCount => string.IsNullOrWhiteSpace(Text) ? 0 : Text.Split('\n').Length;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
