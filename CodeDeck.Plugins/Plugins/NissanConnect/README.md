# NissanConnect

This plugin can be used to show information about a Nissan Leaf electric car. The plugin uses the [Nissan Connect Client Library](https://github.com/hagronnestad/nissan-connect-dotnet) and requires a valid username and password for the Nissan Connect service. The API is not officially supported by Nissan and may stop working at any time. And the information provided by the API may not be up to date.

- [NissanConnect](#nissanconnect)
  - [NissanConnectBatteryLevelTile](#nissanconnectbatteryleveltile)
  - [NissanConnectRangeTile](#nissanconnectrangetile)
  - [NissanConnectChargeStatus](#nissanconnectchargestatus)
  - [NissanConnectBatteryStatusAge](#nissanconnectbatterystatusage)
  - [Examples](#examples)
    - [Plugin Settings](#plugin-settings)
    - [Tile Settings](#tile-settings)
  - [Disclaimer](#disclaimer)


## NissanConnectBatteryLevelTile

Shows the battery level of a Nissan Leaf electric car.

| Setting               | Default | Description                                                                                                                                   |
| --------------------- | ------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| Interval              | `10`    | Interval in minutes at which to query the API and update the Tile. Default `10` minutes.                                                      |
| NickName              | `null`  | Nickname of the car to show in the Tile. This can be found in the NissanConnect App. Optional, uses first car from API if `null`.             |
| ForceRefreshAuto      | `false` | Set to `true` to try and force a data refresh from the car through the API, doesn't always work, be careful to do this at frequent intervals. |
| ForceRefreshTilePress | `false` | Set to `true` to try and force a data refresh from the car through the API when the Tile is pressed.                                          |
| Format                | `{0}`   | Format to use for the Tile text. `{0}` is the battery level in percentage. Example: `🔋\n{0}%`                                                 |
| FormatError           | `🚗❌`    | Text format to use if an error occurs when querying the API.                                                                                  |


## NissanConnectRangeTile

Shows the range of a Nissan Leaf electric car.

| Setting               | Default | Description                                                                                                                                   |
| --------------------- | ------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| Interval              | `10`    | Interval in minutes at which to query the API and update the Tile. Default `10` minutes.                                                      |
| NickName              | `null`  | Nickname of the car to show in the Tile. This can be found in the NissanConnect App. Optional, uses first car from API if `null`.             |
| ForceRefreshAuto      | `false` | Set to `true` to try and force a data refresh from the car through the API, doesn't always work, be careful to do this at frequent intervals. |
| ForceRefreshTilePress | `false` | Set to `true` to try and force a data refresh from the car through the API when the Tile is pressed.                                          |
| Format                | `{0}`   | Format to use for the Tile text. `{0}` is the range with HVAC `ON` and `{1}` is the range with HVAC `OFF`. Example: `🏁\n{0} km\n({1} km)`     |
| FormatError           | `🚗❌`    | Text format to use if an error occurs when querying the API.                                                                                  |


## NissanConnectChargeStatus

Shows the charge status of a Nissan Leaf electric car.

| Setting               | Default | Description                                                                                                                                   |
| --------------------- | ------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| Interval              | `10`    | Interval in minutes at which to query the API and update the Tile. Default `10` minutes.                                                      |
| NickName              | `null`  | Nickname of the car to show in the Tile. This can be found in the NissanConnect App. Optional, uses first car from API if `null`.             |
| ForceRefreshAuto      | `false` | Set to `true` to try and force a data refresh from the car through the API, doesn't always work, be careful to do this at frequent intervals. |
| ForceRefreshTilePress | `false` | Set to `true` to try and force a data refresh from the car through the API when the Tile is pressed.                                          |
| FormatNotCharging     | `🔋`     | Text format to use for the Tile text when car is not charging.                                                                                |
| FormatCharging        | `⚡`     | Text format to use for the Tile text when car is charging.                                                                                    |
| FormatPluggedIn       | `🔌`     | Text format to use for the Tile text when car is plugged in but not charging.                                                                 |
| FormatError           | `🚗❌`    | Text format to use if an error occurs when querying the API.                                                                                  |


## NissanConnectBatteryStatusAge

Shows the age of the battery status of a Nissan Leaf electric car.

| Setting               | Default    | Description                                                                                                                                                        |
| --------------------- | ---------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Interval              | `10`       | Interval in minutes at which to query the API and update the Tile. Default `10` minutes.                                                                           |
| NickName              | `null`     | Nickname of the car to show in the Tile. This can be found in the NissanConnect App. Optional, uses first car from API if `null`.                                  |
| ForceRefreshAuto      | `false`    | Set to `true` to try and force a data refresh from the car through the API, doesn't always work, be careful to do this at frequent intervals.                      |
| ForceRefreshTilePress | `false`    | Set to `true` to try and force a data refresh from the car through the API when the Tile is pressed.                                                               |
| Format                | `{0}\n{1}` | Text Format to use for the Tile text. `{0}` is the age of the battery status (`TimeSpan`) and `{1}` is the date and time (`DateTimeOffset`) of the battery status. |
| FormatAge             | `HH\\:mm`  | Text Format to use for the age of the battery status. This is the format of the `{0}` value in the `Format` setting.                                               |
| FormatDateTime        | `HH\\:mm`  | Text Format to use for the date and time of the battery status. This is the format of the `{1}` value in the `Format` setting.                                     |
| FormatError           | `🚗❌`       | Text format to use if an error occurs when querying the API.                                                                                                       |


## Examples

### Plugin Settings

```json
"Plugins": [
    {
        "Name": "NissanConnect",
        "Settings": {
            "Email": "",
            "Password": ""
        }
    }
],
```

### Tile Settings

```json
{
    "Index": 5,
    "Plugin": "NissanConnect",
    "Tile": "NissanConnectBatteryLevelTile",
    "FontSize": 25,
    "FontBold": true,
    "Settings": {
        "Format": "🔋\n{0}%",
        "ForceRefreshTilePress": "true",
        "NickName": "Leaf1",
        "Interval": "10"
    }
},
{
    "Index": 6,
    "Plugin": "NissanConnect",
    "Tile": "NissanConnectChargeStatus",
    "FontSize": 40,
    "FontBold": true,
    "LineSpacing": 1.2,
    "Settings": {
        "NickName": "Leaf1",
        "Interval": "10"
    }
},
{
    "Index": 7,
    "Plugin": "NissanConnect",
    "Tile": "NissanConnectRangeTile",
    "FontSize": 20,
    "FontBold": true,
    "Settings": {
        "NickName": "Leaf1",
        "Interval": "10",
        "Format": "🏁\n{0} km"
    }
},
{
    "Index": 8,
    "Plugin": "NissanConnect",
    "Tile": "NissanConnectBatteryStatusAge",
    "FontSize": 18,
    "FontBold": true,
    "Settings": {
        "Format": "⌚\n{0}",
        "FormatAge": "h\\t\\ m\\m",
        "NickName": "Leaf1",
        "Interval": "1"
    }
}
```

## Disclaimer
This plugin is not endorsed by, directly affiliated with, maintained, authorized, or sponsored by Nissan Motor Corporation. All product and company names are the registered trademarks of their original owners. The use of any trade name or trademark is for identification and reference purposes only and does not imply any association with the trademark holder of their product brand.
