using SixLabors.ImageSharp;
using System.Diagnostics;

namespace CodeDeck.Plugins.Plugins.Clock;

public partial class Clock : CodeDeckPlugin
{
    public partial class StopWatchTile : Tile
    {
        [Setting] public string Format { get; set; } = "\\⏱'\n'mm\\:ss";
        [Setting] public int Interval { get; set; } = 100;
        [Setting] public int HoldToResetTime { get; set; } = 1000;

        private readonly Stopwatch _sw = new();

        private DateTime? _timePressDown = null;

        private CancellationTokenSource? _ctsCheckLongPress;
        private CancellationTokenSource? _ctsUpdateTile;


        public override async Task Init(CancellationToken cancellationToken)
        {
            FontSize = 25;
            SetText(TimeSpan.Zero);

            await Task.CompletedTask;
        }

        private void SetText(TimeSpan ts)
        {
            Text = ts.ToString(Format);
        }

        private Task UpdateTile(CancellationToken cancellationToken)
        {
            SetText(_sw.Elapsed);
            return Task.CompletedTask;
        }

        private async Task CheckLongPress(CancellationToken cancellationToken)
        {
            if (_timePressDown is not null && ((DateTime.Now - _timePressDown).Value.TotalMilliseconds >= HoldToResetTime))
            {
                await OnLongPress(cancellationToken);
                _ctsCheckLongPress?.Cancel();
            }
        }

        public override async Task OnTilePressDown(CancellationToken cancellationToken)
        {
            _timePressDown = DateTime.Now;
            _ctsCheckLongPress?.Cancel();
            _ctsCheckLongPress = new CancellationTokenSource();
            RunEvery(
                "Long press check",
                TimeSpan.FromMilliseconds(100),
                CheckLongPress,
                runImmediately: false,
                cancellationToken: _ctsCheckLongPress.Token);

            if (_sw.IsRunning)
            {
                _ctsUpdateTile?.Cancel();
                _sw.Stop();
                ShowIndicator = false;
            }
            else
            {
                _ctsUpdateTile?.Cancel();
                _ctsUpdateTile = new CancellationTokenSource();
                RunEvery(
                    "Stopwatch update",
                    TimeSpan.FromMilliseconds(Interval),
                    UpdateTile,
                    cancellationToken: _ctsUpdateTile.Token);
                _sw.Start();
                ShowIndicator = true;
            }

            SetText(_sw.Elapsed);

            await Task.CompletedTask;
        }

        public override Task OnTilePressUp(CancellationToken cancellationToken)
        {
            _ctsCheckLongPress?.Cancel();
            return base.OnTilePressUp(cancellationToken);
        }

        public override Task DeInit()
        {
            _ctsCheckLongPress?.Cancel();
            _ctsUpdateTile?.Cancel();
            return Task.CompletedTask;
        }

        private async Task OnLongPress(CancellationToken cancellationToken)
        {
            ShowIndicator = true;
            var c = IndicatorColor;
            IndicatorColor = Color.Red;
            await Task.Delay(500, cancellationToken);
            ShowIndicator = false;
            IndicatorColor = c;

            _sw.Reset();
            SetText(_sw.Elapsed);
        }
    }
}
