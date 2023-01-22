# Plugin Development

**Code Deck** supports plugins. Plugins are located in the `Plugins`-directory. All plugins are coded in `C#` and gets compiled on startup using `Roslyn`. All the *"builtin"* (included in this repository) plugins work in the same way and their `C#`-scripts are available in the same `Plugins`-directory.

- [Plugin Development](#plugin-development)
  - [Quick Start](#quick-start)
  - [Development Environment \& Debugging](#development-environment--debugging)
    - [Developing Without IDE](#developing-without-ide)
  - [Nuget \& External Libraries](#nuget--external-libraries)


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


## Development Environment & Debugging

- All you need to modify and create plugins is a text editor. A code editor with C# support would be even better.

- If you want to have your plugin included as a part of **Code Deck**, I would suggest cloning the whole **Code Deck** repo from GitHub and create a new plugin in the `CodeDeck.Plugins`-project using Visual Studio or Visual Studio Code.
  - This also improves the development process because the plugin is compiled as part of the project and any errors show up in the IDE.
  - ❕Even though the plugins are compiled as part of the project, the source files still gets copied to the output directory where **Code Deck** will load and compile them on the fly during startup. This means that breakpoints set inside a plugin source file while debugging **Code Deck** will *NOT* be hit.❕


### Developing Without IDE

If you don't want to clone the repo or use an IDE, your only option is to run **Code Deck** and check the console/log output for compilation errors.

An example would look something like this:
```log
CodeDeck.PluginSystem.PluginLoader: Error: SourceFile([383..387)): The name '_ctn' does not exist in the current context
CodeDeck.PluginSystem.PluginLoader: Warning: Plugin Plugins\Counter did not produce an assembly.
```


## Nuget & External Libraries
All Nuget dependencies for the current built in plugins are added to the `CodeDeck.Plugins`-project. This is a temporary solution. I would like to remove all dependencies from the `CodeDeck.Plugins`-project and add support for Nuget at the plugin level.
