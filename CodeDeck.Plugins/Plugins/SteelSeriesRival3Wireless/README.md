# SteelSeriesRival3Wireless

This plugin shows the battery level of a SteelSeries Rival 3 Wireless mouse.

- [SteelSeriesRival3Wireless](#steelseriesrival3wireless)
  - [BatteryTile](#batterytile)
    - [Settings](#settings)
    - [Examples](#examples)


## BatteryTile

Shows the battery level of a SteelSeries Rival 3 Wireless mouse.

### Settings

| Setting            | Default      | Description                                                           |
| ------------------ | ------------ | --------------------------------------------------------------------- |
| Format             | `🔋\n{0}%`    | Text format to use when the mouse is connected.                       |
| FormatDisconnected | `🖱\n❌`       | Text format to use when the mouse is disconnected.                    |
| Interval           | `600 000 ms` | Interval at which to refresh the battery value. Default `10` minutes. |

### Examples

```json
{
    "Plugin": "SteelSeriesRival3Wireless",
    "Tile": "BatteryTile",
    "Settings": {
        "Format": "🖱 🔋\n{0}%",
        "Interval": "30000"
    }
}
```
