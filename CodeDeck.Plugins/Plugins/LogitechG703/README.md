# LogitechG703

This plugin shows the battery level of a Logitech G703 Wireless mouse.

- [LogitechG703](#logitechg703)
  - [BatteryTile](#batterytile)
    - [Settings](#settings)
    - [Examples](#examples)


## BatteryTile

Shows the battery level of a Logitech G703 Wireless mouse.

### Settings

| Setting            | Default            | Description                                                                                                                  |
| ------------------ | ------------------ | ---------------------------------------------------------------------------------------------------------------------------- |
| Format             | `🖱\n{0}%\n{1:N2}V` | Text format to use when the mouse is connected. `{0}` is the battery percentage and `{1}` is the voltage as a decimal value. |
| FormatDisconnected | `🖱\n❌`             | Text format to use when the mouse is disconnected.                                                                           |
| Interval           | `600 000 ms`       | Interval at which to refresh the battery value. Default `10` minutes.                                                        |

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
