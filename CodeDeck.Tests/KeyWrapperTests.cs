using CodeDeck.Models.Configuration;
using CodeDeck.PluginAbstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CodeDeck.Tests;

public class KeyWrapperTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InputIsIgnoredUntilInitializationSucceeds(bool failInitialization)
    {
        await using var environment = new TestEnvironment();
        var plugin = await environment.LoadPluginAsync();
        var wrapper = CreateWrapper(plugin, new()
        {
            ["WaitForInitialization"] = "true",
            ["FailInitialization"] = failInitialization.ToString()
        });
        wrapper.StartInitialization();
        await TestEnvironment.Gate(plugin.Context, "InitStarted").Task.WaitAsync(TimeSpan.FromSeconds(5));

        await wrapper.HandleKeyPressDown();
        await wrapper.HandleKeyPressUp();
        Assert.Equal(0, PressCount(plugin.Context));

        TestEnvironment.Gate(plugin.Context, "InitGate").SetResult();
        await wrapper.InitializationTask.WaitAsync(TimeSpan.FromSeconds(5));
        await wrapper.HandleKeyPressDown();
        Assert.Equal(failInitialization ? 0 : 1, PressCount(plugin.Context));

        await wrapper.DeinitializeAsync();
        wrapper.Dispose();
    }

    [Fact]
    public async Task TeardownDrainsAcceptedPressesAndRejectsNewInput()
    {
        await using var environment = new TestEnvironment();
        var plugin = await environment.LoadPluginAsync();
        var wrapper = CreateWrapper(plugin, new() { ["WaitForPress"] = "true" });
        wrapper.StartInitialization();
        await wrapper.InitializationTask.WaitAsync(TimeSpan.FromSeconds(5));
        var press = wrapper.HandleKeyPressDown();
        await TestEnvironment.Gate(plugin.Context, "PressStarted").Task.WaitAsync(TimeSpan.FromSeconds(5));

        var teardown = wrapper.DeinitializeAsync();
        await wrapper.HandleKeyPressDown();
        await wrapper.HandleKeyPressUp();
        Assert.False(teardown.IsCompleted);
        Assert.False(TestEnvironment.Gate(plugin.Context, "Deinitialized").Task.IsCompleted);
        Assert.Equal(1, PressCount(plugin.Context));

        TestEnvironment.Gate(plugin.Context, "PressGate").SetResult();
        await Task.WhenAll(press, teardown).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(TestEnvironment.Gate(plugin.Context, "Deinitialized").Task.IsCompleted);
        wrapper.Dispose();
    }

    [Fact]
    public async Task TeardownCancelsPendingInitialization()
    {
        await using var environment = new TestEnvironment();
        var plugin = await environment.LoadPluginAsync();
        var wrapper = CreateWrapper(plugin, new() { ["WaitForInitialization"] = "true" });
        wrapper.StartInitialization();
        await TestEnvironment.Gate(plugin.Context, "InitStarted").Task.WaitAsync(TimeSpan.FromSeconds(5));

        await wrapper.DeinitializeAsync().WaitAsync(TimeSpan.FromSeconds(5));
        await wrapper.HandleKeyPressDown();
        Assert.Equal(0, PressCount(plugin.Context));
        wrapper.Dispose();
    }

    private static KeyWrapper CreateWrapper(Plugin plugin, Dictionary<string, string> settings) =>
        new(NullLogger.Instance, new Profile(), new Page(),
            new Key { Plugin = "TestPlugin", Tile = "TestTile", Settings = settings }, plugin);

    private static int PressCount(PluginContext context) => context.GetOrCreateSharedState("PressCount", () => new int[1])[0];
}
