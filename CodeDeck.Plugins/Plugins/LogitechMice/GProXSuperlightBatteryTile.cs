using CodeDeck.PluginAbstractions;
using CodeDeck.Plugins.Plugins.LogitechMice.Hid;
using HidSharp;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeDeck.Plugins.Plugins.LogitechMice;

public partial class LogitechMice : CodeDeckPlugin
{
    public class GProXSuperlightBatteryTile : Tile
    {
        [Setting] public string? Format { get; set; }
        [Setting] public string? FormatDisconnected { get; set; }
        [Setting] public int? Interval { get; set; }
        [Setting] public byte DeviceIndex { get; set; } = 0x01;

        private HidDevice? _device;
        private byte? _batteryFeatureIndex = null;
        private int? _percentage = null;
        private DateTime _lastHandleHidPpReport = DateTime.Now;

        public override async Task Init(CancellationToken cancellationToken)
        {
            _device = FindDevice();

            _ = Task.Run(() => ReadTask(cancellationToken), cancellationToken);
            _ = Task.Run(() => BackgroundTask(cancellationToken), cancellationToken);

            Text = FormatDisconnected ?? "🖱\n❌";

            await GetBatteryInformation(cancellationToken);
        }

        public override async Task OnTilePressUp(CancellationToken cancellationToken)
        {
            await GetBatteryInformation(cancellationToken);
        }

        private static HidDevice? FindDevice()
        {
            return DeviceList.Local
                .GetHidDevices(0x046D, 0xC547)
                .Where(x => x.GetMaxInputReportLength() == 20)
                .FirstOrDefault();
        }

        private async Task ReadTask(CancellationToken cancellationToken)
        {
            HidStream? hidStream = null;
            var buffer = new byte[512];

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _device = FindDevice();
                    if (_device == null)
                    {
                        await Task.Delay(1000, cancellationToken);
                        continue;
                    }

                    hidStream = _device.Open();
                    if (hidStream == null)
                    {
                        await Task.Delay(1000, cancellationToken);
                        continue;
                    }

                    var readBytes = await Task.Factory.FromAsync(hidStream.BeginRead, hidStream.EndRead, buffer, 0, buffer.Length, TaskCreationOptions.None);
                    var data = buffer[0..readBytes];
                    HandleHidPpReport(data);
                }
                catch (TimeoutException)
                {
                    if ((DateTime.Now - _lastHandleHidPpReport).TotalSeconds > 60)
                    {
                        Text = FormatDisconnected ?? "🖱\n❌";
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"{nameof(ReadTask)}: Exception: '{e.Message}'");
                }
            }
        }

        private void HandleHidPpReport(byte[] data)
        {
            _lastHandleHidPpReport = DateTime.Now;

            var p = HidPpReport.FromBytes(data);
            if (p == null) return;
            if (p.DeviceIndex != DeviceIndex) return;
            if (p.FeatureIndex == 0x09) return; // Mouse move?

            // Feature index reply for unified battery (should be 0x06)
            if (p.FeatureIndex == HidPp.HIDPP_PAGE_ROOT_IDX &&
                p.FuncIndex == HidPp.FUNCTION_ROOT_GET_FEATURE)
            {
                _batteryFeatureIndex = p.Params[0];
            }
            // Unified battery result: feature index is the returned index (0x06), FuncIndex == 1 gives percentage in Params[0]
            else if (p.FeatureIndex == _batteryFeatureIndex && p.FuncIndex == 0x01)
            {
                var batteryLevel = p.Params[0];
                _percentage = batteryLevel;
                Text = string.Format(Format ?? "🖱\n{0}%", _percentage);
            }
        }

        private async Task RequestBatteryInformationFeatureIndex(CancellationToken cancellationToken)
        {
            try
            {
                ShowIndicator = true;

                _device = FindDevice();
                if (_device == null) throw new NullReferenceException(nameof(_device));

                using var s = _device.Open();

                var packet = HidPp.CreateGetFeatureIndexPacket(DeviceIndex, HidPp.HIDPP_FEATURE_UNIFIED_BATTERY);
                byte[] report = new byte[_device.GetMaxInputReportLength()];
                Array.Copy(packet, report, packet.Length);
                await s.WriteAsync(report, 0, report.Length, cancellationToken);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{nameof(RequestBatteryInformationFeatureIndex)}: Exception: '{e.Message}'");
            }
            finally
            {
                ShowIndicator = false;
            }
        }

        private async Task GetBatteryInformation(CancellationToken cancellationToken)
        {
            try
            {
                ShowIndicator = true;

                _device = FindDevice();
                if (_device == null) throw new NullReferenceException(nameof(_device));

                using var s = _device.Open();

                await RequestBatteryInformationFeatureIndex(cancellationToken);
                await Task.Delay(250, cancellationToken);
                if (_batteryFeatureIndex == null) throw new Exception($"'{nameof(_batteryFeatureIndex)}' is still 'null'");

                var packet = HidPp.CreateGetUnifiedBatteryPacket(DeviceIndex, _batteryFeatureIndex.Value);
                byte[] report = new byte[_device.GetMaxInputReportLength()];
                Array.Copy(packet, report, packet.Length);
                await s.WriteAsync(report, 0, report.Length, cancellationToken);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{nameof(GetBatteryInformation)}: Exception: '{e.Message}'");
                Text = FormatDisconnected ?? "🖱\n❌";
            }
            finally
            {
                ShowIndicator = false;
            }
        }

        private async Task BackgroundTask(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await GetBatteryInformation(cancellationToken);
                await Task.Delay(Interval ?? 10 * 60 * 1000, cancellationToken);
            }
        }
    }
}
