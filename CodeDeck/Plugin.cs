using CodeDeck.Models.Configuration;
using CodeDeck.PluginAbstractions;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Runtime.Loader;

namespace CodeDeck
{
    public class Plugin : AssemblyLoadContext
    {
        private readonly ILogger _logger;
        private readonly IPluginTaskManager _taskManager;
        private readonly PluginConfiguration? _configuration;
        private AssemblyDependencyResolver? _assemblyDependencyResolver;
        private string? _loadAssemblyFileName;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private static readonly HashSet<string> SharedAssemblyNames = new(StringComparer.OrdinalIgnoreCase)
        {
            typeof(CodeDeckPlugin).Assembly.GetName().Name!,
            typeof(Image).Assembly.GetName().Name!,
            "HidSharp"
        };

        public string PluginPath { get; }
        new public string Name { get; }
        public string AssemblyFileName => Path.Combine(PluginPath, $"{Name}.dll");

        /// <summary>
        /// Root for shadow copies of plugin artifacts. Deliberately a sibling of
        /// "Plugins" rather than a subdirectory of it: the plugin file system watcher
        /// monitors "Plugins" recursively, so copying into it would make every load
        /// trigger another reload.
        /// </summary>
        public static string CacheRootDirectory { get; } = Path.GetFullPath("PluginCache");

        /// <summary>
        /// The shadow copy this plugin was loaded from. Removed when the plugin is unloaded.
        /// </summary>
        public string CacheDirectory { get; }
        public Assembly? Assembly { get; private set; }
        public Type? PluginType { get; private set; }
        public CodeDeckPlugin? PluginInstance { get; private set; }
        public PluginContext Context { get; }
        public bool IsLoaded => PluginType is not null;

        private Plugin(
            ILogger logger,
            IPluginTaskManager taskManager,
            string pluginDirectory,
            PluginConfiguration? configuration,
            CancellationToken hostCancellationToken)
            : base(Path.GetFileName(pluginDirectory), isCollectible: true)
        {
            _logger = logger;
            _taskManager = taskManager;
            _configuration = configuration;
            _cancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(hostCancellationToken);

            PluginPath = pluginDirectory;
            Name = Path.GetFileName(pluginDirectory);
            CacheDirectory = Path.Combine(CacheRootDirectory, Name, Guid.NewGuid().ToString("N"));
            Context = new PluginContext(
                pluginDirectory,
                configuration?.Settings,
                _cancellationTokenSource.Token,
                Guid.NewGuid().ToString("N"),
                taskManager);
        }

        public static Task<Plugin> LoadAsync(
            ILogger logger,
            IPluginTaskManager taskManager,
            string pluginDirectory,
            PluginConfiguration? configuration,
            CancellationToken hostCancellationToken)
        {
            return Task.Run(async () =>
            {
                var plugin = new Plugin(
                    logger,
                    taskManager,
                    pluginDirectory,
                    configuration,
                    hostCancellationToken);
                await plugin.LoadPluginAsync();
                return plugin;
            });
        }

        private async Task LoadPluginAsync()
        {
            if (!File.Exists(AssemblyFileName))
            {
                _logger.LogError($"<{nameof(Plugin)}.{nameof(LoadPluginAsync)}> Plugin assembly does not exist: '{AssemblyFileName}'");
                return;
            }

            try
            {
                _logger.LogInformation($"<{nameof(Plugin)}.{nameof(LoadPluginAsync)}> Loading plugin: '{AssemblyFileName}'");
                _loadAssemblyFileName = CreateLoadAssemblyFileName();
                _assemblyDependencyResolver = new AssemblyDependencyResolver(_loadAssemblyFileName);
                Assembly = LoadFromAssemblyPath(_loadAssemblyFileName);
                PluginType = GetPluginType();

                if (PluginType is null)
                {
                    _logger.LogWarning($"<{nameof(Plugin)}.{nameof(LoadPluginAsync)}> Assembly '{Name}' does not contain a valid plugin.");
                    return;
                }

                PluginInstance = Activator.CreateInstance(PluginType) as CodeDeckPlugin;
                if (PluginInstance is null)
                {
                    _logger.LogWarning($"<{nameof(Plugin)}.{nameof(LoadPluginAsync)}> Could not create plugin '{Name}'.");
                    PluginType = null;
                    return;
                }

                if (_configuration?.Settings is not null)
                {
                    MapSettings(_configuration.Settings, PluginType, PluginInstance);
                }

                await PluginInstance.InitializePluginAsync(Context, Context.CancellationToken);
                _logger.LogInformation($"<{nameof(Plugin)}.{nameof(LoadPluginAsync)}> Loaded plugin '{Name}'.");
            }
            catch (Exception e)
            {
                PluginInstance = null;
                PluginType = null;
                TryDeleteCacheDirectory();
                _logger.LogError($"<{nameof(Plugin)}.{nameof(LoadPluginAsync)}> Could not load plugin '{Name}'. Exception: {e.Message}");
            }
        }

        private string CreateLoadAssemblyFileName()
        {
            foreach (var file in Directory.EnumerateFiles(PluginPath, "*", SearchOption.AllDirectories))
            {
                var destinationFileName = Path.Combine(CacheDirectory, Path.GetRelativePath(PluginPath, file));
                var destinationDirectory = Path.GetDirectoryName(destinationFileName);
                if (destinationDirectory is not null) Directory.CreateDirectory(destinationDirectory);

                File.Copy(file, destinationFileName);
            }

            return Path.Combine(CacheDirectory, $"{Name}.dll");
        }

