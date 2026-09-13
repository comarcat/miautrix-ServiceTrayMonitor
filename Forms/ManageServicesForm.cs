using System.ComponentModel;
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
        private bool _actionInProgress;

        /// <summary>Raised when the user clicks the link to manage which services are monitored.</summary>
        public event Action? ManageMonitoredServicesRequested;

        public ManageServicesForm(ServiceMonitorEngine engine)
        {
            _engine = engine;
            _engine.StatusesUpdated += OnEngineStatusesUpdated;

            Text = "Service Tray Monitor";
            Width = 720;
            Height = 460;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(560, 360);
            Icon = IconFactory.GetTrayIcon(StatusPalette.Running);

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

            _startBtn.Click += (_, _) => Execute("Starting", _engine.StartService);
            _stopBtn.Click += (_, _) => Execute("Stopping", _engine.StopService);
            _pauseBtn.Click += (_, _) => Execute("Pausing", _engine.PauseService);
            _resumeBtn.Click += (_, _) => Execute("Resuming", _engine.ResumeService);
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

        private void OnEngineStatusesUpdated(List<MonitoredService> statuses)
        {
            // Raised on a thread-pool thread. BeginInvoke doesn't hold up polling; the checks cover
            // a window whose handle isn't created yet or is being torn down at exit.
            if (!IsHandleCreated || IsDisposed)
                return;

            try
            {
                BeginInvoke(new Action(() => RefreshGrid(statuses)));
            }
            catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException)
            {
                // Handle destroyed between the check and the call — nothing to update.
            }
        }

        public void RefreshGrid(List<MonitoredService> statuses)
        {
            var rowsByName = new Dictionary<string, DataGridViewRow>(StringComparer.OrdinalIgnoreCase);
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.Tag is MonitoredService existing)
                    rowsByName.TryAdd(existing.ServiceName, row);
            }

            bool sameServices = rowsByName.Count == _grid.Rows.Count
                && rowsByName.Count == statuses.Count
                && statuses.All(s => rowsByName.ContainsKey(s.ServiceName));

            if (sameServices)
            {
                // Same services as before: update in place, which keeps the selection, sort order,
                // and scroll position the user has.
                foreach (var svc in statuses)
                {
                    var row = rowsByName[svc.ServiceName];
                    row.Tag = svc;
                    row.Cells["DisplayName"].Value = svc.DisplayName;
                    row.Cells["Status"].Value = svc.StatusText;
                }
                ReapplySort(_grid.SortedColumn, _grid.SortOrder);
                _grid.Invalidate();
            }
            else
            {
                RebuildRows(statuses);
            }

            _statusLabel.Text = $"Last updated: {DateTime.Now:T}  •  {statuses.Count} service(s) monitored";
            UpdateButtonStates();
        }

        private void RebuildRows(List<MonitoredService> statuses)
        {
            string? selectedName = SelectedService?.ServiceName;
            int firstDisplayedRow = _grid.FirstDisplayedScrollingRowIndex;
            var sortedColumn = _grid.SortedColumn;
            var sortOrder = _grid.SortOrder;

            _grid.Rows.Clear();
            foreach (var svc in statuses)
            {
                int rowIndex = _grid.Rows.Add(svc.DisplayName, svc.ServiceName, svc.StatusText);
                _grid.Rows[rowIndex].Tag = svc;
            }

            ReapplySort(sortedColumn, sortOrder);

            // SelectedService reads CurrentRow, so restore the current cell — setting only
            // Row.Selected would highlight one row while the buttons act on another.
            var rowToSelect = _grid.Rows.Cast<DataGridViewRow>().FirstOrDefault(r =>
                string.Equals((r.Tag as MonitoredService)?.ServiceName, selectedName, StringComparison.OrdinalIgnoreCase));
            try
            {
                if (rowToSelect != null)
                    _grid.CurrentCell = rowToSelect.Cells[0];
                if (firstDisplayedRow >= 0 && _grid.Rows.Count > 0)
                    _grid.FirstDisplayedScrollingRowIndex = Math.Min(firstDisplayedRow, _grid.Rows.Count - 1);
            }
            catch (InvalidOperationException)
            {
                // The grid can refuse these while it isn't displayed; the next refresh restores them.
            }
        }

        private void ReapplySort(DataGridViewColumn? column, SortOrder order)
        {
            if (column == null || order == SortOrder.None)
                return;

            _grid.Sort(column, order == SortOrder.Descending ? ListSortDirection.Descending : ListSortDirection.Ascending);
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
            if (svc == null || _actionInProgress)
            {
                _startBtn.Enabled = _stopBtn.Enabled = _pauseBtn.Enabled = _resumeBtn.Enabled = false;
                return;
            }

            _startBtn.Enabled = ServiceActionRules.CanStart(svc);
            _stopBtn.Enabled = ServiceActionRules.CanStop(svc);
            _pauseBtn.Enabled = ServiceActionRules.CanPause(svc);
            _resumeBtn.Enabled = ServiceActionRules.CanResume(svc);
        }

        private async void Execute(string progressVerb, Func<string, (bool success, string message)> action)
        {
            var svc = SelectedService;
            if (svc == null || _actionInProgress) return;

            _actionInProgress = true;
            UpdateButtonStates();
            _statusLabel.ForeColor = Color.DimGray;
            _statusLabel.Text = $"{svc.DisplayName}: {progressVerb}...";
            UseWaitCursor = true;

            (bool success, string message) result;
            try
            {
                // Actions wait for the service to reach its target state — keep the window responsive.
                result = await Task.Run(() => action(svc.ServiceName));
            }
            catch (Exception ex)
            {
                result = (false, ex.Message);
            }
            finally
            {
                _actionInProgress = false;
                if (!IsDisposed) UseWaitCursor = false;
            }

            if (IsDisposed) return;

            _statusLabel.Text = $"{svc.DisplayName}: {result.message}";
            _statusLabel.ForeColor = result.success ? Color.FromArgb(39, 174, 96) : Color.FromArgb(192, 57, 43);
            UpdateButtonStates();

            if (!result.success)
                MessageBox.Show(this, result.message, "Action failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            _engine.RefreshNow();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _engine.StatusesUpdated -= OnEngineStatusesUpdated;
            base.Dispose(disposing);
        }
    }
}
