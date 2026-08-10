using System.Diagnostics;
using System.Runtime.Versioning;

namespace CodeDeck.Plugins.Plugins.Counter;

[SupportedOSPlatform("windows")]
public class PerformanceCounters : CodeDeckPlugin
{
    public class CpuUsageTile : Tile
    {
        [Setting] public string? Format { get; set; }

        private PerformanceCounter _cpuUsageCounter = new("Processor Information", "% Processor Utility", "_Total");

        public override async Task Init(CancellationToken cancellationToken)
        {
            RunEvery("CPU usage update", TimeSpan.FromSeconds(1), UpdateTile);
            await Task.CompletedTask;
        }

        private Task UpdateTile(CancellationToken cancellationToken)
        {
            var cpuUsage = (int)_cpuUsageCounter.NextValue();
            Text = string.Format(Format ?? "CPU\n{0} %", cpuUsage);
            return Task.CompletedTask;
        }
    }

    public class MemoryUsageTile : Tile
    {
        [Setting] public string? Format { get; set; }

        private PerformanceCounter _memUsageCounter = new("Memory", "% Committed Bytes In Use");

        public override async Task Init(CancellationToken cancellationToken)
        {
            RunEvery("Memory usage update", TimeSpan.FromSeconds(1), UpdateTile);
            await Task.CompletedTask;
        }

        private Task UpdateTile(CancellationToken cancellationToken)
        {
            var memUsage = (int)_memUsageCounter.NextValue();
            Text = string.Format(Format ?? "RAM\n{0} %", memUsage);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Based on:
    /// https://github.com/rocksdanister/lively/blob/d4972447531a0a670ad8f8c4724c7faf7c619d8b/src/livelywpf/livelywpf/Helpers/HWUsageMonitor.cs#L143
    /// </summary>
    public class GpuUsageTile : Tile
    {
        [Setting] public string? Format { get; set; }

        private List<PerformanceCounter> _gpuCounters = new();

        public override async Task Init(CancellationToken cancellationToken)
        {
            var category = new PerformanceCounterCategory("GPU Engine");

            _gpuCounters.AddRange(from string counterName in category.GetInstanceNames()
                                  where counterName.EndsWith("engtype_3D")
                                  from PerformanceCounter counter in category.GetCounters(counterName)
                                  where counter.CounterName == "Utilization Percentage"
                                  select counter);

            RunEvery("GPU usage update", TimeSpan.FromSeconds(1), UpdateTile);
            await Task.CompletedTask;
        }

        private Task UpdateTile(CancellationToken cancellationToken)
        {
            var usage = (int)_gpuCounters.Sum(x => x.NextValue());
            Text = string.Format(Format ?? "GPU\n{0} %", usage);
            return Task.CompletedTask;
        }
    }
}
