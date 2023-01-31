# HyperXCloudFlightWireless

This plugin shows the battery level of a HyperX Cloud Flight Wireless headset.

- [HyperXCloudFlightWireless](#hyperxcloudflightwireless)
  - [BatteryTile](#batterytile)
    - [Settings](#settings)
    - [Examples](#examples)


## BatteryTile

Shows the battery level of a HyperX Cloud Flight Wireless headset.

### Settings

| Setting            | Default      | Description                                                   |
| ------------------ | ------------ | ------------------------------------------------------------- |
| Format             | `🔋\n{0}%`    | Text format to use when headset is in normal use.             |
| FormatCharging     | `⚡\n{0}%`    | Text format to use when headset is charging.                  |
| FormatDisconnected | `🎧\n❌`       | Text format to use when the headset is disconnected.          |
| Interval           | `600 000 ms` | Interval at which to query the headset. Default `10` minutes. |

### Examples

```json
{
    "Plugin": "HyperXCloudFlightWireless",
    "Tile": "BatteryTile",
    "Settings": {
        "Format": "🎧\n{0}%",
        "Interval": "30000"
    }
}
```
