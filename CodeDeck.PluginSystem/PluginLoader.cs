using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace CodeDeck.PluginSystem
{
    public class PluginLoader
    {
        private readonly ILogger<PluginLoader> _logger;

        public List<Plugin> LoadedPlugins = new();

        public PluginLoader(ILogger<PluginLoader> logger)
        {
            _logger = logger;
        }

        public void LoadAllPlugins()
        {
            LoadedPlugins = GetAllPlugins();
            LogPlugins(LoadedPlugins);
        }

        public List<Plugin> GetAllPlugins()
        {
            var pluginDirectories = Directory
                .GetDirectories("Plugins")
                .Select(x => Path.GetFullPath(x));

            if (!pluginDirectories.Any()) return new();

            var plugins = pluginDirectories.Select(x => new Plugin(_logger, x));

            return plugins.ToList();
        }

        public void LogPlugins(List<Plugin> plugins)
        {
            if (!plugins.Any())
            {
                _logger.LogWarning("No plugins found!");
                return;
            }

            _logger.LogInformation($"Found plugin(s): {string.Join(", ", plugins.Select(x => x.Name))}");
        }
    }
}
