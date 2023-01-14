using CodeDeck.PluginSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CodeDeck
{
    public class Program
    {
        private static readonly CancellationTokenSource cts = new CancellationTokenSource();

        public static async Task Main(string[] args)
        {
            using IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((_, services) =>
                {
                    services.AddLogging();

                    services.AddSingleton<ConfigurationProvider>();
                    services.AddSingleton<PluginLoader>();
                    services.AddSingleton<StreamDeckManager>();

                    services.AddHostedService<Worker>();
                })
                .ConfigureLogging((hostBuilder, logging) =>
                {
                    //logging.ClearProviders();

                    logging.SetMinimumLevel(LogLevel.Debug);
                    logging.AddConsole();
                    logging.AddDebug();
                    
                    //logging.AddEventLog();
                })
                .Build();

            await host.RunAsync(cts.Token);
        }

        public static void StopHost()
        {
            cts.Cancel();
        }
    }

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
            await _pluginLoader.LoadPluginsAsync();
            await _streamDeckManager.ApplyConfigurationAsync();

            await Task.Delay(-1, stoppingToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _streamDeckManager.ClearKeys();
            return base.StopAsync(cancellationToken);
        }
    }
}
