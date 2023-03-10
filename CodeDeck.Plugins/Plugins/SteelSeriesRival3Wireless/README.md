# SteelSeriesRival3Wireless

This plugin shows the battery level of a SteelSeries Rival 3 Wireless mouse.

- [SteelSeriesRival3Wireless](#steelseriesrival3wireless)
  - [BatteryTile](#batterytile)
    - [Settings](#settings)
    - [Examples](#examples)


## BatteryTile

Shows the battery level of a SteelSeries Rival 3 Wireless mouse.

### Settings

| Setting            | Default      | Description                                                   |
| ------------------ | ------------ | ------------------------------------------------------------- |
| Format             | `š\n{0}%`    | Text format to use when headset is in normal use.             |
| FormatDisconnected | `š±\nā`       | Text format to use when the headset is disconnected.          |
| Interval           | `600 000 ms` | Interval at which to query the headset. Default `10` minutes. |

### Examples

```json
{
    "Plugin": "SteelSeriesRival3Wireless",
    "Tile": "BatteryTile",
    "Settings": {
        "Format": "š± š\n{0}%",
        "Interval": "30000"
    }
}
```
