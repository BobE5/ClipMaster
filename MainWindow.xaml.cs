using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ClipMaster.Helpers;
using ClipMaster.Models;
using ClipMaster.Services;
using Microsoft.Win32;

namespace ClipMaster
{
    public partial class MainWindow : Window
    {
        private readonly ClipboardService _clipboardService;
        private readonly DatabaseService _databaseService;
        private readonly SoundService _soundService;
        private readonly HotkeyService _hotkeyService;
        private readonly TrayIconService _trayIconService;
        private readonly AppSettings _settings;

        private ObservableCollection<ClipItem> _clips;
        private string _previousText = string.Empty;
        private readonly DispatcherTimer _memoryTimer;
        private bool _isInternalClipboardChange;
        private bool _isReallyClosing;

        #region Win32 API for No-Focus Activation
        
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, 
            int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            _settings = AppSettings.Load();
            _clipboardService = new ClipboardService();
            _databaseService = new DatabaseService();
            _soundService = new SoundService { IsEnabled = _settings.SoundEnabled };
            _hotkeyService = new HotkeyService();
            _trayIconService = new TrayIconService();
            _clips = new ObservableCollection<ClipItem>();

            // Load custom sound if saved
            if (!string.IsNullOrEmpty(_settings.SoundFile) && File.Exists(_settings.SoundFile))
            {
                _soundService.SetClipSound(_settings.SoundFile);
            }

            // Initialize system tray icon
            _trayIconService.Initialize();
            _trayIconService.ShowWindowRequested += (s, e) => Dispatcher.Invoke(() =>
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
            });
            _trayIconService.ExitRequested += (s, e) => Dispatcher.Invoke(() =>
            {
                _isReallyClosing = true;
                Application.Current.Shutdown();
            });
            _trayIconService.ExportRequested += (s, e) => Dispatcher.Invoke(() =>
            {
                Show();
                Activate();
                ExportToFolder();
            });

            ClipListBox.ItemsSource = _clips;

            // Setup memory usage timer
            _memoryTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _memoryTimer.Tick += (s, e) => UpdateMemoryUsage();
            _memoryTimer.Start();

            // Apply settings
            SoundCheckBox.IsChecked = _settings.SoundEnabled;
            AlwaysOnTopCheckBox.IsChecked = _settings.AlwaysOnTop;
            Topmost = _settings.AlwaysOnTop;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Ensure window is visible and activated on startup
            Show();
            Activate();

            // Start clipboard monitoring
            _clipboardService.ClipboardChanged += OnClipboardChanged;
            _clipboardService.StartMonitoring(this);

            // Register global hotkeys
            _hotkeyService.ShowHotkeyPressed += (s, args) => ToggleWindowVisibility();
            _hotkeyService.RegisterHotkeys(this);

            // Load existing clips from database
            LoadClipsFromDatabase();

