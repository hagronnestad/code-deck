using Microsoft.Extensions.Logging;

namespace CodeDeck.PluginAbstractions
{
    /// <summary>
    /// The base class for all plugins
    /// </summary>
    public abstract class CodeDeckPlugin
    {
        private PluginContext? _context;
        private ILogger? _logger;

        protected ILogger Logger => _logger
            ?? throw new InvalidOperationException("The plugin has not been initialized.");

        protected PluginContext Context => _context
            ?? throw new InvalidOperationException("The plugin has not been initialized.");

        public Task InitializePluginAsync(PluginContext context, CancellationToken cancellationToken)
        {
            _context = context;
            _logger = context.CreateLogger(GetType().FullName ?? GetType().Name);
            return InitializeAsync(context, cancellationToken);
        }

        protected void RunEvery(
            string name,
            TimeSpan interval,
            Func<CancellationToken, Task> operation,
            bool runImmediately = true,
            CancellationToken cancellationToken = default)
        {
            Context.RunEvery(GetType().Name, "Plugin", name, interval, operation, runImmediately, cancellationToken);
        }

        protected void RunTask(
            string name,
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default)
        {
            Context.RunTask(GetType().Name, "Plugin", name, operation, cancellationToken);
        }

        public virtual Task InitializeAsync(PluginContext context, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
