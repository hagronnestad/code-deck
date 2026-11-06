using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace CodeDeck.PluginAbstractions
{
    public sealed class PluginContext
    {
        private readonly object _sharedStateLock = new();
        private readonly Dictionary<string, object> _sharedState = new(StringComparer.Ordinal);
        private bool _sharedStateDisposed;
        private readonly ILoggerFactory _loggerFactory;

        public string PluginPath { get; }
        public IReadOnlyDictionary<string, string> Settings { get; }
        public CancellationToken CancellationToken { get; }
        public string TaskScopeId { get; }
        public IPluginTaskManager TaskManager { get; }

        public PluginContext(
            string pluginPath,
            IReadOnlyDictionary<string, string>? settings,
            CancellationToken cancellationToken,
            ILoggerFactory loggerFactory,
            string? taskScopeId = null,
            IPluginTaskManager? taskManager = null)
        {
            ArgumentNullException.ThrowIfNull(loggerFactory);
            _loggerFactory = loggerFactory;
            PluginPath = pluginPath;
            Settings = new ReadOnlyDictionary<string, string>(
                settings is null
                    ? new Dictionary<string, string>()
                    : new Dictionary<string, string>(settings));
            CancellationToken = cancellationToken;
            TaskScopeId = taskScopeId ?? Guid.NewGuid().ToString("N");
            TaskManager = taskManager ?? NoopPluginTaskManager.Instance;
        }

        public ILogger CreateLogger(string categoryName) => _loggerFactory.CreateLogger(categoryName);

        public void RunEvery(
            string pluginName,
            string owner,
            string name,
            TimeSpan interval,
            Func<CancellationToken, Task> operation,
            bool runImmediately = true,
            CancellationToken cancellationToken = default)
        {
            TaskManager.RunRecurring(
                TaskScopeId,
                pluginName,
                owner,
                name,
                interval,
                operation,
                runImmediately,
                CancellationToken,
                cancellationToken.CanBeCanceled ? cancellationToken : CancellationToken);
        }

        public void RunTask(
            string pluginName,
            string owner,
            string name,
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default)
        {
            TaskManager.Run(
                TaskScopeId,
                pluginName,
                owner,
                name,
                operation,
                CancellationToken,
                cancellationToken.CanBeCanceled ? cancellationToken : CancellationToken);
        }

        public Task StopTasksAsync() => TaskManager.StopScopeAsync(TaskScopeId);

        /// <summary>
        /// Gets state shared by all objects created for this plugin load, creating it once when needed.
        /// The state is disposed when the plugin load is unloaded.
        /// </summary>
        public T GetOrCreateSharedState<T>(string key, Func<T> factory)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(factory);

            lock (_sharedStateLock)
            {
                ObjectDisposedException.ThrowIf(_sharedStateDisposed, this);

                if (_sharedState.TryGetValue(key, out var existingState))
                {
                    if (existingState is T typedState) return typedState;

                    throw new InvalidOperationException(
                            $"Shared state '{key}' is already registered as {existingState.GetType().FullName}, not {typeof(T).FullName}.");
                }

                var state = factory()
                    ?? throw new InvalidOperationException($"Shared state factory for '{key}' returned null.");
                _sharedState.Add(key, state);
                return state;
            }
        }

        /// <summary>
        /// Disposes all shared state for this plugin load. Called by the host during plugin unload.
        /// </summary>
        public async Task DisposeSharedStateAsync()
        {
            List<object> states;
            lock (_sharedStateLock)
            {
                if (_sharedStateDisposed) return;

                _sharedStateDisposed = true;
                states = _sharedState.Values.ToList();
                _sharedState.Clear();
            }

            List<Exception>? exceptions = null;
            foreach (var state in states)
            {
                try
                {
                    switch (state)
                    {
                        case IAsyncDisposable asyncDisposable:
                            await asyncDisposable.DisposeAsync();
                            break;
                        case IDisposable disposable:
                            disposable.Dispose();
                            break;
                    }
                }
                catch (Exception exception)
                {
                    (exceptions ??= new List<Exception>()).Add(exception);
                }
            }

            if (exceptions is { Count: > 0 })
            {
                throw new AggregateException("One or more plugin shared-state instances could not be disposed.", exceptions);
            }
        }
    }
}
