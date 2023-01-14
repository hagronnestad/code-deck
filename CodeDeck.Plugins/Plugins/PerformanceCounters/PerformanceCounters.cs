using CodeDeck.PluginAbstractions;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace CodeDeck.Plugins.Plugins.Counter;

[SupportedOSPlatform("windows")]
public class PerformanceCounters : CodeDeckPlugin
{
    public class CpuUsageTile : Tile
    {
        private bool _deInit = false;

        private PerformanceCounter _cpuUsageCounter = new("Processor Information", "% Processor Utility", "_Total");

        public override async Task Init()
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

        public override async Task OnTilePressDown()
        {
            await Task.CompletedTask;
        }
    }

    public class MemoryUsageTile : Tile
    {
        private bool _deInit = false;

        private PerformanceCounter _memUsageCounter = new("Memory", "% Committed Bytes In Use");

        public override async Task Init()
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

        public override async Task OnTilePressDown()
        {
            await Task.CompletedTask;
        }
    }
}
