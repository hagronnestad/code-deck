# WebRequest
The WebRequest plugin contains a collection of tiles that can request and display data from the Internet.


- [WebRequest](#webrequest)
  - [PlainTextTile](#plaintexttile)
  - [Usage](#usage)
    - [Settings](#settings)
    - [Example Config](#example-config)
      - [Url](#url)
      - [Format](#format)
      - [Interval](#interval)
  - [ImageTile](#imagetile)
  - [Usage](#usage-1)
    - [Settings](#settings-1)
    - [Example Config](#example-config-1)
      - [Url](#url-1)
      - [Interval](#interval-1)
      - [Crop](#crop)


## PlainTextTile
This tile does a web request to the specified URL and shows the text result on the key. This tile should be used when requesting plain text (`Content-Type: text/plain`) data.


## Usage
A use case for this tile could be to retrieve and display some sensor data or maybe get a random integer from [random.org](https://www.random.org).


### Settings

| Setting  | Default    | Description                        |
| -------- | ---------- | ---------------------------------- |
| Url      | `null`     | The URL to request.                |
| Format   | `{0}`      | How to format the text on the key. |
| Interval | `60000` ms | Update interval.                   |


### Example Config

```json
{
  "FontSize": 22,
  "FontBold": true,
  "LineSpacing": 1.2,
  "Plugin": "WebRequest",
  "Tile": "PlainTextTile",
  "ActivityIndicatorColor": "#ffffff",
  "BackgroundColor": "#4d0977",
  "TextColor": "#bbbbbb",
  "Settings": {
    "Interval": "10000",
    "Url": "https://www.random.org/integers/?num=1&min=1&max=50&col=1&base=10&format=plain&rnd=new",
    "Format": "🌡️\n{0}°"
  }
}
```


#### Url
The URL to retrieve data from. The URL should point to some data served as plain text (`Content-Type: text/plain`).


#### Format
The format to use when showing the data on the key.

Examples:
- `{0}` - Shows only the retrieved data
- `{0}°` - Shows the data with a degrees symbol suffix
- `{0} %` - Shows the data with a space and a percent symbol suffix
- `TEMP\n{0}°` - Shows the data with a `TEMP`-prefix followed by a linebreak and the data with a degrees symbol suffix


#### Interval
The time between requests to the specified URL.


## ImageTile
This tile does a web request to the specified URL and shows the retrieved image on the key. This tile should be used to retrieve image data. The image data must be in a format listed [here](https://docs.sixlabors.com/articles/imagesharp/imageformats.html).


## Usage
A use case could be to display an image from a webcam.


### Settings

| Setting  | Default    | Description                     |
| -------- | ---------- | ------------------------------- |
| Url      | `null`     | The URL to request.             |
| Interval | `60000` ms | Update interval.                |
| Crop     | `false`    | Crop the image to fill the key. |


### Example Config
```json
  {
  "Plugin": "WebRequest",
  "Tile": "ImageTile",
  "Settings": {
      "Url": "https://heinandre.no/code-deck/Images/icon-128.png"
    }
  }
```


#### Url
The URL to retrieve an image from. The image data must be in a format listed [here](https://docs.sixlabors.com/articles/imagesharp/imageformats.html).


#### Interval
The time between requests to the specified URL.


#### Crop
If set to `true`, the image will be cropped to a square aspect ratio to fill the entire key. The image will be cropped from the center of the image.
