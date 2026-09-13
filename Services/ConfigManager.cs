using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Win32;
using ServiceTrayMonitor.Models;

namespace ServiceTrayMonitor.Services
{
    /// <summary>
    /// Handles reading and writing the app's settings file, stored per-user in
    /// %AppData%\ServiceTrayMonitor\config.json — including the admin credential,
    /// whose password is encrypted with Windows DPAPI before it's written.
    /// </summary>
    public static class ConfigManager
    {
        private static readonly string ConfigDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ServiceTrayMonitor");

        private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

        private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string RunValueName = "ServiceTrayMonitor";

        // Extra entropy mixed into the DPAPI encryption, so this app's blob can't be
        // decrypted by some other app running as the same Windows user.
        private static readonly byte[] CredentialEntropy = Encoding.UTF8.GetBytes("ServiceTrayMonitor.CredEntropy.v1");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                        return settings;
                }
            }
            catch
            {
                // Fall through to defaults if the file is missing or corrupt.
            }

            return new AppSettings();
        }

        public static void Save(AppSettings settings)
        {
            Directory.CreateDirectory(ConfigDir);
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);

            ApplyStartupSetting(settings.StartWithWindows);
        }

        /// <summary>
        /// Reads the admin credential back out of config.json, decrypting the password
        /// with DPAPI. Called on app startup and each time a service action needs it.
        /// Returns an empty (IsConfigured == false) credential if none is saved, or if
        /// the blob can't be decrypted (e.g. config.json was copied to a different
        /// machine or user account — DPAPI ties encryption to the current user).
        /// </summary>
        public static StoredCredential LoadCredential()
        {
            var settings = Load();
            var credential = new StoredCredential
            {
                Domain = settings.CredentialDomain,
                Username = settings.CredentialUsername
            };

            if (string.IsNullOrWhiteSpace(settings.CredentialEncryptedPasswordBase64))
                return credential;

            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(settings.CredentialEncryptedPasswordBase64);
                byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, CredentialEntropy, DataProtectionScope.CurrentUser);
                credential.Password = Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                // Can't decrypt — leave password blank; IsConfigured still reflects Username.
            }

            return credential;
        }

        /// <summary>
        /// Encrypts the credential's password with DPAPI and writes it (plus domain/username)
        /// into config.json, preserving every other setting already saved.
        /// </summary>
        public static void SaveCredential(StoredCredential credential)
        {
            var settings = Load();

            settings.CredentialDomain = credential.Domain;
            settings.CredentialUsername = credential.Username;

            byte[] plainBytes = Encoding.UTF8.GetBytes(credential.Password);
            byte[] encryptedBytes = ProtectedData.Protect(plainBytes, CredentialEntropy, DataProtectionScope.CurrentUser);
            settings.CredentialEncryptedPasswordBase64 = Convert.ToBase64String(encryptedBytes);

            Save(settings);
        }

        /// <summary>Removes the saved credential, keeping every other setting intact.</summary>
        public static void ClearCredential()
        {
            var settings = Load();
            settings.CredentialDomain = string.Empty;
            settings.CredentialUsername = string.Empty;
            settings.CredentialEncryptedPasswordBase64 = string.Empty;
            Save(settings);
        }

        /// <summary>
        /// Adds or removes a "run at Windows startup" registry entry for the current user.
        /// </summary>
        private static void ApplyStartupSetting(bool enabled)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
                if (key == null) return;

                if (enabled)
                {
                    string exePath = Environment.ProcessPath ?? Application.ExecutablePath;
                    key.SetValue(RunValueName, $"\"{exePath}\"");
                }
                else
                {
                    if (key.GetValue(RunValueName) != null)
                        key.DeleteValue(RunValueName);
                }
            }
            catch
            {
                // Non-fatal: startup toggle is a nice-to-have, don't crash the app over it.
            }
        }
    }
}
