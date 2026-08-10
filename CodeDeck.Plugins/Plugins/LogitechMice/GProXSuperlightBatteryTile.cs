using CodeDeck.Plugins.Plugins.LogitechMice.Hid;
using HidSharp;
using System.Diagnostics;

namespace CodeDeck.Plugins.Plugins.LogitechMice;

public partial class LogitechMice : CodeDeckPlugin
{
    public class GProXSuperlightBatteryTile : Tile
    {
        private const string MonitorSharedStateKey = "LogitechMice.GProXSuperlightBatteryMonitor";

        [Setting] public string? Format { get; set; }
        [Setting] public string? FormatDisconnected { get; set; }
        [Setting] public int? Interval { get; set; }
        [Setting] public byte DeviceIndex { get; set; } = 0x01;

        private GProXSuperlightBatteryMonitor? _monitor;
        private bool _isDeinitialized;

        public override Task Init(CancellationToken cancellationToken)
        {
            var interval = TimeSpan.FromMilliseconds(Interval ?? 10 * 60 * 1000);
            _monitor = PluginContext.GetOrCreateSharedState(
                $"{MonitorSharedStateKey}:{DeviceIndex:X2}",
                () => new GProXSuperlightBatteryMonitor(PluginContext, DeviceIndex, interval));

            ApplySnapshot(_monitor.Subscribe(ApplySnapshot));
            return Task.CompletedTask;
        }

        public override Task OnTilePressUp(CancellationToken cancellationToken) =>
            _monitor?.RefreshAsync(cancellationToken) ?? Task.CompletedTask;

        public override Task DeInit()
        {
            _isDeinitialized = true;
            _monitor?.Unsubscribe(ApplySnapshot);
            _monitor = null;
            return Task.CompletedTask;
        }

        private void ApplySnapshot(BatterySnapshot snapshot)
        {
            if (_isDeinitialized) return;

            ShowIndicator = snapshot.IsRefreshing;
            Text = snapshot.Percentage is { } percentage && !snapshot.IsDisconnected
                ? string.Format(Format ?? "🖱\n{0}%", percentage)
                : FormatDisconnected ?? "🖱\n❌";
        }
    }

    private sealed class GProXSuperlightBatteryMonitor : IAsyncDisposable
    {
        private const string TaskOwner = "G Pro X Superlight monitor";
        private readonly PluginContext _context;
        private readonly byte _deviceIndex;
        private readonly SemaphoreSlim _refreshLock = new(1, 1);
        private readonly object _deviceLock = new();
        private readonly object _stateLock = new();
        private readonly object _notificationLock = new();
        private HidDevice? _device;
        private byte? _batteryFeatureIndex;
        private DateTime _lastHidReportAt = DateTime.UtcNow;
        private BatterySnapshot _snapshot = new(null, true, false);
        private Action<BatterySnapshot>? _changed;
        private int _activeRefreshes;
        private bool _disposed;

        public GProXSuperlightBatteryMonitor(PluginContext context, byte deviceIndex, TimeSpan interval)
        {
            _context = context;
            _deviceIndex = deviceIndex;

            // POC: the first tile to create this load-scoped monitor selects its interval.
            // Later tile configurations share it rather than creating another polling job.
            _context.RunTask("LogitechMice", TaskOwner, "HID report reader", ReadTask);
            _context.RunEvery("LogitechMice", TaskOwner, "Battery poll", interval, RefreshAsync);
        }

        public BatterySnapshot Subscribe(Action<BatterySnapshot> changed)
        {
            ArgumentNullException.ThrowIfNull(changed);

            lock (_stateLock)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                _changed += changed;
                return _snapshot;
            }
        }

        public void Unsubscribe(Action<BatterySnapshot> changed)
        {
            lock (_stateLock)
            {
                _changed -= changed;
            }
        }

        public async Task RefreshAsync(CancellationToken cancellationToken)
        {
            BeginRefresh();
            var lockAcquired = false;

            try
            {
                await _refreshLock.WaitAsync(cancellationToken);
                lockAcquired = true;

                var device = FindDevice();
                if (device is null) throw new InvalidOperationException("G Pro X Superlight receiver was not found.");

                await WritePacketAsync(
                    device,
                    HidPp.CreateGetFeatureIndexPacket(_deviceIndex, HidPp.HIDPP_FEATURE_UNIFIED_BATTERY),
                    cancellationToken);

                await Task.Delay(250, cancellationToken);
                var batteryFeatureIndex = BatteryFeatureIndex;
                if (batteryFeatureIndex is null)
                {
                    throw new InvalidOperationException("The unified battery feature index was not returned.");
                }

                await WritePacketAsync(
                    device,
                    HidPp.CreateGetUnifiedBatteryPacket(_deviceIndex, batteryFeatureIndex.Value),
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"{nameof(GProXSuperlightBatteryMonitor)}: Refresh exception: '{exception.Message}'");
                UpdateSnapshot(snapshot => snapshot with { IsDisconnected = true });
            }
            finally
            {
                if (lockAcquired) _refreshLock.Release();
                EndRefresh();
            }
        }

