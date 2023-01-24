# Clock

The clock plugin shows the current time and/or date on a key.

## DigitalClockTile

Shows the current time and/or date in digital format on a key. Time format can be customized. The update interval can be adjusted to fit the chosen time format.

### Settings

| Setting  | Default   | Description                                                                                          |
|----------|-----------|------------------------------------------------------------------------------------------------------|
| Format   | `HH\\:mm` | [Format](https://learn.microsoft.com/en-us/dotnet/api/system.datetime.tostring?view=net-7.0) string. |
| Interval | `1000` ms | Update interval.                                                                                     |

### Example:

```json
    "Settings": {
        "Format": "HH\\:mm\ndddd",
        "Interval": "60000"
    }
```

#### Format
Format string as documented [here](https://learn.microsoft.com/en-us/dotnet/api/system.datetime.tostring?view=net-7.0).

***Notice!*** Some characters like `:` must be escaped using `\`. The `\`-character itself must also be escaped in `JSON`.

#### Interval
Adjust the update interval to the desired resolution. If you include seconds in the time format you probably want to set the interval to `<= 1000 ms`. If your time format only includes hours an minutes, the interval might only need to be `~ 60000 ms`.
