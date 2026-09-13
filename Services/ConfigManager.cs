using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ServiceTrayMonitor.Models;

namespace ServiceTrayMonitor.Services
{
    /// <summary>
    /// Handles reading and writing the app's settings file, stored per-user in
    /// %AppData%\ServiceTrayMonitor\config.json — including the admin credential,
    /// whose password is encrypted with Windows DPAPI before it's written.
    ///
    /// Every save re-reads the file and changes only its own fields (general settings or the
    /// credential), so neither kind of save can overwrite the other with stale values.
    /// </summary>
    public static class ConfigManager
    {
        private static readonly object FileLock = new();
        private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

        // Settable so tests can point the config at a temporary folder.
        internal static string ConfigDir { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ServiceTrayMonitor");

        private static string ConfigPath => Path.Combine(ConfigDir, "config.json");

        // Extra entropy mixed into the DPAPI encryption. It keeps the blob from being reused by
        // unrelated DPAPI consumers, but it is not a secret (it ships in this binary): any process
        // running as the same Windows user can still decrypt the password.
        private static readonly byte[] CredentialEntropy = Encoding.UTF8.GetBytes("ServiceTrayMonitor.CredEntropy.v1");

        /// <summary>
        /// Set when a load found config.json unreadable. A corrupt file is kept next to the original
        /// as config.json.corrupt-yyyyMMdd-HHmmss and defaults are used, so nothing is silently lost.
        /// </summary>
        public static string? LastLoadWarning { get; private set; }

        public static AppSettings Load() => Read(throwOnIoError: false);

        /// <summary>
        /// Saves the general (non-credential) settings, keeping the credential currently on disk.
        /// Returns the settings as saved.
        /// </summary>
        public static AppSettings SaveGeneralSettings(AppSettings general)
        {
            lock (FileLock)
            {
                var settings = Read(throwOnIoError: true);
                settings.MonitoredServiceNames = new List<string>(general.MonitoredServiceNames);
                settings.PollIntervalSeconds = general.PollIntervalSeconds;
                settings.StartWithWindows = general.StartWithWindows;
                Write(settings);
                return settings;
            }
        }

        /// <summary>
        /// Reads the admin credential back out of config.json, decrypting the password
        /// with DPAPI. Called each time a service action needs it.
        /// Returns an empty (IsConfigured == false) credential if none is saved. If the
        /// blob can't be decrypted (e.g. config.json was copied to a different machine or
        /// user account — DPAPI ties encryption to the current user), the password is blank.
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
            lock (FileLock)
            {
                var settings = Read(throwOnIoError: true);

                settings.CredentialDomain = credential.Domain;
                settings.CredentialUsername = credential.Username;

                byte[] plainBytes = Encoding.UTF8.GetBytes(credential.Password);
                byte[] encryptedBytes = ProtectedData.Protect(plainBytes, CredentialEntropy, DataProtectionScope.CurrentUser);
                settings.CredentialEncryptedPasswordBase64 = Convert.ToBase64String(encryptedBytes);

                Write(settings);
            }
        }

        /// <summary>Removes the saved credential, keeping every other setting intact.</summary>
        public static void ClearCredential()
        {
            lock (FileLock)
            {
                var settings = Read(throwOnIoError: true);
                settings.CredentialDomain = string.Empty;
                settings.CredentialUsername = string.Empty;
                settings.CredentialEncryptedPasswordBase64 = string.Empty;
                Write(settings);
            }
        }

        private static AppSettings Read(bool throwOnIoError)
        {
            lock (FileLock)
            {
                if (!File.Exists(ConfigPath))
                    return new AppSettings();

                try
                {
                    var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(ConfigPath));
                    if (settings != null)
                    {
                        settings.MonitoredServiceNames ??= new List<string>();
                        return settings;
                    }

                    BackUpCorruptFile("the file holds no settings");
                }
                catch (JsonException ex)
                {
                    BackUpCorruptFile(ex.Message);
                }
                catch (Exception ex) when (!throwOnIoError && ex is IOException or UnauthorizedAccessException)
                {
                    // A save rethrows instead, so a temporarily locked file is never replaced by defaults.
                    LastLoadWarning = $"config.json couldn't be read ({ex.Message}). Default settings are in use.";
                }

                return new AppSettings();
            }
        }

        private static void BackUpCorruptFile(string reason)
        {
            string backupName = $"config.json.corrupt-{DateTime.Now:yyyyMMdd-HHmmss}";
            try
            {
                File.Move(ConfigPath, Path.Combine(ConfigDir, backupName), overwrite: true);
                LastLoadWarning = $"config.json couldn't be read ({reason}). It was kept as {backupName} and default settings are in use.";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                LastLoadWarning = $"config.json couldn't be read ({reason}) or backed up ({ex.Message}). Default settings are in use.";
            }
        }

        private static void Write(AppSettings settings)
        {
            Directory.CreateDirectory(ConfigDir);
            string json = JsonSerializer.Serialize(settings, WriteOptions);

            // Write a temp file and swap it in, so a crash mid-write can't leave a truncated config.
            string tempPath = ConfigPath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, ConfigPath, overwrite: true);
        }
    }
}
