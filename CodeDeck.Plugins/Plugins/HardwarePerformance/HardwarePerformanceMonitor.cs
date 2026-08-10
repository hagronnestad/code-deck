using System.Diagnostics;

namespace CodeDeck.Plugins.Plugins.HardwarePerformance;

internal sealed class HardwarePerformanceMonitor : IDisposable
{
    private const string PluginName = "HardwarePerformance";
    private const string TaskOwner = "Hardware performance monitor";

    private readonly object _stateLock = new();
    private readonly GpuUsageSampler _gpuUsageSampler = new();
    private SystemCpuTimes? _previousCpuTimes;
    private HardwarePerformanceSnapshot _snapshot;
    private Action<HardwarePerformanceSnapshot>? _changed;
    private bool _disposed;

    public HardwarePerformanceMonitor(PluginContext context)
    {
        if (WindowsPerformanceApi.TryGetSystemCpuTimes(out var cpuTimes))
        {
            _previousCpuTimes = cpuTimes;
        }

        _snapshot = ReadInitialSnapshot();

        context.RunEvery(
            PluginName,
            TaskOwner,
            "System usage sample",
            TimeSpan.FromSeconds(1),
            SampleAsync,
            runImmediately: false);
    }

    public HardwarePerformanceSnapshot Subscribe(Action<HardwarePerformanceSnapshot> changed)
    {
        ArgumentNullException.ThrowIfNull(changed);

        lock (_stateLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _changed += changed;
            return _snapshot;
        }
    }

    public void Unsubscribe(Action<HardwarePerformanceSnapshot> changed)
    {
        lock (_stateLock)
        {
            _changed -= changed;
        }
    }

    public void Dispose()
    {
        lock (_stateLock)
        {
            if (_disposed) return;

            _disposed = true;
            _changed = null;
        }

        _gpuUsageSampler.Dispose();
    }

    private Task SampleAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cpuUsage = ReadCpuUsage();
        var memory = ReadMemoryUsage();
        var gpuUsage = _gpuUsageSampler.ReadUsagePercent();

        HardwarePerformanceSnapshot snapshot;
        Action<HardwarePerformanceSnapshot>? changed;
        lock (_stateLock)
        {
            if (_disposed) return Task.CompletedTask;

            snapshot = new HardwarePerformanceSnapshot(
                cpuUsage,
                memory.UsagePercent,
                memory.UsedBytes,
                memory.TotalBytes,
                gpuUsage);

            if (_snapshot == snapshot) return Task.CompletedTask;

            _snapshot = snapshot;
            changed = _changed;
        }

        if (changed is not null)
        {
            foreach (var subscriber in changed.GetInvocationList().Cast<Action<HardwarePerformanceSnapshot>>())
            {
                try
                {
                    subscriber(snapshot);
                }
                catch (Exception exception)
                {
                    Debug.WriteLine($"{PluginName}: snapshot subscriber failed: {exception.Message}");
                }
            }
        }

        return Task.CompletedTask;
    }

    private HardwarePerformanceSnapshot ReadInitialSnapshot()
    {
        var memory = ReadMemoryUsage();
        return new HardwarePerformanceSnapshot(
            null,
            memory.UsagePercent,
            memory.UsedBytes,
            memory.TotalBytes,
            null);
    }

    private double? ReadCpuUsage()
    {
        if (!WindowsPerformanceApi.TryGetSystemCpuTimes(out var currentTimes)) return null;

        var previousTimes = _previousCpuTimes;
        _previousCpuTimes = currentTimes;
        if (previousTimes is null) return null;

        var idleDelta = currentTimes.Idle - previousTimes.Value.Idle;
        var kernelDelta = currentTimes.Kernel - previousTimes.Value.Kernel;
        var userDelta = currentTimes.User - previousTimes.Value.User;
        var totalDelta = kernelDelta + userDelta;
        if (totalDelta == 0 || idleDelta > totalDelta) return null;

        return Math.Clamp((totalDelta - idleDelta) * 100d / totalDelta, 0d, 100d);
    }

    private static SystemMemoryUsage ReadMemoryUsage()
    {
        if (WindowsPerformanceApi.TryGetMemoryUsage(out var memory)) return memory;

        Debug.WriteLine($"{PluginName}: GlobalMemoryStatusEx failed.");
        return default;
    }
}

internal readonly record struct HardwarePerformanceSnapshot(
    double? CpuUsagePercent,
    uint MemoryUsagePercent,
    ulong UsedPhysicalMemoryBytes,
    ulong TotalPhysicalMemoryBytes,
    double? GpuUsagePercent);

internal readonly record struct SystemCpuTimes(ulong Idle, ulong Kernel, ulong User);

internal readonly record struct SystemMemoryUsage(uint UsagePercent, ulong UsedBytes, ulong TotalBytes);
