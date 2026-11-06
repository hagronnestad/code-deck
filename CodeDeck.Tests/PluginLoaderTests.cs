namespace CodeDeck.Tests;

public class PluginLoaderTests
{
    [Fact]
    public async Task FailedReplacementPreservesTheCurrentPlugin()
    {
        await using var environment = new TestEnvironment();
        var loader = environment.CreateLoader();
        await loader.LoadAllPluginsAsync();
        var original = Assert.Single(loader.LoadedPlugins);
        await File.WriteAllTextAsync(Path.Combine(environment.PluginDirectory, "TestPlugin.dll"), "not an assembly");

        var result = await loader.TryReloadPluginAsync("TestPlugin");

        Assert.Null(result);
        Assert.Same(original, Assert.Single(loader.LoadedPlugins));
        Assert.False(original.Context.CancellationToken.IsCancellationRequested);
    }
}
