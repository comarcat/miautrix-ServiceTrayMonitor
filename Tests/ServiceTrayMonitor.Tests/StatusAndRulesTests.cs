using System.ServiceProcess;
using ServiceTrayMonitor.Models;
using ServiceTrayMonitor.Services;
using Xunit;

namespace ServiceTrayMonitor.Tests
{
    public class StatusAndRulesTests
    {
        private static MonitoredService Svc(ServiceControllerStatus status, bool exists = true, bool canPause = false) =>
            new() { ServiceName = "svc", DisplayName = "Svc", Status = status, Exists = exists, CanPauseAndContinue = canPause };

        [Theory]
        [InlineData(ServiceControllerStatus.Running, true, 46, 204, 113)]
        [InlineData(ServiceControllerStatus.Stopped, true, 231, 76, 60)]
        [InlineData(ServiceControllerStatus.Paused, true, 241, 196, 15)]
        [InlineData(ServiceControllerStatus.StartPending, true, 52, 152, 219)]
        [InlineData(ServiceControllerStatus.StopPending, true, 52, 152, 219)]
        public void StatusColor_MatchesPalette(ServiceControllerStatus status, bool exists, int r, int g, int b)
        {
            var color = Svc(status, exists).StatusColor;
            Assert.Equal((r, g, b), (color.R, color.G, color.B));
        }

        // DEF-012: a service that no longer exists showed a red dot; the README documents gray.
        [Fact]
        public void StatusColor_NotFound_IsGray()
        {
            var svc = Svc(ServiceControllerStatus.Stopped, exists: false);
            Assert.Equal(StatusPalette.Unknown, svc.StatusColor);
            Assert.Equal("Not found", svc.StatusText);
        }

        [Fact]
        public void SummaryColor_Empty_IsGray() =>
            Assert.Equal(StatusPalette.Unknown, IconFactory.SummaryColor(Array.Empty<MonitoredService>()));

        [Fact]
        public void SummaryColor_AnyStoppedOrMissing_IsRed()
        {
            Assert.Equal(StatusPalette.Stopped, IconFactory.SummaryColor(new[] { Svc(ServiceControllerStatus.Running), Svc(ServiceControllerStatus.Stopped) }));
            Assert.Equal(StatusPalette.Stopped, IconFactory.SummaryColor(new[] { Svc(ServiceControllerStatus.Running), Svc(ServiceControllerStatus.Running, exists: false) }));
        }

        [Fact]
        public void SummaryColor_PendingOrPaused_IsAmber()
        {
            Assert.Equal(StatusPalette.Paused, IconFactory.SummaryColor(new[] { Svc(ServiceControllerStatus.Running), Svc(ServiceControllerStatus.StartPending) }));
            Assert.Equal(StatusPalette.Paused, IconFactory.SummaryColor(new[] { Svc(ServiceControllerStatus.Paused) }));
        }

        [Fact]
        public void SummaryColor_AllRunning_IsGreen() =>
            Assert.Equal(StatusPalette.Running, IconFactory.SummaryColor(new[] { Svc(ServiceControllerStatus.Running), Svc(ServiceControllerStatus.Running) }));

        [Fact]
        public void ActionRules_Running_PausableService()
        {
            var svc = Svc(ServiceControllerStatus.Running, canPause: true);
            Assert.False(ServiceActionRules.CanStart(svc));
            Assert.True(ServiceActionRules.CanStop(svc));
            Assert.True(ServiceActionRules.CanPause(svc));
            Assert.False(ServiceActionRules.CanResume(svc));
        }

        [Fact]
        public void ActionRules_Stopped()
        {
            var svc = Svc(ServiceControllerStatus.Stopped, canPause: true);
            Assert.True(ServiceActionRules.CanStart(svc));
            Assert.False(ServiceActionRules.CanStop(svc));
            Assert.False(ServiceActionRules.CanPause(svc));
            Assert.False(ServiceActionRules.CanResume(svc));
        }

        [Fact]
        public void ActionRules_Paused_OffersResume()
        {
            var svc = Svc(ServiceControllerStatus.Paused, canPause: true);
            Assert.True(ServiceActionRules.CanResume(svc));
            Assert.False(ServiceActionRules.CanPause(svc));
        }

        [Fact]
        public void ActionRules_RunningNonPausable_NoPause() =>
            Assert.False(ServiceActionRules.CanPause(Svc(ServiceControllerStatus.Running, canPause: false)));

        [Fact]
        public void ActionRules_NotFound_OffersNothing()
        {
            var svc = Svc(ServiceControllerStatus.Stopped, exists: false);
            Assert.False(ServiceActionRules.CanStart(svc) || ServiceActionRules.CanStop(svc) ||
                         ServiceActionRules.CanPause(svc) || ServiceActionRules.CanResume(svc));
        }
    }
}
