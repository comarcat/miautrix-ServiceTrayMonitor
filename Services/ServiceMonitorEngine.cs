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
        private List<string> _monitoredNames = new();

        public List<MonitoredService> CurrentStatuses { get; private set; } = new();

        /// <summary>Raised after each poll with the freshest statuses.</summary>
        public event Action<List<MonitoredService>>? StatusesUpdated;

        public ServiceMonitorEngine(int pollIntervalSeconds)
        {
            _timer = new System.Timers.Timer(Math.Max(1, pollIntervalSeconds) * 1000);
            _timer.Elapsed += (_, _) => RefreshNow();
            _timer.AutoReset = true;
        }

        public void UpdateMonitoredList(List<string> serviceNames)
        {
            _monitoredNames = serviceNames;
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
        /// Queries the SCM for every monitored service and updates CurrentStatuses.
        /// Safe to call from any thread; the event handler is responsible for
        /// marshalling back to the UI thread if needed.
        /// </summary>
        public void RefreshNow()
        {
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

        public static List<ServiceController> GetAllServices()
        {
            return ServiceController.GetServices()
                .OrderBy(s => s.DisplayName)
                .ToList();
        }

        public (bool success, string message) StartService(string name)
        {
            var cred = ConfigManager.LoadCredential();
            if (cred.IsConfigured)
                return ElevatedServiceRunner.Start(name, cred);

            try
            {
                using var sc = new ServiceController(name);
                if (sc.Status is ServiceControllerStatus.Running or ServiceControllerStatus.StartPending)
                    return (true, "Already running.");

                sc.Start();
                sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(15));
                return (true, "Started.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public (bool success, string message) StopService(string name)
        {
            var cred = ConfigManager.LoadCredential();
            if (cred.IsConfigured)
                return ElevatedServiceRunner.Stop(name, cred);

            try
            {
                using var sc = new ServiceController(name);
                if (sc.Status == ServiceControllerStatus.Stopped)
                    return (true, "Already stopped.");

                sc.Stop();
                sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(15));
                return (true, "Stopped.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public (bool success, string message) PauseService(string name)
        {
            var cred = ConfigManager.LoadCredential();
            if (cred.IsConfigured)
                return ElevatedServiceRunner.Pause(name, cred);

            try
            {
                using var sc = new ServiceController(name);
                if (!sc.CanPauseAndContinue)
                    return (false, "This service does not support pause/continue.");

                sc.Pause();
                sc.WaitForStatus(ServiceControllerStatus.Paused, TimeSpan.FromSeconds(15));
                return (true, "Paused.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public (bool success, string message) ResumeService(string name)
        {
            var cred = ConfigManager.LoadCredential();
            if (cred.IsConfigured)
                return ElevatedServiceRunner.Resume(name, cred);

            try
            {
                using var sc = new ServiceController(name);
                sc.Continue();
                sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(15));
                return (true, "Resumed.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public void Dispose()
        {
            _timer.Stop();
            _timer.Dispose();
        }
    }
}
