using System.ServiceProcess;
using ServiceTrayMonitor.Models;
using ServiceTrayMonitor.Services;

namespace ServiceTrayMonitor.Forms
{
    /// <summary>
    /// Main visible window: a grid of monitored services with color-coded status
    /// and buttons to start/stop/pause/resume the selected one.
    /// </summary>
    public class ManageServicesForm : Form
    {
        private readonly ServiceMonitorEngine _engine;
        private readonly DataGridView _grid;
        private readonly Button _startBtn, _stopBtn, _pauseBtn, _resumeBtn, _refreshBtn;
        private readonly Label _statusLabel;

        /// <summary>Raised when the user clicks the link to manage which services are monitored.</summary>
        public event Action? ManageMonitoredServicesRequested;

        public ManageServicesForm(ServiceMonitorEngine engine)
        {
            _engine = engine;
            _engine.StatusesUpdated += statuses =>
            {
                if (IsHandleCreated)
                    Invoke(() => RefreshGrid(statuses));
            };

            Text = "Service Tray Monitor";
            Width = 720;
            Height = 460;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(560, 360);
            Icon = IconFactory.GetTrayIcon(Color.FromArgb(46, 204, 113));

            // Hide instead of close, so the tray keeps running.
            FormClosing += (_, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    Hide();
                }
            };

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
            };
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "DisplayName", HeaderText = "Service", Width = 260 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ServiceName", HeaderText = "Internal Name", Width = 160 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", Width = 130 });
            _grid.CellFormatting += Grid_CellFormatting;
            _grid.SelectionChanged += (_, _) => UpdateButtonStates();

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                Padding = new Padding(8),
                FlowDirection = FlowDirection.LeftToRight
            };

            _startBtn = new Button { Text = "Start", Width = 90, Height = 34 };
            _stopBtn = new Button { Text = "Stop", Width = 90, Height = 34 };
            _pauseBtn = new Button { Text = "Pause", Width = 90, Height = 34 };
            _resumeBtn = new Button { Text = "Resume", Width = 90, Height = 34 };
            _refreshBtn = new Button { Text = "Refresh", Width = 90, Height = 34 };

            _startBtn.Click += (_, _) => Execute(_engine.StartService);
            _stopBtn.Click += (_, _) => Execute(_engine.StopService);
            _pauseBtn.Click += (_, _) => Execute(_engine.PauseService);
            _resumeBtn.Click += (_, _) => Execute(_engine.ResumeService);
            _refreshBtn.Click += (_, _) => _engine.RefreshNow();

            buttonPanel.Controls.AddRange(new Control[] { _startBtn, _stopBtn, _pauseBtn, _resumeBtn, _refreshBtn });

            _statusLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                ForeColor = Color.DimGray
            };

            var manageServicesLink = new LinkLabel
            {
                Text = "Manage which services are monitored →",
                Dock = DockStyle.Top,
                Height = 28,
                Padding = new Padding(8, 6, 0, 0)
            };
            manageServicesLink.LinkClicked += (_, _) => ManageMonitoredServicesRequested?.Invoke();

            Controls.Add(_grid);
            Controls.Add(_statusLabel);
            Controls.Add(buttonPanel);
            Controls.Add(manageServicesLink);

            RefreshGrid(_engine.CurrentStatuses);
        }

        public void RefreshGrid(List<MonitoredService> statuses)
        {
            var selectedName = _grid.CurrentRow?.Cells["ServiceName"].Value as string;

            _grid.Rows.Clear();
            foreach (var svc in statuses)
            {
                int rowIndex = _grid.Rows.Add(svc.DisplayName, svc.ServiceName, svc.StatusText);
                _grid.Rows[rowIndex].Tag = svc;

                if (svc.ServiceName == selectedName)
                    _grid.Rows[rowIndex].Selected = true;
            }

            _statusLabel.Text = $"Last updated: {DateTime.Now:T}  •  {statuses.Count} service(s) monitored";
            UpdateButtonStates();
        }

        private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex != 2 || e.RowIndex < 0) return; // Status column
            if (_grid.Rows[e.RowIndex].Tag is not MonitoredService svc) return;

            e.CellStyle!.ForeColor = Color.White;
            e.CellStyle.BackColor = svc.StatusColor;
            e.CellStyle.SelectionBackColor = ControlPaint.Dark(svc.StatusColor, 0.1f);
        }

        private MonitoredService? SelectedService =>
            _grid.CurrentRow?.Tag as MonitoredService;

        private void UpdateButtonStates()
        {
            var svc = SelectedService;
            if (svc == null || !svc.Exists)
            {
                _startBtn.Enabled = _stopBtn.Enabled = _pauseBtn.Enabled = _resumeBtn.Enabled = false;
                return;
            }

            _startBtn.Enabled = svc.Status != ServiceControllerStatus.Running;
            _stopBtn.Enabled = svc.Status != ServiceControllerStatus.Stopped;
            _pauseBtn.Enabled = svc.CanPauseAndContinue && svc.Status == ServiceControllerStatus.Running;
            _resumeBtn.Enabled = svc.Status == ServiceControllerStatus.Paused;
        }

        private void Execute(Func<string, (bool success, string message)> action)
        {
            var svc = SelectedService;
            if (svc == null) return;

            Cursor = Cursors.WaitCursor;
            var (success, message) = action(svc.ServiceName);
            Cursor = Cursors.Default;

            _statusLabel.Text = $"{svc.DisplayName}: {message}";
            _statusLabel.ForeColor = success ? Color.FromArgb(39, 174, 96) : Color.FromArgb(192, 57, 43);

            if (!success)
                MessageBox.Show(this, message, "Action failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            _engine.RefreshNow();
        }
    }
}
