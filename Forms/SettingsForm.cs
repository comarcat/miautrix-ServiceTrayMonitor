using ServiceTrayMonitor.Models;
using ServiceTrayMonitor.Services;

namespace ServiceTrayMonitor.Forms
{
    /// <summary>
    /// Lets the user choose which installed Windows services to monitor,
    /// plus a couple of general options. Raises SettingsSaved on Save; the tray
    /// context persists the result.
    /// </summary>
    public class SettingsForm : Form
    {
        private readonly CheckedListBox _list;
        private readonly TextBox _searchBox;
        private readonly NumericUpDown _intervalUpDown;
        private readonly CheckBox _startWithWindowsBox;
        private List<InstalledService> _allServices = new();
        private ServiceSelection _selection = new(Array.Empty<string>());
        private bool _populating;

        public event Action<AppSettings>? SettingsSaved;

        public SettingsForm()
        {
            Text = "Manage Monitored Services";
            Width = 520;
            Height = 560;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(420, 420);
            Icon = IconFactory.GetTrayIcon(StatusPalette.Pending);

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
            _list.Format += (_, e) =>
            {
                if (e.ListItem is InstalledService svc)
                    e.Value = svc.Label;
            };
            // Record every check/uncheck in the selection, which outlives the filtered list.
            _list.ItemCheck += (_, e) =>
            {
                if (!_populating && _list.Items[e.Index] is InstalledService svc)
                    _selection.Set(svc.ServiceName, e.NewValue == CheckState.Checked);
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
            _intervalUpDown = new NumericUpDown { Minimum = 2, Maximum = 300, Value = 5 };
            intervalPanel.Controls.Add(_intervalUpDown);

            _startWithWindowsBox = new CheckBox
            {
                Text = "Start automatically when Windows starts",
                AutoSize = true
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
            var saveBtn = new Button { Text = "Save", Width = 100, Height = 32 };
            var cancelBtn = new Button { Text = "Cancel", Width = 100, Height = 32 };
            saveBtn.Click += (_, _) => SaveAndClose();
            cancelBtn.Click += (_, _) => Hide();
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
        }

        /// <summary>
        /// Resets the window to the given saved settings and reloads the installed services.
        /// Called each time the window is opened.
        /// </summary>
        public void LoadFrom(AppSettings settings)
        {
            _selection = new ServiceSelection(settings.MonitoredServiceNames);
            _intervalUpDown.Value = Math.Clamp(settings.PollIntervalSeconds, (int)_intervalUpDown.Minimum, (int)_intervalUpDown.Maximum);
            _startWithWindowsBox.Checked = settings.StartWithWindows;
            LoadServices();

            _populating = true;
            _searchBox.Text = string.Empty; // TextChanged repopulates when the filter had text
            _populating = false;
            PopulateList(string.Empty);
        }

        private void LoadServices()
        {
            UseWaitCursor = true;
            try
            {
                _allServices = ServiceMonitorEngine.GetAllServices();
            }
            catch (Exception ex)
            {
                _allServices = new List<InstalledService>();
                MessageBox.Show(this, $"Couldn't load the list of Windows services:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        private void PopulateList(string filter)
        {
            _populating = true;
            _list.BeginUpdate();
            try
            {
                _list.Items.Clear();
                foreach (var svc in _allServices)
                {
                    if (!ServiceSelection.MatchesFilter(svc, filter))
                        continue;

                    int idx = _list.Items.Add(svc);
                    if (_selection.IsChecked(svc.ServiceName))
                        _list.SetItemChecked(idx, true);
                }
            }
            finally
            {
                _list.EndUpdate();
                _populating = false;
            }
        }

        private void SaveAndClose()
        {
            var updated = new AppSettings
            {
                // From the selection, not the visible list: services hidden by the filter stay monitored.
                MonitoredServiceNames = _selection.ToOrderedList(_allServices.Select(s => s.ServiceName)),
                PollIntervalSeconds = (int)_intervalUpDown.Value,
                StartWithWindows = _startWithWindowsBox.Checked
            };

            SettingsSaved?.Invoke(updated);
            Hide();
        }
    }
}
