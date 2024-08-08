using CodeDeck.PluginAbstractions;
using HidSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class LogitechG703 : CodeDeckPlugin
{
    public class BatteryTile : Tile
    {
        [Setting] public string? Format { get; set; }
        [Setting] public string? FormatDisconnected { get; set; }
        [Setting] public int? Interval { get; set; }
        [Setting] public byte DeviceIndex { get; set; } = 0x01;

        private HidDevice? _device;
        private byte? _batteryFeatureIndex = null;
        private double? _voltage = null;
        private int? _percentage = null;

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
            var devices = DeviceList.Local
                .GetHidDevices(0x046D, 0xC539)
                // Filter to pick the correct endpoint
                .Where(x => x.GetMaxInputReportLength() == 20).ToList();

            return devices?.FirstOrDefault();
        }

        private async Task ReadTask(CancellationToken cancellationToken)
        {
            HidStream? hidStream = null;
            var buffer = new byte[512];

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Find device or wait and try again
                    _device = FindDevice();
                    if (_device == null)
                    {
                        await Task.Delay(1000, cancellationToken);
                        continue;
                    }

                    // Open device or wait and try again
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
                    if ((DateTime.Now - _lastHandleHidPpReport).TotalSeconds > 30)
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

        private DateTime _lastHandleHidPpReport = DateTime.Now;

        private void HandleHidPpReport(byte[] data)
        {
            _lastHandleHidPpReport = DateTime.Now;

            var p = HidPp.HidPpReport.FromBytes(data);

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
            else if (p.FeatureIndex == _batteryFeatureIndex &&
                p.FuncIndex == HidPp.CMD_BATTERY_VOLTAGE_GET_BATTERY_VOLTAGE)
            {
                var voltage = (p.Params[0] << 8 | p.Params[1]);
                _voltage = voltage / 1000.0f;
                _percentage = HidPp.GetBatteryPercentageFromVoltage(voltage);

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

                byte[] report = new byte[_device.GetMaxInputReportLength()];
                var packet = HidPp.CreateGetFeatureIndexPacket(DeviceIndex, HidPp.HIDPP_FEATURE_BATTERY_VOLTAGE);
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

                byte[] report = new byte[_device.GetMaxInputReportLength()];
                var packet = HidPp.CreateGetBatteryInformationPacket(DeviceIndex, _batteryFeatureIndex.Value);
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
            for (; ; )
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Debug.WriteLine($"{nameof(BackgroundTask)} in {nameof(BatteryTile)} was cancelled!");
                    return;
                }

                await GetBatteryInformation(cancellationToken);
                await Task.Delay(Interval ?? 10 * 60 * 1000, cancellationToken);
            }
        }
    }

    // HID++ class pieced together from the Linux kernel source code and other various sources
    // https://github.com/torvalds/linux/blob/master/drivers/hid/hid-logitech-hidpp.c
    private class HidPp
    {
        public const byte SOFTWARE_ID = 0x08;
        public const byte MESSAGE_TYPE_SHORT = 0x10;
        public const byte MESSAGE_TYPE_LONG = 0x11;

        public const ushort HIDPP_FEATURE_ROOT = 0x0000;
        public const ushort HIDPP_FEATURE_BATTERY_VOLTAGE = 0x1001;

        public const byte HIDPP_PAGE_ROOT_IDX = 0x00;
        public const byte FUNCTION_ROOT_GET_FEATURE = 0x00;
        public const byte FUNCTION_ROOT_GET_PROTOCOL_VERSION = 0x10;
        public const byte CMD_BATTERY_VOLTAGE_GET_BATTERY_VOLTAGE = 0x00;

        private static int[] _voltagesToPercentLut = {
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

        public static int GetBatteryPercentageFromVoltage(int voltage)
        {
            for (int i = 0; i < _voltagesToPercentLut.Length; i++)
            {
                if (voltage >= _voltagesToPercentLut[i]) return 100 - i;
            }

            return 0;
        }

        public static byte[] CreateGetFeatureIndexPacket(byte device, ushort feature)
        {
            var packet = new List<byte>
            {
                MESSAGE_TYPE_LONG,
                device,
                HIDPP_PAGE_ROOT_IDX, // Feature index
                FUNCTION_ROOT_GET_FEATURE | SOFTWARE_ID
            };
            packet.AddRange(BitConverter.GetBytes(feature).Reverse());
            return packet.ToArray();
        }

        public static byte[] CreateGetBatteryInformationPacket(byte device, byte featureIndex)
        {
            var packet = new List<byte>
            {
                MESSAGE_TYPE_LONG,
                device,
                featureIndex,
                CMD_BATTERY_VOLTAGE_GET_BATTERY_VOLTAGE | SOFTWARE_ID
            };
            return packet.ToArray();
        }

        public static byte[] CreateGetFirmwareVersionPacket(byte device)
        {
            var packet = new List<byte>
            {
                MESSAGE_TYPE_LONG,
                device,
                0x03, // Feature index
                FUNCTION_ROOT_GET_PROTOCOL_VERSION | SOFTWARE_ID
            };
            return packet.ToArray();
        }

        public static byte[] CreatePingPacket(byte device)
        {
            var packet = new List<byte>
            {
                MESSAGE_TYPE_LONG,
                device,
                HIDPP_PAGE_ROOT_IDX, // Feature index
                FUNCTION_ROOT_GET_PROTOCOL_VERSION | SOFTWARE_ID,
                0x00,
                0x00,
                0xAA
            };
            return packet.ToArray();
        }


        public class HidPpReport
        {
            public byte ReportId;
            public byte DeviceIndex;
            public byte FeatureIndex;
            public byte FuncIndexAndSoftwareId;
            public byte[] Params = new byte[20 - 4];

            public byte FuncIndex => (byte) (FuncIndexAndSoftwareId >> 4);
            public byte SoftwareId => (byte) (FuncIndexAndSoftwareId & 0x0F);

            public static HidPpReport? FromBytes(byte[] bytes)
            {
                if (bytes == null || bytes.Length < 20) return null;

                using var ms = new MemoryStream(bytes);
                using var r = new BinaryReader(ms);

                var report = new HidPpReport();
                report.ReportId = r.ReadByte();
                report.DeviceIndex = r.ReadByte();
                report.FeatureIndex = r.ReadByte();
                report.FuncIndexAndSoftwareId = r.ReadByte();
                report.Params = r.ReadBytes(report.Params.Length);

                return report;
            }
        }
    }
}
