namespace CodeDeck.PluginAbstractions;

public enum PluginTaskState
{
    Waiting,
    Running,
    Stopped
}

public sealed record PluginTaskSnapshot(
    string ScopeId,
    string PluginName,
    string Owner,
    string Name,
    TimeSpan? Interval,
    PluginTaskState State,
    DateTimeOffset? LastStartedAt,
    TimeSpan? LastDuration,
    DateTimeOffset? LastSucceededAt,
    string? LastError);

public interface IPluginTaskManager
{
    IReadOnlyList<PluginTaskSnapshot> GetTasks();

    void RunRecurring(
        string scopeId,
        string pluginName,
        string owner,
        string name,
        TimeSpan interval,
        Func<CancellationToken, Task> operation,
        bool runImmediately,
        CancellationToken pluginCancellationToken,
        CancellationToken cancellationToken);

    void Run(
        string scopeId,
        string pluginName,
        string owner,
        string name,
        Func<CancellationToken, Task> operation,
        CancellationToken pluginCancellationToken,
        CancellationToken cancellationToken);

    Task StopScopeAsync(string scopeId);
    Task StopOwnerAsync(string scopeId, string owner);
}

public sealed class NoopPluginTaskManager : IPluginTaskManager
{
    public static NoopPluginTaskManager Instance { get; } = new();

    private NoopPluginTaskManager()
    {
    }

    public IReadOnlyList<PluginTaskSnapshot> GetTasks() => Array.Empty<PluginTaskSnapshot>();

    public void RunRecurring(
        string scopeId,
        string pluginName,
        string owner,
        string name,
        TimeSpan interval,
        Func<CancellationToken, Task> operation,
        bool runImmediately,
        CancellationToken pluginCancellationToken,
        CancellationToken cancellationToken)
    {
    }

    public void Run(
        string scopeId,
        string pluginName,
        string owner,
        string name,
        Func<CancellationToken, Task> operation,
        CancellationToken pluginCancellationToken,
        CancellationToken cancellationToken)
    {
    }

    public Task StopScopeAsync(string scopeId) => Task.CompletedTask;
    public Task StopOwnerAsync(string scopeId, string owner) => Task.CompletedTask;
}
