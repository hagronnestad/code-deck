# Weather
The Weather plugin contains a collection of tiles for displaying weather data.


- [Weather](#weather)
  - [YrImageTile](#yrimagetile)
  - [Usage](#usage)
    - [Settings](#settings)
    - [Example Config](#example-config)
      - [Interval](#interval)
      - [Lat / Lon](#lat--lon)
      - [Period](#period)


## YrImageTile
This tile uses the [yr.no](https://developer.yr.no/) weather API to show an image that describes the upcoming weather.


## Usage
The tile can be configured to show an image that describes the weather in the next 1, 6 or 12 hours.

Updates run immediately and then at the configured interval. Key presses also
update the image, without overlapping that tile's periodic request. A failed
request, unavailable forecast period, or missing icon is reported through
`ILogger` and recurring-task diagnostics instead of silently leaving a blank tile.
The last successful image is retained and the next scheduled update retries.

The plugin identifies itself to MET Norway using a User-Agent with the Code Deck
repository URL. Tiles share one HTTP client per plugin load, without changing
headers during requests. Icons are loaded from the plugin's own `Icons` directory;
deploy all of that directory along with the assembly.


### Settings

| Setting  | Default      | Description                                                |
| -------- | ------------ | ---------------------------------------------------------- |
| Interval | `600 000` ms | Update interval. Default is 10 minutes (`10 * 60 * 1000`). |
| Lat      | `0`          | The latitude of the location to fetch weather data for.    |
| Lon      | `0`          | The longitude of the location to fetch weather data for.   |
| Period   | `null`       | The period of the weather summary.                         |



### Example Config

```json
{
  "Plugin": "Weather",
  "Tile": "YrImageTile",
  "Text": "Next Hour",
  "FontSize": 14,
  "TextOffsetY": 20,
  "TextColor": "#ffffff",
  "ImagePadding": 7,
  "ImageOffsetX": 0,
  "ImageOffsetY": -8,
  "Settings": {
    "Lat": "59.1484",
    "Lon": "5.2611",
    "Period": "Next1Hours"
  }
}
```


#### Interval
The time between requesting an update from the Weather API. The interval must be set to a reasonable value.


#### Lat / Lon
The location to fetch weather data for.

Use dot-decimal coordinates regardless of the computer's locale. Legacy
comma-decimal settings remain accepted. Latitude must be between -90 and 90,
longitude between -180 and 180, and both must be finite. API coordinates are
formatted with at most four decimal places, as required by MET Norway.


#### Period
The period of the weather summary. This is based on the available data provided by the API.

Possible values are:
- `Next1Hours` (This is the default period if no period is specified.)
- `Next6Hours`
- `Next12Hours`
