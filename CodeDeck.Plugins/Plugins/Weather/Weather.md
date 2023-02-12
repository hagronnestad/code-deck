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
    "Lat": "59,1484",
    "Lon": "5,2611",
    "Period": "Next1Hours"
  }
}
```


#### Interval
The time between requesting an update from the Weather API. The interval must be set to a reasonable value.


#### Lat / Lon
The location to fetch weather data for.


#### Period
The period of the weather summary. This is based on the available data provided by the API.

Possible values are:
- `Next1Hours` (This is the default period if no period is specified.)
- `Next6Hours`
- `Next12Hours`
