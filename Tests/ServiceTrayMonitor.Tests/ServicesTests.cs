using System.ComponentModel;
using System.Xml.Linq;
using ServiceTrayMonitor.Models;
using ServiceTrayMonitor.Services;
using Xunit;

namespace ServiceTrayMonitor.Tests
{
    // DEF-003: "Start with Windows" used the HKCU Run key, which Windows skips for elevated apps.
    public class StartupManagerTests
    {
        private static readonly XNamespace Ns = "http://schemas.microsoft.com/windows/2004/02/mit/task";

        [Fact]
        public void TaskXml_RunsAtLogonElevated_WithoutTimeLimit()
        {
            var doc = XDocument.Parse(StartupManager.BuildTaskXml(@"C:\Program Files\Miautrix\STM\ServiceTrayMonitor.exe", @"CONTOSO\jdoe"));

            Assert.Equal(@"CONTOSO\jdoe", doc.Descendants(Ns + "LogonTrigger").Single().Element(Ns + "UserId")!.Value);
            Assert.Equal("HighestAvailable", doc.Descendants(Ns + "RunLevel").Single().Value);
            Assert.Equal("InteractiveToken", doc.Descendants(Ns + "LogonType").Single().Value);
            Assert.Equal("PT0S", doc.Descendants(Ns + "ExecutionTimeLimit").Single().Value);
            Assert.Equal(@"C:\Program Files\Miautrix\STM\ServiceTrayMonitor.exe", doc.Descendants(Ns + "Command").Single().Value);
        }

        [Fact]
        public void TaskXml_EscapesSpecialCharacters()
        {
            var doc = XDocument.Parse(StartupManager.BuildTaskXml(@"C:\R&D <tools>\ServiceTrayMonitor.exe", "PC\\a&b"));

            Assert.Equal(@"C:\R&D <tools>\ServiceTrayMonitor.exe", doc.Descendants(Ns + "Command").Single().Value);
            Assert.Equal("PC\\a&b", doc.Descendants(Ns + "UserId").First().Value);
        }
    }

    public class ServiceActionsTests
    {
        [Fact]
        public void IsAccessDenied_DetectsWrappedWin32Error5() =>
            Assert.True(ServiceActions.IsAccessDenied(new InvalidOperationException("Cannot open service", new Win32Exception(5))));

        [Fact]
        public void IsAccessDenied_IgnoresOtherErrors() =>
            Assert.False(ServiceActions.IsAccessDenied(new InvalidOperationException("Not found", new Win32Exception(1060))));

        [Fact]
        public void Describe_IncludesWin32Reason()
        {
            var ex = new InvalidOperationException("Cannot open Foo service on computer '.'.", new Win32Exception(5));
            Assert.Contains(new Win32Exception(5).Message, ServiceActions.Describe(ex));
        }

        // DEF-005: no credential → a clear failure, without attempting a logon.
        [Fact]
        public void CredentialRunner_WithoutCredential_FailsCleanly()
        {
            var (success, message) = ElevatedServiceRunner.Run(new StoredCredential(), () => (true, "should not run"));
            Assert.False(success);
            Assert.Equal("No admin credential is configured.", message);

            Assert.False(ElevatedServiceRunner.TestCredential(new StoredCredential()).success);
        }
    }

    /// <summary>Read-only checks against this machine's Service Control Manager.</summary>
    public class ServiceMonitorEngineTests
    {
        [Fact]
        public async Task Refresh_ReportsExistingAndMissingServices()
        {
            using var engine = new ServiceMonitorEngine(300);
            var updated = new TaskCompletionSource<List<MonitoredService>>(TaskCreationOptions.RunContinuationsAsynchronously);
            engine.StatusesUpdated += s => updated.TrySetResult(s);

            engine.UpdateMonitoredList(new[] { "EventLog", "STM_DoesNotExist_7f3a" });
            var statuses = await updated.Task.WaitAsync(TimeSpan.FromSeconds(15));

            Assert.Equal(2, statuses.Count);
            Assert.True(statuses[0].Exists);
            Assert.False(string.IsNullOrEmpty(statuses[0].DisplayName));
            Assert.False(statuses[1].Exists);
            Assert.Equal("Not found", statuses[1].StatusText);
        }

        // DEF-009: the engine kept a reference to the caller's list.
        [Fact]
        public async Task UpdateMonitoredList_CopiesTheCallersList()
        {
            using var engine = new ServiceMonitorEngine(300);
            var names = new List<string> { "EventLog" };
            var first = new TaskCompletionSource<List<MonitoredService>>(TaskCreationOptions.RunContinuationsAsynchronously);
            engine.StatusesUpdated += s => first.TrySetResult(s);

            engine.UpdateMonitoredList(names);
            await first.Task.WaitAsync(TimeSpan.FromSeconds(15));
            names.Add("Spooler");

            var second = new TaskCompletionSource<List<MonitoredService>>(TaskCreationOptions.RunContinuationsAsynchronously);
            engine.StatusesUpdated += s => second.TrySetResult(s);
            engine.RefreshNow();

            Assert.Single(await second.Task.WaitAsync(TimeSpan.FromSeconds(15)));
        }

        [Fact]
        public void GetAllServices_IsSortedByDisplayName()
        {
            var services = ServiceMonitorEngine.GetAllServices();

            Assert.NotEmpty(services);
            Assert.Equal(services.OrderBy(s => s.DisplayName, StringComparer.CurrentCultureIgnoreCase).Select(s => s.ServiceName),
                         services.Select(s => s.ServiceName));
        }
    }
}
