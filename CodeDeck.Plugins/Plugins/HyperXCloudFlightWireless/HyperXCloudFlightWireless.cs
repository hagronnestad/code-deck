using CodeDeck.PluginAbstractions;
using HidSharp;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Inspired by: https://github.com/franco-giordano/cloud-flight-monitor
/// </summary>
public class HyperXCloudFlightWireless : CodeDeckPlugin
{
    public class BatteryTile : Tile
    {
        [Setting] public string? Format { get; set; }
        [Setting] public string? FormatCharging { get; set; }
        [Setting] public string? FormatDisconnected { get; set; }
        [Setting] public int? Interval { get; set; }
        [Setting] public bool UseNGenuityBatteryMap { get; set; } = true;

        private int[] _batteryLevelsNGenuityMap = new int[] { 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 95, 95, 95, 95, 95, 90, 90, 90, 90, 90, 85, 85, 85, 85, 85, 80, 80, 80, 80, 80, 80, 75, 75, 75, 75, 75, 70, 70, 70, 70, 65, 65, 60, 60, 60, 60, 60, 55, 55, 50, 45, 45, 40, 40, 35, 35, 30, 30, 25, 25, 20, 20, 20, 20, 20, 20, 15, 15, 15, 15, 15, 15, 10, 10, 10, 10, 10, 10, 5, 5, 5, 5, 5, 5, 0, 0, 0, 0, 0 }
            .Reverse()
            .ToArray();

        private HidDevice? _device;

        public override async Task Init(CancellationToken cancellationToken)
        {
            _device = DeviceList.Local
                .GetHidDevices(2385, 5828)
                .Where(x => x.GetMaxInputReportLength() == 20)
                .FirstOrDefault();

            _ = Task.Run(() => BackgroundTask(cancellationToken), cancellationToken);

            await Task.CompletedTask;
        }

        public override async Task OnTilePressUp(CancellationToken cancellationToken)
        {
            await GetAsync(cancellationToken);
        }

        private async Task GetAsync(CancellationToken cancellationToken)
        {
            try
            {
                ShowIndicator = true;

                if (_device == null) return;

                HidStream s = _device.Open();

                byte[] report = new byte[20];
                report[0] = 0x21;
                report[1] = 0xff;
                report[2] = 0x05;

                await Task.Factory.FromAsync(s.BeginWrite, s.EndWrite, report, 0, report.Length, TaskCreationOptions.None);
                await Task.Factory.FromAsync(s.BeginRead, s.EndRead, report, 0, report.Length, TaskCreationOptions.None);

                var statusWord = BitConverter.ToUInt16(new byte[] { report[4], report[3] }, 0);

                // Bit 12 indicates charging
                var charging = (statusWord & (1 << 12)) != 0;

                // The SoC values below are NOT linear to the actual capacity,
                // it's probably linear to some voltage range
                if (charging)
                {
                    // 6 bits are used for the battery level when charging,
                    // that's a range from 0-63
                    var v = statusWord & 0b111111;
                    var p = v * 100 / 64;
                    Text = string.Format(FormatCharging ?? "⚡\n{0}%", p);
                }
                else
                {
                    // 9 bits are used for the battery level when discharging,
                    // that's a range from 0-511
                    var v = statusWord & 0b111111111;
                    var p = v * 100 / 512;
                    if (UseNGenuityBatteryMap) p = _batteryLevelsNGenuityMap[p];
                    Text = string.Format(Format ?? "🔋\n{0}%", p);
                }
            }
            catch
            {
                Text = FormatDisconnected ?? "🎧\n❌";
            }
            finally
            {
                ShowIndicator = false;
            }
        }

        private async Task BackgroundTask(CancellationToken cancellationToken)
        {
            for (; ; )
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Debug.WriteLine($"{nameof(BackgroundTask)} in {nameof(BatteryTile)} was cancelled!");
                    return;
                }

                GetAsync(cancellationToken).Wait(cancellationToken);
                await Task.Delay(Interval ?? 10 * 60 * 1000, cancellationToken);
            }
        }
    }
}
