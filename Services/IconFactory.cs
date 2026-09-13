namespace ServiceTrayMonitor.Services
{
    /// <summary>
    /// Draws small colored-circle icons at runtime so the app doesn't need
    /// any external .ico assets. Used for both the tray icon and menu bitmaps.
    /// </summary>
    public static class IconFactory
    {
        private static readonly Dictionary<int, Icon> _iconCache = new();
        private static readonly Dictionary<int, Bitmap> _bitmapCache = new();

        public static Icon GetTrayIcon(Color color)
        {
            int key = color.ToArgb();
            if (_iconCache.TryGetValue(key, out var cached))
                return cached;

            using var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using var brush = new SolidBrush(color);
                using var pen = new Pen(Color.FromArgb(60, 0, 0, 0), 1.5f);
                g.FillEllipse(brush, 3, 3, 26, 26);
                g.DrawEllipse(pen, 3, 3, 26, 26);
            }

            IntPtr hIcon = bmp.GetHicon();
            var icon = (Icon)Icon.FromHandle(hIcon).Clone();
            _iconCache[key] = icon;
            return icon;
        }

        public static Bitmap GetMenuDot(Color color)
        {
            int key = color.ToArgb();
            if (_bitmapCache.TryGetValue(key, out var cached))
                return cached;

            var bmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using var brush = new SolidBrush(color);
                g.FillEllipse(brush, 2, 2, 12, 12);
            }
            _bitmapCache[key] = bmp;
            return bmp;
        }

        /// <summary>
        /// Combines all monitored statuses into one summary color for the tray icon:
        /// red if anything's stopped that shouldn't be, amber if pending, else green.
        /// </summary>
        public static Color SummaryColor(IEnumerable<Models.MonitoredService> services)
        {
            var list = services.ToList();
            if (list.Count == 0) return Color.Gray;

            if (list.Any(s => !s.Exists)) return Color.FromArgb(231, 76, 60);
            if (list.Any(s => s.Status == System.ServiceProcess.ServiceControllerStatus.Stopped))
                return Color.FromArgb(231, 76, 60);
            if (list.Any(s => s.StatusText.Contains("Pending")))
                return Color.FromArgb(241, 196, 15);
            if (list.Any(s => s.Status == System.ServiceProcess.ServiceControllerStatus.Paused))
                return Color.FromArgb(241, 196, 15);

            return Color.FromArgb(46, 204, 113);
        }
    }
}
