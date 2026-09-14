using ServiceTrayMonitor.Licensing;
using ServiceTrayMonitor.Models;
using ServiceTrayMonitor.Services;

namespace ServiceTrayMonitor.Forms
{
    /// <summary>
    /// License key entry. At startup it gates the app — closing it, or an activation error, exits.
    /// Opened from the License window it activates a different key, and a failure keeps the
    /// current license.
    /// </summary>
    public class ActivationForm : Form
    {
        private static readonly Color ErrorColor = Color.FromArgb(192, 57, 43);

        private readonly LicenseManager _licensing;
        private readonly bool _startupGate;
        private readonly TextBox _keyBox;
        private readonly Label _messageLabel;
        private readonly Label _statusLabel;
        private readonly ProgressBar _progress;
        private readonly Button _activateBtn, _checkBtn, _closeBtn;
        private bool _busy;

        /// <summary>
        /// Shows the activation window unless the license already lets the app run.
        /// Returns true when the app may start.
        /// </summary>
        public static bool EnsureLicensed(LicenseManager licensing)
        {
            var decision = licensing.Evaluate();
            if (decision.Access is LicenseAccess.Licensed or LicenseAccess.Grace)
                return true;

            using var form = new ActivationForm(licensing, startupGate: true);
            return form.ShowDialog() == DialogResult.OK;
        }

        public ActivationForm(LicenseManager licensing, bool startupGate)
        {
            _licensing = licensing;
            _startupGate = startupGate;

            Text = "Activate Service Tray Monitor";
            Width = 500;
            Height = 330;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = startupGate;
            Icon = IconFactory.GetTrayIcon(StatusPalette.Pending);

            var intro = new Label
            {
                Text = startupGate
                    ? $"Service Tray Monitor {Application.ProductVersion} must be activated before it can run.\nEnter the license key you received."
                    : "Enter the new license key. Your current license stays active unless the new key is accepted.",
                Dock = DockStyle.Top,
                Height = 52,
                Padding = new Padding(12, 12, 12, 0)
            };

            _messageLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 52,
                Padding = new Padding(12, 4, 12, 0),
                ForeColor = ErrorColor
            };
            if (startupGate && licensing.Decision.Access == LicenseAccess.Blocked)
                _messageLabel.Text = licensing.Decision.Summary;
            else if (startupGate && !string.IsNullOrEmpty(licensing.State.LastMessage))
                _messageLabel.Text = "Last activation attempt: " + licensing.State.LastMessage;

            _keyBox = new TextBox
            {
                Dock = DockStyle.Top,
                CharacterCasing = CharacterCasing.Upper,
                MaxLength = 40,
                Font = new Font(FontFamily.GenericMonospace, 11f),
                PlaceholderText = "XXXX-XXXXX-XXXX-XXXX-XXXX-XXXX-XX"
            };
            _keyBox.TextChanged += (_, _) => UpdateControls();

            var formatHint = new Label
            {
                Text = "Format: groups of 4-5-4-4-4-4-2 letters or digits, separated by hyphens.",
                Dock = DockStyle.Top,
                Height = 22,
                ForeColor = Color.DimGray
            };

            var keyPanel = new Panel { Dock = DockStyle.Top, Height = 58, Padding = new Padding(12, 4, 12, 0) };
            keyPanel.Controls.Add(formatHint);
            keyPanel.Controls.Add(_keyBox);

            _progress = new ProgressBar { Dock = DockStyle.Top, Height = 6, Style = ProgressBarStyle.Marquee, Visible = false };