        /// <summary>
        /// Removes shadow copies left behind by previous runs. Safe to call at startup,
        /// before any plugin has been loaded, because nothing holds those files then.
        /// </summary>
        public static void CleanCacheRoot(ILogger logger)
        {
            if (!Directory.Exists(CacheRootDirectory)) return;

            try
            {
                Directory.Delete(CacheRootDirectory, true);
                logger.LogInformation($"<{nameof(Plugin)}.{nameof(CleanCacheRoot)}> Removed stale plugin shadow copies.");
            }
            catch (Exception e)
            {
                logger.LogWarning($"<{nameof(Plugin)}.{nameof(CleanCacheRoot)}> Could not remove '{CacheRootDirectory}'. Exception: {e.Message}");
            }
        }

        /// <summary>
        /// Best effort: the assemblies stay mapped until the collectible load context is
        /// actually collected, so this usually only succeeds for plugins that never
        /// loaded. Whatever survives is removed by <see cref="CleanCacheRoot"/> next start.
        /// </summary>
        private void TryDeleteCacheDirectory()
        {
            if (!Directory.Exists(CacheDirectory)) return;

            try
            {
                Directory.Delete(CacheDirectory, true);
            }
            catch (Exception e)
            {
                _logger.LogDebug($"<{nameof(Plugin)}.{nameof(TryDeleteCacheDirectory)}> Deferred cleanup of '{CacheDirectory}'. Exception: {e.Message}");
            }
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (assemblyName.Name is not null && SharedAssemblyNames.Contains(assemblyName.Name))
            {
                return AssemblyLoadContext.Default.Assemblies
                    .FirstOrDefault(assembly => assembly.GetName().Name == assemblyName.Name)
                    ?? AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
            }

            var resolver = _assemblyDependencyResolver
                ?? throw new InvalidOperationException("The plugin dependency resolver has not been initialized.");
            var assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
            return assemblyPath is null ? null : LoadFromAssemblyPath(assemblyPath);
        }

        private Type? GetPluginType()
        {
            if (Assembly is null) return null;

            return Assembly
                .GetTypes()
                .FirstOrDefault(type => type.BaseType == typeof(CodeDeckPlugin));
        }

        public Tile? CreateTileInstance(string tileTypeName, Dictionary<string, string>? settings)
        {
            if (PluginType is null) return null;

            var tileType = PluginType
                .GetNestedTypes()
                .FirstOrDefault(type => type.BaseType == typeof(Tile) && type.Name == tileTypeName);

            if (tileType is null || Activator.CreateInstance(tileType) is not Tile tileInstance)
            {
                return null;
            }

            if (settings is not null)
            {
                MapSettings(settings, tileType, tileInstance);
            }

            return tileInstance;
        }

        public async Task UnloadPluginAsync()
        {
            Cancel();
            try
            {
                await Context.StopTasksAsync();
            }
            finally
            {
                try
                {
                    await Context.DisposeSharedStateAsync();
                }
                finally
                {
                    Assembly = null;
                    PluginType = null;
                    PluginInstance = null;
                    _cancellationTokenSource.Dispose();
                    Unload();
                    TryDeleteCacheDirectory();
                    _logger.LogInformation($"<{nameof(Plugin)}.{nameof(UnloadPluginAsync)}> Released plugin '{Name}' for unloading.");
                }
            }
        }

        public void Cancel()
        {
            if (_cancellationTokenSource.IsCancellationRequested) return;

            _logger.LogInformation($"<{nameof(Plugin)}.{nameof(Cancel)}> Cancelling plugin '{Name}'.");
            _cancellationTokenSource.Cancel();
        }

        private void MapSettings(Dictionary<string, string>? settings, Type type, object? instance = null)
        {
            if (instance is Tile tile)
            {
                tile.Settings = settings;
            }

            var settingProperties = type.GetProperties()
                .Where(property => property.CustomAttributes.Any(attribute =>
                    attribute.AttributeType.Name == nameof(SettingAttribute)));

            foreach (var property in settingProperties)
            {
                if (!(settings?.TryGetValue(property.Name, out var value) ?? false)) continue;

                if (property.PropertyType == typeof(string))
                {
                    property.SetValue(instance, value);
                }
                else if (property.PropertyType == typeof(bool?) || property.PropertyType == typeof(bool))
                {
                    if (bool.TryParse(value, out var parsedValue)) property.SetValue(instance, parsedValue);
                }
                else if (property.PropertyType == typeof(byte?) || property.PropertyType == typeof(byte))
                {
                    if (byte.TryParse(value, out var parsedValue)) property.SetValue(instance, parsedValue);
                }
                else if (property.PropertyType == typeof(int?) || property.PropertyType == typeof(int))
                {
                    if (int.TryParse(value, out var parsedValue)) property.SetValue(instance, parsedValue);
                }
                else if (property.PropertyType == typeof(double?) || property.PropertyType == typeof(double))
                {
                    if (double.TryParse(value, out var parsedValue)) property.SetValue(instance, parsedValue);
                }
                else
                {
                    _logger.LogWarning($"<{nameof(Plugin)}.{nameof(MapSettings)}> Cannot map setting '{property.Name}' because data type '{property.PropertyType.Name}' is not supported.");
                }
            }
        }
    }
}
