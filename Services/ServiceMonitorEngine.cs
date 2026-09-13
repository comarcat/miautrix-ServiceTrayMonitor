using System.ServiceProcess;
using ServiceTrayMonitor.Models;

namespace ServiceTrayMonitor.Services
{
    /// <summary>
    /// Polls the Windows Service Control Manager for the status of every service
    /// the user has chosen to monitor, and exposes actions to control them.
    /// </summary>
    public class ServiceMonitorEngine : IDisposable
    {
        private readonly System.Timers.Timer _timer;
        private readonly object _refreshLock = new();
        private volatile List<string> _monitoredNames = new();
        private volatile bool _disposed;

        public List<MonitoredService> CurrentStatuses { get; private set; } = new();

        /// <summary>Raised on a thread-pool thread after each poll with the freshest statuses.</summary>
        public event Action<List<MonitoredService>>? StatusesUpdated;

        public ServiceMonitorEngine(int pollIntervalSeconds)
        {
            _timer = new System.Timers.Timer(Math.Max(1, pollIntervalSeconds) * 1000);
            _timer.Elapsed += (_, _) => PollTick();
            _timer.AutoReset = true;
        }

        public void UpdateMonitoredList(IEnumerable<string> serviceNames)
        {
            _monitoredNames = serviceNames.ToList(); // own copy — callers may keep mutating theirs
            RefreshNow();
        }

        public void SetPollInterval(int seconds)
        {
            _timer.Interval = Math.Max(1, seconds) * 1000;
        }

        public void Start()
        {
            RefreshNow();
            _timer.Start();
        }

        public void Stop() => _timer.Stop();

        /// <summary>
        /// Queues a status refresh on the thread pool and returns immediately, so the UI never waits
        /// on the SCM. Refreshes run one at a time; the event handler is responsible for
        /// marshalling back to the UI thread.
        /// </summary>
        public void RefreshNow() => Task.Run(() =>
        {
            lock (_refreshLock)
                PollOnce();
        });

        private void PollTick()
        {
            // A tick that arrives while a refresh is still running is skipped, so slow SCM
            // queries can never pile up behind each other.
            if (!Monitor.TryEnter(_refreshLock))
                return;

            try { PollOnce(); }
            finally { Monitor.Exit(_refreshLock); }
        }

        private void PollOnce()
        {
            if (_disposed)
                return;

            var results = new List<MonitoredService>();

            foreach (var name in _monitoredNames)
            {
                var info = new MonitoredService { ServiceName = name };
                try
                {
                    using var sc = new ServiceController(name);
                    sc.Refresh();
                    info.DisplayName = sc.DisplayName;
                    info.Status = sc.Status;
                    info.CanPauseAndContinue = sc.CanPauseAndContinue;
                    info.Exists = true;
                }
                catch
                {
                    info.DisplayName = name;
                    info.Exists = false;
                }

                results.Add(info);
            }

            CurrentStatuses = results;
            StatusesUpdated?.Invoke(results);
        }

        public static List<InstalledService> GetAllServices()
        {
            var services = ServiceController.GetServices();
            try
            {
                return services
                    .Select(s => new InstalledService(s.ServiceName, s.DisplayName))
                    .OrderBy(s => s.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            }
            finally
            {
                foreach (var sc in services)
                    sc.Dispose();
            }
        }

        // Service actions block until the service reaches its target state (up to
        // ServiceActions.WaitTimeout) — call them off the UI thread.
        public (bool success, string message) StartService(string name) => Execute(name, ServiceActions.Start);

        public (bool success, string message) StopService(string name) => Execute(name, ServiceActions.Stop);

        public (bool success, string message) PauseService(string name) => Execute(name, ServiceActions.Pause);

        public (bool success, string message) ResumeService(string name) => Execute(name, ServiceActions.Resume);

        /// <summary>
        /// Runs the action with the app's own (elevated) token first. If Windows denies access and an
        /// admin credential is saved, the same action is retried as that account.
        /// </summary>
        private static (bool success, string message) Execute(string serviceName, Func<ServiceController, (bool success, string message)> action)
        {
            try
            {
                return RunAction(serviceName, action);
            }
            catch (Exception ex) when (ServiceActions.IsAccessDenied(ex))
            {
                var credential = ConfigManager.LoadCredential();
                if (!credential.IsConfigured)
                    return (false, "Access is denied. Save an account with rights to control this service under Admin Credentials, then try again.");

                return ElevatedServiceRunner.Run(credential, () => RunAction(serviceName, action));
            }
            catch (Exception ex)
            {
                return (false, ServiceActions.Describe(ex));
            }
        }

        private static (bool success, string message) RunAction(string serviceName, Func<ServiceController, (bool success, string message)> action)
        {
            using var sc = new ServiceController(serviceName);
            return action(sc);
        }

        public void Dispose()
        {
            _disposed = true;
            _timer.Stop();
            _timer.Dispose();
        }
    }
}
