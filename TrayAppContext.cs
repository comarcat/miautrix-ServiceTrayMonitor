using ServiceTrayMonitor.Forms;
using ServiceTrayMonitor.Models;
using ServiceTrayMonitor.Services;

namespace ServiceTrayMonitor
{
    /// <summary>
    /// The app has no main window on launch — it lives entirely in the system tray.
    /// This ApplicationContext owns the NotifyIcon and keeps the app alive as long
    /// as the icon exists.
    /// </summary>
    public class TrayAppContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly ContextMenuStrip _menu;
        private readonly Font _boldMenuFont;
        private readonly ServiceMonitorEngine _engine;
        private readonly SynchronizationContext _uiContext;
        private AppSettings _settings;
        private bool _exiting;

        private ManageServicesForm? _manageForm;
        private SettingsForm? _settingsForm;
        private CredentialsForm? _credentialsForm;

        public TrayAppContext()
        {
            // No control exists yet, so WinForms hasn't installed its synchronization context on
            // this thread (Application.Run only does that later). Install it now: status updates
            // from the polling thread are posted through it and must run on this UI thread.
            if (SynchronizationContext.Current is not WindowsFormsSynchronizationContext)
                SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
            _uiContext = SynchronizationContext.Current!;

            _settings = ConfigManager.Load();
            _engine = new ServiceMonitorEngine(_settings.PollIntervalSeconds);
            _engine.StatusesUpdated += OnStatusesUpdated;
            _engine.UpdateMonitoredList(_settings.MonitoredServiceNames);

            _menu = new ContextMenuStrip();
            _boldMenuFont = new Font(_menu.Font, FontStyle.Bold);
            _trayIcon = new NotifyIcon
            {
                Icon = IconFactory.GetTrayIcon(StatusPalette.Unknown),
                Visible = true,
                Text = "Service Tray Monitor",
                ContextMenuStrip = _menu
            };

            _trayIcon.DoubleClick += (_, _) => OpenManageServices();
            _menu.Opening += (_, _) => BuildMenu();

            BuildMenu();
            _engine.Start();

            if (ConfigManager.LastLoadWarning is { } warning)
                ShowBalloon("Settings reset", warning, ToolTipIcon.Warning);

            // Re-register the startup task on launch (e.g. after an upgrade moved the exe).
            if (_settings.StartWithWindows)
                ApplyStartupSetting(true);
            else
                StartupManager.RemoveLegacyRunEntry();
        }

