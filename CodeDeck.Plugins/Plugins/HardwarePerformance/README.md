# HardwarePerformance

`HardwarePerformance` provides CPU, physical-memory, and GPU usage tiles from one
shared one-second sampler. CPU and memory use lightweight Win32 APIs. GPU usage
uses one dynamic PDH wildcard query and reports the busiest GPU engine, matching
the aggregation model used by Task Manager.

## CpuUsageTile

Shows total CPU utilization calculated from `GetSystemTimes`.

| Setting | Default | Description |
| --- | --- | --- |
| `Format` | `CPU\n{0} %` | `{0}` is the rounded utilization percentage. |

```json
{
    "Plugin": "HardwarePerformance",
    "Tile": "CpuUsageTile"
}
```

## MemoryUsageTile

Shows physical memory usage from `GlobalMemoryStatusEx`. This matches Task
Manager's **Memory** figure: usable physical memory minus currently available
physical memory.

| Setting | Default | Description |
| --- | --- | --- |
| `Format` | `RAM\n{0} %` | `{0}` is percentage, `{1}` is used GB, and `{2}` is usable total GB. |

```json
{
    "Plugin": "HardwarePerformance",
    "Tile": "MemoryUsageTile",
    "Settings": {
        "Format": "RAM\n{1:0.0}/{2:0.0}\n{0}%"
    }
}
```

## GpuUsageTile

Shows the busiest engine across all WDDM GPUs. The wildcard query tracks GPU
engine instances as processes start and exit instead of retaining stale
per-process counters.

| Setting | Default | Description |
| --- | --- | --- |
| `Format` | `GPU\n{0} %` | `{0}` is the rounded utilization percentage. |

```json
{
    "Plugin": "HardwarePerformance",
    "Tile": "GpuUsageTile"
}
```
