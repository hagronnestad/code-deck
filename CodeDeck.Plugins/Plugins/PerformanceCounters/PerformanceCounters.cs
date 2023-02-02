using CodeDeck.PluginAbstractions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

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
            _ = Task.Run(() => UpdateTile(cancellationToken), cancellationToken);
            await Task.CompletedTask;
        }

        private async Task UpdateTile(CancellationToken cancellationToken)
        {
            for (; ; )
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var cpuUsage = (int)_cpuUsageCounter.NextValue();
                Text = string.Format(Format ?? "CPU\n{0} %", cpuUsage);
                await Task.Delay(1000, cancellationToken);
            }
        }
    }

    public class MemoryUsageTile : Tile
    {
        [Setting] public string? Format { get; set; }

        private PerformanceCounter _memUsageCounter = new("Memory", "% Committed Bytes In Use");

        public override async Task Init(CancellationToken cancellationToken)
        {
            _ = Task.Run(() => UpdateTile(cancellationToken), cancellationToken);
            await Task.CompletedTask;
        }

        private async Task UpdateTile(CancellationToken cancellationToken)
        {
            for (; ; )
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var memUsage = (int)_memUsageCounter.NextValue();
                Text = string.Format(Format ?? "RAM\n{0} %", memUsage);
                await Task.Delay(1000, cancellationToken);
            }
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

            _ = Task.Run(() => UpdateTile(cancellationToken), cancellationToken);
            await Task.CompletedTask;
        }

        private async Task UpdateTile(CancellationToken cancellationToken)
        {
            for (; ; )
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var usage = (int)_gpuCounters.Sum(x => x.NextValue());
                Text = string.Format(Format ?? "GPU\n{0} %", usage);

                await Task.Delay(1000, cancellationToken);
            }
        }
    }
}