        public ValueTask DisposeAsync()
        {
            lock (_stateLock)
            {
                if (_disposed) return ValueTask.CompletedTask;

                _disposed = true;
                _changed = null;
            }

            lock (_deviceLock)
            {
                _device = null;
            }

            return ValueTask.CompletedTask;
        }

        private async Task ReadTask(CancellationToken cancellationToken)
        {
            var buffer = new byte[512];

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var device = FindDevice();
                    if (device is null)
                    {
                        UpdateSnapshot(snapshot => snapshot with { IsDisconnected = true });
                        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                        continue;
                    }

                    using var stream = device.Open();
                    using var cancellationRegistration = cancellationToken.Register(stream.Dispose);
                    var readBytes = await Task.Factory.FromAsync(
                        stream.BeginRead,
                        stream.EndRead,
                        buffer,
                        0,
                        buffer.Length,
                        TaskCreationOptions.None);
                    HandleHidPpReport(buffer[..readBytes]);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (TimeoutException)
                {
                    if ((DateTime.UtcNow - _lastHidReportAt).TotalSeconds > 60)
                    {
                        UpdateSnapshot(snapshot => snapshot with { IsDisconnected = true });
                    }
                }
                catch (Exception exception)
                {
                    Debug.WriteLine($"{nameof(GProXSuperlightBatteryMonitor)}: Reader exception: '{exception.Message}'");
                    UpdateSnapshot(snapshot => snapshot with { IsDisconnected = true });
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                }
            }
        }

        private void HandleHidPpReport(byte[] data)
        {
            _lastHidReportAt = DateTime.UtcNow;

            var report = HidPpReport.FromBytes(data);
            if (report is null || report.DeviceIndex != _deviceIndex || report.FeatureIndex == 0x09) return;

            if (report.FeatureIndex == HidPp.HIDPP_PAGE_ROOT_IDX &&
                report.FuncIndex == HidPp.FUNCTION_ROOT_GET_FEATURE)
            {
                lock (_stateLock)
                {
                    _batteryFeatureIndex = report.Params[0];
                }
            }
            else if (report.FeatureIndex == BatteryFeatureIndex && report.FuncIndex == 0x01)
            {
                UpdateSnapshot(snapshot => new BatterySnapshot(report.Params[0], false, snapshot.IsRefreshing));
            }
        }

        private byte? BatteryFeatureIndex
        {
            get
            {
                lock (_stateLock)
                {
                    return _batteryFeatureIndex;
                }
            }
        }

        private HidDevice? FindDevice()
        {
            var device = DeviceList.Local
                .GetHidDevices(0x046D, 0xC547)
                .FirstOrDefault(candidate => candidate.GetMaxInputReportLength() == 20);

            lock (_deviceLock)
            {
                _device = device;
            }

            return device;
        }

        private static async Task WritePacketAsync(HidDevice device, byte[] packet, CancellationToken cancellationToken)
        {
            using var stream = device.Open();
            var report = new byte[device.GetMaxInputReportLength()];
            Array.Copy(packet, report, packet.Length);
            await stream.WriteAsync(report, 0, report.Length, cancellationToken);
        }

        private void UpdateSnapshot(Func<BatterySnapshot, BatterySnapshot> update)
        {
            lock (_notificationLock)
            {
                Action<BatterySnapshot>? changed;
                BatterySnapshot snapshot;
                lock (_stateLock)
                {
                    if (_disposed) return;

                    snapshot = update(_snapshot);
                    if (_snapshot == snapshot) return;

                    _snapshot = snapshot;
                    changed = _changed;
                }

                if (changed is null) return;
                foreach (Action<BatterySnapshot> subscriber in changed.GetInvocationList())
                {
                    try
                    {
                        subscriber(snapshot);
                    }
                    catch (Exception exception)
                    {
                        Debug.WriteLine($"{nameof(GProXSuperlightBatteryMonitor)}: Subscriber exception: '{exception.Message}'");
                    }
                }
            }
        }

        private void BeginRefresh()
        {
            UpdateSnapshot(snapshot =>
            {
                _activeRefreshes++;
                return snapshot with { IsRefreshing = true };
            });
        }

        private void EndRefresh()
        {
            UpdateSnapshot(snapshot =>
            {
                _activeRefreshes--;
                return snapshot with { IsRefreshing = _activeRefreshes > 0 };
            });
        }
    }

    private sealed record BatterySnapshot(byte? Percentage, bool IsDisconnected, bool IsRefreshing);
}
