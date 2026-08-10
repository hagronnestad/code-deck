using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace CodeDeck
{
    public class Program
    {
        private static readonly CancellationTokenSource cts = new CancellationTokenSource();
        private static PluginLoader? _pluginLoader;
        private static IPluginTaskManager? _pluginTaskManager;

        public static async Task Main(string[] args)
        {
            var cwd = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location ?? Directory.GetCurrentDirectory());
            if (cwd is not null) Directory.SetCurrentDirectory(cwd);

            using IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((_, services) =>
                {
                    services.AddLogging();

                    services.AddSingleton<ConfigurationProvider>();
                    services.AddSingleton<PluginTaskManager>();
                    services.AddSingleton<IPluginTaskManager>(serviceProvider => serviceProvider.GetRequiredService<PluginTaskManager>());
                    services.AddSingleton<PluginLoader>();
                    services.AddSingleton<ProcessMonitor>();
                    services.AddSingleton<StreamDeckManager>();

                    services.AddHostedService<Worker>();
                })
                .ConfigureLogging((hostBuilder, logging) =>
                {
                    logging.ClearProviders();

                    logging.SetMinimumLevel(LogLevel.Debug);
                    logging.AddConsole();
                    logging.AddDebug();

                    //logging.AddEventLog();
                })
                .Build();

            _pluginLoader = host.Services.GetRequiredService<PluginLoader>();
            _pluginTaskManager = host.Services.GetRequiredService<IPluginTaskManager>();
            await host.RunAsync(cts.Token);
        }

        public static void StopHost()
        {
            cts.Cancel();
        }

        public static PluginRuntimeSnapshot GetPluginRuntimeSnapshot() => new(
            _pluginLoader?.GetPluginStatuses() ?? Array.Empty<PluginStatusSnapshot>(),
            _pluginTaskManager?.GetTasks() ?? Array.Empty<PluginTaskSnapshot>());
    }

    public sealed record PluginStatusSnapshot(string Name, bool IsLoaded, long ArtifactSizeBytes);

    public sealed record PluginRuntimeSnapshot(
        IReadOnlyList<PluginStatusSnapshot> Plugins,
        IReadOnlyList<PluginTaskSnapshot> Tasks);

    public sealed class Worker : BackgroundService
    {
        private readonly ConfigurationProvider _configurationProvider;
        private readonly PluginLoader _pluginLoader;
        private readonly StreamDeckManager _streamDeckManager;

        public Worker(ConfigurationProvider configurationProvider, PluginLoader pluginLoader, StreamDeckManager streamDeckManager)
        {
            _configurationProvider = configurationProvider;
            _pluginLoader = pluginLoader;
            _streamDeckManager = streamDeckManager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _streamDeckManager.Start();

            await Task.Delay(-1, stoppingToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _streamDeckManager.StopAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}
