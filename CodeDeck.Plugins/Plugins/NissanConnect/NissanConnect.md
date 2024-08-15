# Runner

This plugin can start programs and open files and websites as well as run other shell actions.

- [Runner](#runner)
  - [RunTile](#runtile)
    - [Settings](#settings)
      - [Program](#program)
      - [Arguments](#arguments)
      - [UseShellExecute](#useshellexecute)
    - [Examples](#examples)
  - [OpenWebsiteTile](#openwebsitetile)
    - [Settings](#settings-1)
      - [Program](#program-1)
    - [Examples](#examples-1)


## RunTile

Runs a program by specifying an executable file or shortcut file.

### Settings

| Setting         | Default | Description                             |
| --------------- | ------- | --------------------------------------- |
| Program         | `null`  | The program, shortcut or action to run. |
| Arguments       | `null`  | The arguments sent to the program.      |
| UseShellExecute | `false` | Set to `true` to run using the shell.   |

#### Program
The name of the application to start, the name of a document, a folder or website.

#### Arguments
The set of command-line arguments to use when starting the application.

#### UseShellExecute
A value indicating whether to use the operating system shell to start the process. This must be set to `true` for most actions except running an executable file using its full file name.

### Examples

Run `calc.exe`:
```json
    {
        "Plugin": "Runner",
        "Tile": "RunTile",
        "Settings": {
            "Program": "calc.exe",
        }
    }
```

Open `Network Connections`:
```json
    {
        "Plugin": "Runner",
        "Tile": "RunTile",
        "Settings": {
            "Program": "ncpa.cpl",
            "Arguments": null,
            "UseShellExecute": "true"
        }
    }
```



## OpenWebsiteTile

Opens a website. This tile is a wrapper for using `RunTile` with a website address as the `Program` and `UseShellExecute` set to `true`.

This tile also tries to fetch the favicon of the website and use that as the tile image unless the image property has been overridden in the configuration.

### Settings

| Setting | Default | Description          |
| ------- | ------- | -------------------- |
| Url     | `null`  | The website to open. |


#### Program
The address of a website to open.

### Examples

Open the `Code Deck` website:
```json
    {
        "Plugin": "Runner",
        "Tile": "OpenWebsiteTile",
        "Settings": {
            "Url": "https://heinandre.no/code-deck"
        }
    },
```
