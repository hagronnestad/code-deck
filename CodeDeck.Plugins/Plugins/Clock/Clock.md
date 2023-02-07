# Clock
The clock plugin contains a collection of time related tiles.


- [Clock](#clock)
  - [DigitalClockTile](#digitalclocktile)
    - [Settings](#settings)
    - [Example Config](#example-config)
      - [Format](#format)
      - [Interval](#interval)
  - [StopWatchTile](#stopwatchtile)
    - [Usage](#usage)
    - [Settings](#settings-1)
    - [Example Config](#example-config-1)
      - [Format](#format-1)
      - [Interval](#interval-1)
      - [HoldResetTime](#holdresettime)


## DigitalClockTile

Shows the current time and/or date in digital format on a key. Time format can be customized. The update interval can be adjusted to fit the chosen time format.

### Settings

| Setting  | Default   | Description                                                                                          |
| -------- | --------- | ---------------------------------------------------------------------------------------------------- |
| Format   | `HH\\:mm` | [Format](https://learn.microsoft.com/en-us/dotnet/api/system.datetime.tostring?view=net-7.0) string. |
| Interval | `1000` ms | Update interval.                                                                                     |

### Example Config

```json
{
    "Plugin": "Clock",
    "Tile": "DigitalClockTile",
    "Settings": {
        "Format": "HH\\:mm\ndddd",
        "Interval": "60000"
    }
}
```

#### Format
Format string as documented [here](https://learn.microsoft.com/en-us/dotnet/api/system.datetime.tostring?view=net-7.0).

***Notice!*** Some characters like `:` must be escaped using `\`. The `\`-character itself must also be escaped in `JSON`.

#### Interval
Adjust the update interval to the desired resolution. If you include seconds in the time format you probably want to set the interval to `<= 1000 ms`. If your time format only includes hours an minutes, the interval might only need to be `~ 60000 ms`.



## StopWatchTile

A stopwatch with customizable format. The update interval can be adjusted to fit the chosen time format.

### Usage
Press the key once to start the stopwatch, press the key again to stop it. Hold the key for `500 ms` (configurable using `HoldToResetTime`) to reset the stopwatch. The activity indicator is shown while the stopwatch is running.


### Settings

| Setting         | Default          | Description                                                                                                     |
| --------------- | ---------------- | --------------------------------------------------------------------------------------------------------------- |
| Format          | `\\⏱'\n'mm\\:ss` | [Format](https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-timespan-format-strings) string. |
| Interval        | `100` ms         | Update interval.                                                                                                |
| HoldToResetTime | `500` ms         | Minimum time to hold the key down to reset the stopwatch.                                                       |

### Example Config

A stopwatch configured with a format and interval allowing for higher resolution.

```json
{
    "Plugin": "Clock",
    "Tile": "StopWatchTile",
    "Settings": {
        "Format": "\\⏱'\n'mm\\:ss\\.ff",
        "Interval": "10",
        "HoldToResetTime": "500"
    }
}
```

#### Format
Format string as documented [here](https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-timespan-format-strings).

***Notice!*** Some characters like `:` must be escaped using `\`. The `\`-character itself must also be escaped in `JSON`.

#### Interval
Adjust the update interval to the desired resolution. If you include seconds in the time format you probably want to set the interval to `<= 1000 ms`. If your time format includes milliseconds, the interval must be at the most `< 100 ms`, depending how high resolution you want.

#### HoldResetTime
The minimum amount of time in `milliseconds` to hold the associated key for before the stopwatch is reset.
