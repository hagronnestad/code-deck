using Microsoft.Extensions.Logging;

namespace CodeDeck
{
    public sealed record PluginReloadResult(string PluginName, Plugin? PreviousPlugin);

    public class PluginLoader
    {
        private readonly ILogger<PluginLoader> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ConfigurationProvider _configurationProvider;
        private readonly IPluginTaskManager _taskManager;
        private readonly FileSystemWatcher? _pluginFileSystemWatcher;
        private readonly Timer _pluginReloadTimer;
        private readonly HashSet<string> _changedPluginNames = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _changedPluginNamesLock = new();
        private readonly object _loadedPluginsLock = new();
        private readonly SemaphoreSlim _loadLock = new(1, 1);
        private readonly CancellationTokenSource _shutdownTokenSource = new();

        private List<Plugin> _loadedPlugins = new();
        public IReadOnlyList<Plugin> LoadedPlugins
        {
            get
            {
                lock (_loadedPluginsLock)
                {
                    return _loadedPlugins.ToList();
                }
            }
        }
        public event EventHandler<IReadOnlyCollection<string>>? PluginsChanged;

        public PluginLoader(
            ILogger<PluginLoader> logger,
            ILoggerFactory loggerFactory,
            ConfigurationProvider configurationProvider,
            IPluginTaskManager taskManager)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _configurationProvider = configurationProvider;
            _taskManager = taskManager;
            _pluginReloadTimer = new Timer(_ => RaisePluginsChanged());

            // Nothing is loaded yet, so any shadow copies still on disk are leftovers
            // from a previous run and can be removed.
            Plugin.CleanCacheRoot(_logger);

            if (Directory.Exists("Plugins"))
            {
                _pluginFileSystemWatcher = new FileSystemWatcher("Plugins")
                {
                    IncludeSubdirectories = true,
                    EnableRaisingEvents = true,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
                };

                _pluginFileSystemWatcher.Changed += HandlePluginFileSystemWatcherChanged;
                _pluginFileSystemWatcher.Created += HandlePluginFileSystemWatcherChanged;
                _pluginFileSystemWatcher.Renamed += HandlePluginFileSystemWatcherRenamed;
            }
        }

        public async Task LoadAllPluginsAsync()
        {
            // Loading and publishing must be atomic with respect to TryReloadPluginAsync,
            // otherwise a replacement plugin loaded concurrently is discarded by the
            // wholesale list swap below and leaks its assembly load context and tasks.
            await _loadLock.WaitAsync();
            try
            {
                var plugins = await GetAllPluginsAsync();
                List<Plugin> previouslyLoadedPlugins;
                lock (_loadedPluginsLock)
                {
                    previouslyLoadedPlugins = _loadedPlugins;
                    _loadedPlugins = plugins;
                }

                await Task.WhenAll(previouslyLoadedPlugins.Select(plugin => plugin.UnloadPluginAsync()));
                LogPlugins(plugins);
            }
            finally
            {
                _loadLock.Release();
            }
        }

        public async Task<PluginReloadResult?> TryReloadPluginAsync(string pluginName)
        {
            var pluginDirectory = Path.GetFullPath(Path.Combine("Plugins", pluginName));
            if (!Directory.Exists(pluginDirectory))
            {
                return null;
            }

            if (!IsPluginUsedByConfiguration(pluginName))
            {
                _logger.LogInformation($"Ignoring change to plugin '{pluginName}'; it is not used by the configuration.");
                return null;
            }

            await _loadLock.WaitAsync();
            try
            {
                _logger.LogInformation($"Reloading plugin '{pluginName}'.");
                var replacementPlugin = await Plugin.LoadAsync(
                    _logger,
                    _loggerFactory,
                    _taskManager,
                    pluginDirectory,
                    GetPluginConfiguration(pluginName),
                    _shutdownTokenSource.Token);
                if (!replacementPlugin.IsLoaded)
                {
                    _logger.LogWarning($"Plugin '{pluginName}' reload failed; preserving the current plugin.");
                    await replacementPlugin.UnloadPluginAsync();
                    return null;
                }

                Plugin? existingPlugin;
                lock (_loadedPluginsLock)
                {
                    existingPlugin = _loadedPlugins.FirstOrDefault(plugin => plugin.Name == pluginName);
                    if (existingPlugin is not null)
                    {
                        _loadedPlugins.Remove(existingPlugin);
                    }

                    _loadedPlugins.Add(replacementPlugin);
                }

                _logger.LogInformation($"Plugin '{pluginName}' replacement loaded; waiting to unload the previous version.");
                return new PluginReloadResult(pluginName, existingPlugin);
            }
            finally
            {
                _loadLock.Release();
            }
        }

