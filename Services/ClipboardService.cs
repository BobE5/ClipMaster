using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ClipMaster.Services
{
    public class ClipboardService : IDisposable
    {
        private HwndSource? _hwndSource;
        private IntPtr _hwnd;
        private IntPtr _nextClipboardViewer;
        private bool _isMonitoring;
        private string _lastClipboardText = string.Empty;

        public event EventHandler<ClipboardChangedEventArgs>? ClipboardChanged;

        #region Win32 API

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("psapi.dll")]
        private static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, 
            [Out] char[] lpBaseName, [In] [MarshalAs(UnmanagedType.U4)] int nSize);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        private const int WM_DRAWCLIPBOARD = 0x0308;
        private const int WM_CHANGECBCHAIN = 0x030D;
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint PROCESS_VM_READ = 0x0010;

        #endregion

        public void StartMonitoring(Window window)
        {
            if (_isMonitoring) return;

            var helper = new WindowInteropHelper(window);
            _hwnd = helper.Handle;

            if (_hwnd == IntPtr.Zero)
            {
                helper.EnsureHandle();
                _hwnd = helper.Handle;
            }

            _hwndSource = HwndSource.FromHwnd(_hwnd);
            _hwndSource?.AddHook(WndProc);

            _nextClipboardViewer = SetClipboardViewer(_hwnd);
            _isMonitoring = true;
        }

        public void StopMonitoring()
        {
            if (!_isMonitoring) return;

            ChangeClipboardChain(_hwnd, _nextClipboardViewer);
            _hwndSource?.RemoveHook(WndProc);
            _isMonitoring = false;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_DRAWCLIPBOARD:
                    OnClipboardChanged();
                    SendMessage(_nextClipboardViewer, msg, wParam, lParam);
                    handled = true;
                    break;

                case WM_CHANGECBCHAIN:
                    if (wParam == _nextClipboardViewer)
                    {
                        _nextClipboardViewer = lParam;
                    }
                    else if (_nextClipboardViewer != IntPtr.Zero)
                    {
                        SendMessage(_nextClipboardViewer, msg, wParam, lParam);
                    }
                    handled = true;
                    break;
            }

            return IntPtr.Zero;
        }

        private void OnClipboardChanged()
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    var text = Clipboard.GetText();
                    
                    // Avoid duplicates
                    if (text != _lastClipboardText && !string.IsNullOrWhiteSpace(text))
                    {
                        _lastClipboardText = text;
                        var sourceApp = GetForegroundWindowProcessName();
                        
                        ClipboardChanged?.Invoke(this, new ClipboardChangedEventArgs
                        {
                            Text = text,
                            SourceApplication = sourceApp,
                            Timestamp = DateTime.Now
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Clipboard error: {ex.Message}");
            }
        }

        private string GetForegroundWindowProcessName()
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                GetWindowThreadProcessId(hwnd, out uint processId);
                
                IntPtr processHandle = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, processId);
                if (processHandle != IntPtr.Zero)
                {
                    try
                    {
                        char[] buffer = new char[1024];
                        uint length = GetModuleFileNameEx(processHandle, IntPtr.Zero, buffer, buffer.Length);
                        if (length > 0)
                        {
                            var fullPath = new string(buffer, 0, (int)length);
                            return System.IO.Path.GetFileNameWithoutExtension(fullPath);
                        }
                    }
                    finally
                    {
                        CloseHandle(processHandle);
                    }
                }
            }
            catch
            {
                // Ignore errors getting process name
            }
            return "Unknown";
        }

        public void Dispose()
        {
            StopMonitoring();
        }
    }

    public class ClipboardChangedEventArgs : EventArgs
    {
        public string Text { get; set; } = string.Empty;
        public string SourceApplication { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
