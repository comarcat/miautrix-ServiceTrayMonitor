using System.Diagnostics;
using ServiceTrayMonitor.Models;

namespace ServiceTrayMonitor.Services
{
    /// <summary>
    /// Executes service control actions (start/stop/pause/continue) under a specific
    /// Windows account instead of the interactively logged-in user — so the app itself
    /// doesn't have to run elevated as long as the stored account is an admin (or has
    /// the right service-control permissions).
    ///
    /// Implementation note: this shells out to sc.exe rather than using ServiceController
    /// directly, because ServiceController always acts as the current process's token —
    /// there's no built-in way to pass explicit credentials to it. Process.Start, on the
    /// other hand, supports launching as a different user via UserName/Domain/PasswordInClearText.
    ///
    /// Caveat: if the stored account has UAC enabled and is not the built-in Administrator,
    /// Windows may still hand back a filtered (non-admin) token for an interactive-style
    /// logon. If service actions fail with "Access is denied" even though the credential
    /// is correct, use a dedicated service account with UAC not applicable to it, or the
    /// built-in Administrator account.
    /// </summary>
    public static class ElevatedServiceRunner
    {
        public static (bool success, string message) RunAction(string serviceName, string scAction, StoredCredential credential)
        {
            if (!credential.IsConfigured)
                return (false, "No admin credential is configured.");

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"{scAction} \"{serviceName}\"",
                    UserName = credential.Username,
                    Domain = string.IsNullOrWhiteSpace(credential.Domain) ? Environment.MachineName : credential.Domain,
                    PasswordInClearText = credential.Password,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    LoadUserProfile = false
                };

                using var process = Process.Start(psi);
                if (process == null)
                    return (false, "Could not start sc.exe.");

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit(15000);

                if (process.ExitCode == 0)
                    return (true, $"{scAction} succeeded.");

                string detail = !string.IsNullOrWhiteSpace(error) ? error : output;
                if (string.IsNullOrWhiteSpace(detail))
                    detail = $"sc.exe exited with code {process.ExitCode}.";

                return (false, AppendAccessDeniedHint(detail.Trim()));
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// "Access is denied" from sc.exe with a valid credential almost always means the
        /// account's token got UAC-filtered (non-admin token) even though the account is
        /// an admin — a known behavior when launching a process with explicit credentials.
        /// Point the user at the two real fixes instead of leaving a bare error.
        /// </summary>
        private static string AppendAccessDeniedHint(string detail)
        {
            if (detail.IndexOf("Access is denied", StringComparison.OrdinalIgnoreCase) < 0)
                return detail;

            return detail +
                "\n\nThis usually means the account's admin token got filtered by UAC, even though " +
                "the password is correct. Either use the built-in \"Administrator\" account, or grant " +
                "this account explicit rights on the service directly (sc sdset) so it doesn't need to " +
                "be an admin at all — see the README for the exact command.";
        }

        public static (bool success, string message) Start(string serviceName, StoredCredential credential) =>
            RunAction(serviceName, "start", credential);

        public static (bool success, string message) Stop(string serviceName, StoredCredential credential) =>
            RunAction(serviceName, "stop", credential);

        public static (bool success, string message) Pause(string serviceName, StoredCredential credential) =>
            RunAction(serviceName, "pause", credential);

        public static (bool success, string message) Resume(string serviceName, StoredCredential credential) =>
            RunAction(serviceName, "continue", credential);

        /// <summary>
        /// Quick credential check: tries to query the SCM as the given user.
        /// Returns success/failure so the Credentials form can give immediate feedback.
        /// </summary>
        public static (bool success, string message) TestCredential(StoredCredential credential)
        {
            if (!credential.IsConfigured)
                return (false, "Enter a username first.");

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = "query state= all",
                    UserName = credential.Username,
                    Domain = string.IsNullOrWhiteSpace(credential.Domain) ? Environment.MachineName : credential.Domain,
                    PasswordInClearText = credential.Password,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    LoadUserProfile = false
                };

                using var process = Process.Start(psi);
                if (process == null) return (false, "Could not start sc.exe.");

                string error = process.StandardError.ReadToEnd();
                process.WaitForExit(10000);

                return process.ExitCode == 0
                    ? (true, "Credential works.")
                    : (false, string.IsNullOrWhiteSpace(error) ? $"Failed (exit code {process.ExitCode})." : error.Trim());
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
