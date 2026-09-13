using ServiceTrayMonitor.Models;
using ServiceTrayMonitor.Services;

namespace ServiceTrayMonitor.Forms
{
    /// <summary>
    /// Lets the user choose which installed Windows services to monitor,
    /// plus a couple of general options. Saves back to config.json on Save.
    /// </summary>
    public class SettingsForm : Form
    {
        private readonly CheckedListBox _list;
        private readonly TextBox _searchBox;
        private readonly NumericUpDown _intervalUpDown;
        private readonly CheckBox _startWithWindowsBox;
        private readonly AppSettings _settings;
        private List<System.ServiceProcess.ServiceController> _allServices = new();

        public event Action<AppSettings>? SettingsSaved;

        public SettingsForm(AppSettings currentSettings)
        {
            _settings = currentSettings;

            Text = "Manage Monitored Services";
            Width = 520;
            Height = 560;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(420, 420);
            Icon = IconFactory.GetTrayIcon(Color.FromArgb(52, 152, 219));

            var topLabel = new Label
            {
                Text = "Select the services you want to monitor and control from the tray:",
                Dock = DockStyle.Top,
                Height = 32,
                Padding = new Padding(8, 8, 8, 0)
            };

            _searchBox = new TextBox
            {
                Dock = DockStyle.Top,
                PlaceholderText = "Filter services...",
                Margin = new Padding(8)
            };
            var searchPanel = new Panel { Dock = DockStyle.Top, Height = 32, Padding = new Padding(8, 0, 8, 4) };
            searchPanel.Controls.Add(_searchBox);
            _searchBox.Dock = DockStyle.Fill;
            _searchBox.TextChanged += (_, _) => PopulateList(_searchBox.Text);

            _list = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                CheckOnClick = true,
                IntegralHeight = false
            };

            var optionsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 76,
                Padding = new Padding(8),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            var intervalPanel = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
            intervalPanel.Controls.Add(new Label { Text = "Poll interval (seconds):", AutoSize = true, Padding = new Padding(0, 6, 6, 0) });
            _intervalUpDown = new NumericUpDown { Minimum = 2, Maximum = 300, Value = Math.Max(2, currentSettings.PollIntervalSeconds) };
            intervalPanel.Controls.Add(_intervalUpDown);

            _startWithWindowsBox = new CheckBox
            {
                Text = "Start automatically when Windows starts",
                AutoSize = true,
                Checked = currentSettings.StartWithWindows
            };

            optionsPanel.Controls.Add(intervalPanel);
            optionsPanel.Controls.Add(_startWithWindowsBox);

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                Padding = new Padding(8),
                FlowDirection = FlowDirection.RightToLeft
            };
            var saveBtn = new Button { Text = "Save", Width = 100, Height = 32, DialogResult = DialogResult.OK };
            var cancelBtn = new Button { Text = "Cancel", Width = 100, Height = 32 };
            saveBtn.Click += (_, _) => SaveAndClose();
            cancelBtn.Click += (_, _) => Close();
            buttonPanel.Controls.Add(saveBtn);
            buttonPanel.Controls.Add(cancelBtn);

            Controls.Add(_list);
            Controls.Add(optionsPanel);
            Controls.Add(buttonPanel);
            Controls.Add(searchPanel);
            Controls.Add(topLabel);

            FormClosing += (_, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    Hide();
                }
            };

            Load += (_, _) => LoadServices();
        }

        private void LoadServices()
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                _allServices = ServiceMonitorEngine.GetAllServices();
                PopulateList(string.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Couldn't load the list of Windows services:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void PopulateList(string filter)
        {
            _list.Items.Clear();
            var monitored = new HashSet<string>(_settings.MonitoredServiceNames, StringComparer.OrdinalIgnoreCase);

            foreach (var svc in _allServices)
            {
                string label = $"{svc.DisplayName}  ({svc.ServiceName})";
                if (!string.IsNullOrWhiteSpace(filter) &&
                    label.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                int idx = _list.Items.Add(svc);
                if (monitored.Contains(svc.ServiceName))
                    _list.SetItemChecked(idx, true);
            }

            _list.DisplayMember = "DisplayName";
            _list.Format += (_, e) =>
            {
                if (e.ListItem is System.ServiceProcess.ServiceController sc)
                    e.Value = $"{sc.DisplayName}  ({sc.ServiceName})";
            };
        }

        private void SaveAndClose()
        {
            var chosen = new List<string>();
            foreach (var item in _list.CheckedItems)
            {
                if (item is System.ServiceProcess.ServiceController sc)
                    chosen.Add(sc.ServiceName);
            }

            _settings.MonitoredServiceNames = chosen;
            _settings.PollIntervalSeconds = (int)_intervalUpDown.Value;
            _settings.StartWithWindows = _startWithWindowsBox.Checked;

            SettingsSaved?.Invoke(_settings);
            Hide();
        }
    }
}
