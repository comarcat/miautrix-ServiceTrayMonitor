using ServiceTrayMonitor.Licensing;
using ServiceTrayMonitor.Models;
using ServiceTrayMonitor.Services;

namespace ServiceTrayMonitor.Forms
{
    /// <summary>
    /// Tray menu "License…": status, masked key, subscription expiry, and actions to check now
    /// or activate a different key.
    /// </summary>
    public class LicenseForm : Form
    {
        private readonly LicenseManager _licensing;
        private readonly Label _statusValue, _keyValue, _expiryValue, _activationValue, _verifiedValue, _deadlineValue, _messageLabel;
        private readonly Button _checkBtn, _changeKeyBtn, _closeBtn;
        private bool _busy;

        public LicenseForm(LicenseManager licensing)
        {
            _licensing = licensing;

            Text = "License";
            Width = 540;
            Height = 360;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Icon = IconFactory.GetTrayIcon(StatusPalette.Running);

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 200,
                ColumnCount = 2,
                Padding = new Padding(12)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            AddRow(table, "Product:", $"Service Tray Monitor {Application.ProductVersion}");
            _statusValue = AddRow(table, "Status:", string.Empty);
            _keyValue = AddRow(table, "License key:", string.Empty);
            _expiryValue = AddRow(table, "Subscription expiry:", string.Empty);
            _activationValue = AddRow(table, "Activation ID:", string.Empty);
            _verifiedValue = AddRow(table, "Last verified:", string.Empty);
            _deadlineValue = AddRow(table, "Confirm by:", string.Empty);

            _messageLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 48,
                Padding = new Padding(12, 0, 12, 0),
                ForeColor = Color.DimGray
            };

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                Padding = new Padding(8),
                FlowDirection = FlowDirection.RightToLeft
            };
            _closeBtn = new Button { Text = "Close", Width = 90, Height = 32 };
            _changeKeyBtn = new Button { Text = "Activate with a different key...", Width = 210, Height = 32 };
            _checkBtn = new Button { Text = "Check now", Width = 100, Height = 32 };

            _closeBtn.Click += (_, _) => Hide();
            _changeKeyBtn.Click += (_, _) => ChangeKey();
            _checkBtn.Click += async (_, _) => await CheckNowAsync();

            buttonPanel.Controls.Add(_closeBtn);
            buttonPanel.Controls.Add(_changeKeyBtn);
            buttonPanel.Controls.Add(_checkBtn);

            Controls.Add(_messageLabel);
            Controls.Add(table);
            Controls.Add(buttonPanel);

            // Hide instead of close, so the tray keeps running.
            FormClosing += (_, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    if (!_busy) Hide();
                }
            };

            VisibleChanged += (_, _) =>
            {
                if (Visible) RefreshView();
            };
            _licensing.DecisionChanged += OnDecisionChanged;
        }

        private static Label AddRow(TableLayoutPanel table, string caption, string value)
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
            table.Controls.Add(new Label { Text = caption, TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, row);
            var valueLabel = new Label { Text = value, TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill, AutoEllipsis = true };
            table.Controls.Add(valueLabel, 1, row);
            return valueLabel;
        }

        private void OnDecisionChanged(LicenseDecision decision)
        {
            if (Visible && !IsDisposed)
                RefreshView();
        }

        private void RefreshView()
        {
            var state = _licensing.State;
            var decision = _licensing.Decision;
            var file = _licensing.SavedLicense;

            (_statusValue.Text, _statusValue.ForeColor) = decision.Access switch
            {
                LicenseAccess.Licensed => ("Activated", Color.FromArgb(39, 174, 96)),
                LicenseAccess.Grace => (decision.Summary, Color.FromArgb(211, 84, 0)),
                LicenseAccess.Blocked => (decision.Summary, Color.FromArgb(192, 57, 43)),
                _ => ("Not activated", Color.FromArgb(192, 57, 43)),
            };

            _keyValue.Text = string.IsNullOrEmpty(state.LicenseKey) ? "—" : LicenseManager.MaskKey(state.LicenseKey);
            _expiryValue.Text = file is { SignatureVerified: true }
                ? file.SubscriptionExpiryUtc is DateTime expiry ? expiry.ToLocalTime().ToString("d") : "None (perpetual)"
                : "Unknown until the server confirms the license";
            _activationValue.Text = state.ActivationId?.ToString() ?? "—";
            _verifiedValue.Text = state.LastVerifiedUtc?.ToLocalTime().ToString("g") ?? "Never";
            _deadlineValue.Text = decision.DeadlineUtc?.ToLocalTime().ToString("g") ?? "—";

            _messageLabel.Text = string.IsNullOrEmpty(state.LastMessage)
                ? string.Empty
                : $"Last server contact: {state.LastMessage}";
        }

        private async Task CheckNowAsync()
        {
            SetBusy(true);
            _messageLabel.Text = "Checking the license with the licensing server...";
            try
            {
                var outcome = await _licensing.CheckInAsync();
                if (!IsDisposed)
                {
                    RefreshView();
                    if (!outcome.Success || outcome.Message != outcome.Decision.Summary)
                        _messageLabel.Text = outcome.Message;
                }
            }
            catch (Exception ex)
            {
                if (!IsDisposed) _messageLabel.Text = ex.Message;
            }
            finally
            {
                if (!IsDisposed) SetBusy(false);
            }
        }

        private void ChangeKey()
        {
            using (var activation = new ActivationForm(_licensing, startupGate: false))
                activation.ShowDialog(this);

            if (!IsDisposed)
                RefreshView();
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;
            UseWaitCursor = busy;
            _checkBtn.Enabled = _changeKeyBtn.Enabled = _closeBtn.Enabled = !busy;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _licensing.DecisionChanged -= OnDecisionChanged;
            base.Dispose(disposing);
        }
    }
}
