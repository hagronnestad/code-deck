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
    public class G703BatteryTile : Tile
    {
        [Setting] public string? Format { get; set; }
        [Setting] public string? FormatDisconnected { get; set; }
        [Setting] public int? Interval { get; set; }
        [Setting] public byte DeviceIndex { get; set; } = 0x01;

        private HidDevice? _device;
        private byte? _batteryFeatureIndex = null;
        private double? _voltage = null;
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
                .GetHidDevices(0x046D, 0xC539)
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
                    Debug.WriteLine($"{nameof(RequestBatteryInformationFeatureIndex)}: Exception: '{e.Message}'");
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

            // Get feature response
            if (p.FeatureIndex == HidPp.HIDPP_PAGE_ROOT_IDX &&
                p.FuncIndex == HidPp.FUNCTION_ROOT_GET_FEATURE)
            {
                _batteryFeatureIndex = p.Params[0];
            }
            // Get battery information result
            else if (_batteryFeatureIndex != null &&
                p.FeatureIndex == _batteryFeatureIndex &&
                p.FuncIndex == HidPp.CMD_BATTERY_VOLTAGE_GET_BATTERY_VOLTAGE)
            {
                var voltage = (p.Params[0] << 8 | p.Params[1]);
                _voltage = voltage / 1000.0;
                _percentage = GetBatteryPercentageFromVoltage(voltage);

                Text = string.Format(Format ?? "🖱\n{0}%\n{1:N2}V", _percentage, _voltage);
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

                var packet = HidPp.CreateGetFeatureIndexPacket(DeviceIndex, HidPp.HIDPP_FEATURE_BATTERY_VOLTAGE);
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

                var packet = HidPp.CreateGetBatteryInformationPacket(DeviceIndex, _batteryFeatureIndex.Value);
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

        private static readonly int[] _voltagesToPercentLut = {
            4186, 4156, 4143, 4133, 4122, 4113, 4103, 4094, 4086, 4075,
            4067, 4059, 4051, 4043, 4035, 4027, 4019, 4011, 4003, 3997,
            3989, 3983, 3976, 3969, 3961, 3955, 3949, 3942, 3935, 3929,
            3922, 3916, 3909, 3902, 3896, 3890, 3883, 3877, 3870, 3865,
            3859, 3853, 3848, 3842, 3837, 3833, 3828, 3824, 3819, 3815,
            3811, 3808, 3804, 3800, 3797, 3793, 3790, 3787, 3784, 3781,
            3778, 3775, 3772, 3770, 3767, 3764, 3762, 3759, 3757, 3754,
            3751, 3748, 3744, 3741, 3737, 3734, 3730, 3726, 3724, 3720,
            3717, 3714, 3710, 3706, 3702, 3697, 3693, 3688, 3683, 3677,
            3671, 3666, 3662, 3658, 3654, 3646, 3633, 3612, 3579, 3537
        };

        private static int GetBatteryPercentageFromVoltage(int voltage)
        {
            for (int i = 0; i < _voltagesToPercentLut.Length; i++)
            {
                if (voltage >= _voltagesToPercentLut[i]) return 100 - i;
            }
            return 0;
        }
    }
}
