using System.ServiceProcess;

namespace ServiceTrayMonitor.Models
{
    /// <summary>
    /// Single source of truth for status colors — menu dots, grid cells, and the tray icon
    /// summary all read from here, so a palette change applies everywhere at once.
    /// </summary>
    public static class StatusPalette
    {
        public static readonly Color Running = Color.FromArgb(46, 204, 113);   // green
        public static readonly Color Stopped = Color.FromArgb(231, 76, 60);    // red
        public static readonly Color Paused = Color.FromArgb(241, 196, 15);    // amber
        public static readonly Color Pending = Color.FromArgb(52, 152, 219);   // blue
        public static readonly Color Unknown = Color.Gray;                     // not found / nothing configured

        public static Color For(ServiceControllerStatus status, bool exists)
        {
            if (!exists)
                return Unknown;

            return status switch
            {
                ServiceControllerStatus.Running => Running,
                ServiceControllerStatus.Stopped => Stopped,
                ServiceControllerStatus.Paused => Paused,
                _ when IsPending(status) => Pending,
                _ => Unknown
            };
        }

        public static bool IsPending(ServiceControllerStatus status) =>
            status is ServiceControllerStatus.StartPending
                or ServiceControllerStatus.StopPending
                or ServiceControllerStatus.ContinuePending
                or ServiceControllerStatus.PausePending;
    }

    /// <summary>
    /// Which actions are offered for a service in its current state. Shared by the tray menu
    /// and the main window so both always enable the same commands.
    /// </summary>
    public static class ServiceActionRules
    {
        public static bool CanStart(MonitoredService svc) =>
            svc.Exists && svc.Status != ServiceControllerStatus.Running;

        public static bool CanStop(MonitoredService svc) =>
            svc.Exists && svc.Status != ServiceControllerStatus.Stopped;

        public static bool CanPause(MonitoredService svc) =>
            svc.Exists && svc.CanPauseAndContinue && svc.Status == ServiceControllerStatus.Running;

        public static bool CanResume(MonitoredService svc) =>
            svc.Exists && svc.Status == ServiceControllerStatus.Paused;
    }
}
