using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using CodeDeck.PluginAbstractions;

namespace CodeDeck.PluginSystem
{
    public class Plugin
    {
        private readonly ILogger _logger;

        /// <summary>
        /// The location of the plugin directory on disk.
        /// </summary>
        public string PluginPath { get; set; }

        /// <summary>
        /// The name of the plugin. The plugin name is the same as the plugin directory name.
        /// </summary>
        public string Name { get; set; }

        public string BuildPath => Path.Combine(PluginPath, "bin");
        public string AssemblyFileName => Path.Combine(BuildPath, $"{Name}.dll");
        public string PdbFileName => Path.Combine(BuildPath, $"{Name}.pdb");

        public List<FileInfo>? SourceFiles { get; set; }

        public FileInfo? AssemblyFileInfo { get; set; }
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
            Directory.CreateDirectory(BuildPath);

            SourceFiles = GetAllSourceFiles();
            AssemblyFileInfo = GetAssemblyFileInfo();

            if (AssemblyFileInfo == null || !IsAssemblyUpToDate())
            {
                _logger.LogInformation($"<{nameof(Plugin)}.{nameof(Init)}> Compiling plugin: '{Name}'");
                Compile();
            }
            else
            {
                _logger.LogInformation($"<{nameof(Plugin)}.{nameof(Init)}> Loading cached plugin: '{AssemblyFileInfo.FullName}'");
                Assembly = Assembly.LoadFile(AssemblyFileInfo.FullName);
            }

            ReadPluginType();
        }

        public List<FileInfo> GetAllSourceFiles()
        {
            var sourceFiles = Directory.GetFiles(PluginPath, "*.cs", SearchOption.AllDirectories);
            var sourceFilesInfo = sourceFiles.Select(x => new FileInfo(x));
            
            return sourceFilesInfo.ToList();
        }

        public FileInfo? GetAssemblyFileInfo()
        {
            if (File.Exists(AssemblyFileName))
            {
                return new FileInfo(AssemblyFileName);
            }

            return null;
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

        public bool Compile()
        {
            if (SourceFiles is null || !SourceFiles.Any()) return false;

            var syntaxTrees = new List<SyntaxTree>();

            foreach (var sf in SourceFiles)
            {
                var data = File.ReadAllBytes(sf.FullName);
                var sourceText = SourceText.From(data, data.Length, Encoding.UTF8, SourceHashAlgorithm.Sha1, false, true);
                var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, null, sf.FullName);

                syntaxTrees.Add(syntaxTree);
            }

            var trustedAssembliesPaths = ((string)(AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? ""))
                .Split(Path.PathSeparator);
            
            var references = trustedAssembliesPaths
                .Select(p => MetadataReference.CreateFromFile(p))
                .ToList();

            var compilation = CSharpCompilation.Create(
                null,
                syntaxTrees,
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            var emitResult = compilation.Emit(AssemblyFileName, PdbFileName);

            if (!emitResult.Success)
            {
                // When the compilation fails, it leaves behind zero byte files, remove them
                if (File.Exists(AssemblyFileName)) File.Delete(AssemblyFileName);
                if (File.Exists(PdbFileName)) File.Delete(PdbFileName);

                foreach (var d in emitResult.Diagnostics)
                {
                    _logger.LogError($"<{nameof(Plugin)}.{nameof(Compile)}> Error: {d.Location}: {d.GetMessage()}");
                }
                return false;
            }

            Assembly = Assembly.LoadFile(AssemblyFileName);
            return true;
        }

        private bool ReadPluginType()
        {
            if (Assembly is null) return false;

            var pluginType = Assembly
                .GetTypes()
                .FirstOrDefault(x => x.BaseType != null && x.BaseType.Equals(typeof(CodeDeckPlugin)));

            if (pluginType == null)
            {
                _logger.LogWarning("Assembly does not contain a valid plugin!");
                return false;
            }

            PluginType = pluginType;
            return true;
        }

        public Tile? CreateTileInstance(string tileTypeName, Dictionary<string, string>? settings)
        {
            if (PluginType is null) return null;

            var tileType = PluginType
                .GetNestedTypes()
                .Where(x => x.BaseType == typeof(Tile))
                .Where(x => x.Name == tileTypeName)
                .FirstOrDefault();

            if (tileType is null) return null;
            if (Activator.CreateInstance(tileType) is not Tile tileInstance) return null;

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
                    if (p.PropertyType == typeof(bool?) || p.PropertyType == typeof(bool))
                    {
                        if (bool.TryParse(value, out var parsedValue))
                        {
                            p.SetValue(tileInstance, parsedValue);
                        }
                    }
                    else if (p.PropertyType == typeof(int?) || p.PropertyType == typeof(int))
                    {
                        if (int.TryParse(value, out var parsedValue))
                        {
                            p.SetValue(tileInstance, parsedValue);
                        }
                    }
                    else if (p.PropertyType == typeof(double?) || p.PropertyType == typeof(double))
                    {
                        if (double.TryParse(value, out var parsedValue))
                        {
                            p.SetValue(tileInstance, parsedValue);
                        }
                    }
                    else
                    {
                        p.SetValue(tileInstance, value);
                    }
                }
            }
        }
    }
}
