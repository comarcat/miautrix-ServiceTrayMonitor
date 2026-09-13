using ServiceTrayMonitor.Models;
using ServiceTrayMonitor.Services;

namespace ServiceTrayMonitor.Forms
{
    /// <summary>
    /// Lets the user enter (or clear) the Windows account used to start/stop/pause
    /// services. Saved via ConfigManager into config.json, encrypted with DPAPI.
    /// </summary>
    public class CredentialsForm : Form
    {
        private readonly TextBox _domainBox;
        private readonly TextBox _usernameBox;
        private readonly TextBox _passwordBox;
        private readonly Label _statusLabel;

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
                Text = "Enter a Windows account with permission to control services.\n" +
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
            var saveBtn = new Button { Text = "Save", Width = 90, Height = 32 };
            var testBtn = new Button { Text = "Test", Width = 90, Height = 32 };
            var clearBtn = new Button { Text = "Clear", Width = 90, Height = 32 };

            saveBtn.Click += (_, _) => SaveCredential();
            testBtn.Click += (_, _) => TestCredential();
            clearBtn.Click += (_, _) => ClearCredential();

            buttonPanel.Controls.Add(saveBtn);
            buttonPanel.Controls.Add(testBtn);
            buttonPanel.Controls.Add(clearBtn);

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

            Load += (_, _) => LoadExisting();
        }

        private void LoadExisting()
        {
            var cred = ConfigManager.LoadCredential();
            _domainBox.Text = cred.Domain;
            _usernameBox.Text = cred.Username;
            _passwordBox.Text = string.Empty; // never re-display the password
            _statusLabel.Text = cred.IsConfigured
                ? $"Currently configured: {cred.DisplayLogin}"
                : "No credential saved yet — service actions will use the app's own admin session.";
        }

        private StoredCredential BuildFromForm() => new()
        {
            Domain = _domainBox.Text.Trim(),
            Username = _usernameBox.Text.Trim(),
            Password = _passwordBox.Text
        };

        private void SaveCredential()
        {
            var cred = BuildFromForm();
            if (!cred.IsConfigured)
            {
                MessageBox.Show(this, "Enter a username.", "Missing info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ConfigManager.SaveCredential(cred);
            _statusLabel.Text = $"Saved: {cred.DisplayLogin}";
            _statusLabel.ForeColor = Color.FromArgb(39, 174, 96);
            CredentialUpdated?.Invoke();
        }

        private void TestCredential()
        {
            var cred = BuildFromForm();
            Cursor = Cursors.WaitCursor;
            var (success, message) = ElevatedServiceRunner.TestCredential(cred);
            Cursor = Cursors.Default;

            _statusLabel.Text = message;
            _statusLabel.ForeColor = success ? Color.FromArgb(39, 174, 96) : Color.FromArgb(192, 57, 43);
        }

        private void ClearCredential()
        {
            ConfigManager.ClearCredential();
            _domainBox.Text = string.Empty;
            _usernameBox.Text = string.Empty;
            _passwordBox.Text = string.Empty;
            _statusLabel.Text = "Credential cleared — service actions will use the app's own admin session.";
            _statusLabel.ForeColor = Color.DimGray;
            CredentialUpdated?.Invoke();
        }
    }
}
