using CodeDeck.PluginAbstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace CodeDeck;

public sealed class PluginTaskManager : IPluginTaskManager
{
    private sealed class ManagedTask
    {
        private readonly object _sync = new();

        public Guid Id { get; } = Guid.NewGuid();
        public string ScopeId { get; }
        public string PluginName { get; }
        public string Owner { get; }
        public string Name { get; }
        public TimeSpan? Interval { get; }
        public CancellationTokenSource CancellationTokenSource { get; }
        public Task Task { get; set; } = Task.CompletedTask;
        public PluginTaskState State { get; private set; } = PluginTaskState.Waiting;
        public DateTimeOffset? LastStartedAt { get; private set; }
        public TimeSpan? LastDuration { get; private set; }
        public DateTimeOffset? LastSucceededAt { get; private set; }
        public string? LastError { get; private set; }

        public ManagedTask(
            string scopeId,
            string pluginName,
            string owner,
            string name,
            TimeSpan? interval,
            CancellationToken pluginCancellationToken,
            CancellationToken cancellationToken)
        {
            ScopeId = scopeId;
            PluginName = pluginName;
            Owner = owner;
            Name = name;
            Interval = interval;
            CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                pluginCancellationToken,
                cancellationToken);
        }

        public void StartRun()
        {
            lock (_sync)
            {
                State = PluginTaskState.Running;
                LastStartedAt = DateTimeOffset.Now;
            }
        }

        public void CompleteRun(TimeSpan duration)
        {
            lock (_sync)
            {
                LastDuration = duration;
                LastSucceededAt = DateTimeOffset.Now;
                LastError = null;
                State = Interval is null ? PluginTaskState.Stopped : PluginTaskState.Waiting;
            }
        }

        public void FailRun(TimeSpan duration, Exception exception)
        {
            lock (_sync)
            {
                LastDuration = duration;
                LastError = exception.Message;
                State = Interval is null ? PluginTaskState.Stopped : PluginTaskState.Waiting;
            }
        }

        public void Stop()
        {
            lock (_sync)
            {
                State = PluginTaskState.Stopped;
            }
        }

        /// <summary>
        /// Cancels the task, tolerating a concurrent self-disposal by the task body.
        /// </summary>
        public void TryCancel()
        {
            try
            {
                CancellationTokenSource.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        public PluginTaskSnapshot Snapshot()
        {
            lock (_sync)
            {
                return new PluginTaskSnapshot(
                    ScopeId,
                    PluginName,
                    Owner,
                    Name,
                    Interval,
                    State,
                    LastStartedAt,
                    LastDuration,
                    LastSucceededAt,
                    LastError);
            }
        }
    }

    private readonly ILogger<PluginTaskManager> _logger;
    private readonly ConcurrentDictionary<Guid, ManagedTask> _tasks = new();
    private readonly ConcurrentDictionary<string, byte> _stoppedScopes = new();
    private readonly ConcurrentDictionary<(string ScopeId, string Owner), byte> _stoppedOwners = new();

    public PluginTaskManager(ILogger<PluginTaskManager> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<PluginTaskSnapshot> GetTasks() => _tasks.Values
        .Select(task => task.Snapshot())
        .OrderBy(task => task.PluginName)
        .ThenBy(task => task.Owner)
        .ThenBy(task => task.Name)
        .ToList();

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
        if (interval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval));

        Start(scopeId, pluginName, owner, name, interval, pluginCancellationToken, cancellationToken, async (task, token) =>
        {
            token.ThrowIfCancellationRequested();

            if (!runImmediately) await Task.Delay(interval, token);

            while (!token.IsCancellationRequested)
            {
                await ExecuteOperationAsync(task, operation, token);
                await Task.Delay(interval, token);
            }
        });
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
        Start(scopeId, pluginName, owner, name, null, pluginCancellationToken, cancellationToken,
            (task, token) => ExecuteOperationAsync(task, operation, token));
    }

