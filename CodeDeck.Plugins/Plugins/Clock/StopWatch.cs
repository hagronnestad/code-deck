using CodeDeck.PluginAbstractions;
using SixLabors.ImageSharp;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

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
        private CancellationTokenSource? _ctsFrameworkAndCheckLongPressCombined;
        private CancellationTokenSource? _ctsUpdateTile;
        private CancellationTokenSource? _ctsFrameworkAndUpdateTileCombined;


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

        private async Task UpdateTile(CancellationToken cancellationToken)
        {
            for (; ; )
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                SetText(_sw.Elapsed);
                await Task.Delay(Interval, cancellationToken);
            }
        }

        private async Task CheckLongPress(CancellationToken cancellationToken)
        {
            for (; ; )
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (_timePressDown is not null && ((DateTime.Now - _timePressDown).Value.TotalMilliseconds >= HoldToResetTime))
                {
                    await OnLongPress(cancellationToken);
                    return;
                }

                await Task.Delay(100, cancellationToken);
            }
        }

        public override async Task OnTilePressDown(CancellationToken cancellationToken)
        {
            _timePressDown = DateTime.Now;
            _ctsCheckLongPress = new CancellationTokenSource();
            _ctsFrameworkAndCheckLongPressCombined = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _ctsCheckLongPress.Token);
            _ = CheckLongPress(_ctsFrameworkAndCheckLongPressCombined.Token);

            if (_sw.IsRunning)
            {
                _ctsFrameworkAndUpdateTileCombined?.Cancel();
                _sw.Stop();
                ShowIndicator = false;
            }
            else
            {
                _ctsUpdateTile = new CancellationTokenSource();
                _ctsFrameworkAndUpdateTileCombined = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _ctsUpdateTile.Token);
                _ = UpdateTile(_ctsFrameworkAndUpdateTileCombined.Token);
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
