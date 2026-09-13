using ServiceTrayMonitor.Models;
using ServiceTrayMonitor.Services;

namespace ServiceTrayMonitor.Forms
{
    /// <summary>
    /// Lets the user enter (or clear) the Windows account used to start/stop/pause
    /// services when the app's own rights are refused. Saved via ConfigManager into
    /// config.json, encrypted with DPAPI.
    /// </summary>
    public class CredentialsForm : Form
    {
        private readonly TextBox _domainBox;
        private readonly TextBox _usernameBox;
        private readonly TextBox _passwordBox;
        private readonly Label _statusLabel;
        private readonly Button _saveBtn, _testBtn, _clearBtn;

        /// <summary>Raised after the user saves or clears the credential.</summary>
        public event Action? CredentialUpdated;

        public CredentialsForm()
        {
            Text = "Admin Credentials";
            Width = 420;
            Height = 300;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var info = new Label
            {
                Text = "Account used when Windows refuses a service action to the app itself.\n" +
                       "Leave Domain blank for a local account. The password is stored\n" +
                       "encrypted (DPAPI) and only readable by your Windows user account.",
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(10, 10, 10, 0)
            };

            var form = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 110,
                ColumnCount = 2,
                Padding = new Padding(10)
            };
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _domainBox = new TextBox { Dock = DockStyle.Fill };
            _usernameBox = new TextBox { Dock = DockStyle.Fill };
            _passwordBox = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true };

            form.Controls.Add(new Label { Text = "Domain:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 0);
            form.Controls.Add(_domainBox, 1, 0);
            form.Controls.Add(new Label { Text = "Username:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 1);
            form.Controls.Add(_usernameBox, 1, 1);
            form.Controls.Add(new Label { Text = "Password:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 2);
            form.Controls.Add(_passwordBox, 1, 2);

            _statusLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(10, 0, 10, 0),
                ForeColor = Color.DimGray
            };

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                Padding = new Padding(8),
                FlowDirection = FlowDirection.RightToLeft
            };
            _saveBtn = new Button { Text = "Save", Width = 90, Height = 32 };
            _testBtn = new Button { Text = "Test", Width = 90, Height = 32 };
            _clearBtn = new Button { Text = "Clear", Width = 90, Height = 32 };

            _saveBtn.Click += (_, _) => SaveCredential();
            _testBtn.Click += (_, _) => TestCredential();
            _clearBtn.Click += (_, _) => ClearCredential();

            buttonPanel.Controls.Add(_saveBtn);
            buttonPanel.Controls.Add(_testBtn);
            buttonPanel.Controls.Add(_clearBtn);

            Controls.Add(_statusLabel);
            Controls.Add(form);
            Controls.Add(info);
            Controls.Add(buttonPanel);

            FormClosing += (_, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    Hide();
                }
            };

            // Reload each time the window is shown — it's hidden, not closed, between uses.
            VisibleChanged += (_, _) =>
            {
                if (Visible) LoadExisting();
            };
        }

        private void LoadExisting()
        {
            var cred = ConfigManager.LoadCredential();
            _domainBox.Text = cred.Domain;
            _usernameBox.Text = cred.Username;
            _passwordBox.Text = string.Empty; // never re-display the password
            _passwordBox.PlaceholderText = cred.IsConfigured ? "(leave blank to keep the saved password)" : string.Empty;
            _statusLabel.ForeColor = Color.DimGray;
            _statusLabel.Text = cred.IsConfigured
                ? $"Currently configured: {cred.DisplayLogin}"
                : "No credential saved — service actions use the app's own admin rights only.";
        }

        /// <summary>
        /// Reads the form. A blank password keeps the saved one when the login is unchanged, since
        /// the saved password is never shown again — without this, re-saving would erase it.
        /// </summary>
        private StoredCredential BuildFromForm()
        {
            var cred = new StoredCredential
            {
                Domain = _domainBox.Text.Trim(),
                Username = _usernameBox.Text.Trim(),
                Password = _passwordBox.Text
            };

            if (cred.Password.Length == 0)
            {
                var saved = ConfigManager.LoadCredential();
                if (saved.IsConfigured && string.Equals(saved.DisplayLogin, cred.DisplayLogin, StringComparison.OrdinalIgnoreCase))
                    cred.Password = saved.Password;
            }

            return cred;
        }

        private void SaveCredential()
        {
            var cred = BuildFromForm();
            if (!cred.IsConfigured)
            {
                MessageBox.Show(this, "Enter a username.", "Missing info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (cred.Password.Length == 0)
            {
                MessageBox.Show(this, "Enter the account's password.", "Missing info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                ConfigManager.SaveCredential(cred);
            }
            catch (Exception ex)
            {
                SetStatus($"Not saved: {ex.Message}", success: false);
                return;
            }

            _passwordBox.Text = string.Empty;
            _passwordBox.PlaceholderText = "(leave blank to keep the saved password)";
            SetStatus($"Saved: {cred.DisplayLogin}", success: true);
            CredentialUpdated?.Invoke();
        }

        private async void TestCredential()
        {
            var cred = BuildFromForm();
            SetButtonsEnabled(false);
            UseWaitCursor = true;
            _statusLabel.ForeColor = Color.DimGray;
            _statusLabel.Text = "Testing...";

            (bool success, string message) result;
            try
            {
                result = await Task.Run(() => ElevatedServiceRunner.TestCredential(cred));
            }
            catch (Exception ex)
            {
                result = (false, ex.Message);
            }

            if (IsDisposed) return;
            UseWaitCursor = false;
            SetButtonsEnabled(true);
            SetStatus(result.message, result.success);
        }

        private void ClearCredential()
        {
            try
            {
                ConfigManager.ClearCredential();
            }
            catch (Exception ex)
            {
                SetStatus($"Not cleared: {ex.Message}", success: false);
                return;
            }

            _domainBox.Text = string.Empty;
            _usernameBox.Text = string.Empty;
            _passwordBox.Text = string.Empty;
            _passwordBox.PlaceholderText = string.Empty;
            _statusLabel.Text = "Credential cleared — service actions use the app's own admin rights only.";
            _statusLabel.ForeColor = Color.DimGray;
            CredentialUpdated?.Invoke();
        }

        private void SetStatus(string text, bool success)
        {
            _statusLabel.Text = text;
            _statusLabel.ForeColor = success ? Color.FromArgb(39, 174, 96) : Color.FromArgb(192, 57, 43);
        }

        private void SetButtonsEnabled(bool enabled)
        {
            _saveBtn.Enabled = _testBtn.Enabled = _clearBtn.Enabled = enabled;
        }
    }
}
