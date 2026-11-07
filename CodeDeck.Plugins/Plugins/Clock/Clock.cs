namespace CodeDeck.Plugins.Plugins.Clock;

public partial class Clock : CodeDeckPlugin
{
    public partial class DigitalClockTile : Tile
    {
        [Setting] public string? Format { get; set; }
        [Setting] public int? Interval { get; set; }

        public override async Task Init(CancellationToken cancellationToken)
        {
            RunEvery("Clock update", TimeSpan.FromMilliseconds(Interval ?? 1000), UpdateTile);
            await Task.CompletedTask;
        }

        private Task UpdateTile(CancellationToken cancellationToken)
        {
            Text = DateTime.Now.ToString(Format ?? "HH\\:mm");
            return Task.CompletedTask;
        }

        public override async Task OnTilePressDown(CancellationToken cancellationToken)
        {
            Text = DateTime.Now.ToString(Format ?? "HH\\:mm");

            await Task.CompletedTask;
        }
    }
}
