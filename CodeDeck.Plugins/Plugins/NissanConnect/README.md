# NissanConnect

This plugin can be used to show information about a Nissan Leaf electric car. The plugin uses the [Nissan Connect Client Library](https://github.com/hagronnestad/nissan-connect-dotnet) and requires a valid username and password for the Nissan Connect service. The API is not officially supported by Nissan and may stop working at any time. And the information provided by the API may not be up to date.

- [NissanConnect](#nissanconnect)
  - [Vehicle selection](#vehicle-selection)
  - [NissanConnectBatteryLevelTile](#nissanconnectbatteryleveltile)
  - [NissanConnectRangeTile](#nissanconnectrangetile)
  - [NissanConnectChargeStatus](#nissanconnectchargestatus)
  - [NissanConnectBatteryStatusAge](#nissanconnectbatterystatusage)
  - [Examples](#examples)
    - [Plugin Settings](#plugin-settings)
    - [Tile Settings](#tile-settings)
  - [Disclaimer](#disclaimer)

## Vehicle selection

Each tile can select a car by `Vin` or `NickName`. Prefer `Vin` because it is a
stable vehicle identifier; nicknames can change, be absent, or move between the
API's `nickname` and `vehicleNickName` fields.

Selection uses the following priority:

1. If `Vin` is set, select the car with that VIN.
2. Otherwise, if `NickName` is set, match it case-insensitively against either
   nickname field returned by the API.
3. If neither setting is set, use the first car returned by the API.

Blank identifiers are treated as unset. An unmatched explicit VIN does not fall
back to a nickname or another car. Each tile retries vehicle selection on every
update, including key presses. A missing vehicle, missing VIN, failed API request,
or invalid format displays `FormatError` and reports the failure through the host's
task diagnostics or key-press logging.

Authentication tokens are cached in the plugin's `token.cache` file. Treat this
file as sensitive. If cached authentication can no longer be refreshed, the
plugin uses the configured `Email` and `Password` to log in again and retries the
failed read once. Failed initialization is retried by subsequent tile updates.

All tiles honor `ForceRefreshAuto` and `ForceRefreshTilePress`. A forced refresh
sends a remote battery-refresh request, waits 60 seconds, then reads the status.
A rejected refresh request is reported as an error. Keep automatic refresh
disabled unless needed, and avoid frequent requests to the car.


## NissanConnectBatteryLevelTile

Shows the battery level of a Nissan Leaf electric car.

| Setting               | Default | Description                                                                                                                                   |
| --------------------- | ------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| Interval              | `10`    | Interval in minutes at which to query the API and update the Tile. Default `10` minutes.                                                      |
| Vin                   | `null`  | VIN of the car to show. Preferred over `NickName` when set.                                                                                   |
| NickName              | `null`  | Nickname shown in the NissanConnect app. Matches either nickname field returned by the API when `Vin` is not set.                            |
| ForceRefreshAuto      | `false` | Set to `true` to try and force a data refresh from the car through the API, doesn't always work, be careful to do this at frequent intervals. |
| ForceRefreshTilePress | `false` | Set to `true` to try and force a data refresh from the car through the API when the Tile is pressed.                                          |
| Format                | `{0}`   | Format to use for the Tile text. `{0}` is the battery level in percentage. Example: `🔋\n{0}%`                                                 |
| FormatError           | `🚗❌`    | Text format to use if an error occurs when querying the API.                                                                                  |


## NissanConnectRangeTile

Shows the range of a Nissan Leaf electric car.

| Setting               | Default | Description                                                                                                                                   |
| --------------------- | ------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| Interval              | `10`    | Interval in minutes at which to query the API and update the Tile. Default `10` minutes.                                                      |
| Vin                   | `null`  | VIN of the car to show. Preferred over `NickName` when set.                                                                                   |
| NickName              | `null`  | Nickname shown in the NissanConnect app. Matches either nickname field returned by the API when `Vin` is not set.                            |
| ForceRefreshAuto      | `false` | Set to `true` to try and force a data refresh from the car through the API, doesn't always work, be careful to do this at frequent intervals. |
| ForceRefreshTilePress | `false` | Set to `true` to try and force a data refresh from the car through the API when the Tile is pressed.                                          |
| Format                | `🏁\n{0} km\n({1} km)` | Format to use for the Tile text. `{0}` is the range with HVAC `ON` and `{1}` is the range with HVAC `OFF`. Example: `🏁\n{0} km\n({1} km)` |
| FormatError           | `🚗❌`    | Text format to use if an error occurs when querying the API.                                                                                  |


## NissanConnectChargeStatus

Shows the charge status of a Nissan Leaf electric car.

The tile displays `FormatCharging` only when both charging and plugged in,
`FormatPluggedIn` when plugged in but not charging (including completed or
scheduled charging), and `FormatNotCharging` when unplugged and not charging.
API error states, unknown state values, and the contradictory combination of
charging while unplugged display `FormatError` and report a diagnostic instead
of leaving an old state on screen.

| Setting               | Default | Description                                                                                                                                   |
| --------------------- | ------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| Interval              | `10`    | Interval in minutes at which to query the API and update the Tile. Default `10` minutes.                                                      |
| Vin                   | `null`  | VIN of the car to show. Preferred over `NickName` when set.                                                                                   |
| NickName              | `null`  | Nickname shown in the NissanConnect app. Matches either nickname field returned by the API when `Vin` is not set.                            |
| ForceRefreshAuto      | `false` | Set to `true` to try and force a data refresh from the car through the API, doesn't always work, be careful to do this at frequent intervals. |
| ForceRefreshTilePress | `false` | Set to `true` to try and force a data refresh from the car through the API when the Tile is pressed.                                          |
| FormatNotCharging     | `🔋`     | Text format when the car is unplugged and not charging.                                                                                      |
| FormatCharging        | `⚡`     | Text format when the car is plugged in and charging.                                                                                         |
| FormatPluggedIn       | `🔌`     | Text format to use for the Tile text when car is plugged in but not charging.                                                                 |
| FormatError           | `🚗❌`    | Text format to use if an error occurs when querying the API.                                                                                  |


## NissanConnectBatteryStatusAge

Shows the age of the battery status of a Nissan Leaf electric car.

| Setting               | Default    | Description                                                                                                                                                        |
| --------------------- | ---------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Interval              | `10`       | Interval in minutes at which to query the API and update the Tile. Default `10` minutes.                                                                           |
| Vin                   | `null`     | VIN of the car to show. Preferred over `NickName` when set.                                                                                                        |
| NickName              | `null`     | Nickname shown in the NissanConnect app. Matches either nickname field returned by the API when `Vin` is not set.                                                 |
| ForceRefreshAuto      | `false`    | Set to `true` to try and force a data refresh from the car through the API, doesn't always work, be careful to do this at frequent intervals.                      |
| ForceRefreshTilePress | `false`    | Set to `true` to try and force a data refresh from the car through the API when the Tile is pressed.                                                               |
| Format                | `{0}\n{1}` | Text Format to use for the Tile text. `{0}` is the age of the battery status (`TimeSpan`) and `{1}` is the date and time (`DateTimeOffset`) of the battery status. |
| FormatAge             | `hh\\:mm`  | `TimeSpan` format for the age (`{0}`). Use lowercase `h`/`hh`, not `HH`; hours are the component within a day. Use `d\\.hh\\:mm` to include days. |
| FormatDateTime        | `HH\\:mm`  | Text Format to use for the date and time of the battery status. This is the format of the `{1}` value in the `Format` setting.                                     |
| FormatError           | `🚗❌`       | Text format to use if an error occurs when querying the API.                                                                                                       |

If an existing configuration explicitly sets `FormatAge` to `HH\\:mm`, change it
to `hh\\:mm`. Uppercase `HH` remains valid for `FormatDateTime`, but not for a
`TimeSpan`.


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
        "Vin": "YOUR_VEHICLE_VIN",
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
        "Vin": "YOUR_VEHICLE_VIN",
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
        "Vin": "YOUR_VEHICLE_VIN",
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
        "Vin": "YOUR_VEHICLE_VIN",
        "Interval": "1"
    }
}
```

## Disclaimer
This plugin is not endorsed by, directly affiliated with, maintained, authorized, or sponsored by Nissan Motor Corporation. All product and company names are the registered trademarks of their original owners. The use of any trade name or trademark is for identification and reference purposes only and does not imply any association with the trademark holder of their product brand.