        public async Task<List<Plugin>> GetAllPluginsAsync()
        {
            var pluginsByUsage = GetPluginDirectories()
                .ToLookup(path => IsPluginUsedByConfiguration(Path.GetFileName(path)));

            var skippedPlugins = pluginsByUsage[false].Select(Path.GetFileName).ToArray();
            if (skippedPlugins.Length > 0)
            {
                _logger.LogInformation(
                    "Skipping {SkippedCount} plugin(s) not used by the configuration: {SkippedPlugins}.",
                    skippedPlugins.Length,
                    string.Join(", ", skippedPlugins));
            }

            var pluginDirectories = pluginsByUsage[true].ToArray();
            if (pluginDirectories.Length == 0) return new();

            var plugins = await Task.WhenAll(pluginDirectories
                .Select(path => Plugin.LoadAsync(
                    _logger,
                    _loggerFactory,
                    _taskManager,
                    path,
                    GetPluginConfiguration(Path.GetFileName(path)),
                    _shutdownTokenSource.Token)));

            var unloadedPlugins = plugins.Where(plugin => !plugin.IsLoaded).ToList();
            await Task.WhenAll(unloadedPlugins.Select(plugin => plugin.UnloadPluginAsync()));
            return plugins.Where(plugin => plugin.IsLoaded).ToList();
        }

        public void CancelAllPlugins()
        {
            if (!_shutdownTokenSource.IsCancellationRequested)
            {
                _shutdownTokenSource.Cancel();
            }

            foreach (var plugin in LoadedPlugins)
            {
                plugin.Cancel();
            }
        }

        /// <summary>
        /// Reports every plugin found on disk, including ones that are present but not
        /// loaded, so the plugin manager can distinguish "unused" from "missing".
        /// </summary>
        public IReadOnlyList<PluginStatusSnapshot> GetPluginStatuses()
        {
            var loadedPlugins = LoadedPlugins.ToDictionary(plugin => plugin.Name, StringComparer.OrdinalIgnoreCase);

            return GetPluginDirectories()
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrEmpty(name))
                .Select(name => new PluginStatusSnapshot(
                    name!,
                    loadedPlugins.ContainsKey(name!),
                    GetDirectorySize(Path.GetFullPath(Path.Combine("Plugins", name!)))))
                .OrderBy(plugin => plugin.Name)
                .ToList();
        }

        public async Task UnloadAllPluginsAsync()
        {
            List<Plugin> plugins;
            lock (_loadedPluginsLock)
            {
                plugins = _loadedPlugins;
                _loadedPlugins = new();
            }

            await Task.WhenAll(plugins.Select(plugin => plugin.UnloadPluginAsync()));
        }

        private void HandlePluginFileSystemWatcherChanged(object sender, FileSystemEventArgs e)
        {
            if (!IsPluginArtifact(e.FullPath)) return;
            SchedulePluginReload(e.FullPath);
        }

        private void HandlePluginFileSystemWatcherRenamed(object sender, RenamedEventArgs e)
        {
            if (!IsPluginArtifact(e.FullPath)) return;
            SchedulePluginReload(e.FullPath);
        }

        private static bool IsPluginArtifact(string path)
        {
            var fileName = Path.GetFileName(path);
            return fileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".deps.json", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
        }

        private void SchedulePluginReload(string path)
        {
            var pluginName = GetPluginNameFromArtifactPath(path);
            if (pluginName is null) return;

            lock (_changedPluginNamesLock)
            {
                _changedPluginNames.Add(pluginName);
            }

            _pluginReloadTimer.Change(750, Timeout.Infinite);
        }

        private void RaisePluginsChanged()
        {
            string[] changedPluginNames;
            lock (_changedPluginNamesLock)
            {
                changedPluginNames = _changedPluginNames.ToArray();
                _changedPluginNames.Clear();
            }

            if (!changedPluginNames.Any()) return;

            _logger.LogInformation($"Plugin artifact changes detected: {string.Join(", ", changedPluginNames)}.");
            PluginsChanged?.Invoke(this, changedPluginNames);
        }

        private static string? GetPluginNameFromArtifactPath(string path)
        {
            var relativePath = Path.GetRelativePath("Plugins", path);
            var pluginName = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .FirstOrDefault();

            return string.IsNullOrWhiteSpace(pluginName) ? null : pluginName;
        }

        private PluginConfiguration? GetPluginConfiguration(string pluginName)
        {
            return _configurationProvider.LoadedConfiguration
                .Plugins?
                .FirstOrDefault(x => string.Equals(x.Name, pluginName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// A plugin is only loaded when the configuration actually uses it: either a key
        /// references it, or it has its own settings entry. Loading everything found on
        /// disk costs a shadow copy, an assembly load context and any background work the
        /// plugin starts in its initializer, all for tiles that are never shown.
        /// </summary>
        private bool IsPluginUsedByConfiguration(string pluginName)
        {
            if (GetPluginConfiguration(pluginName) is not null) return true;

            return _configurationProvider.LoadedFlatConfiguration
                .Any(configuration => string.Equals(configuration.Key.Plugin, pluginName, StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<string> GetPluginDirectories()
        {
            if (!Directory.Exists("Plugins")) return Array.Empty<string>();

            return Directory
                .GetDirectories("Plugins")
                .Select(Path.GetFullPath)
                .Where(path => File.Exists(Path.Combine(path, $"{Path.GetFileName(path)}.dll")));
        }

        private static long GetDirectorySize(string directory)
        {
            try
            {
                return Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
                    .Sum(file => new FileInfo(file).Length);
            }
            catch (IOException)
            {
                return 0;
            }
            catch (UnauthorizedAccessException)
            {
                return 0;
            }
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
