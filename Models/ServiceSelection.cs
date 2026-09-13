namespace ServiceTrayMonitor.Models
{
    /// <summary>A service installed on this machine, as listed in the Manage Monitored Services window.</summary>
    public record InstalledService(string ServiceName, string DisplayName)
    {
        public string Label => $"{DisplayName}  ({ServiceName})";
    }

    /// <summary>
    /// The set of services checked in the Manage Monitored Services window. Kept apart from the
    /// visible (filtered) list, so typing in the filter never loses a check, and services hidden
    /// by the filter — or no longer installed — are still saved.
    /// </summary>
    public class ServiceSelection
    {
        private readonly List<string> _checked = new();

        public ServiceSelection(IEnumerable<string> initiallyChecked)
        {
            foreach (var name in initiallyChecked)
                Set(name, true);
        }

        public bool IsChecked(string serviceName) =>
            _checked.Contains(serviceName, StringComparer.OrdinalIgnoreCase);

        public void Set(string serviceName, bool isChecked)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                return;

            if (isChecked)
            {
                if (!IsChecked(serviceName))
                    _checked.Add(serviceName);
            }
            else
            {
                _checked.RemoveAll(n => string.Equals(n, serviceName, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Checked names in the order they should be saved: installed services in display order,
        /// then any checked names that aren't installed right now, in their original order.
        /// </summary>
        public List<string> ToOrderedList(IEnumerable<string> installedInDisplayOrder)
        {
            var installed = installedInDisplayOrder.ToList();
            var result = installed.Where(IsChecked).ToList();
            result.AddRange(_checked.Where(n => !installed.Contains(n, StringComparer.OrdinalIgnoreCase)));
            return result;
        }

        public static bool MatchesFilter(InstalledService service, string? filter) =>
            string.IsNullOrWhiteSpace(filter) ||
            service.Label.Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
