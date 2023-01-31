# CounterStrikeGlobalOffensiveNetCon

This is a plugin to interact with the CS:GO console.

- [CounterStrikeGlobalOffensiveNetCon](#counterstrikeglobaloffensivenetcon)
  - [Usage](#usage)
  - [ExecuteCommand](#executecommand)
    - [Settings](#settings)
    - [Examples](#examples)
      - [Good Game Key](#good-game-key)
      - [Set In Game Volume To 10% Key](#set-in-game-volume-to-10-key)

## Usage
Use launch option `-netconport <number>` on CS:GO to set a port number for netcon to listen on.

## ExecuteCommand
Send a command to the CS:GO console.

### Settings
| Setting    | Default     | Description                                                            |
| ---------- | ----------- | ---------------------------------------------------------------------- |
| HostName   | `127.0.0.1` | The host to connect to. Can be a hostname or an IP address.            |
| NetConPort | `0`         | This is the port number from the `-netconport <number>` launch option. |
| Command    | `null`      | The command you want to send to the console.                           |

### Examples

#### Good Game Key
```json
{
    "Text": "GG",
    "FontSize": 40,
    "FontBold": true,
    "Plugin": "CounterStrikeGlobalOffensiveNetCon",
    "Tile": "ExecuteCommand",
    "Settings": {
        "NetConPort": "1337",
        "Command": "say gg\n"
    }
}
```

#### Set In Game Volume To 10% Key
```json
{
    "Text": "🔊\n10%",
    "FontSize": 25,
    "FontBold": true,
    "Plugin": "CounterStrikeGlobalOffensiveNetCon",
    "Tile": "ExecuteCommand",
    "Settings": {
        "NetConPort": "1337",
        "Command": "volume 0.1\n"
    }
}
```