            UpdateClipCount();
            UpdateMemoryUsage();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isReallyClosing)
            {
                // Actually close the application
                return;
            }

            // Minimize to tray instead of closing
            e.Cancel = true;
            Hide();
            _trayIconService.ShowBalloonTip("ClipMaster", "Minimized to tray. Use Ctrl+Shift+V or click tray icon to show.");
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            // Hide window when it loses focus (user clicks elsewhere)
            if (!_settings.AlwaysOnTop)
            {
                Hide();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _clipboardService.Dispose();
            _databaseService.Dispose();
            _soundService.Dispose();
            _hotkeyService.Dispose();
            _trayIconService.Dispose();
            _memoryTimer.Stop();
            base.OnClosed(e);
        }

        #region Clipboard Handling

        private void OnClipboardChanged(object? sender, ClipboardChangedEventArgs e)
        {
            if (_isInternalClipboardChange) return;

            Dispatcher.Invoke(() =>
            {
                var clip = new ClipItem
                {
                    Text = e.Text,
                    Timestamp = e.Timestamp,
                    SourceApp = e.SourceApplication
                };

                // Add to database
                var id = _databaseService.AddClip(clip);
                if (id > 0)
                {
                    clip.Id = id;
                    _clips.Insert(0, clip);

                    // Trim history if needed
                    while (_clips.Count > _settings.MaxHistoryItems)
                    {
                        _clips.RemoveAt(_clips.Count - 1);
                    }

                    // Play sound notification
                    _soundService.PlayClipSound();

                    // Update UI
                    LastClipText.Text = $"Last clip: {e.Timestamp:h:mm tt} from {e.SourceApplication}";
                    UpdateClipCount();

                    // Show window without stealing focus
                    ShowWithoutActivation();
                }
            });
        }

        private void ShowWithoutActivation()
        {
            if (!IsVisible)
            {
                Show();
            }

            if (_settings.AlwaysOnTop)
            {
                var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, 
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }
        }

        #endregion

        #region UI Event Handlers

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized 
                    ? WindowState.Normal 
                    : WindowState.Maximized;
            }
            else
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Open settings window
            MessageBox.Show("Settings dialog coming soon!", "Settings", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SoundCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_soundService == null || _settings == null) return;
            _settings.SoundEnabled = SoundCheckBox.IsChecked ?? true;
            _soundService.IsEnabled = _settings.SoundEnabled;
        }

        private void ChangeSoundButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Notification Sound",
                Filter = "Audio Files (*.mp3;*.wav)|*.mp3;*.wav|MP3 Files (*.mp3)|*.mp3|WAV Files (*.wav)|*.wav|All Files (*.*)|*.*",
                DefaultExt = ".mp3"
            };

            if (dialog.ShowDialog() == true)
            {
                if (_soundService.SetClipSound(dialog.FileName))
                {
                    _settings.SoundFile = dialog.FileName;
                    _soundService.TestClipSound();
                    MessageBox.Show("Sound changed successfully!", "Sound Changed",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Failed to load the selected sound file.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AlwaysOnTopCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_settings == null) return;
            _settings.AlwaysOnTop = AlwaysOnTopCheckBox.IsChecked ?? true;
            Topmost = _settings.AlwaysOnTop;
        }

        private void SearchTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var searchTerm = SearchTextBox.Text;

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                LoadClipsFromDatabase();
            }
            else
            {
                var results = _databaseService.SearchClips(searchTerm);
                _clips.Clear();
                foreach (var clip in results)
                {
                    _clips.Add(clip);
                }
            }
        }

        private void ClipListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ClipListBox.SelectedItem is ClipItem selectedClip)
            {
                _previousText = CurrentTextBox.Text;
                CurrentTextBox.Text = selectedClip.Text;
                UpdateStats();
            }
        }

        private void CurrentTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateStats();
        }

        private void UpdateStats()
        {
            var stats = TextTransformHelper.GetStatistics(CurrentTextBox.Text);
            StatsText.Text = $"{stats.chars} chars | {stats.words} words | {stats.lines} lines";
        }

        #endregion

        #region Transform Operations

        private void Transform_Uppercase(object sender, RoutedEventArgs e)
        {
            SaveUndoState();
            CurrentTextBox.Text = TextTransformHelper.ToUpperCase(CurrentTextBox.Text);
        }

        private void Transform_Lowercase(object sender, RoutedEventArgs e)
        {
            SaveUndoState();
            CurrentTextBox.Text = TextTransformHelper.ToLowerCase(CurrentTextBox.Text);
        }

        private void Transform_TitleCase(object sender, RoutedEventArgs e)
        {
            SaveUndoState();
            CurrentTextBox.Text = TextTransformHelper.ToTitleCase(CurrentTextBox.Text);
        }

        private void Transform_SentenceCase(object sender, RoutedEventArgs e)
        {
            SaveUndoState();
            CurrentTextBox.Text = TextTransformHelper.ToSentenceCase(CurrentTextBox.Text);
        }

        private void Transform_RemoveEOL(object sender, RoutedEventArgs e)
        {
            SaveUndoState();
            CurrentTextBox.Text = TextTransformHelper.RemoveLineBreaks(CurrentTextBox.Text);
        }

        private void Transform_TrimSpaces(object sender, RoutedEventArgs e)
        {
            SaveUndoState();
            CurrentTextBox.Text = TextTransformHelper.TrimSpaces(CurrentTextBox.Text);
        }

        private void Transform_SortLines(object sender, RoutedEventArgs e)
        {
            SaveUndoState();
            CurrentTextBox.Text = TextTransformHelper.SortLines(CurrentTextBox.Text);
        }

        private void Transform_Reverse(object sender, RoutedEventArgs e)
        {
            SaveUndoState();
            CurrentTextBox.Text = TextTransformHelper.ReverseText(CurrentTextBox.Text);
        }

        private void Transform_RemoveFormatting(object sender, RoutedEventArgs e)
        {
            SaveUndoState();
            CurrentTextBox.Text = TextTransformHelper.RemoveHtmlTags(CurrentTextBox.Text);
        }

        private void Extract_URLs(object sender, RoutedEventArgs e)
        {
            SaveUndoState();
            CurrentTextBox.Text = TextTransformHelper.ExtractUrls(CurrentTextBox.Text);
        }

        private void Extract_Emails(object sender, RoutedEventArgs e)
        {
            SaveUndoState();
            CurrentTextBox.Text = TextTransformHelper.ExtractEmails(CurrentTextBox.Text);
        }

        private void Extract_Numbers(object sender, RoutedEventArgs e)
        {
            SaveUndoState();
            CurrentTextBox.Text = TextTransformHelper.ExtractNumbers(CurrentTextBox.Text);
        }

        private void Transform_LineNumbers(object sender, RoutedEventArgs e)
        {
            SaveUndoState();
            CurrentTextBox.Text = TextTransformHelper.AddLineNumbers(CurrentTextBox.Text);
        }

        private void Transform_RemoveDuplicates(object sender, RoutedEventArgs e)
        {
            SaveUndoState();
            CurrentTextBox.Text = TextTransformHelper.RemoveDuplicateLines(CurrentTextBox.Text);
        }

        private void Transform_JsonFormat(object sender, RoutedEventArgs e)
        {
            SaveUndoState();
            CurrentTextBox.Text = TextTransformHelper.JsonPrettify(CurrentTextBox.Text);
        }

        private void OpenRegexWindow_Click(object sender, RoutedEventArgs e)
        {
            var regexWindow = new RegexTransformWindow(CurrentTextBox.Text)
            {
                Owner = this
            };

            if (regexWindow.ShowDialog() == true && regexWindow.Applied)
            {
                SaveUndoState();
                CurrentTextBox.Text = regexWindow.TransformedText;
            }
        }

        #endregion

        #region Action Buttons

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_previousText))
            {
                var current = CurrentTextBox.Text;
                CurrentTextBox.Text = _previousText;
                _previousText = current;
            }
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(CurrentTextBox.Text))
            {
                _isInternalClipboardChange = true;
                try
                {
                    Clipboard.SetText(CurrentTextBox.Text);
                    _soundService.PlaySuccessSound();
                }
                finally
                {
                    // Reset after a short delay
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
                    timer.Tick += (s, args) =>
                    {
                        _isInternalClipboardChange = false;
                        timer.Stop();
                    };
                    timer.Start();
                }
            }
        }

        private void PasteAndSendButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(CurrentTextBox.Text))
            {
                _isInternalClipboardChange = true;
                try
                {
                    Clipboard.SetText(CurrentTextBox.Text);
                    
                    // Hide window
                    Hide();
                    
                    // Simulate Ctrl+V after a short delay
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
                    timer.Tick += (s, args) =>
                    {
                        timer.Stop();
                        SendKeys.SendCtrlV();
                        _isInternalClipboardChange = false;
                    };
                    timer.Start();
                }
                catch
                {
                    _isInternalClipboardChange = false;
                }
            }
        }

        private void ClearAllButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to clear all clipboard history?\n\nFavorited items will be preserved.",
                "Clear History",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _databaseService.ClearHistory(keepFavorites: true);
                LoadClipsFromDatabase();
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = "txt",
                FileName = $"ClipMaster_Export_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (dialog.ShowDialog() == true)
            {
                _databaseService.ExportToFile(dialog.FileName);
                MessageBox.Show($"Exported {_clips.Count} clips to:\n{dialog.FileName}",
                    "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExportToFolder()
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select folder to export clips (e.g., Google Drive folder)",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var folderPath = dialog.SelectedPath;
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
                var fileName = Path.Combine(folderPath, $"ClipMaster_Export_{timestamp}.txt");

                _databaseService.ExportToFile(fileName);

                MessageBox.Show($"Exported {_clips.Count} clips to:\n{fileName}\n\nThis folder will sync to cloud if it's a cloud drive folder.",
                    "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion

        #region Helper Methods

        private void LoadClipsFromDatabase()
        {
            var clips = _databaseService.GetAllClips(_settings.MaxHistoryItems);
            _clips.Clear();
            foreach (var clip in clips)
            {
                _clips.Add(clip);
            }
        }

        private void UpdateClipCount()
        {
            ClipCountText.Text = $"{_clips.Count} clips stored";
        }

        private void UpdateMemoryUsage()
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var memoryMB = process.WorkingSet64 / (1024.0 * 1024.0);
            MemoryText.Text = $"Memory: {memoryMB:F1} MB";
        }

        private void SaveUndoState()
        {
            _previousText = CurrentTextBox.Text;
        }

        private void ToggleWindowVisibility()
        {
            Dispatcher.Invoke(() =>
            {
                if (IsVisible)
                {
                    Hide();
                }
                else
                {
                    Show();
                    Activate();
                }
            });
        }

        #endregion
    }

    // Helper class for sending keystrokes
    public static class SendKeys
    {
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const byte VK_CONTROL = 0x11;
        private const byte VK_V = 0x56;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        public static void SendCtrlV()
        {
            keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
            keybd_event(VK_V, 0, 0, UIntPtr.Zero);
            keybd_event(VK_V, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
    }
}
