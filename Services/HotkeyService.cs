using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ClipMaster.Services
{
    public class HotkeyService : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID_SHOW = 9000;
        private const int HOTKEY_ID_PASTE = 9001;
        
        // Modifier keys
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;
        private const uint MOD_NOREPEAT = 0x4000;

        // Virtual key codes
        private const uint VK_V = 0x56;
        private const uint VK_C = 0x43;

        private IntPtr _hwnd;
        private HwndSource? _hwndSource;

        public event EventHandler? ShowHotkeyPressed;
        public event EventHandler? PasteHotkeyPressed;

        public bool RegisterHotkeys(Window window)
        {
            var helper = new WindowInteropHelper(window);
            _hwnd = helper.Handle;

            if (_hwnd == IntPtr.Zero)
            {
                helper.EnsureHandle();
                _hwnd = helper.Handle;
            }

            _hwndSource = HwndSource.FromHwnd(_hwnd);
            _hwndSource?.AddHook(WndProc);

            // Register Ctrl+Shift+V for show/hide
            bool success1 = RegisterHotKey(_hwnd, HOTKEY_ID_SHOW, 
                MOD_CONTROL | MOD_SHIFT | MOD_NOREPEAT, VK_V);

            // Register Ctrl+Shift+C for quick paste from history
            bool success2 = RegisterHotKey(_hwnd, HOTKEY_ID_PASTE, 
                MOD_CONTROL | MOD_SHIFT | MOD_NOREPEAT, VK_C);

            return success1 && success2;
        }

        public void UnregisterHotkeys()
        {
            if (_hwnd != IntPtr.Zero)
            {
                UnregisterHotKey(_hwnd, HOTKEY_ID_SHOW);
                UnregisterHotKey(_hwnd, HOTKEY_ID_PASTE);
            }
            _hwndSource?.RemoveHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;

            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                
                if (id == HOTKEY_ID_SHOW)
                {
                    ShowHotkeyPressed?.Invoke(this, EventArgs.Empty);
                    handled = true;
                }
                else if (id == HOTKEY_ID_PASTE)
                {
                    PasteHotkeyPressed?.Invoke(this, EventArgs.Empty);
                    handled = true;
                }
            }

            return IntPtr.Zero;
        }

        public void Dispose()
        {
            UnregisterHotkeys();
        }
    }
}
