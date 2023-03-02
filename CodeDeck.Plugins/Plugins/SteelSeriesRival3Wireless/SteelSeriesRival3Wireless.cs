using CodeDeck.PluginAbstractions;
using HidSharp;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

        public override async Task Init(CancellationToken cancellationToken)
        {
            _device = DeviceList.Local
                .GetHidDevices(0x1038, 0x1830)
                // Filter to pick the correct endpoint
                .Where(x => x.GetMaxFeatureReportLength() == 515)
                .Where(x => x.GetMaxInputReportLength() == 65)
                .Where(x => x.GetMaxOutputReportLength() == 65)
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

                byte[] report = new byte[3];
                report[0] = 0x00;
                report[1] = 0xAA;
                report[2] = 0x01;

                byte[] response = new byte[3];

                await Task.Factory.FromAsync(s.BeginWrite, s.EndWrite, report, 0, report.Length, TaskCreationOptions.None);
                await Task.Factory.FromAsync(s.BeginRead, s.EndRead, response, 0, response.Length, TaskCreationOptions.None);

                Text = string.Format(Format ?? "🔋\n{0}%", response[1]);
            }
            catch
            {
                Text = FormatDisconnected ?? "🖱\n❌";
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
