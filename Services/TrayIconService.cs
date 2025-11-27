using System;
using System.Drawing;
using System.Windows.Forms;

namespace ClipMaster.Services
{
    public class TrayIconService : IDisposable
    {
        private NotifyIcon? _notifyIcon;
        private ContextMenuStrip? _contextMenu;

        public event EventHandler? ShowWindowRequested;
        public event EventHandler? ExitRequested;
        public event EventHandler? ExportRequested;

        public void Initialize()
        {
            _contextMenu = new ContextMenuStrip();
            _contextMenu.Items.Add("Show ClipMaster", null, (s, e) => ShowWindowRequested?.Invoke(this, EventArgs.Empty));
            _contextMenu.Items.Add("-");
            _contextMenu.Items.Add("Export Clips...", null, (s, e) => ExportRequested?.Invoke(this, EventArgs.Empty));
            _contextMenu.Items.Add("-");
            _contextMenu.Items.Add("Exit", null, (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty));

            _notifyIcon = new NotifyIcon
            {
                Icon = CreateDefaultIcon(),
                Visible = true,
                Text = "ClipMaster - Ctrl+Shift+V to show",
                ContextMenuStrip = _contextMenu
            };

            _notifyIcon.DoubleClick += (s, e) => ShowWindowRequested?.Invoke(this, EventArgs.Empty);
        }

        private Icon CreateDefaultIcon()
        {
            // Create a simple clipboard-like icon programmatically
            using var bitmap = new Bitmap(16, 16);
            using var g = Graphics.FromImage(bitmap);

            // Background
            g.Clear(Color.FromArgb(59, 130, 246)); // Blue color

            // Simple clipboard shape
            using var pen = new Pen(Color.White, 1);
            g.DrawRectangle(pen, 2, 4, 11, 10);
            g.FillRectangle(Brushes.White, 5, 2, 6, 3);

            return Icon.FromHandle(bitmap.GetHicon());
        }

        public void ShowBalloonTip(string title, string text, ToolTipIcon icon = ToolTipIcon.Info)
        {
            _notifyIcon?.ShowBalloonTip(3000, title, text, icon);
        }

        public void UpdateTooltip(string text)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Text = text.Length > 63 ? text.Substring(0, 60) + "..." : text;
            }
        }

        public void Dispose()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
            _contextMenu?.Dispose();
        }
    }
}
