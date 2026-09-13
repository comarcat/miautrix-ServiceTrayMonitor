using System.ServiceProcess;

namespace ServiceTrayMonitor.Models
{
    /// <summary>
    /// Represents the live state of a Windows Service that the app is watching.
    /// </summary>
    public class MonitoredService
    {
        public string ServiceName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public ServiceControllerStatus Status { get; set; } = ServiceControllerStatus.Stopped;
        public bool CanPauseAndContinue { get; set; }
        public bool Exists { get; set; } = true;

        /// <summary>
        /// Color used in the UI and tray icon to represent this service's current status.
        /// Palette lives in <see cref="StatusPalette"/>.
        /// </summary>
        public Color StatusColor => StatusPalette.For(Status, Exists);

        public string StatusText => Exists ? Status.ToString() : "Not found";
    }

    /// <summary>
    /// Persisted app configuration: which services the user chose to monitor, and poll interval.
    /// </summary>
    public class AppSettings
    {
        public List<string> MonitoredServiceNames { get; set; } = new();
        public int PollIntervalSeconds { get; set; } = 5;
        public bool StartWithWindows { get; set; } = false;

        // Admin credential used to control services, stored alongside the rest of the
        // config. The password is NEVER stored here in plain text — CredentialEncryptedPasswordBase64
        // holds the DPAPI-encrypted bytes (see ConfigManager.SaveCredential/LoadCredential).
        public string CredentialDomain { get; set; } = string.Empty;
        public string CredentialUsername { get; set; } = string.Empty;
        public string CredentialEncryptedPasswordBase64 { get; set; } = string.Empty;
    }
}
