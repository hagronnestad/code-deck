using CodeDeck.Models.Configuration;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CodeDeck
{
    public class PluginLoader
    {
        private readonly ILogger<PluginLoader> _logger;
        private readonly StreamDeckConfiguration _streamDeckConfiguration;

        public List<Plugin> LoadedPlugins = new();

        public PluginLoader(ILogger<PluginLoader> logger, ConfigurationProvider configurationProvider)
        {
            _logger = logger;
            _streamDeckConfiguration = configurationProvider.LoadedConfiguration;
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

            var plugins = pluginDirectories.Select(
                x => new Plugin(_logger, x, GetPluginConfiguration(Path.GetFileName(x))));

            return plugins.ToList();
        }

        private PluginConfiguration? GetPluginConfiguration(string pluginName)
        {
            var config = _streamDeckConfiguration
                .Plugins?
                .FirstOrDefault(x => x.Name?.ToLower() == pluginName.ToLower());

            return config;
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
