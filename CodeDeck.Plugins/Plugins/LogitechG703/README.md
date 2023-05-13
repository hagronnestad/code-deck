# LogitechG703

This plugin shows the battery level of a Logitech G703 Wireless mouse.

- [LogitechG703](#logitechg703)
  - [BatteryTile](#batterytile)
    - [Settings](#settings)
    - [Examples](#examples)


## BatteryTile

Shows the battery level of a Logitech G703 Wireless mouse.

### Settings

| Setting            | Default            | Description                                                   |
| ------------------ | ------------------ | ------------------------------------------------------------- |
| Format             | `🖱\n{0}%\n{1:N2}V` | Text format to use when headset is in normal use.             |
| FormatDisconnected | `🖱\n❌`             | Text format to use when the headset is disconnected.          |
| Interval           | `600 000 ms`       | Interval at which to query the headset. Default `10` minutes. |

### Examples

```json
{
    "Plugin": "LogitechG703",
    "Tile": "BatteryTile",
    "Settings": {
        "Format": "🖱️\n{0}%\n{1:N2}V",
        "FormatDisconnected": "🖱️\n💤",
        "Interval": "30000"
    }
}
```
