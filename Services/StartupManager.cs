using System.Diagnostics;
using System.Security;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32;

namespace ServiceTrayMonitor.Services
{
    /// <summary>
    /// "Start automatically when Windows starts". The app requires administrator rights, and
    /// Windows silently skips elevated programs listed under the HKCU Run key at logon — so startup
    /// uses a per-user Task Scheduler task that runs at logon with highest privileges instead.
    /// </summary>
    public static class StartupManager
    {
        private const string LegacyRunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string LegacyRunValueName = "ServiceTrayMonitor";

        internal static string TaskName => $"ServiceTrayMonitor ({Environment.UserName})";

        public static (bool success, string message) Apply(bool enabled)
        {
            RemoveLegacyRunEntry();

            try
            {
                return enabled ? CreateOrUpdateTask() : DeleteTask();
            }
            catch (Exception ex)
            {
                return (false, $"Couldn't update the startup task: {ex.Message}");
            }
        }

        /// <summary>Removes the Run-key entry written by version 1.0.0, which never started the elevated app.</summary>
        public static void RemoveLegacyRunEntry()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(LegacyRunKey, writable: true);
                if (key?.GetValue(LegacyRunValueName) != null)
                    key.DeleteValue(LegacyRunValueName);
            }
            catch
            {
                // Non-fatal: a leftover Run entry is inert.
            }
        }

        private static (bool success, string message) CreateOrUpdateTask()
        {
            string exePath = Environment.ProcessPath ?? Application.ExecutablePath;
            string userId = WindowsIdentity.GetCurrent().Name;
            string xmlPath = Path.Combine(Path.GetTempPath(), $"ServiceTrayMonitor-task-{Guid.NewGuid():N}.xml");

            try
            {
                // schtasks reads task XML as UTF-16, matching the declaration in BuildTaskXml.
                File.WriteAllText(xmlPath, BuildTaskXml(exePath, userId), Encoding.Unicode);
                var (exitCode, output) = RunSchtasks($"/Create /TN \"{TaskName}\" /XML \"{xmlPath}\" /F");
                return exitCode == 0
                    ? (true, "Startup task registered.")
                    : (false, $"Couldn't register the startup task: {output}");
            }
            finally
            {
                try { File.Delete(xmlPath); } catch { /* temp file; ignore */ }
            }
        }

        private static (bool success, string message) DeleteTask()
        {
            if (RunSchtasks($"/Query /TN \"{TaskName}\"").exitCode != 0)
                return (true, "No startup task to remove.");

            var (exitCode, output) = RunSchtasks($"/Delete /TN \"{TaskName}\" /F");
            return exitCode == 0
                ? (true, "Startup task removed.")
                : (false, $"Couldn't remove the startup task: {output}");
        }

        /// <summary>
        /// Logon-triggered task for one user, run with that user's interactive token at highest
        /// privileges. ExecutionTimeLimit PT0S disables the default 72-hour limit, which would
        /// otherwise end the tray app after three days.
        /// </summary>
        internal static string BuildTaskXml(string exePath, string userId)
        {
            string exe = SecurityElement.Escape(exePath);
            string user = SecurityElement.Escape(userId);

            return $"""
                <?xml version="1.0" encoding="UTF-16"?>
                <Task version="1.2" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
                  <RegistrationInfo>
                    <Description>Starts Service Tray Monitor at logon with administrator rights.</Description>
                  </RegistrationInfo>
                  <Triggers>
                    <LogonTrigger>
                      <Enabled>true</Enabled>
                      <UserId>{user}</UserId>
                    </LogonTrigger>
                  </Triggers>
                  <Principals>
                    <Principal id="Author">
                      <UserId>{user}</UserId>
                      <LogonType>InteractiveToken</LogonType>
                      <RunLevel>HighestAvailable</RunLevel>
                    </Principal>
                  </Principals>
                  <Settings>
                    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
                    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
                    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
                    <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>
                  </Settings>
                  <Actions Context="Author">
                    <Exec>
                      <Command>{exe}</Command>
                    </Exec>
                  </Actions>
                </Task>
                """;
        }

        private static (int exitCode, string output) RunSchtasks(string arguments)
        {
            var psi = new ProcessStartInfo("schtasks.exe", arguments)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
                return (-1, "Could not start schtasks.exe.");

            // Drain both pipes concurrently so a full buffer can never block the child process.
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit(15000))
            {
                try { process.Kill(); } catch { /* already exited */ }
                return (-1, "schtasks.exe timed out.");
            }

            return (process.ExitCode, $"{stderr.Result} {stdout.Result}".Trim());
        }
    }
}
