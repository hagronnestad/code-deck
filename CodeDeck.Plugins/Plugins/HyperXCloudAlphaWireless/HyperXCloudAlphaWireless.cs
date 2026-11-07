using HidSharp;

/// <summary>
/// HID report bytes found here:
/// https://github.com/auto94/HyperX-Cloud-2-Battery-Monitor/blob/main/Cloud2BatteryMonitorUI/MainForm.cpp#L94
/// </summary>
public class HyperXCloudAlphaWireless : CodeDeckPlugin
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

        protected virtual HidDevice? FindDevice() => DeviceList.Local
            .GetHidDevices(0x03F0, 0x098D)
            .FirstOrDefault(x => x.GetMaxInputReportLength() == 31);

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

                if (_device == null) return;

                using var s = _device.Open();
                using var registration = cancellationToken.Register(s.Dispose);

                byte[] report = new byte[20];
                report[0] = 0x21;
                report[1] = 0xbb;
                report[2] = 0x0b;

                await Task.Factory.FromAsync(s.BeginWrite, s.EndWrite, report, 0, report.Length, TaskCreationOptions.None);
                await Task.Factory.FromAsync(s.BeginRead, s.EndRead, report, 0, report.Length, TaskCreationOptions.None);

                var batteryLevel = report[3];

                Text = string.Format(Format ?? "🔋\n{0}%", batteryLevel);
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }
            catch (Exception exception)
            {
                Text = FormatDisconnected ?? "🎧\n❌";
                Logger.LogWarning(exception, "HyperX Cloud Alpha battery update failed.");
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
