using CodeDeck.PluginAbstractions;

namespace CodeDeck.TestPlugin;

public class TestPlugin : CodeDeckPlugin
{
    public class TestTile : Tile
    {
        [Setting] public bool WaitForInitialization { get; set; }
        [Setting] public bool FailInitialization { get; set; }
        [Setting] public bool WaitForPress { get; set; }

        private TaskCompletionSource Gate(string name) =>
            PluginContext.GetOrCreateSharedState(name,
                () => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));

        public override async Task Init(CancellationToken cancellationToken)
        {
            Gate("InitStarted").TrySetResult();
            if (WaitForInitialization) await Gate("InitGate").Task.WaitAsync(cancellationToken);
            if (FailInitialization) throw new InvalidOperationException("Initialization failed.");
        }

        public override async Task OnTilePressDown(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref PluginContext.GetOrCreateSharedState("PressCount", () => new int[1])[0]);
            Gate("PressStarted").TrySetResult();
            if (WaitForPress) await Gate("PressGate").Task;
        }

        public override Task OnTilePressUp(CancellationToken cancellationToken) => OnTilePressDown(cancellationToken);

        public override Task DeInit()
        {
            Gate("Deinitialized").TrySetResult();
            return Task.CompletedTask;
        }
    }
}
