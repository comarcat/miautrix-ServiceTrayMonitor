using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;
using ServiceTrayMonitor.Models;

namespace ServiceTrayMonitor.Services
{
    /// <summary>
    /// Runs service control actions as the stored admin credential. ServiceMonitorEngine uses it
    /// when the app's own token is refused ("Access is denied").
    ///
    /// Implementation note: the account is signed in with LogonUser and impersonated around a normal
    /// ServiceController call — no child process, no redirected pipes, no password on a command line.
    /// A batch logon is requested first: Windows doesn't apply UAC token filtering to batch logons,
    /// so an administrator account keeps its full token. If the account lacks the "Log on as a batch
    /// job" right, an interactive logon is used instead, which UAC may filter — the Test button
    /// reports whether a full administrator token was obtained.
    /// </summary>
    public static class ElevatedServiceRunner
    {
        private const int Logon32LogonInteractive = 2;
        private const int Logon32LogonBatch = 4;
        private const int Logon32ProviderDefault = 0;
        private const int ErrorLogonTypeNotGranted = 1385;
        private const uint ScManagerAllAccess = 0xF003F;

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool LogonUser(string username, string? domain, string password,
            int logonType, int logonProvider, out SafeAccessTokenHandle token);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr OpenSCManager(string? machineName, string? databaseName, uint desiredAccess);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool CloseServiceHandle(IntPtr handle);

        public static (bool success, string message) Run(StoredCredential credential, Func<(bool success, string message)> action)
        {
            var (token, error) = Logon(credential);
            if (token == null)
                return (false, error);

            using (token)
            {
                try
                {
                    return WindowsIdentity.RunImpersonated(token, action);
                }
                catch (Exception ex) when (ServiceActions.IsAccessDenied(ex))
                {
                    return (false,
                        $"Access is denied for {credential.DisplayLogin} as well. Windows may have given this account " +
                        "a filtered (non-administrator) token, or the account has no rights on this service. Use Test " +
                        "under Admin Credentials to check the token, or grant the account rights on the service " +
                        "(see the README).");
                }
                catch (Exception ex)
                {
                    return (false, ServiceActions.Describe(ex));
                }
            }
        }

        /// <summary>
        /// Signs the account in and checks that it holds a full administrator token with full access
        /// to the Service Control Manager. Returns success/failure so the Credentials form can give
        /// immediate feedback.
        /// </summary>
        public static (bool success, string message) TestCredential(StoredCredential credential)
        {
            var (token, error) = Logon(credential);
            if (token == null)
                return (false, error);

            using (token)
            using (var identity = new WindowsIdentity(token.DangerousGetHandle()))
            {
                bool adminToken = new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
                bool fullScmAccess = WindowsIdentity.RunImpersonated(token, () =>
                {
                    IntPtr scm = OpenSCManager(null, null, ScManagerAllAccess);
                    if (scm == IntPtr.Zero)
                        return false;
                    CloseServiceHandle(scm);
                    return true;
                });

                return adminToken && fullScmAccess
                    ? (true, $"Credential works: {credential.DisplayLogin} signed in with a full administrator token.")
                    : (false, $"Signed in as {credential.DisplayLogin}, but without a full administrator token. " +
                              "Actions will only work on services this account was granted rights to (see the README).");
            }
        }

        private static (SafeAccessTokenHandle? token, string error) Logon(StoredCredential credential)
        {
            if (!credential.IsConfigured)
                return (null, "No admin credential is configured.");

            // UPN logins (user@domain) need a null domain; a blank domain means a local account.
            string? domain = credential.Username.Contains('@') ? null
                : string.IsNullOrWhiteSpace(credential.Domain) ? "." : credential.Domain;

            if (LogonUser(credential.Username, domain, credential.Password, Logon32LogonBatch, Logon32ProviderDefault, out var batchToken))
                return (batchToken, string.Empty);

            int error = Marshal.GetLastWin32Error();
            batchToken.Dispose();

            if (error == ErrorLogonTypeNotGranted)
            {
                if (LogonUser(credential.Username, domain, credential.Password, Logon32LogonInteractive, Logon32ProviderDefault, out var interactiveToken))
                    return (interactiveToken, string.Empty);

                error = Marshal.GetLastWin32Error();
                interactiveToken.Dispose();
            }

            return (null, $"Sign-in failed for {credential.DisplayLogin}: {new Win32Exception(error).Message}");
        }
    }
}
