![Alt text](Images/icon-128.png) 

# Code Deck


## Description

**Code Deck** is a cross platform and open source alternative to the official Stream Deck application.

The `Code`-part of the name is a reference to how configuration and plugins work in **Code Deck**. The configuration is done using `JSON` and all plugins are `C#`-scripts.

![Alt text](Screenshots/00-streamdeck.png)
*Picture of **Code Deck** running my configuration on my Stream Deck.*


## Table of Contents

- [Code Deck](#code-deck)
  - [Description](#description)
  - [Table of Contents](#table-of-contents)
  - [Concept](#concept)
    - [Key](#key)
    - [Page](#page)
    - [Profile](#profile)
    - [Tile](#tile)
    - [Plugin](#plugin)
  - [Graphical User Interface](#graphical-user-interface)
  - [Configuration](#configuration)
    - [Configuration File Structure](#configuration-file-structure)
      - [Stream Deck](#stream-deck)
      - [Profile](#profile-1)
      - [Page](#page-1)
      - [Key](#key-1)
  - [Image, Icons \& Emojis](#image-icons--emojis)
    - [Image](#image)
    - [Emojis](#emojis)
    - [Icon Fonts](#icon-fonts)
  - [Plugins](#plugins)
    - [List Of Built In Plugins \& Tiles](#list-of-built-in-plugins--tiles)
    - [Plugin Development](#plugin-development)
  - [Supported Stream Decks](#supported-stream-decks)
    - [Tested Stream Decks](#tested-stream-decks)
  - [Attributions](#attributions)
  - [Disclaimer](#disclaimer)


## Concept

**Code Deck** was made with *control* and *extensibility* as the primary goals. **Code Deck** strives to be light-weight, but that heavily depends on the users plugins and configuration.


### Key
- A single key on the Stream Deck.
- There are multiple key types:
  - `Normal`
    - Normal key that can be customized with the available settings.
      - May optionally be associated with a plugin and a tile.
  - `Page`
    - Navigates to the specified page.
  - `Back`
    - Navigates to the previous page.

### Page
- A page is a logical representation of a group of keys.
- A page can contain one or more keys.

### Profile
- A profile is a logical representation of a group of pages.
- There are multiple profile types:
  - `Normal`
  - `LockScreen`
    - **Code Deck** switches to this profile when the computer is locked.
      - Currently only supported on Windows 🪟.

### Tile
- A tile implements a piece of functionality.
- A tile can modify all properties of a key.
- A tile can react to key presses.

### Plugin
- A plugin can contain one or more tiles.
- Plugins are written in `C#`.
- Plugins are compiled on the fly when **Code Deck** starts.
- All plugins can be modified by the user.


## Graphical User Interface
**Code Deck** *DOES NOT* have a graphical user interface for configuration.

Windows users may use `CodeDeck.Windows.exe` which adds an icon to the notification area on the taskbar. The icon has a context menu with easy access to the configuration file and a way to easily exit **Code Deck**.

![Alt text](Screenshots/01-notificationicon.png)


## Configuration
**Code Deck** is configured using a single `JSON`-file. The configuration file must be located in your user folder. If no configuration file exists, a default configuration will be created on startup. Changes to the configuration are automatically applied.

Full path to configuration file:
- 🪟 Windows
  - `C:\Users\{username}\.codedeck\deck.json`

- 🐧 Linux
  - `~/.codedeck/deck.json`
  - `/home/{username}/.codedeck/deck.json`


### Configuration File Structure

```json
{
  // Stream Deck
  "Brightness": 100,
  "Profiles": [
    {
      // Profile
      "Name": "DefaultProfile",
      "Pages": [
        {
          // Page
          "Name": "DefaultPage",
          "Keys": [
            {
              // Key
              "Index": 1,
              "Text": "CODE"
            },
            // More Keys ...
          ]
        },
        // More Pages ...
      ]
    },
    // More Profiles ...
  ]
}
```

#### Stream Deck

```json
{
  "DevicePath": null,
  "Brightness": 100,
  "FallbackFont": "Segoe UI Emoji",
  "Profiles": []
}
```

| Field        | Values           | Description                                                                                                              |
| ------------ | ---------------- | ------------------------------------------------------------------------------------------------------------------------ |
| DevicePath   | `null`           | Path to a specific device, can be used in the case of multiple connected devices.                                        |
| Brightness   | `100`            | Brightness in percent.                                                                                                   |
| FallbackFont | `Segoe UI Emoji` | Font to use if a glyph is not available in the `Key`-font. Set this to an emoji or icon font to easily use icons/emojis. |
| Profiles     | `Profile[]`      | An array of `Profile` objects.                                                                                           |


#### Profile

```json
{
  "Name": "DefaultProfile",
  "ProfileType": "Normal",
  "Pages": []
}
```

| Field       | Values                        | Description                 |
| ----------- | ----------------------------- | --------------------------- |
| ProfileType | `"Normal"` \| `"LockScreen"*` | Profile type.               |
| Name        | `DefaultProfile`              | Name of the profile.        |
| Pages       | `Page[]`                      | An array of `Page` objects. |

`*"LockScreen"` is only supported on Windows for now. If a profile of this type exists, **Code Deck** will automatically change to that profile when the computer is locked.


#### Page

```json
{
  "Name": "DefaultPage",
  "Pages": []
}
```

| Field | Values           | Description                 |
| ----- | ---------------- | --------------------------- |
| Name  | `DefaultProfile` | Name of the profile.        |
| Keys  | `Key[]`          | An array of `Page` objects. |


#### Key

```json
{
  "Index": 0,
  "Text": "Clock",
  "Font": "Ubuntu",
  "FontSize": 18,
  "TextColor": "#ffffff",
  "BackgroundColor": "#000000",
  "Image": "image.png",
  "ImagePadding": 5,
  "ShowFolderIndicator": false,
  "FolderIndicatorColor": "#ff0000",
  "ActivityIndicatorColor": "#00ff00",
  "KeyType": "Normal",
  "Plugin": "Clock",
  "Tile": "DigitalClockTile",
  "Settings": {
    "Format": "dd.\nMMM",
    "Interval": "60000"
  }
}
```

| Field                  | Example Values                     | Description                                                                                                                                           |
| ---------------------- | ---------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------- |
| Index                  | `0` ... `X`                        | Index of the key on the Stream Deck. `X` = number of keys - `1`.                                                                                      |
| Text                   | `"Some Text"`                      | The text to show on the key. Text is always centered. Use `\n` for multiple lines.                                                                    |
| Font                   | `"Font Name"`                      | Name of the font to use for the `Text`.                                                                                                               |
| FontSize               | `10`                               | Size of the `Font`.                                                                                                                                   |
| FontBold               | `true` \| `false`                  |                                                                                                                                                       |
| FontItalic             | `true` \| `false`                  |                                                                                                                                                       |
| LineSpacing            | `1.0`                              | Spacing between lines. Value is in percent of the line height. `1.1` means the spacing is 10% more than the line height.                              |
| TextColor              | `"#ffffff"`                        | Color of the `Text`.                                                                                                                                  |
| BackgroundColor        | `"#000000"`                        | Color of the background color of the key.                                                                                                             |
| Image                  | `filename.png`                     | Path to an image file. The image will be shown on the key.                                                                                            |
| ImagePadding           | `5`                                | A padding to apply to the image. Very useful when trying to match all image sizes.                                                                    |
| ShowFolderIndicator    | `true` \| `false`                  | An optional indicator to show on keys with `KeyType` = `Page`. The indicator is a line at the bottom of the key.                                      |
| FolderIndicatorColor   | `"#cccccc"`                        |                                                                                                                                                       |
| ActivityIndicatorColor | `"#ffff00"`                        | Color of the activity indicator. Some plugins may show this indicator while doing some task. The indicator is a small circle in the top right corner. |
| KeyType                | `"Normal"` \| `"Back"` \| `"Page"` | The type of key. Default is `"Normal"`. `"Page"` is a key that navigates to another page. `"Back"` is a key that navigates backwards.                 |
| Profile                | `"ProfileName"`                    | The name of the `Profile` that contains the `Page` to navigate to.                                                                                    |
| Page                   | `"PageName"`                       | The name of the `Page` to navigate to.                                                                                                                |
| Plugin                 | `"PluginName"`                     | The name of the `Plugin` to associate with this key.                                                                                                  |
| Tile                   | `"TileName"`                       | The name of the `Tile` to associate with this key.                                                                                                    |
| Settings               | `{ "Key": "Value", ... }`          | A key/value dictionary containing the settings needed to configure the `Tile`. All values ***MUST*** be `string`s.                                    |

All fields are optional except for `Index`.


## Image, Icons & Emojis

### Image
The `Key.Image` field can be set to the path of an image file. The image will be drawn to key. Supported formats are listed [here](https://docs.sixlabors.com/articles/imagesharp/imageformats.html).


### Emojis
As an alternative to supplying an image, you can use emojis. Use the `Text` field to specify one or more emojis. To use emojis, you have to specify a font that supports them. On Windows you can set the `Font` field to [`Segoe UI Emoji`](https://learn.microsoft.com/en-us/typography/font-list/segoe-ui-emoji).

You can also use other third party emoji fonts.

Adjust the `FontSize` field for a bigger *"icon"*.

Example:
```json
{
  "Text": "🔒",
  "Font": "Segoe UI Emoji",
  "FontSize": 50
}
```

### Icon Fonts

[`Segoe Fluent Icons`](https://learn.microsoft.com/en-us/windows/apps/design/style/segoe-fluent-icons-font) is another option on Windows.

This font contains a ***lot*** of glyphs. These glyphs can be combined with the `TextColor` field like a normal font.

There are multiple third party icon fonts that should work in the same way.

All glyphs and their unicode value is listed [here](https://learn.microsoft.com/en-us/windows/apps/design/style/segoe-fluent-icons-font). Prefix the unicode value with `\u` in the `JSON`-configuration.

Example:
```json
{
  "Text": "\ue72e",
  "TextColor": "#ff0000",
  "Font": "Segoe Fluent Icons",
  "FontSize": 50
}
```


## Plugins

**Code Deck** has a powerful, but easy to use plugin system.

Plugins are located in the `Plugins`-directory. All plugins are coded in `C#` and gets compiled on startup using `Roslyn`. All the *"built in"* plugins work in the same way and their `C#`-scripts are available in the same `Plugins`-directory.

Documentation for each plugin can be found as a markdown file in each plugins directory. The below table links directly to each plugins documentation.

### List Of Built In Plugins & Tiles

| Name                               | Tile                                                                                      | OS Support | Description                 |
| ---------------------------------- | ----------------------------------------------------------------------------------------- | ---------- | --------------------------- |
| AudioDeviceSwitcher                | AudioDeviceSwitcherTile                                                                   | 🪟          |                             |
| Clock                              | [DigitalClockTile](CodeDeck.Plugins/Plugins/Clock/Clock.md)                               | 🪟🐧         |                             |
|                                    | [StopWatchTile](CodeDeck.Plugins/Plugins/Clock/Clock.md#stopwatchtile)                    | 🪟🐧         |                             |
| Counter                            | [CounterTile](CodeDeck.Plugins/Plugins/Counter/Counter.md)                                | 🪟🐧         |                             |
| CounterStrikeGlobalOffensiveNetCon | [ExecuteCommand](CodeDeck.Plugins/Plugins/CounterStrikeGlobalOffensiveNetCon/README.md)   | 🪟🐧*        | *Linux support is untested. |
| HyperXCloudFlightWireless          | [BatteryTile](CodeDeck.Plugins/Plugins/HyperXCloudFlightWireless/README.md)               | 🪟🐧*        | *Linux support is untested. |
| Lock                               | [LockTile](CodeDeck.Plugins/Plugins/Lock/Lock.md)                                         | 🪟          |                             |
| MediaKeys                          | MuteTile                                                                                  | 🪟          |                             |
|                                    | VolumeDownTile                                                                            | 🪟          |                             |
|                                    | VolumeUpTile                                                                              | 🪟          |                             |
|                                    | NextTrackTile                                                                             | 🪟          |                             |
|                                    | PreviousTrackTile                                                                         | 🪟          |                             |
|                                    | StopTile                                                                                  | 🪟          |                             |
|                                    | PlayPauseTile                                                                             | 🪟          |                             |
| PerformanceCounters                | [CpuUsageTile](CodeDeck.Plugins/Plugins/PerformanceCounters/README.md#cpuusagetile)       | 🪟          |                             |
|                                    | [MemoryUsageTile](CodeDeck.Plugins/Plugins/PerformanceCounters/README.md#memoryusagetile) | 🪟          |                             |
|                                    | [GpuUsageTile](CodeDeck.Plugins/Plugins/PerformanceCounters/README.md#gpuusagetile)       | 🪟          |                             |
| Runner                             | [RunTile](CodeDeck.Plugins/Plugins/Runner/Runner.md#runtile)                              | 🪟🐧         |                             |
|                                    | [OpenWebsiteTile](CodeDeck.Plugins/Plugins/Runner/Runner.md#openwebsitetile)              | 🪟🐧         |                             |
| Template                           | TemplateTileOne                                                                           | 🪟🐧         |                             |
|                                    | TemplateTileTwo                                                                           | 🪟🐧         |                             |
| KeyboardSimulator                  | TyperTile                                                                                 | 🪟          |                             |
|                                    | HotkeyTile                                                                                | 🪟          |                             |
| WebRequest                         | PlainTextTile                                                                             | 🪟🐧         |                             |


### Plugin Development
Read about plugin development [here!](CodeDeck.Plugins/Plugins/)


## Supported Stream Decks

**Code Deck** *should* work with any Stream Deck hardware supported by [`OpenMacroBoard` & `StreamDeckSharp`](https://github.com/OpenMacroBoard/StreamDeckSharp).

### Tested Stream Decks

| Stream Deck Hardware          | Status   | Notes                                                                                                                 |
| ----------------------------- | -------- | --------------------------------------------------------------------------------------------------------------------- |
| Stream Deck Mini              | Untested |                                                                                                                       |
| Stream Deck (Standard) (MK.1) | ✔️        |                                                                                                                       |
| Stream Deck (Standard) (MK.2) | Untested |                                                                                                                       |
| Stream Deck +                 | Untested | Stream Deck + has an additional screen (or screen area at least) and rotary knobs. The "standard" buttons might work. |
| Stream Deck XL                | Untested |                                                                                                                       |


## Attributions

**Code Deck** makes heavy use of the following great libraries:
- [StreamDeckSharp](https://github.com/OpenMacroBoard/StreamDeckSharp)
- [ImageSharp](https://github.com/SixLabors/ImageSharp)


## Disclaimer
**Code Deck** is not endorsed by, directly affiliated with, maintained, authorized, or sponsored by Elgato. All product and company names are the registered trademarks of their original owners. The use of any trade name or trademark is for identification and reference purposes only and does not imply any association with the trademark holder of their product brand.
