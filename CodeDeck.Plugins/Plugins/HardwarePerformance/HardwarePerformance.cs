using System.Globalization;
using System.Runtime.Versioning;

namespace CodeDeck.Plugins.Plugins.HardwarePerformance;

[SupportedOSPlatform("windows")]
public class HardwarePerformance : CodeDeckPlugin
{
    private const string MonitorSharedStateKey = "HardwarePerformance.Monitor";

    private static HardwarePerformanceMonitor GetMonitor(PluginContext context) =>
        context.GetOrCreateSharedState(
            MonitorSharedStateKey,
            () => new HardwarePerformanceMonitor(context));

    public class CpuUsageTile : PerformanceTile
    {
        [Setting] public string? Format { get; set; }

        private protected override string FormatSnapshot(HardwarePerformanceSnapshot snapshot) =>
            snapshot.CpuUsagePercent is { } usage
                ? FormatValue(Format, "CPU\n{0} %", Math.Round(usage))
                : "CPU\nN/A";
    }

    public class MemoryUsageTile : PerformanceTile
    {
        [Setting] public string? Format { get; set; }

        private protected override string FormatSnapshot(HardwarePerformanceSnapshot snapshot)
        {
            var usedGigabytes = snapshot.UsedPhysicalMemoryBytes / 1024d / 1024d / 1024d;
            var totalGigabytes = snapshot.TotalPhysicalMemoryBytes / 1024d / 1024d / 1024d;

            return FormatValue(
                Format,
                "RAM\n{0} %",
                snapshot.MemoryUsagePercent,
                usedGigabytes,
                totalGigabytes);
        }
    }

    public class GpuUsageTile : PerformanceTile
    {
        [Setting] public string? Format { get; set; }

        private protected override string FormatSnapshot(HardwarePerformanceSnapshot snapshot) =>
            snapshot.GpuUsagePercent is { } usage
                ? FormatValue(Format, "GPU\n{0} %", Math.Round(usage))
                : "GPU\nN/A";
    }

    public abstract class PerformanceTile : Tile
    {
        private HardwarePerformanceMonitor? _monitor;
        private bool _isDeinitialized;

        public override Task Init(CancellationToken cancellationToken)
        {
            _monitor = GetMonitor(PluginContext);
            ApplySnapshot(_monitor.Subscribe(ApplySnapshot));
            return Task.CompletedTask;
        }

        public override Task DeInit()
        {
            _isDeinitialized = true;
            _monitor?.Unsubscribe(ApplySnapshot);
            _monitor = null;
            return Task.CompletedTask;
        }

        private protected abstract string FormatSnapshot(HardwarePerformanceSnapshot snapshot);

        protected static string FormatValue(string? format, string defaultFormat, params object[] arguments)
        {
            try
            {
                return string.Format(CultureInfo.CurrentCulture, format ?? defaultFormat, arguments);
            }
            catch (FormatException)
            {
                return string.Format(CultureInfo.CurrentCulture, defaultFormat, arguments);
            }
        }

        private void ApplySnapshot(HardwarePerformanceSnapshot snapshot)
        {
            if (_isDeinitialized) return;
            Text = FormatSnapshot(snapshot);
        }
    }
}
