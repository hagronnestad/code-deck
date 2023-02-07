using CodeDeck.PluginAbstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CodeDeck.Plugins.Plugins.Clock;

public class Clock : CodeDeckPlugin
{
    public class DigitalClockTile : Tile
    {
        [Setting] public string? Format { get; set; }
        [Setting] public int? Interval { get; set; }

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

                Text = DateTime.Now.ToString(Format ?? "HH\\:mm");
                await Task.Delay(Interval ?? 1000, cancellationToken);
            }
        }

        public override async Task OnTilePressDown(CancellationToken cancellationToken)
        {
            Text = DateTime.Now.ToString(Format ?? "HH\\:mm");

            await Task.CompletedTask;
        }
    }
}
