# HyperXCloudAlphaWireless

This plugin shows the battery level of a HyperX Cloud Alpha Wireless headset.

- [HyperXCloudAlphaWireless](#hyperxcloudalphawireless)
  - [BatteryTile](#batterytile)
    - [Settings](#settings)
    - [Examples](#examples)


## BatteryTile

Shows the battery level of a HyperX Cloud Alpha Wireless headset.

### Settings

| Setting               | Default      | Description                                                                                |
| --------------------- | ------------ | ------------------------------------------------------------------------------------------ |
| Format                | `🔋\n{0}%`    | Text format to use when headset is in normal use.                                          |
| FormatDisconnected    | `🎧\n❌`       | Text format to use when the headset is disconnected.                                       |
| Interval              | `600 000 ms` | Interval at which to query the headset. Default `10` minutes.                              |

### Examples

```json
{
    "Plugin": "HyperXCloudAlphaWireless",
    "Tile": "BatteryTile",
    "Settings": {
        "Format": "🎧\n{0}%",
        "Interval": "30000"
    }
}
```