        /// <summary>
        /// Rebuilds the right-click menu contents each time it's about to open,
        /// so statuses and the service list are always current.
        /// </summary>
        private void BuildMenu()
        {
            ClearMenu();

            _menu.Items.Add(new ToolStripMenuItem("Open Service Monitor", null, (_, _) => OpenManageServices()) { Font = _boldMenuFont });
            _menu.Items.Add(new ToolStripSeparator());

            if (_engine.CurrentStatuses.Count == 0)
            {
                _menu.Items.Add(new ToolStripMenuItem("No services configured yet...") { Enabled = false });
            }
            else
            {
                foreach (var svc in _engine.CurrentStatuses)
                {
                    var item = new ToolStripMenuItem($"{svc.DisplayName}  [{svc.StatusText}]")
                    {
                        Image = IconFactory.GetMenuDot(svc.StatusColor)
                    };

                    item.DropDownItems.Add(new ToolStripMenuItem("Start", null, (_, _) => RunAction(svc.ServiceName, _engine.StartService)) { Enabled = ServiceActionRules.CanStart(svc) });
                    item.DropDownItems.Add(new ToolStripMenuItem("Stop", null, (_, _) => RunAction(svc.ServiceName, _engine.StopService)) { Enabled = ServiceActionRules.CanStop(svc) });
                    item.DropDownItems.Add(new ToolStripMenuItem("Pause", null, (_, _) => RunAction(svc.ServiceName, _engine.PauseService)) { Enabled = ServiceActionRules.CanPause(svc) });
                    item.DropDownItems.Add(new ToolStripMenuItem("Resume", null, (_, _) => RunAction(svc.ServiceName, _engine.ResumeService)) { Enabled = ServiceActionRules.CanResume(svc) });

                    _menu.Items.Add(item);
                }
            }

            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(new ToolStripMenuItem("Manage Monitored Services...", null, (_, _) => OpenSettings()));
            _menu.Items.Add(new ToolStripMenuItem("Admin Credentials...", null, (_, _) => OpenCredentials()));
            _menu.Items.Add(new ToolStripMenuItem("Refresh Now", null, (_, _) => _engine.RefreshNow()));
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) => ExitApp()));
        }

        /// <summary>
        /// Items.Clear() doesn't dispose the removed items, so the menu leaked on every open.
        /// The status-dot images belong to IconFactory's cache — detach them before disposing.
        /// </summary>
        private void ClearMenu()
        {
            var items = _menu.Items.Cast<ToolStripItem>().ToList();
            _menu.Items.Clear();
            foreach (var item in items)
                DisposeMenuItem(item);
        }

        private static void DisposeMenuItem(ToolStripItem item)
        {
            if (item is ToolStripMenuItem menuItem)
            {
                foreach (var child in menuItem.DropDownItems.Cast<ToolStripItem>().ToList())
                    DisposeMenuItem(child);
            }

            item.Image = null;
            item.Dispose();
        }

        private async void RunAction(string serviceName, Func<string, (bool success, string message)> action)
        {
            (bool success, string message) result;
            try
            {
                // Actions wait for the service to reach its target state — keep that off the UI thread.
                result = await Task.Run(() => action(serviceName));
            }
            catch (Exception ex)
            {
                result = (false, ex.Message);
            }

            if (_exiting) return;

            ShowBalloon(result.success ? "Success" : "Action failed", $"{serviceName}: {result.message}",
                result.success ? ToolTipIcon.Info : ToolTipIcon.Error);
            _engine.RefreshNow();
        }

        private void ShowBalloon(string title, string text, ToolTipIcon icon)
        {
            _trayIcon.BalloonTipTitle = title;
            _trayIcon.BalloonTipText = text;
            _trayIcon.BalloonTipIcon = icon;
            _trayIcon.ShowBalloonTip(3000);
        }

        private void OnStatusesUpdated(List<MonitoredService> statuses)
        {
            // This fires from a thread-pool thread — marshal onto the UI thread before
            // touching the NotifyIcon.
            _uiContext.Post(_ => ApplyStatusesToTray(statuses), null);
        }

        private void ApplyStatusesToTray(List<MonitoredService> statuses)
        {
            if (_exiting) return;

            _trayIcon.Icon = IconFactory.GetTrayIcon(IconFactory.SummaryColor(statuses));

            var tooltipLines = statuses.Take(8).Select(s => $"{s.DisplayName}: {s.StatusText}");
            string tooltip = statuses.Count == 0
                ? "Service Tray Monitor — no services configured"
                : string.Join("\n", tooltipLines);

            // NotifyIcon.Text has a 127-char limit.
            _trayIcon.Text = tooltip.Length > 127 ? tooltip[..124] + "..." : tooltip;

            // ManageServicesForm subscribes to _engine.StatusesUpdated itself and marshals to its own UI thread.
        }

        private void OpenManageServices()
        {
            if (_manageForm == null || _manageForm.IsDisposed)
            {
                _manageForm = new ManageServicesForm(_engine);
                _manageForm.ManageMonitoredServicesRequested += OpenSettings;
            }
            _manageForm.RefreshGrid(_engine.CurrentStatuses);
            _manageForm.Show();
            _manageForm.WindowState = FormWindowState.Normal;
            _manageForm.Activate();
        }

        private void OpenSettings()
        {
            if (_settingsForm == null || _settingsForm.IsDisposed)
            {
                _settingsForm = new SettingsForm();
                _settingsForm.SettingsSaved += OnSettingsSaved;
            }

            // Reset to the saved settings each time the window opens, so edits from a cancelled
            // session don't linger — but don't wipe edits in a window that's already open.
            if (!_settingsForm.Visible)
                _settingsForm.LoadFrom(_settings);

            _settingsForm.Show();
            _settingsForm.Activate();
        }

        private void OpenCredentials()
        {
            if (_credentialsForm == null || _credentialsForm.IsDisposed)
            {
                _credentialsForm = new CredentialsForm();
            }
            _credentialsForm.Show();
            _credentialsForm.Activate();
        }

        private void OnSettingsSaved(AppSettings updated)
        {
            try
            {
                // Merges into the file on disk, so a credential saved since startup is kept.
                _settings = ConfigManager.SaveGeneralSettings(updated);
            }
            catch (Exception ex)
            {
                ShowBalloon("Settings not saved", ex.Message, ToolTipIcon.Error);
                return;
            }

            _engine.SetPollInterval(_settings.PollIntervalSeconds);
            _engine.UpdateMonitoredList(_settings.MonitoredServiceNames);
            ApplyStartupSetting(_settings.StartWithWindows);
        }

        private async void ApplyStartupSetting(bool enabled)
        {
            var (success, message) = await Task.Run(() => StartupManager.Apply(enabled));
            if (!success && !_exiting)
                ShowBalloon("Start with Windows", message, ToolTipIcon.Warning);
        }

        private void ExitApp()
        {
            _exiting = true;
            _engine.StatusesUpdated -= OnStatusesUpdated;
            _engine.Dispose();

            _trayIcon.Visible = false;
            _trayIcon.Dispose();

            _manageForm?.Dispose();
            _settingsForm?.Dispose();
            _credentialsForm?.Dispose();

            Application.Exit();
        }
    }
}
