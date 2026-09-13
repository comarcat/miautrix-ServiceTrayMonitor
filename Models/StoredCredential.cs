namespace ServiceTrayMonitor.Models
{
    /// <summary>
    /// The admin (or service-account) credential used to start/stop/pause services.
    /// The password is never kept as plain text on disk — see ConfigManager, which
    /// encrypts it with Windows DPAPI before saving.
    /// </summary>
    public class StoredCredential
    {
        public string Domain { get; set; } = string.Empty;   // leave blank for a local account
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // only ever held in memory, decrypted on demand

        public bool IsConfigured => !string.IsNullOrWhiteSpace(Username);

        public string DisplayLogin => string.IsNullOrWhiteSpace(Domain) ? Username : $"{Domain}\\{Username}";
    }
}
