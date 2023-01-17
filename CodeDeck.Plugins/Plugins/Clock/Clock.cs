using CodeDeck.PluginAbstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CodeDeck.Plugins.Plugins.Counter;

public class Clock : CodeDeckPlugin
{
    public class DigitalClockTile : Tile
    {
        private string? _format;
        private int _interval = 1000;

        public override async Task Init(CancellationToken cancellationToken)
        {
            if (Settings?.TryGetValue("format", out var format) ?? false)
            {
                _format = format;
            } else
            {
                _format = "HH\\:mm";
            }

            if (Settings?.TryGetValue("interval", out var interval) ?? false)
            {
                if (int.TryParse(interval, out var i)) _interval = i;
            }

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

                Text = DateTime.Now.ToString(_format);
                await Task.Delay(_interval, cancellationToken);
            }
        }

        public override async Task OnTilePressDown(CancellationToken cancellationToken)
        {
            Text = DateTime.Now.ToString(_format);

            await Task.CompletedTask;
        }
    }
}
