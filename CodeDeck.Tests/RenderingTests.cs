using System.Collections.Concurrent;
using System.Reflection;
using CodeDeck.Models.Configuration;
using CodeDeck.Services;
using Microsoft.Extensions.Logging.Abstractions;
using OpenMacroBoard.SDK;

namespace CodeDeck.Tests;

public class RenderingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NavigationDiscardsRendersFromAnEarlierPageGeneration(bool returnToSamePage)
    {
        await using var environment = new TestEnvironment();
        var (manager, board) = await CreateManager(environment);
        manager.NavigateToPage("Test", "A");
        var key = manager.KeyWrappers.Single(wrapper => wrapper.Page.Name == "A");
        manager.BlockNextRender(key);
        key.Key.Text = "Old update";
        var update = Task.Run(key.NotifyChange_Action);
        KeyBitmap expected;
        try
        {
            await manager.RenderBlocked.Task.WaitAsync(TimeSpan.FromSeconds(5));
            manager.NavigateToPage("Test", "B");
            if (returnToSamePage)
            {
                key.Key.Text = "Newest update";
                manager.NavigateToPreviousPage();
            }
            expected = board.Bitmaps[0];
        }
        finally
        {
            manager.ReleaseRender();
        }
        await update.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Same(expected, board.Bitmaps[0]);
        if (returnToSamePage) Assert.Same(expected, key.CachedComposedKeyBitmapImage);
        await manager.StopAsync();
    }

    [Fact]
    public async Task RemovedWrappersCannotPublishLateRenders()
    {
        await using var environment = new TestEnvironment();
        var (manager, board) = await CreateManager(environment);
        manager.NavigateToPage("Test", "A");
        var key = manager.KeyWrappers.Single(wrapper => wrapper.Page.Name == "A");
        manager.BlockNextRender(key);
        var update = Task.Run(key.NotifyChange_Action);
        int writes;
        try
        {
            await manager.RenderBlocked.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await manager.DestroyKeyWrappers();
            writes = board.WriteCount;
        }
        finally
        {
            manager.ReleaseRender();
        }
        await update.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(writes, board.WriteCount);
        Assert.Empty(manager.KeyWrappers);
        await manager.StopAsync();
    }

    [Fact]
    public async Task RefreshDoesNotHoldNavigationLockWhileWaitingForATileRender()
    {
        await using var environment = new TestEnvironment();
        var (manager, board) = await CreateManager(environment);
        manager.NavigateToPage("Test", "A");
        var key = manager.KeyWrappers.Single(wrapper => wrapper.Page.Name == "A");
        var tileLocked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var refreshStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var update = Task.Run(() =>
        {
            lock (key.RenderLock)
            {
                tileLocked.SetResult();
                if (!refreshStarted.Task.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException();
                key.NotifyChange_Action();
            }
        });
        await tileLocked.Task.WaitAsync(TimeSpan.FromSeconds(5));
        board.OnNextWrite = () => refreshStarted.TrySetResult();

        await Task.WhenAll(update, Task.Run(manager.RefreshPage)).WaitAsync(TimeSpan.FromSeconds(5));
        await manager.StopAsync();
    }

    private static async Task<(BlockingManager Manager, RecordingBoard Board)> CreateManager(TestEnvironment environment)
    {
        var configuration = new StreamDeckConfiguration
        {
            Profiles = [new Profile
            {
                Name = "Test",
                Pages = [
                    new Page { Name = "A", Keys = [new Key { Index = 0, Text = "A" }] },
                    new Page { Name = "B", Keys = [new Key { Index = 0, Text = "B" }] }
                ]
            }]
        };
        var provider = new ConfigurationProvider(NullLogger<ConfigurationProvider>.Instance, configuration);
        var loader = environment.CreateLoader(configuration);
        var boardInterface = DispatchProxy.Create<IMacroBoard, RecordingBoard>();
        var board = (RecordingBoard)boardInterface;
        var manager = new BlockingManager(provider, loader, boardInterface);
        await manager.CreateKeyWrappers();
        return (manager, board);
    }

    private sealed class BlockingManager(ConfigurationProvider configuration, PluginLoader plugins, IMacroBoard board)
        : StreamDeckManager(NullLogger<StreamDeckManager>.Instance, configuration, new ProcessMonitor(), plugins, board)
    {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private KeyWrapper? _blockedKey;
        private int _blockNext;
        public TaskCompletionSource RenderBlocked { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void BlockNextRender(KeyWrapper key)
        {
            _blockedKey = key;
            _blockNext = 1;
        }

        public void ReleaseRender() => _release.TrySetResult();

        public override KeyBitmap CreateKeyBitmap(KeyWrapper keyWrapper)
        {
            var bitmap = base.CreateKeyBitmap(keyWrapper);
            if (ReferenceEquals(keyWrapper, _blockedKey) && Interlocked.Exchange(ref _blockNext, 0) == 1)
            {
                RenderBlocked.TrySetResult();
                if (!_release.Task.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException();
            }
            return bitmap;
        }
    }

    public class RecordingBoard : DispatchProxy
    {
        private readonly IKeyLayout _keys = Create<IKeyLayout, KeyLayoutProxy>();
        private int _writes;
        public ConcurrentDictionary<int, KeyBitmap> Bitmaps { get; } = new();
        public int WriteCount => Volatile.Read(ref _writes);
        public Action? OnNextWrite;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            switch (targetMethod?.Name)
            {
                case "get_Keys": return _keys;
                case "get_IsConnected": return true;
                case "SetKeyBitmap":
                    Interlocked.Exchange(ref OnNextWrite, null)?.Invoke();
                    Bitmaps[(int)args![0]!] = (KeyBitmap)args[1]!;
                    Interlocked.Increment(ref _writes);
                    break;
            }
            return null;
        }
    }

    public class KeyLayoutProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_KeySize" => 72,
            "get_CountX" => 5,
            "get_CountY" => 3,
            "get_Count" => 15,
            "get_GapSize" => 0,
            _ => throw new NotSupportedException(targetMethod?.Name)
        };
    }
}
