using HidSharp;

/// <summary>
/// Based on rivalcfg: https://github.com/flozz/rivalcfg/blob/master/rivalcfg/devices/rival3_wireless.py
/// </summary>
public class SteelSeriesRival3Wireless : CodeDeckPlugin
{
    public class BatteryTile : Tile
    {
        [Setting] public string? Format { get; set; }
        [Setting] public string? FormatDisconnected { get; set; }
        [Setting] public int? Interval { get; set; }

        private HidDevice? _device;
        private readonly SemaphoreSlim _updateLock = new(1, 1);

        public override async Task Init(CancellationToken cancellationToken)
        {
            _device = FindDevice();

            RunEvery(
                "Battery poll",
                TimeSpan.FromMilliseconds(Interval ?? 10 * 60 * 1000),
                GetAsync,
                cancellationToken: cancellationToken);

            await Task.CompletedTask;
        }

        protected virtual HidDevice? FindDevice()
        {
            return DeviceList.Local
                .GetHidDevices(0x1038, 0x1830)
                // Filter to pick the correct endpoint
                .Where(x => x.GetMaxFeatureReportLength() == 515)
                .Where(x => x.GetMaxInputReportLength() == 65)
                .Where(x => x.GetMaxOutputReportLength() == 65)
                .FirstOrDefault();
        }

        public override async Task OnTilePressUp(CancellationToken cancellationToken)
        {
            await GetAsync(cancellationToken);
        }

        private async Task GetAsync(CancellationToken cancellationToken)
        {
            await _updateLock.WaitAsync(cancellationToken);
            try
            {
                ShowIndicator = true;

                if (_device == null) _device = FindDevice();
                if (_device == null) throw new ArgumentNullException(nameof(_device));

                using var s = _device.Open();
                using var registration = cancellationToken.Register(s.Dispose);

                byte[] report = new byte[3];
                report[0] = 0x00;
                report[1] = 0xAA;
                report[2] = 0x01;

                byte[] response = new byte[3];

                await Task.Factory.FromAsync(s.BeginWrite, s.EndWrite, report, 0, report.Length, TaskCreationOptions.None);
                await Task.Factory.FromAsync(s.BeginRead, s.EndRead, response, 0, response.Length, TaskCreationOptions.None);

                Text = string.Format(Format ?? "🔋\n{0}%", response[1]);
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }
            catch (Exception exception)
            {
                Text = FormatDisconnected ?? "🖱\n❌";
                Logger.LogWarning(exception, "SteelSeries Rival 3 battery update failed.");
                throw;
            }
            finally
            {
                ShowIndicator = false;
                _updateLock.Release();
            }
        }
    }
}
