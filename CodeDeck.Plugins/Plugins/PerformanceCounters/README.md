# PerformanceCounters

This plugin implements tiles to show current CPU, RAM and GPU usage.

- [PerformanceCounters](#performancecounters)
  - [CpuUsageTile](#cpuusagetile)
    - [Settings](#settings)
    - [Examples](#examples)
  - [MemoryUsageTile](#memoryusagetile)
    - [Settings](#settings-1)
    - [Examples](#examples-1)
  - [GpuUsageTile](#gpuusagetile)
    - [Settings](#settings-2)
    - [Examples](#examples-2)


## CpuUsageTile

This tile shows the current CPU usage.

### Settings

| Setting | Default      | Description               |
| ------- | ------------ | ------------------------- |
| Format  | `CPU\n{0} %` | The string format to use. |

### Examples

```json
{
    "Plugin": "PerformanceCounters",
    "Tile": "CpuUsageTile",
    "Settings": {
        "Format": "🧠\n{0} %"
    }
}
```


## MemoryUsageTile

This tile shows the current RAM usage.

### Settings

| Setting | Default      | Description               |
| ------- | ------------ | ------------------------- |
| Format  | `RAM\n{0} %` | The string format to use. |

### Examples

```json
{
    "Plugin": "PerformanceCounters",
    "Tile": "MemoryUsageTile"
}
```


## GpuUsageTile

This tile shows the current GPU usage.

### Settings

| Setting | Default      | Description               |
| ------- | ------------ | ------------------------- |
| Format  | `GPU\n{0} %` | The string format to use. |

### Examples

```json
{
    "Plugin": "PerformanceCounters",
    "Tile": "GpuUsageTile"
}
```
