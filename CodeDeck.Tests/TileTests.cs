using CodeDeck.PluginAbstractions;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace CodeDeck.Tests;

public class TileTests
{
    [Fact]
    public async Task OwnedImagesAreDisposedOnReplacementAndTeardownButSharedImagesAreNot()
    {
        var tile = new ImageTile();
        var context = new PluginContext(".", null, CancellationToken.None, NullLoggerFactory.Instance);
        await tile.InitializeAsync(context, CancellationToken.None);
        var first = new Image<Rgba32>(2, 2);
        var second = new Image<Rgba32>(2, 2);
        using var shared = new Image<Rgba32>(2, 2);
        tile.Publish(first);
        tile.Publish(second);
        Assert.Throws<ObjectDisposedException>(() => first.Clone());
        tile.Image = shared;

        await tile.DeinitializeAsync();
        Assert.Throws<ObjectDisposedException>(() => second.Clone());
        using var clone = shared.Clone();
        var late = new Image<Rgba32>(2, 2);
        Assert.Throws<ObjectDisposedException>(() => tile.Publish(late));
        Assert.Throws<ObjectDisposedException>(() => late.Clone());
    }

    [Fact]
    public async Task TileTeardownDrainsItsTasksWithoutStoppingOtherOwners()
    {
        var manager = new PluginTaskManager(NullLogger<PluginTaskManager>.Instance);
        var context = new PluginContext(".", null, CancellationToken.None, NullLoggerFactory.Instance, taskManager: manager);
        var tile = new WorkingTile();
        await tile.InitializeAsync(context, CancellationToken.None);
        await tile.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var otherStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        context.RunEvery("Test", "Other", "Poll", TimeSpan.FromMinutes(1), async token =>
        {
            otherStarted.SetResult();
            await Task.Delay(Timeout.Infinite, token);
        });
        await otherStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await tile.DeinitializeAsync().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(tile.WorkFinished);
        Assert.True(tile.Deinitialized);
        Assert.Single(manager.GetTasks(), task => task.Owner == "Other");
        await context.StopTasksAsync().WaitAsync(TimeSpan.FromSeconds(5));
    }

    private sealed class ImageTile : Tile
    {
        public void Publish(Image image) => SetOwnedImage(image);
    }

    private sealed class WorkingTile : Tile
    {
        private readonly CancellationTokenSource _additionalCancellation = new();
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool WorkFinished { get; private set; }
        public bool Deinitialized { get; private set; }

        public override Task Init(CancellationToken cancellationToken)
        {
            RunTask("Work", async token =>
            {
                Started.SetResult();
                try { await Task.Delay(Timeout.Infinite, token); }
                finally { WorkFinished = true; }
            }, _additionalCancellation.Token);
            return Task.CompletedTask;
        }

        public override Task DeInit()
        {
            Assert.True(WorkFinished);
            _additionalCancellation.Dispose();
            Deinitialized = true;
            return Task.CompletedTask;
        }
    }
}
