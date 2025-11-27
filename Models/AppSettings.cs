using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ClipMaster.Models
{
    public class AppSettings : INotifyPropertyChanged
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClipMaster", "settings.json");

        private bool _soundEnabled = true;
        private bool _alwaysOnTop = false;
        private bool _startWithWindows = false;
        private bool _startMinimized = false;
        private int _maxHistoryItems = 500;
        private double _windowOpacity = 1.0;
        private string _soundFile = "clip.wav";

        public bool SoundEnabled
        {
            get => _soundEnabled;
            set { _soundEnabled = value; OnPropertyChanged(); Save(); }
        }

        public bool AlwaysOnTop
        {
            get => _alwaysOnTop;
            set { _alwaysOnTop = value; OnPropertyChanged(); Save(); }
        }

        public bool StartWithWindows
        {
            get => _startWithWindows;
            set { _startWithWindows = value; OnPropertyChanged(); Save(); }
        }

        public bool StartMinimized
        {
            get => _startMinimized;
            set { _startMinimized = value; OnPropertyChanged(); Save(); }
        }

        public int MaxHistoryItems
        {
            get => _maxHistoryItems;
            set { _maxHistoryItems = value; OnPropertyChanged(); Save(); }
        }

        public double WindowOpacity
        {
            get => _windowOpacity;
            set { _windowOpacity = value; OnPropertyChanged(); Save(); }
        }

        public string SoundFile
        {
            get => _soundFile;
            set { _soundFile = value; OnPropertyChanged(); Save(); }
        }

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
            }
            return new AppSettings();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
