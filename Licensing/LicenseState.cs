using System.Text.Json;

namespace ServiceTrayMonitor.Licensing
{
    /// <summary>
    /// Activation state, persisted per machine in %ProgramData%\ServiceTrayMonitor\license.json.
    /// The signed license file is the source of truth for status and dates; the other fields
    /// record what this installation has tried and when.
    /// </summary>
    public class LicenseState
    {
        /// <summary>Generated once for this installation and reused for every activation and check-in.</summary>
        public Guid InstallGuid { get; set; }

        public string LicenseKey { get; set; } = string.Empty;

        /// <summary>Null until an activation has reached the server.</summary>
        public Guid? ActivationId { get; set; }

        /// <summary>Latest signed license file from the server (base64), re-verified at every start.</summary>
        public string? LicenseFileBase64 { get; set; }

        public int CheckIntervalHours { get; set; } = 6;
        public int SubscriptionGraceDays { get; set; } = 30;
        public DateTime? ReviewDeadlineUtc { get; set; }

        /// <summary>Start of the 7-day window in which the app runs without an approved license.</summary>
        public DateTime? GraceStartedUtc { get; set; }

        /// <summary>Last time the server confirmed an approved, signature-verified license.</summary>
        public DateTime? LastVerifiedUtc { get; set; }

        public DateTime? LastCheckAttemptUtc { get; set; }

        /// <summary>Latest clock value seen, so moving the clock back can't stretch a time window.</summary>
        public DateTime LastSeenUtc { get; set; }

        /// <summary>Set when the server rejected the license this installation holds (revoked, locked, ...).</summary>
        public string? BlockedReason { get; set; }

        public string? LastResultCode { get; set; }
        public string? LastMessage { get; set; }
    }

    public static class LicenseStateStore
    {
        private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

        // Settable so tests can point the store at a temporary folder.
        internal static string StateDir { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "ServiceTrayMonitor");

        private static string StatePath => Path.Combine(StateDir, "license.json");

        /// <summary>
        /// Loads the saved state, or a fresh unactivated state with a new InstallGuid. An unreadable
        /// file is kept as license.json.corrupt-yyyyMMdd-HHmmss and the installation must activate again.
        /// </summary>
        public static LicenseState Load()
        {
            if (File.Exists(StatePath))
            {
                try
                {
                    if (JsonSerializer.Deserialize<LicenseState>(File.ReadAllText(StatePath)) is { } state)
                    {
                        if (state.InstallGuid == Guid.Empty)
                            state.InstallGuid = Guid.NewGuid();
                        return state;
                    }
                }
                catch (JsonException)
                {
                    // fall through to the backup below
                }

                File.Move(StatePath, Path.Combine(StateDir, $"license.json.corrupt-{DateTime.Now:yyyyMMdd-HHmmss}"), overwrite: true);
            }

            return new LicenseState { InstallGuid = Guid.NewGuid() };
        }

        public static void Save(LicenseState state)
        {
            Directory.CreateDirectory(StateDir);
            string tempPath = StatePath + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(state, WriteOptions));
            File.Move(tempPath, StatePath, overwrite: true);
        }
    }
}
