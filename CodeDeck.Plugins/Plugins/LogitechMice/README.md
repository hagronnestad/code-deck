# Logitech Mice

This plugin provides battery level information for Logitech mice.


- [Logitech Mice](#logitech-mice)
  - [Supported Mice](#supported-mice)
  - [G703BatteryTile](#g703batterytile)
    - [Settings](#settings)
    - [Examples](#examples)
  - [GProXSuperlightBatteryTile](#gproxsuperlightbatterytile)
    - [Settings](#settings-1)
    - [Examples](#examples-1)


## Supported Mice
- Logitech G703 Wireless
- Logitech G Pro X Superlight Wireless


## G703BatteryTile
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
    "Plugin": "LogitechMice",
    "Tile": "BatteryTile",
    "Settings": {
        "Format": "🖱️\n{0}%\n{1:N2}V",
        "FormatDisconnected": "🖱️\n💤",
        "Interval": "30000"
    }
}
```

## GProXSuperlightBatteryTile
Shows the battery level of a Logitech G Pro X Superlight Wireless mouse.

### Settings

| Setting            | Default      | Description                                                                      |
| ------------------ | ------------ | -------------------------------------------------------------------------------- |
| Format             | `🖱\n{0}%`    | Text format to use when the mouse is connected. `{0}` is the battery percentage. |
| FormatDisconnected | `🖱\n❌`       | Text format to use when the mouse is disconnected.                               |
| Interval           | `600 000 ms` | Interval at which to refresh the battery value. Default `10` minutes.            |

### Examples

```json
{
    "Plugin": "LogitechGProXSuperlight",
    "Tile": "BatteryTile",
    "Settings": {
        "Format": "🖱️\n{0}%",
        "FormatDisconnected": "🖱️\n💤",
        "Interval": "30000"
    }
}
```
