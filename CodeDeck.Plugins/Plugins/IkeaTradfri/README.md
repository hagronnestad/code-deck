# IkeaTradfri

This plugin can control IKEA Trådfri lights, outlets and blinds.

- [IkeaTradfri](#ikeatradfri)
  - [TradfriLightTile](#tradfrilighttile)
    - [Usage](#usage)
    - [Settings](#settings)
    - [Example Config](#example-config)
      - [Lights](#lights)
      - [On](#on)
      - [Brightness](#brightness)
      - [Color](#color)
  - [TradfriBlindTile](#tradfriblindtile)
    - [Usage](#usage-1)
    - [Settings](#settings-1)
    - [Example Config](#example-config-1)
      - [Blinds](#blinds)
      - [Position](#position)
  - [TradfriOutletTile](#tradfrioutlettile)
    - [Usage](#usage-2)
    - [Settings](#settings-2)
    - [Example Config](#example-config-2)
      - [Outlets](#outlets)
      - [On](#on-1)


## TradfriLightTile
This tile controls IKEA Trådfri Lights. The tile can be configured to turn a light ON or OFF as well as set the brightness for dimmable lights.


### Usage
Configure the tile with a single or multiple named lights. The default behaviour turns the light(s) ON when the key is pressed.


### Settings

| Setting    | Default | Description                                                      |
| ---------- | ------- | ---------------------------------------------------------------- |
| Lights     | `null`  | A list of named lights to control.                               |
| On         | `true`  | Can be set to `true`, `false`.                                   |
| Brightness | `null`  | Controls the brightness. Value should be a percentage (`0-100`). |
| Color      | `null`  | Sets the color temperature of the light.                         |


### Example Config
This example config will create a key that sets the brightness off all my office lights to 50%.

```json
{
  "Plugin": "IkeaTradfri",
  "Tile": "TradfriLightTile",
  "Settings": {
    "Lights": "Kontor 1, Kontor 2, Kontor 3, Kontor 4, Kontor 5, Kontor 6, Kontor 7, Kontor 8",
    "Brightness": "50"
  }
}
```


#### Lights
A list of one or more named lights to control. The light names can be separated by a `,` or a `;`.

#### On
Set to `true` or `false` to create a key that turns a light ON or OFF.

#### Brightness
Controls the brightness. Value should be a percentage (`0-100`).

#### Color
Sets the color temperature of the light. Valid color temperatures are: `cool`, `warm`, `incadescent`, `candle`.


---


## TradfriBlindTile
This tile controls IKEA Trådfri Blinds. The tile can be configured to fully or partially open and close blinds.


### Usage
Configure the tile with a single or multiple named blinds.


### Settings

| Setting  | Default | Description                                            |
| -------- | ------- | ------------------------------------------------------ |
| Blinds   | `null`  | A list of named blinds to control.                     |
| Position | `null`  | A percentage indicating the amount of coverage wanted. |


### Example Config
This example config will create a key that sets my blind to 50% coverage.

```json
{
  "Plugin": "IkeaTradfri",
  "Tile": "TradfriBlindTile",
  "Settings": {
    "Blinds": "Vindu Kontor",
    "Position": "50"
  }
}
```


#### Blinds
A list of one or more named blinds to control. The blind names can be separated by a `,` or a `;`.

#### Position
A percentage (`0-100`) indicating the amount of coverage wanted, `0` is fully open and `100` is fully closed. A value of `50` would position the blind halfway down the window.


---


## TradfriOutletTile
This tile controls IKEA Trådfri Outlets. The tile can be configured to turn an outlet either ON or OFF with separate keys or toggle it ON/OFF with the same key.


### Usage
Configure the tile with a single or multiple named outlets. The default behaviour toggles the outlets ON/OFF with a key press.


### Settings

| Setting | Default | Description                              |
| ------- | ------- | ---------------------------------------- |
| Outlets | `null`  | A list of named outlets to control.      |
| On      | `null`  | Can be set to `true`, `false` or `null`. |


### Example Config
This example config will create a key that toggles the "Lab Equipment"-outlet on and off.

```json
{
  "Text": "🔌",
  "FontSize": 50,
  "Plugin": "IkeaTradfri",
  "Tile": "TradfriOutletTile",
  "Settings": {
    "Outlets": "Lab Equipment"
  }
},
```


#### Outlets
A list of one or more named outlets to control. The outlet names can be separated by a `,` or a `;`.

#### On
Set to `true` or `false` to create a key that turns an outlet ON or OFF. Use `null` if you want the key to toggle the outlet.
