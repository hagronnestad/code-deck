using CodeDeck.PluginAbstractions;
using HidSharp;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

        public override async Task Init(CancellationToken cancellationToken)
        {
            _device = DeviceList.Local
                .GetHidDevices(0x03F0, 0x098D)
                .Where(x => x.GetMaxInputReportLength() == 31)
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
                report[1] = 0xbb;
                report[2] = 0x0b;

                await Task.Factory.FromAsync(s.BeginWrite, s.EndWrite, report, 0, report.Length, TaskCreationOptions.None);
                await Task.Factory.FromAsync(s.BeginRead, s.EndRead, report, 0, report.Length, TaskCreationOptions.None);

                var batteryLevel = report[3];

                Text = string.Format(Format ?? "🔋\n{0}%", batteryLevel);
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
