using CodeDeck.PluginAbstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace CodeDeck.PluginSystem
{
    public class Plugin : AssemblyLoadContext
    {
        private readonly ILogger _logger;

        /// <summary>
        /// The location of the plugin directory on disk.
        /// </summary>
        public string PluginPath { get; set; }

        /// <summary>
        /// The name of the plugin. The plugin name is the same as the plugin directory name.
        /// </summary>
        new public string Name { get; set; }

        public string BuildPath => Path.Combine(PluginPath, "bin");
        public string LibrariesPath => Path.Combine(PluginPath, "lib");
        //public string NugetPath => Path.Combine(PluginPath, "nuget");
        public string AssemblyFileName => Path.Combine(BuildPath, $"{Name}.dll");
        public string PdbFileName => Path.Combine(BuildPath, $"{Name}.pdb");

        public List<FileInfo>? SourceFiles { get; set; }

        public FileInfo? AssemblyFileInfo => File.Exists(AssemblyFileName) ? new FileInfo(AssemblyFileName) : null;

        public Assembly? Assembly { get; set; }

        public Type? PluginType { get; set; }


        public Plugin(ILogger logger, string pluginDirectory)
        {
            _logger = logger;

            PluginPath = pluginDirectory;
            Name = Path.GetFileName(pluginDirectory);

            Init();
        }

        public void Init()
        {
            // Create default directories
            Directory.CreateDirectory(BuildPath);

            SourceFiles = GetAllSourceFiles();

            // Recompile plugin if a compiled assembly doesn't exist or
            // if a source files has been changed since last compilation
            if (AssemblyFileInfo == null || !IsAssemblyUpToDate())
            {
                _logger.LogInformation($"<{nameof(Plugin)}.{nameof(Init)}> Compiling plugin: '{Name}'");
                if (!CompilePlugin())
                {
                    _logger.LogError($"<{nameof(Plugin)}.{nameof(Init)}> Encountered errors while compiling plugin: '{Name}'");
                    return;
                }
            }

            _logger.LogInformation($"<{nameof(Plugin)}.{nameof(Init)}> Loading plugin: '{AssemblyFileName}'");

            // Load the plugin and libraries used by the plugin into the AssemblyLoadContext
            LoadAllLibraries();
            Assembly = LoadFromAssemblyPath(AssemblyFileName);
            
            // Get the plugin type inside the loaded assembly
            PluginType = GetPluginType();
            if (PluginType == null)
            {
                _logger.LogWarning($"<{nameof(Plugin)}.{nameof(Init)}> Assembly; '{Name}' does not contain a valid plugin!");
            }
        }

        public void LoadAllLibraries()
        {
            if (!Directory.Exists(LibrariesPath)) return;

            var assemblies = Directory.GetFiles(LibrariesPath, "*.dll", SearchOption.AllDirectories);
            foreach (var assembly in assemblies)
            {
                try
                {
                    LoadFromAssemblyPath(Path.GetFullPath(assembly));
                }
                catch (Exception e)
                {
                    _logger.LogWarning($"<{nameof(Plugin)}.{nameof(LoadAllLibraries)}> Error while loading library: '{Path.GetFullPath(assembly)}'. Exception: {e.Message}");
                }
            }
        }

        public List<FileInfo> GetAllSourceFiles()
        {
            var sourceFiles = Directory.GetFiles(PluginPath, "*.cs", SearchOption.AllDirectories);
            var sourceFilesInfo = sourceFiles.Select(x => new FileInfo(x));

            return sourceFilesInfo.ToList();
        }

        public bool IsAssemblyUpToDate()
        {
            if (AssemblyFileInfo is null || SourceFiles is null) return false;

            var assemblyWriteTime = AssemblyFileInfo.LastWriteTimeUtc;
            var latestSourceFileWriteTime = SourceFiles
                .OrderByDescending(x => x.LastWriteTimeUtc)
                .First().LastWriteTimeUtc;

            return assemblyWriteTime > latestSourceFileWriteTime;
        }

        public bool CompilePlugin()
        {
            if (SourceFiles is null || !SourceFiles.Any()) return false;

            // Read all source files and parse into syntax trees
            var syntaxTrees = new List<SyntaxTree>();
            foreach (var sf in SourceFiles)
            {
                var data = File.ReadAllBytes(sf.FullName);
                var sourceText = SourceText.From(data, data.Length, Encoding.UTF8, SourceHashAlgorithm.Sha1, false, true);
                var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, null, sf.FullName);

                syntaxTrees.Add(syntaxTree);
            }

            // Get trusted assemblies
            var trustedAssembliesPaths = ((string)(AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? ""))
                .Split(Path.PathSeparator).ToList();

            // Get assemblies in the libraries directory
            if (Directory.Exists(LibrariesPath))
            {
                trustedAssembliesPaths.AddRange(Directory.GetFiles(LibrariesPath, "*.dll", SearchOption.AllDirectories));
            }

            // Convert all assembly paths into references
            var references = trustedAssembliesPaths
                .Select(p => MetadataReference.CreateFromFile(p))
                .ToList();

            // Create the compilation
            var compilation = CSharpCompilation.Create(
                Name,
                syntaxTrees,
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, platform: Platform.AnyCpu));

            // Try to compile and emit assembly and PDB 
            var emitResult = compilation.Emit(AssemblyFileName, PdbFileName);
            if (!emitResult.Success)
            {
                // When the compilation fails, it leaves behind zero byte files, remove them
                if (File.Exists(AssemblyFileName)) File.Delete(AssemblyFileName);
                if (File.Exists(PdbFileName)) File.Delete(PdbFileName);

                foreach (var d in emitResult.Diagnostics)
                {
                    _logger.LogError($"<{nameof(Plugin)}.{nameof(CompilePlugin)}> Error: {d.Location}: {d.GetMessage()}");
                }
                return false;
            }

            return true;
        }

        private Type? GetPluginType()
        {
            if (Assembly is null) return null;

            var pluginType = Assembly
                .GetTypes()
                .FirstOrDefault(x => x.BaseType != null && x.BaseType.Equals(typeof(CodeDeckPlugin)));

            return pluginType;
        }

        public Tile? CreateTileInstance(string tileTypeName, Dictionary<string, string>? settings)
        {
            if (PluginType is null)
            {
                return null;
            }

            var tileType = PluginType
                .GetNestedTypes()
                .Where(x => x.BaseType == typeof(Tile))
                .Where(x => x.Name == tileTypeName)
                .FirstOrDefault();

            if (tileType is null)
            {
                return null;
            }

            if (Activator.CreateInstance(tileType) is not Tile tileInstance)
            {
                return null;
            }

            MapSettingsToTile(settings, tileType, tileInstance);

            return tileInstance;
        }

        private void MapSettingsToTile(Dictionary<string, string>? settings, Type tileType, Tile tileInstance)
        {
            // Assign the raw key settings dictionary to the Tile instance
            tileInstance.Settings = settings;

            // Map key settings to Tile properties that are annotated with the SettingAttribute

            // Get all properties with the SettingAttribute
            var settingProperties = tileType.GetProperties()
                .Where(x => x.CustomAttributes.Any(ca => ca.AttributeType.Name == nameof(SettingAttribute)))
                .ToList();

            // Try to parse the setting into the correct type and assign the value to the property
            foreach (var p in settingProperties)
            {
                if (settings?.TryGetValue(p.Name, out var value) ?? false)
                {
                    // Parse string
                    if (p.PropertyType.Name == typeof(string).Name)
                    {
                        p.SetValue(tileInstance, value);
                    }
                    // Parse bool
                    else if (p.PropertyType == typeof(bool?) || p.PropertyType == typeof(bool))
                    {
                        if (bool.TryParse(value, out var parsedValue))
                        {
                            p.SetValue(tileInstance, parsedValue);
                        }
                    }
                    // Parse int
                    else if (p.PropertyType == typeof(int?) || p.PropertyType == typeof(int))
                    {
                        if (int.TryParse(value, out var parsedValue))
                        {
                            p.SetValue(tileInstance, parsedValue);
                        }
                    }
                    // Parse double
                    else if (p.PropertyType == typeof(double?) || p.PropertyType == typeof(double))
                    {
                        if (double.TryParse(value, out var parsedValue))
                        {
                            p.SetValue(tileInstance, parsedValue);
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"<{nameof(Plugin)}.{nameof(MapSettingsToTile)}> Can not map setting '{p.Name}' because data type '{p.PropertyType.Name}' is not supported.");
                    }
                }
            }
        }
    }
}