            _statusLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(12, 6, 12, 0),
                ForeColor = Color.DimGray
            };

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                Padding = new Padding(8),
                FlowDirection = FlowDirection.RightToLeft
            };
            _closeBtn = new Button { Text = startupGate ? "Exit" : "Cancel", Width = 100, Height = 32 };
            _activateBtn = new Button { Text = "Activate", Width = 100, Height = 32 };
            _checkBtn = new Button
            {
                Text = "Check again",
                Width = 110,
                Height = 32,
                // Offered at startup when a key is saved but not currently usable (e.g. awaiting approval).
                Visible = startupGate && !string.IsNullOrEmpty(licensing.State.LicenseKey)
            };

            _closeBtn.Click += (_, _) => Close();
            _activateBtn.Click += async (_, _) => await ActivateAsync();
            _checkBtn.Click += async (_, _) => await CheckAgainAsync();

            buttonPanel.Controls.Add(_closeBtn);
            buttonPanel.Controls.Add(_activateBtn);
            buttonPanel.Controls.Add(_checkBtn);

            Controls.Add(_statusLabel);
            Controls.Add(_progress);
            Controls.Add(keyPanel);
            Controls.Add(_messageLabel);
            Controls.Add(intro);
            Controls.Add(buttonPanel);

            AcceptButton = _activateBtn;
            FormClosing += (_, e) =>
            {
                if (_busy)
                    e.Cancel = true;   // wait for the server call to finish
            };

            // A saved key that was blocked may have been approved since: check once on open.
            Shown += async (_, _) =>
            {
                if (_checkBtn.Visible)
                    await CheckAgainAsync();
            };

            UpdateControls();
        }

        private async Task ActivateAsync()
        {
            SetBusy(true, "Activating...");
            ActivationOutcome outcome;
            try
            {
                outcome = await _licensing.ActivateAsync(_keyBox.Text);
            }
            catch (Exception ex)
            {
                outcome = new ActivationOutcome(false, true, ex.Message, _licensing.Decision);
            }
            SetBusy(false, string.Empty);

            if (outcome.Success)
            {
                ReportAllowed(outcome);
                DialogResult = DialogResult.OK;
                return;
            }

            if (_startupGate)
            {
                MessageBox.Show(this, $"Activation failed:\n\n{outcome.Message}\n\nService Tray Monitor will now close.",
                    "Activation failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.Cancel;
            }
            else
            {
                MessageBox.Show(this, $"The new key couldn't be activated:\n\n{outcome.Message}",
                    "Activation failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _statusLabel.ForeColor = ErrorColor;
                _statusLabel.Text = "The new key wasn't activated.";
            }
        }

        private async Task CheckAgainAsync()
        {
            SetBusy(true, "Checking the license with the licensing server...");
            ActivationOutcome outcome;
            try
            {
                outcome = await _licensing.CheckInAsync();
            }
            catch (Exception ex)
            {
                outcome = new ActivationOutcome(false, false, ex.Message, _licensing.Decision);
            }
            SetBusy(false, string.Empty);

            if (outcome.Success)
            {
                ReportAllowed(outcome);
                DialogResult = DialogResult.OK;
                return;
            }

            _messageLabel.Text = outcome.Decision.Access == LicenseAccess.Blocked ? outcome.Decision.Summary : outcome.Message;
            _statusLabel.ForeColor = Color.DimGray;
            _statusLabel.Text = "Enter a license key to activate, or Exit.";
        }

        private void ReportAllowed(ActivationOutcome outcome)
        {
            var decision = outcome.Decision;
            if (decision.Access == LicenseAccess.Licensed)
            {
                MessageBox.Show(this, "Service Tray Monitor is activated.", "Activated",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string deadline = decision.DeadlineUtc?.ToLocalTime().ToString("g") ?? "the end of the 7-day period";
            string detail = outcome.Message == decision.Summary ? decision.Summary : $"{decision.Summary}\n{outcome.Message}";
            MessageBox.Show(this,
                $"{detail}\n\nService Tray Monitor will keep checking with the licensing server and can run until {deadline}. " +
                "If the license isn't confirmed by then, it will close until it is activated.",
                "License not confirmed yet", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SetBusy(bool busy, string status)
        {
            _busy = busy;
            _progress.Visible = busy;
            UseWaitCursor = busy;
            _statusLabel.ForeColor = Color.DimGray;
            _statusLabel.Text = status;
            UpdateControls();
        }

        private void UpdateControls()
        {
            _activateBtn.Enabled = !_busy && LicenseManager.KeyFormat.IsMatch(LicenseManager.NormalizeKey(_keyBox.Text));
            _checkBtn.Enabled = !_busy;
            _closeBtn.Enabled = !_busy;
            _keyBox.Enabled = !_busy;
        }
    }
}
