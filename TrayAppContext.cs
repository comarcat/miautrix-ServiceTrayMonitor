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
        private readonly ServiceMonitorEngine _engine;
        private AppSettings _settings;

        private ManageServicesForm? _manageForm;
        private SettingsForm? _settingsForm;
        private CredentialsForm? _credentialsForm;
        private readonly SynchronizationContext _uiContext;

        public TrayAppContext()
        {
            // Captured here on the UI thread; used later to safely marshal
            // NotifyIcon updates that arrive from the polling timer's background thread.
            _uiContext = SynchronizationContext.Current ?? new SynchronizationContext();

            _settings = ConfigManager.Load();
            _engine = new ServiceMonitorEngine(_settings.PollIntervalSeconds);
            _engine.UpdateMonitoredList(_settings.MonitoredServiceNames);
            _engine.StatusesUpdated += OnStatusesUpdated;

            _menu = new ContextMenuStrip();
            _trayIcon = new NotifyIcon
            {
                Icon = IconFactory.GetTrayIcon(Color.Gray),
                Visible = true,
                Text = "Service Tray Monitor",
                ContextMenuStrip = _menu
            };

            _trayIcon.DoubleClick += (_, _) => OpenManageServices();
            _menu.Opening += (_, _) => BuildMenu();

            BuildMenu();
            _engine.Start();
        }

        /// <summary>
        /// Rebuilds the right-click menu contents each time it's about to open,
        /// so statuses and the service list are always current.
        /// </summary>
        private void BuildMenu()
        {
            _menu.Items.Clear();

            _menu.Items.Add(new ToolStripMenuItem("Open Service Monitor", null, (_, _) => OpenManageServices()) { Font = new Font(_menu.Font, FontStyle.Bold) });
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

                    var startItem = new ToolStripMenuItem("Start", null, (_, _) => RunAction(svc.ServiceName, _engine.StartService));
                    var stopItem = new ToolStripMenuItem("Stop", null, (_, _) => RunAction(svc.ServiceName, _engine.StopService));
                    var pauseItem = new ToolStripMenuItem("Pause", null, (_, _) => RunAction(svc.ServiceName, _engine.PauseService));
                    var resumeItem = new ToolStripMenuItem("Resume", null, (_, _) => RunAction(svc.ServiceName, _engine.ResumeService));

                    startItem.Enabled = svc.Exists && svc.Status != System.ServiceProcess.ServiceControllerStatus.Running;
                    stopItem.Enabled = svc.Exists && svc.Status != System.ServiceProcess.ServiceControllerStatus.Stopped;
                    pauseItem.Enabled = svc.Exists && svc.CanPauseAndContinue && svc.Status == System.ServiceProcess.ServiceControllerStatus.Running;
                    resumeItem.Enabled = svc.Exists && svc.Status == System.ServiceProcess.ServiceControllerStatus.Paused;

                    item.DropDownItems.Add(startItem);
                    item.DropDownItems.Add(stopItem);
                    item.DropDownItems.Add(pauseItem);
                    item.DropDownItems.Add(resumeItem);

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

        private void RunAction(string serviceName, Func<string, (bool success, string message)> action)
        {
            var (success, message) = action(serviceName);
            _trayIcon.BalloonTipTitle = success ? "Success" : "Action failed";
            _trayIcon.BalloonTipText = $"{serviceName}: {message}";
            _trayIcon.BalloonTipIcon = success ? ToolTipIcon.Info : ToolTipIcon.Error;
            _trayIcon.ShowBalloonTip(3000);
            _engine.RefreshNow();
        }

        private void OnStatusesUpdated(List<MonitoredService> statuses)
        {
            // This fires from the polling timer's background thread — marshal
            // onto the UI thread before touching the NotifyIcon.
            _uiContext.Post(_ => ApplyStatusesToTray(statuses), null);
        }

        private void ApplyStatusesToTray(List<MonitoredService> statuses)
        {
            var summary = IconFactory.SummaryColor(statuses);
            _trayIcon.Icon = IconFactory.GetTrayIcon(summary);

            var tooltipLines = statuses.Take(8).Select(s => $"{s.DisplayName}: {s.StatusText}");
            string tooltip = statuses.Count == 0
                ? "Service Tray Monitor — no services configured"
                : string.Join("\n", tooltipLines);

            // NotifyIcon.Text has a 127-char limit.
            _trayIcon.Text = tooltip.Length > 127 ? tooltip[..124] + "..." : tooltip;

            // Note: ManageServicesForm subscribes to _engine.StatusesUpdated itself and
            // marshals back to its UI thread via Invoke — we don't touch its grid here,
            // since this handler runs on the polling timer's background thread.
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
                _settingsForm = new SettingsForm(_settings);
                _settingsForm.SettingsSaved += OnSettingsSaved;
            }
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
            _settings = updated;
            ConfigManager.Save(_settings);
            _engine.SetPollInterval(_settings.PollIntervalSeconds);
            _engine.UpdateMonitoredList(_settings.MonitoredServiceNames);
        }

        private void ExitApp()
        {
            _trayIcon.Visible = false;
            _engine.Dispose();
            Application.Exit();
        }
    }
}
