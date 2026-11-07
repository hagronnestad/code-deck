using CodeDeck.PluginAbstractions;
using CodeDeck.Plugins.Plugins.LogitechMice;
using HidSharp;
using Microsoft.Extensions.Logging.Abstractions;

namespace CodeDeck.Tests;

public class HidTileTests
{
    [Theory]
    [InlineData("Alpha")]
    [InlineData("Flight")]
    [InlineData("Rival")]
    public async Task CancellingABlockedPollClosesItsStream(string model)
    {
        var device = new TestDevice();
        Tile tile = model switch
        {
            "Alpha" => new AlphaTile(device),
            "Flight" => new FlightTile(device),
            _ => new RivalTile(device)
        };
        var context = new PluginContext(".", null, CancellationToken.None, NullLoggerFactory.Instance);
        await tile.InitializeAsync(context, CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        var poll = tile.OnTilePressUp(cancellation.Token);
        await device.Opened.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var stream = device.Streams.Single();
        await stream.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => poll.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.True(stream.Disposed);
        await tile.DeinitializeAsync();
    }

    [Fact]
    public async Task G703ReaderClosesEachStreamBeforeOpeningTheNextAndOnCancellation()
    {
        var device = new TestDevice { CompleteFirstRead = true };
        var manager = new ReaderTaskManager();
        var context = new PluginContext(".", null, CancellationToken.None, NullLoggerFactory.Instance, taskManager: manager);
        var tile = new G703Tile(device);
        await tile.InitializeAsync(context, CancellationToken.None);
        await device.SecondReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(device.Streams[0].Disposed);

        await tile.DeinitializeAsync().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.All(device.Streams, stream => Assert.True(stream.Disposed));
        Assert.Empty(manager.GetTasks());
    }

    private sealed class AlphaTile(TestDevice device) : HyperXCloudAlphaWireless.BatteryTile
    {
        protected override HidDevice? FindDevice() => device;
    }

    private sealed class FlightTile(TestDevice device) : HyperXCloudFlightWireless.BatteryTile
    {
        protected override HidDevice? FindDevice() => device;
    }

    private sealed class RivalTile(TestDevice device) : SteelSeriesRival3Wireless.BatteryTile
    {
        protected override HidDevice? FindDevice() => device;
    }

    private sealed class G703Tile(TestDevice device) : LogitechMice.G703BatteryTile
    {
        protected override HidDevice? FindDevice() => device;
    }

    private sealed class TestDevice : HidDevice
    {
        public List<TestStream> Streams { get; } = [];
        public bool CompleteFirstRead { get; init; }
        public TaskCompletionSource Opened { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource SecondReadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override string DevicePath => "test";
        public override int ProductID => 1;
        public override int VendorID => 1;
        public override int ReleaseNumberBcd => 0;
        public override int GetMaxInputReportLength() => 64;
        public override int GetMaxOutputReportLength() => 64;
        public override int GetMaxFeatureReportLength() => 64;
        public override string GetFileSystemName() => "test";

        protected override DeviceStream OpenDeviceDirectly(OpenConfiguration openConfig)
        {
            var stream = new TestStream(this, CompleteFirstRead && Streams.Count == 0,
                Streams.Count == 1 ? SecondReadStarted : null);
            Streams.Add(stream);
            Opened.TrySetResult();
            return stream;
        }
    }

    private sealed class TestStream(TestDevice device, bool completeRead, TaskCompletionSource? readStarted) : HidStream(device)
    {
        private readonly TaskCompletionSource _closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Disposed { get; private set; }
        public override int ReadTimeout { get; set; }
        public override int WriteTimeout { get; set; }

        public override int Read(byte[] buffer, int offset, int count) =>
            ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ReadStarted.TrySetResult();
            readStarted?.TrySetResult();
            if (completeRead) return 0;
            await _closed.Task;
            throw new ObjectDisposedException(nameof(TestStream));
        }

        public override void Write(byte[] buffer, int offset, int count) { }
        public override void GetFeature(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void SetFeature(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            _closed.TrySetResult();
            base.Dispose(disposing);
        }
    }

    private sealed class ReaderTaskManager : IPluginTaskManager
    {
        private readonly PluginTaskManager _manager = new(NullLogger<PluginTaskManager>.Instance);
        public IReadOnlyList<PluginTaskSnapshot> GetTasks() => _manager.GetTasks();
        public Task StopScopeAsync(string scopeId) => _manager.StopScopeAsync(scopeId);
        public Task StopOwnerAsync(string scopeId, string owner) => _manager.StopOwnerAsync(scopeId, owner);
        public void Run(string scopeId, string pluginName, string owner, string name, Func<CancellationToken, Task> operation,
            CancellationToken pluginCancellationToken, CancellationToken cancellationToken) =>
            _manager.Run(scopeId, pluginName, owner, name, operation, pluginCancellationToken, cancellationToken);
        public void RunRecurring(string scopeId, string pluginName, string owner, string name, TimeSpan interval,
            Func<CancellationToken, Task> operation, bool runImmediately, CancellationToken pluginCancellationToken,
            CancellationToken cancellationToken)
        { }
    }
}
