using CodeDeck.PluginAbstractions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace CodeDeck.Plugins.Plugins.Counter;

[SupportedOSPlatform("windows")]
public class PerformanceCounters : CodeDeckPlugin
{
    public class CpuUsageTile : Tile
    {
        private bool _deInit = false;

        private PerformanceCounter _cpuUsageCounter = new("Processor Information", "% Processor Utility", "_Total");

        public override async Task Init(CancellationToken cancellationToken)
        {
            _ = Task.Run(UpdateTile);
            await Task.CompletedTask;
        }

        private async Task UpdateTile()
        {
            while (!_deInit)
            {
                var cpuUsage = (int)_cpuUsageCounter.NextValue();
                Text = $"CPU\n{cpuUsage} %";
                await Task.Delay(1000);
            }
        }

        public override Task DeInit()
        {
            _deInit = true;
            return base.DeInit();
        }

        public override async Task OnTilePressDown(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
    }

    public class MemoryUsageTile : Tile
    {
        private bool _deInit = false;

        private PerformanceCounter _memUsageCounter = new("Memory", "% Committed Bytes In Use");

        public override async Task Init(CancellationToken cancellationToken)
        {
            _ = Task.Run(UpdateTile);
            await Task.CompletedTask;
        }

        private async Task UpdateTile()
        {
            while (!_deInit)
            {
                var memUsage = (int)_memUsageCounter.NextValue();
                Text = $"RAM\n{memUsage} %";
                await Task.Delay(1000);
            }
        }

        public override Task DeInit()
        {
            _deInit = true;
            return base.DeInit();
        }

        public override async Task OnTilePressDown(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
    }

    public class GpuUsageTile : Tile
    {
        private bool _deInit = false;

        public override async Task Init(CancellationToken cancellationToken)
        {
            _ = Task.Run(UpdateTile);
            await Task.CompletedTask;
        }

        private async Task UpdateTile()
        {
            while (!_deInit)
            {
                var usage = (int) await GetGpuUsage();
                Text = $"GPU\n{usage} %";
                await Task.Delay(1000);
            }
        }

        public override Task DeInit()
        {
            _deInit = true;
            return base.DeInit();
        }

        public override async Task OnTilePressDown(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Taken from:
        /// https://github.com/rocksdanister/lively/blob/d4972447531a0a670ad8f8c4724c7faf7c619d8b/src/livelywpf/livelywpf/Helpers/HWUsageMonitor.cs#L143
        /// </summary>
        /// <returns></returns>
        private async Task<float> GetGpuUsage()
        {
            try
            {
                var category = new PerformanceCounterCategory("GPU Engine");
                var counterNames = category.GetInstanceNames();
                var gpuCounters = new List<PerformanceCounter>();
                var result = 0f;

                foreach (string counterName in counterNames)
                {
                    if (counterName.EndsWith("engtype_3D"))
                    {
                        foreach (PerformanceCounter counter in category.GetCounters(counterName))
                        {
                            if (counter.CounterName == "Utilization Percentage")
                            {
                                gpuCounters.Add(counter);
                            }
                        }
                    }
                }

                gpuCounters.ForEach(x =>
                {
                    _ = x.NextValue();
                });

                await Task.Delay(1000);

                gpuCounters.ForEach(x =>
                {
                    result += x.NextValue();
                });

                return result;
            }
            catch
            {
                return 0f;
            }
        }
    }
}
