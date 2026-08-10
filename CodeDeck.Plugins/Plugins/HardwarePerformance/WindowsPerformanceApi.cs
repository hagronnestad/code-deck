using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CodeDeck.Plugins.Plugins.HardwarePerformance;

internal static class WindowsPerformanceApi
{
    public static bool TryGetSystemCpuTimes(out SystemCpuTimes cpuTimes)
    {
        if (!GetSystemTimes(out var idle, out var kernel, out var user))
        {
            cpuTimes = default;
            return false;
        }

        cpuTimes = new SystemCpuTimes(idle.ToUInt64(), kernel.ToUInt64(), user.ToUInt64());
        return true;
    }

    public static bool TryGetMemoryUsage(out SystemMemoryUsage memoryUsage)
    {
        var status = new MemoryStatusEx
        {
            Length = (uint)Marshal.SizeOf<MemoryStatusEx>()
        };

        if (!GlobalMemoryStatusEx(ref status))
        {
            memoryUsage = default;
            return false;
        }

        memoryUsage = new SystemMemoryUsage(
            status.MemoryLoad,
            status.TotalPhysical - status.AvailablePhysical,
            status.TotalPhysical);
        return true;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(
        out FileTime idleTime,
        out FileTime kernelTime,
        out FileTime userTime);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct FileTime
    {
        private readonly uint _low;
        private readonly uint _high;

        public ulong ToUInt64() => ((ulong)_high << 32) | _low;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }
}

internal sealed class GpuUsageSampler : IDisposable
{
    private const uint ErrorSuccess = 0;
    private const uint PdhMoreData = 0x800007D2;
    private const uint PdhFormatDouble = 0x00000200;
    private const uint PdhCStatusValidData = 0x00000000;
    private const uint PdhCStatusNewData = 0x00000001;
    private const string GpuCounterPath = @"\GPU Engine(*)\Utilization Percentage";

    private IntPtr _query;
    private IntPtr _counter;
    private bool _hasBaseline;
    private uint? _lastFailure;

    public GpuUsageSampler()
    {
        TryInitialize();
    }

    public double? ReadUsagePercent()
    {
        if (_query == IntPtr.Zero)
        {
            TryInitialize();
            return null;
        }

        var status = PdhCollectQueryData(_query);
        if (status != ErrorSuccess)
        {
            HandleFailure(status);
            return null;
        }

        if (!_hasBaseline)
        {
            _hasBaseline = true;
            return null;
        }

        return ReadFormattedValues();
    }

    public void Dispose()
    {
        Reset();
    }

    private bool TryInitialize()
    {
        Reset();

        var status = PdhOpenQueryW(IntPtr.Zero, UIntPtr.Zero, out _query);
        if (status != ErrorSuccess)
        {
            HandleFailure(status);
            return false;
        }

        status = PdhAddEnglishCounterW(_query, GpuCounterPath, UIntPtr.Zero, out _counter);
        if (status != ErrorSuccess)
        {
            HandleFailure(status);
            return false;
        }

        status = PdhCollectQueryData(_query);
        if (status != ErrorSuccess)
        {
            HandleFailure(status);
            return false;
        }

        _hasBaseline = true;
        _lastFailure = null;
        return true;
    }

    private double? ReadFormattedValues()
    {
        uint bufferSize = 0;
        var status = PdhGetFormattedCounterArrayW(
            _counter,
            PdhFormatDouble,
            ref bufferSize,
            out var itemCount,
            IntPtr.Zero);

        if (status != PdhMoreData || bufferSize == 0 || itemCount == 0)
        {
            if (status != ErrorSuccess && status != PdhMoreData) HandleFailure(status);
            return 0d;
        }

        var buffer = Marshal.AllocHGlobal(checked((int)bufferSize));
        try
        {
            status = PdhGetFormattedCounterArrayW(
                _counter,
                PdhFormatDouble,
                ref bufferSize,
                out itemCount,
                buffer);

            if (status != ErrorSuccess)
            {
                HandleFailure(status);
                return null;
            }

            var usageByEngine = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var itemSize = Marshal.SizeOf<PdhFormattedCounterValueItem>();
            for (var index = 0; index < itemCount; index++)
            {
                var item = Marshal.PtrToStructure<PdhFormattedCounterValueItem>(
                    IntPtr.Add(buffer, checked((int)index * itemSize)));

                if (item.Value.Status is not (PdhCStatusValidData or PdhCStatusNewData)) continue;

                var instanceName = Marshal.PtrToStringUni(item.Name);
                if (string.IsNullOrEmpty(instanceName) || !double.IsFinite(item.Value.Value.DoubleValue)) continue;

                var engineName = GetEngineName(instanceName);
                usageByEngine.TryGetValue(engineName, out var currentUsage);
                usageByEngine[engineName] = currentUsage + Math.Max(0d, item.Value.Value.DoubleValue);
            }

            return usageByEngine.Count == 0
                ? 0d
                : Math.Clamp(usageByEngine.Values.Max(), 0d, 100d);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static string GetEngineName(string instanceName)
    {
        var luidIndex = instanceName.IndexOf("luid_", StringComparison.OrdinalIgnoreCase);
        return luidIndex < 0 ? instanceName : instanceName[luidIndex..];
    }

    private void HandleFailure(uint status)
    {
        if (_lastFailure != status)
        {
            Debug.WriteLine($"HardwarePerformance: GPU performance query failed with PDH status 0x{status:X8}.");
            _lastFailure = status;
        }

        Reset();
    }

    private void Reset()
    {
        if (_query != IntPtr.Zero)
        {
            PdhCloseQuery(_query);
        }

        _query = IntPtr.Zero;
        _counter = IntPtr.Zero;
        _hasBaseline = false;
    }

    [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
    private static extern uint PdhOpenQueryW(
        IntPtr dataSource,
        UIntPtr userData,
        out IntPtr query);

    [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
    private static extern uint PdhAddEnglishCounterW(
        IntPtr query,
        string fullCounterPath,
        UIntPtr userData,
        out IntPtr counter);

    [DllImport("pdh.dll")]
    private static extern uint PdhCollectQueryData(IntPtr query);

    [DllImport("pdh.dll", CharSet = CharSet.Unicode)]
    private static extern uint PdhGetFormattedCounterArrayW(
        IntPtr counter,
        uint format,
        ref uint bufferSize,
        out uint itemCount,
        IntPtr itemBuffer);

    [DllImport("pdh.dll")]
    private static extern uint PdhCloseQuery(IntPtr query);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct PdhFormattedCounterValueItem
    {
        public readonly IntPtr Name;
        public readonly PdhFormattedCounterValue Value;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct PdhFormattedCounterValue
    {
        public readonly uint Status;
        public readonly PdhValueUnion Value;
    }

    [StructLayout(LayoutKind.Explicit)]
    private readonly struct PdhValueUnion
    {
        [FieldOffset(0)]
        public readonly double DoubleValue;
    }
}
