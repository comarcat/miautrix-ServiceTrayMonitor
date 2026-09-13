using ServiceTrayMonitor.Models;
using Xunit;

namespace ServiceTrayMonitor.Tests
{
    // DEF-002: filtering lost checks, and Save dropped monitored services hidden by the filter.
    public class ServiceSelectionTests
    {
        private static readonly string[] Installed = { "Alpha", "Bravo", "Charlie" };

        [Fact]
        public void ChecksMadeWhileFiltered_AreKept()
        {
            var selection = new ServiceSelection(new[] { "Alpha" });

            // User filters down to Bravo and checks it; Alpha is hidden but must survive.
            selection.Set("Bravo", true);

            Assert.Equal(new[] { "Alpha", "Bravo" }, selection.ToOrderedList(Installed));
        }

        [Fact]
        public void MonitoredButNotInstalled_IsKept()
        {
            var selection = new ServiceSelection(new[] { "Removed", "Charlie" });

            Assert.Equal(new[] { "Charlie", "Removed" }, selection.ToOrderedList(Installed));
        }

        [Fact]
        public void Uncheck_IsCaseInsensitive_AndNoDuplicates()
        {
            var selection = new ServiceSelection(new[] { "Alpha", "ALPHA", "Bravo" });
            selection.Set("alpha", false);

            Assert.False(selection.IsChecked("Alpha"));
            Assert.Equal(new[] { "Bravo" }, selection.ToOrderedList(Installed));
        }

        [Fact]
        public void SavedOrder_FollowsDisplayOrder()
        {
            var selection = new ServiceSelection(new[] { "Charlie", "Alpha" });

            Assert.Equal(new[] { "Alpha", "Charlie" }, selection.ToOrderedList(Installed));
        }

        [Theory]
        [InlineData("", true)]
        [InlineData("  ", true)]
        [InlineData("spool", true)]
        [InlineData("PRINT", true)]
        [InlineData("dns", false)]
        public void MatchesFilter_DisplayOrServiceName(string filter, bool expected) =>
            Assert.Equal(expected, ServiceSelection.MatchesFilter(new InstalledService("Spooler", "Print Spooler"), filter));
    }
}
