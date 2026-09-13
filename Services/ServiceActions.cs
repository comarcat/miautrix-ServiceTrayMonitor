using System.ComponentModel;
using System.ServiceProcess;

namespace ServiceTrayMonitor.Services
{
    /// <summary>
    /// The four service control actions, with identical semantics whichever account runs them
    /// (the app's own token, or the stored admin credential via impersonation). Each action waits
    /// for the target state, so "success" always means the service actually got there.
    /// </summary>
    public static class ServiceActions
    {
        public static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(15);

        private const int ErrorAccessDenied = 5;

        public static (bool success, string message) Start(ServiceController sc)
        {
            sc.Refresh();
            switch (sc.Status)
            {
                case ServiceControllerStatus.Running:
                    return (true, "Already running.");
                case ServiceControllerStatus.Paused:
                    return (false, "The service is paused. Use Resume instead.");
                case ServiceControllerStatus.StartPending:
                    break;
                default:
                    sc.Start();
                    break;
            }

            return WaitFor(sc, ServiceControllerStatus.Running, "Started.");
        }

        public static (bool success, string message) Stop(ServiceController sc)
        {
            sc.Refresh();
            if (sc.Status == ServiceControllerStatus.Stopped)
                return (true, "Already stopped.");

            if (sc.Status != ServiceControllerStatus.StopPending)
                sc.Stop();

            return WaitFor(sc, ServiceControllerStatus.Stopped, "Stopped.");
        }

        public static (bool success, string message) Pause(ServiceController sc)
        {
            sc.Refresh();
            if (!sc.CanPauseAndContinue)
                return (false, "This service does not support pause/continue.");
            if (sc.Status == ServiceControllerStatus.Paused)
                return (true, "Already paused.");

            if (sc.Status != ServiceControllerStatus.PausePending)
                sc.Pause();

            return WaitFor(sc, ServiceControllerStatus.Paused, "Paused.");
        }

        public static (bool success, string message) Resume(ServiceController sc)
        {
            sc.Refresh();
            if (sc.Status == ServiceControllerStatus.Running)
                return (true, "Already running.");

            if (sc.Status != ServiceControllerStatus.ContinuePending)
                sc.Continue();

            return WaitFor(sc, ServiceControllerStatus.Running, "Resumed.");
        }

        /// <summary>True when the exception (or any inner exception) is a Windows "Access is denied".</summary>
        public static bool IsAccessDenied(Exception ex)
        {
            for (Exception? e = ex; e != null; e = e.InnerException)
            {
                if (e is UnauthorizedAccessException || e is Win32Exception { NativeErrorCode: ErrorAccessDenied })
                    return true;
            }
            return false;
        }

        /// <summary>ServiceController wraps the Win32 reason in an inner exception; surface both.</summary>
        public static string Describe(Exception ex) =>
            ex.InnerException is Win32Exception win32 && !ex.Message.Contains(win32.Message, StringComparison.OrdinalIgnoreCase)
                ? $"{ex.Message} {win32.Message}"
                : ex.Message;

        private static (bool success, string message) WaitFor(ServiceController sc, ServiceControllerStatus target, string successMessage)
        {
            try
            {
                sc.WaitForStatus(target, WaitTimeout);
                return (true, successMessage);
            }
            catch (System.ServiceProcess.TimeoutException)
            {
                sc.Refresh();
                return (false, $"The request was accepted, but the service is still {sc.Status} after {WaitTimeout.TotalSeconds:0} seconds.");
            }
        }
    }
}
