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
        /// </summary>
        public Color StatusColor => Status switch
        {
            ServiceControllerStatus.Running => Color.FromArgb(46, 204, 113),   // green
            ServiceControllerStatus.Stopped => Color.FromArgb(231, 76, 60),    // red
            ServiceControllerStatus.Paused => Color.FromArgb(241, 196, 15),    // amber
            ServiceControllerStatus.StartPending => Color.FromArgb(52, 152, 219),  // blue
            ServiceControllerStatus.StopPending => Color.FromArgb(52, 152, 219),   // blue
            ServiceControllerStatus.ContinuePending => Color.FromArgb(52, 152, 219),
            ServiceControllerStatus.PausePending => Color.FromArgb(52, 152, 219),
            _ => Color.Gray
        };

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
