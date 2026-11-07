using CodeDeck.Models.Configuration;
using CodeDeck.PluginAbstractions;
using Microsoft.Extensions.Logging.Abstractions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace CodeDeck.Tests;

internal sealed class TestEnvironment : IAsyncDisposable
{
    private readonly string _previousDirectory = Directory.GetCurrentDirectory();
    public string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), $"CodeDeckTests-{Guid.NewGuid():N}");
    public string PluginDirectory => Path.Combine(DirectoryPath, "Plugins", "TestPlugin");
    public PluginTaskManager Tasks { get; } = new(NullLogger<PluginTaskManager>.Instance);
    public Plugin? Plugin { get; private set; }
    public PluginLoader? Loader { get; private set; }

    public TestEnvironment()
    {
        Directory.CreateDirectory(PluginDirectory);
        var fixture = typeof(TestPlugin.TestPlugin).Assembly.Location;
        File.Copy(fixture, Path.Combine(PluginDirectory, "TestPlugin.dll"));
        File.Copy(Path.ChangeExtension(fixture, ".deps.json"), Path.Combine(PluginDirectory, "TestPlugin.deps.json"));
        Directory.SetCurrentDirectory(DirectoryPath);
    }

    public async Task<Plugin> LoadPluginAsync()
    {
        Plugin = await CodeDeck.Plugin.LoadAsync(NullLogger.Instance, NullLoggerFactory.Instance, Tasks,
            PluginDirectory, null, CancellationToken.None);
        Assert.True(Plugin.IsLoaded);
        return Plugin;
    }

    public PluginLoader CreateLoader(StreamDeckConfiguration? configuration = null)
    {
        configuration ??= new StreamDeckConfiguration
        {
            Plugins = [new PluginConfiguration { Name = "TestPlugin" }]
        };
        var provider = new ConfigurationProvider(NullLogger<ConfigurationProvider>.Instance, configuration);
        return Loader = new PluginLoader(NullLogger<PluginLoader>.Instance, NullLoggerFactory.Instance, provider, Tasks);
    }

    public static TaskCompletionSource Gate(PluginContext context, string name) =>
        context.GetOrCreateSharedState(name, () => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));

    public async ValueTask DisposeAsync()
    {
        if (Loader is not null)
        {
            Loader.CancelAllPlugins();
            await Loader.UnloadAllPluginsAsync();
            Loader.Dispose();
        }
        if (Plugin is not null) await Plugin.UnloadPluginAsync();
        Directory.SetCurrentDirectory(_previousDirectory);
        // Windows keeps mapped shadow-copy DLLs locked until their load contexts are collected.
        try { Directory.Delete(DirectoryPath, true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