    public async Task StopScopeAsync(string scopeId)
    {
        // Mark the scope stopped first, so tasks registered concurrently (e.g. from a
        // key press racing a reload) are rejected instead of outliving the plugin.
        _stoppedScopes.TryAdd(scopeId, 0);

        // Re-read until no new tasks appear, since a registration may have been in
        // flight when the scope was marked as stopped.
        while (true)
        {
            var tasks = _tasks.Values.Where(task => task.ScopeId == scopeId).ToList();
            if (!tasks.Any())
            {
                foreach (var key in _stoppedOwners.Keys.Where(key => key.ScopeId == scopeId))
                {
                    _stoppedOwners.TryRemove(key, out _);
                }
                return;
            }

            _logger.LogInformation("Stopping {TaskCount} managed task(s) for plugin scope {ScopeId}.", tasks.Count, scopeId);
            foreach (var task in tasks) task.TryCancel();

            // The task bodies remove themselves from the dictionary and dispose their
            // cancellation token sources once they observe the cancellation.
            await Task.WhenAll(tasks.Select(task => task.Task));
        }
    }

    public async Task StopOwnerAsync(string scopeId, string owner)
    {
        _stoppedOwners.TryAdd((scopeId, owner), 0);
        while (true)
        {
            var tasks = _tasks.Values
                .Where(task => task.ScopeId == scopeId && task.Owner == owner)
                .ToList();
            if (tasks.Count == 0) return;

            foreach (var task in tasks) task.TryCancel();
            await Task.WhenAll(tasks.Select(task => task.Task));
        }
    }

    private bool IsScopeStopped(string scopeId) => _stoppedScopes.ContainsKey(scopeId);

    private void Start(
        string scopeId,
        string pluginName,
        string owner,
        string name,
        TimeSpan? interval,
        CancellationToken pluginCancellationToken,
        CancellationToken cancellationToken,
        Func<ManagedTask, CancellationToken, Task> runner)
    {
        foreach (var existing in _tasks.Values.Where(task =>
                     task.ScopeId == scopeId
                     && task.Owner == owner
                     && task.Name == name))
        {
            existing.TryCancel();
        }

        var task = new ManagedTask(
            scopeId,
            pluginName,
            owner,
            name,
            interval,
            pluginCancellationToken,
            cancellationToken);
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Assign the Task before publishing the ManagedTask, so a concurrent
        // StopScopeAsync can never observe the Task.CompletedTask placeholder and
        // conclude that the task has already finished. The gate keeps the body from
        // deregistering itself before registration completes.
        task.Task = Task.Run(async () =>
        {
            await ready.Task;

            try
            {
                await runner(task, task.CancellationTokenSource.Token);
            }
            catch (OperationCanceledException) when (task.CancellationTokenSource.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Managed task {Plugin}/{Owner}/{TaskName} stopped unexpectedly.",
                    pluginName,
                    owner,
                    name);
            }
            finally
            {
                task.Stop();
                _tasks.TryRemove(task.Id, out _);
                task.CancellationTokenSource.Dispose();
            }
        });

        if (!_tasks.TryAdd(task.Id, task)) throw new InvalidOperationException("Could not track plugin task.");

        // A scope that has already been stopped must not gain new work, otherwise
        // plugin code could still be running when its assembly load context is unloaded.
        if (IsScopeStopped(scopeId) || _stoppedOwners.ContainsKey((scopeId, owner)))
        {
            _logger.LogWarning(
                "Ignoring managed task {Plugin}/{Owner}/{TaskName} for stopped plugin scope {ScopeId}.",
                pluginName,
                owner,
                name,
                scopeId);
            task.TryCancel();
        }
        else
        {
            _logger.LogInformation(
                "Registered managed {Kind} task {Plugin}/{Owner}/{TaskName}.",
                interval is null ? "continuous" : "recurring",
                pluginName,
                owner,
                name);
        }

        ready.SetResult();
    }

    private async Task ExecuteOperationAsync(
        ManagedTask task,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        task.StartRun();
        var stopwatch = Stopwatch.StartNew();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await operation(cancellationToken);
            task.CompleteRun(stopwatch.Elapsed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            task.Stop();
            throw;
        }
        catch (Exception exception)
        {
            task.FailRun(stopwatch.Elapsed, exception);
            _logger.LogError(
                exception,
                "Managed task {Plugin}/{Owner}/{TaskName} failed.",
                task.PluginName,
                task.Owner,
                task.Name);
        }
    }
}
