# NetworkTools

This plugin contains a collection of network related tiles.

- [NetworkTools](#networktools)
  - [ExternalIpTile](#externaliptile)
    - [Usage](#usage)
    - [Settings](#settings)
    - [Example Config](#example-config)
      - [ApiUrl](#apiurl)
      - [Format](#format)
      - [Interval](#interval)
      - [PadOctets](#padoctets)
      - [ShowIpOnTwoLines](#showipontwolines)


## ExternalIpTile
This tile queries an external API that returns your external (public) IP address.


### Usage
The tile will query your external IP address from the specified API at the specified interval and show the IP address on the key using the specified format string.

You can manually trigger an update by pressing the key.


### Settings

| Setting          | Default                 | Description                                                              |
| ---------------- | ----------------------- | ------------------------------------------------------------------------ |
| ApiUrl           | `https://api.ipify.org` | API to use to get the external IP address.                               |
| Format           | `Ext. IP:\n{0}`         | A custom format string; `{0}` is replaced by the IP address.             |
| Interval         | `60000` ms              | Update interval.                                                         |
| PadOctets        | `false`                 | Set to `true` to pad all IP octets to three digits for better alignment. |
| ShowIpOnTwoLines | `true`                  | Controls if the IP address should be wrapped on two lines or not.        |

### Example Config

```json
{
    "Plugin": "NetworkTools",
    "Tile": "ExternalIpTile",
    "FontSize": 16,
    "LineSpacing": 1.2,
    "Font": "Cascadia Code",
    "Settings": {
        "Interval": "3000",
        "ShowIpOnTwoLines": "true",
        "PadOctets": "false",
        "Format": "🌍\n{0}"
    }
}
```


#### ApiUrl
This is the URL to an API used to get the external IP address. The default API provider is; [`ipify API`](https://www.ipify.org/). It's possible to use a custom endpoint or another API service. The enpoint should return the `IPv4` address as `Content-Type: text/plain`.

#### Format
A custom format string; {0} is replaced by the IP address. Example using an emoji; `🌍\n{0}`.

#### Interval
The interval between calls to the API. 

#### PadOctets
Set to `true` to pad all IP octets to three digits for better alignment. Example; `192.168.001.010` instead of `192.168.1.10`. Padding might look better when combined with `ShowIpOnTwoLines: true`.

#### ShowIpOnTwoLines
Controls if the IP address should be wrapped on two lines or not. Setting this to `true` will show the first two IP address octets on one line and the next two on the next line.
