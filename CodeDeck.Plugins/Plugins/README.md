# Plugin Development

**Code Deck** supports plugins. Plugins are located in the `Plugins`-directory. All plugins are coded in `C#` and gets compiled on startup using `Roslyn`. All the *"builtin"* (included in this repository) plugins work in the same way and their `C#`-scripts are available in the same `Plugins`-directory.

- [Plugin Development](#plugin-development)
  - [Quick Start](#quick-start)
  - [Plugin Directory Structure](#plugin-directory-structure)
  - [Develop A *User* Plugin](#develop-a-user-plugin)
    - [Debugging](#debugging)
    - [External Libraries](#external-libraries)
    - [Nuget](#nuget)
      - [Example From The `PerformanceCounters` Plugin](#example-from-the-performancecounters-plugin)
  - [Develop A *Builtin* Plugin](#develop-a-builtin-plugin)
    - [`CodeDeck.Plugins`-project](#codedeckplugins-project)
    - [Documentation](#documentation)


## Quick Start
It's very easy to make your own plugins for **Code Deck** by using one of the existing plugins as a template. The [Counter-plugin](Counter/Counter.cs) might be a good starting point for a minimal plugin implementation.

Have a look at the [Template-plugin](Template/Template.cs) for a more detailed overview of all the plugin features.

Below is a summary of the plugin skeleton and features:

```csharp
// A wrapper class for the Plugin
public class Template : CodeDeckPlugin
{
    // Static field that can be shared between the Tile classes
    private static DateTime _dateTimeNow;

    // A static constructor that initializes shared static fields
    static Template()
    {
        _dateTimeNow = DateTime.Now;
    }

    // An example of how to implement a new Tile, a Tile class MUST extend the Tile class
    public class TemplateTileOne : Tile
    {
        // This property will be automatically bound to the appropriate setting from
        // the JSON configuration file.
        [Setting] public int? Counter { get; set; }

        // The constructor for a Tile
        // The constructor may be used for assigning private fields, but DO NOT start long
        // running tasks in the constructor!
        public TemplateTileOne()
        {
            Counter = 0;
        }

        // This method is called by the plugin system when this Tile is instantiated.
        // Use this method as you would normally use the constructor. Make sure to forward
        // the CancellationToken to any long running tasks or background tasks.
        public override Task Init(CancellationToken cancellationToken);

        // This method is called when a key associated with this Tile is pressed
        public override Task OnTilePressDown(CancellationToken cancellationToken);

        // This method is called when a key associated with this Tile is released
        public override Task OnTilePressUp(CancellationToken cancellationToken);

        // This method is called when a Tile instance is removed
        public override Task DeInit();
    }


    // A plugin my contain multiple Tile types
    public class TemplateTileTwo : Tile
    {
        // A Tile can override only some of the Tile methods
        public override Task OnTilePressDown(CancellationToken cancellationToken);
    }
}

```


## Plugin Directory Structure



```powershell
"Code Deck Root Directory"
└───Plugins
    └───MyCustomPlugin
        │   # All C# source files making up the plugin
        │   SourceFile1.cs
        │   SourceFile2.cs
        │   ...
        │
        ├───bin
        │       # Compiled assemblies, no user files should be placed here
        │       MyCustomPlugin.dll
        │       MyCustomPlugin.pdb
        │
        └───lib
                # Any external libraries goes in here
                MyExternalLib1.dll
                MyExternalLib2.dll
                ...
```


## Develop A *User* Plugin
This is a plugin developed by a regular user.

All you need to modify or create plugins is a text editor. A code editor with `C#` syntax highlighting is recommended.


### Debugging

When developing a plugin it is suggested to run **Code Deck** through `CodeDeck.exe` in a terminal. This will give you access to the log output which shows errors and warnings.

An example would look something like this:
```log
CodeDeck.PluginSystem.PluginLoader: Error: SourceFile([383..387)): The name '_ctn' does not exist in the current context
CodeDeck.PluginSystem.PluginLoader: Warning: Plugin Plugins\Counter did not produce an assembly.
```

### External Libraries

It is possible to use external libraries by adding them to a `lib` directory.

*Example from `AudioDeviceSwitcher` plugin:*
```powershell
Plugins
├───AudioDeviceSwitcher.cs
│
├───bin
│       AudioDeviceSwitcher.dll
│       AudioDeviceSwitcher.pdb
│
└───lib
        AudioSwitcher.AudioApi.CoreAudio.dll
        AudioSwitcher.AudioApi.dll
        System.Drawing.Common.dll
        System.Windows.Extensions.dll
```

### Nuget
Automatic Nuget support through `packages.json` or similar is planned, but not implemented. It's possible to download Nuget-packages and add them to the `lib` directory manually.

***⚠️ Make sure to add the correct (runtime/framework version) assembly from the Nuget and only one assembly with the same name. Code Deck currently uses `net7.0`.***

#### Example From The `PerformanceCounters` Plugin

Install `System.Diagnostics.PerformanceCounter` using `nuget.exe`:

```powershell
> nuget install System.Diagnostics.PerformanceCounter

Installing package 'System.Diagnostics.PerformanceCounter' to 'Plugins\PerformanceCounters'.
  GET https://api.nuget.org/v3/registration5-gz-semver2/system.diagnostics.performancecounter/index.json
  OK https://api.nuget.org/v3/registration5-gz-semver2/system.diagnostics.performancecounter/index.json 606ms


Attempting to gather dependency information for package 'System.Diagnostics.PerformanceCounter.7.0.0' with respect to project 'Plugins\PerformanceCounters', targeting 'Any,Version=v0.0'
Gathering dependency information took 18 ms
Attempting to resolve dependencies for package 'System.Diagnostics.PerformanceCounter.7.0.0' with DependencyBehavior 'Lowest'
Resolving dependency information took 0 ms
Resolving actions to install package 'System.Diagnostics.PerformanceCounter.7.0.0'
Resolved actions to install package 'System.Diagnostics.PerformanceCounter.7.0.0'
Retrieving package 'System.Diagnostics.PerformanceCounter 7.0.0' from 'nuget.org'.
Adding package 'System.Diagnostics.PerformanceCounter.7.0.0' to folder 'Plugins\PerformanceCounters'
Added package 'System.Diagnostics.PerformanceCounter.7.0.0' to folder 'Plugins\PerformanceCounters'
Successfully installed 'System.Diagnostics.PerformanceCounter 7.0.0' to Plugins\PerformanceCounters
Executing nuget actions took 236 ms
```

Now lets have a look inside the Nuget and figure out which assembly to use.

```powershell
> cd System.Diagnostics.PerformanceCounter.7.0.0

> tree /F

├───lib
│   ├───net462
│   │       System.Diagnostics.PerformanceCounter.dll
│   │       System.Diagnostics.PerformanceCounter.xml
│   │
│   ├───net6.0
│   │       System.Diagnostics.PerformanceCounter.dll
│   │       System.Diagnostics.PerformanceCounter.xml
│   │
│   ├───net7.0
│   │       System.Diagnostics.PerformanceCounter.dll
│   │       System.Diagnostics.PerformanceCounter.xml
│   │
│   └───netstandard2.0
│           System.Diagnostics.PerformanceCounter.dll
│           System.Diagnostics.PerformanceCounter.xml
│
└───runtimes
    └───win
        └───lib
            ├───net6.0
            │       System.Diagnostics.PerformanceCounter.dll
            │       System.Diagnostics.PerformanceCounter.xml
            │
            └───net7.0
                    System.Diagnostics.PerformanceCounter.dll
                    System.Diagnostics.PerformanceCounter.xml


```
If `lib` contains an assembly for `net7.0` (or `net6.0`), this is usually the assembly you want.

However, for `System.Diagnostics.PerformanceCounter.dll`, the assembly in `lib` will give you an error similar to: "Performance Counters are not supported on this platform." at runtime and that's because `System.Diagnostics.PerformanceCounter.dll` is platform specific.

In the above case we have to use the `System.Diagnostics.PerformanceCounter.dll` assembly from `System.Diagnostics.PerformanceCounter.7.0.0\runtimes\win\lib\net7.0`.

Copy the correct assembly to the plugins `lib` directory:

```powershell
\publish\Plugins>tree /F

└───PerformanceCounters
    │   PerformanceCounters.cs
    │
    └───lib
            System.Diagnostics.PerformanceCounter.dll

```

**Code Deck** should now be able to compile the plugin against the added assembly and reference the added assembly at runtime.


## Develop A *Builtin* Plugin
This is inteded for advanced users and mainly for plugins that are general enough that it makes sense to include them into **Code Deck** itself.

If you want to have your plugin included as a part of the **Code Deck** application, I would suggest cloning the whole **Code Deck** repo from GitHub and creating a new plugin directory in the `CodeDeck.Plugins`-project using Visual Studio or Visual Studio Code.


### `CodeDeck.Plugins`-project
The `CodeDeck.Plugins`-project is just a container project for the plugins. Each plugin should have their own directory and the source files should be set to `Build Action: Content` and `Copy to output directory: Copy if newer`.

The `CodeDeck.Plugins`-project should ***NOT*** reference any external packages.


### Documentation
Add a `README.md` file in the same directory as the rest of the plugin. Use the `README.md` from one of the other plugins as a template.
