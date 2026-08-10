# Plugin Development

Every built-in plugin is a standard .NET project located in its plugin directory. Plugins reference `CodeDeck.PluginAbstractions`, which defines `CodeDeckPlugin`, `Tile`, and `[Setting]`.

## Built-in plugin workflow

Open `CodeDeck.sln` to develop CodeDeck and its built-in plugins together. Each plugin is visible as its own project and has normal NuGet restore, IntelliSense, analyzers, build output, and PDB/source mapping.

Building either host project stages all plugin assemblies and their dependencies into its output directory:

```text
CodeDeck.Windows/bin/Debug/net10.0-windows7.0/
└── Plugins/
    └── Clock/
        ├── Clock.dll
        ├── Clock.deps.json
        └── Clock.pdb
```

CodeDeck loads these compiled assemblies directly. When the host is running, changes in the staged plugin artifacts trigger a debounced reload: existing tiles are deinitialized, the old plugin load context is unloaded, and the rebuilt assembly is loaded from a shadow copy. Shadow copying lets Windows replace plugin DLLs while CodeDeck is running.

## Creating a built-in plugin

Create a directory under `CodeDeck.Plugins/Plugins` containing a project named after the plugin:

```text
CodeDeck.Plugins/Plugins/MyPlugin/
├── MyPlugin.csproj
└── MyPlugin.cs
```

The shared `Directory.Build.props` automatically targets `net10.0`, enables nullable reference types, and references `CodeDeck.PluginAbstractions`. Add the project to `CodeDeck.sln`.

```csharp
using CodeDeck.PluginAbstractions;
using System.Threading;
using System.Threading.Tasks;

public class MyPlugin : CodeDeckPlugin
{
    public class MyTile : Tile
    {
        [Setting] public string? Message { get; set; }

        public override Task Init(CancellationToken cancellationToken)
        {
            Text = Message ?? "Hello";
            return Task.CompletedTask;
        }
    }
}
```

Add ordinary `PackageReference` items to the plugin project for dependencies. Platform-specific plugins should explicitly target `net10.0-windows`.

## Plugin context

CodeDeck creates one instance of each plugin class for every load. Override `InitializeAsync` for plugin-wide initialization; it receives an immutable `PluginContext` containing that plugin's directory, plugin-level configuration settings, and cancellation token.

```csharp
public override Task InitializeAsync(PluginContext context, CancellationToken cancellationToken)
{
    var cacheFile = Path.Combine(context.PluginPath, "cache.json");
    return Task.CompletedTask;
}
```

Every tile receives the same context through `PluginContext` before its `Init` method runs. Its `Init` and key-press methods receive a token linked to both the tile and plugin lifetime. Key-specific settings remain bound to the tile's `[Setting]` properties. Reloading creates fresh plugin and tile instances with a new context.

## Per-load shared state

Use `PluginContext.GetOrCreateSharedState` for state that must be shared by all
tiles in one plugin load, such as a single hardware connection or cache. The
factory runs once per key and the state is disposed (`IAsyncDisposable` or
`IDisposable`) after that load's managed tasks stop during plugin unload.

```csharp
var monitor = PluginContext.GetOrCreateSharedState(
    "MyPlugin.DeviceMonitor",
    () => new DeviceMonitor(PluginContext));
```

Each tile should subscribe in `Init` and unsubscribe in `DeInit`; its display
formatting remains tile-specific while the monitor owns the shared I/O.

## Managed plugin tasks

Use `RunEvery` from a `Tile` or `CodeDeckPlugin` for periodic work. CodeDeck runs the
operation serially (it never overlaps a prior run), supplies a lifecycle cancellation
token, records failures, and waits for managed tasks before unloading a plugin.

```csharp
public override Task Init(CancellationToken cancellationToken)
{
    RunEvery(
        "Status update",
        TimeSpan.FromMinutes(1),
        async token => Text = await _client.GetStringAsync("https://example.test/status", token));
    return Task.CompletedTask;
}
```

The operation runs immediately by default; pass `runImmediately: false` to wait for
the first interval. For a non-periodic long-running operation such as a HID reader,
use `RunTask`. Both helpers can take an additional cancellation token for work that
ends before the tile/plugin lifetime. Do not start untracked fire-and-forget loops.

The Windows tray menu's **Plugin Tasks...** window shows loaded plugins and managed
task state, timing, and errors. Its artifact-size column is the total staged plugin
directory size. It is not, and cannot be, an exact measurement of managed memory per
AssemblyLoadContext.

## Tile lifecycle

`Init` runs after a tile is created. `OnTilePressDown` and `OnTilePressUp` handle key actions. `DeInit` runs when configuration or a plugin reload replaces the tile. Long-running work must observe the supplied cancellation token and `DeInit` must await any work the tile started before it returns.

Tile properties such as `Text`, `Image`, `BackgroundColor`, and `ShowIndicator` notify CodeDeck to redraw the key.

## Build staging and MSBuild

MSBuild project files can import shared `.props` and `.targets` files. A `.props` file supplies build settings; a `.targets` file defines build steps. `AfterTargets="Build"` makes a step run after the ordinary project build. `ItemGroup` creates a list of files or projects, and tasks such as `MSBuild` and `Copy` operate on that list.

| File | Responsibility |
| --- | --- |
| `Directory.Build.props` | Automatically applies common plugin settings to every project below `Plugins/`: .NET target framework, nullable/implicit-using options, a reference to `CodeDeck.PluginAbstractions`, and copying NuGet runtime dependencies beside the plugin DLL. |
| `Directory.Build.targets` | Automatically runs after an individual plugin build. It stages that plugin's output in the Windows host output directory. `DesignTimeBuild` prevents IntelliSense and other VS design-time builds from copying files. |
| `BuiltInPlugins.targets` | Imported by `CodeDeck.csproj`. After CodeDeck builds, it builds every built-in plugin and passes CodeDeck's `Plugins` output directory as the staging destination. |
| `HostPluginArtifacts.targets` | Imported by the Windows and Linux host projects. After a host builds, it copies the staged plugin artifacts from the CodeDeck output directory into that host's output directory. |

Visual Studio build behavior follows from those imports:

1. **Build Solution** builds CodeDeck, stages every plugin with it, then builds the selected host and copies those artifacts to the host output.
2. **Build one plugin** builds and stages only that plugin directly into the Windows host output. This is the fast development path for rebuilding a plugin while CodeDeck is running.
