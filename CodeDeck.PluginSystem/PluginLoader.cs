using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace CodeDeck.PluginSystem
{
    public class PluginLoader
    {
        private readonly ILogger<PluginLoader> _logger;

        public List<LoadedPlugin> LoadedPlugins = new();

        public PluginLoader(ILogger<PluginLoader> logger)
        {
            _logger = logger;
            _logger.BeginScope<PluginLoader>(this);
        }

        public async Task LoadPluginsAsync()
        {
            var pluginFolders = Directory.GetDirectories("Plugins");
            _logger.LogInformation($"Found plugins: {string.Join(", ",
                pluginFolders.Select(x => Path.GetDirectoryName(x.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar)))}");

            var assemblies = new List<Assembly>();

            foreach (var pf in pluginFolders)
            {
                var pluginFolder = pf.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

                var codeFiles = Directory.GetFiles(pluginFolder, "*.cs");
                var codeFileContents = new List<string>();

                foreach (var cf in codeFiles)
                {
                    var usings = new List<string>() {
                        $"using {nameof(CodeDeck)}.{nameof(PluginAbstractions)};"
                    };
                    
                    var code = File.ReadAllText(cf);

                    foreach (var u in usings)
                    {
                        if (code.ToLower().Contains(u.ToLower())) continue;
                        code = u + code + '\n';
                    }

                    codeFileContents.Add(code);
                    var assembly = CompilePlugin(codeFileContents);

                    if (assembly != null)
                    {
                        _logger.LogInformation($"Plugin: {Path.GetDirectoryName(pluginFolder)} successfully compiled.");
                        assemblies.Add(assembly);
                    }
                    else
                    {
                        _logger.LogWarning($"Plugin {Path.GetDirectoryName(pluginFolder)} did not produce an assembly.");
                    }
                }
            }

            await Task.WhenAll(assemblies.Select(x => LoadPluginAsync(x)));
        }

        private async Task<LoadedPlugin> LoadPluginAsync(Assembly assembly)
        {
            return await Task.Run(() => {
                var loadedPlugin = new LoadedPlugin(assembly);
                LoadedPlugins.Add(loadedPlugin);
                _logger.LogInformation($"Plugin loaded: {loadedPlugin.Name}");
                return Task.FromResult(loadedPlugin);
            });
        }

        public Assembly? CompilePlugin(List<string> codeFiles)
        {
            var syntaxTrees = codeFiles.Select(x => CSharpSyntaxTree.ParseText(x)).ToList();

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

            using (var dllStream = new MemoryStream())
            using (var pdbStream = new MemoryStream())
            {
                var emitResult = compilation.Emit(dllStream, pdbStream);

                if (!emitResult.Success)
                {
                    foreach (var d in emitResult.Diagnostics)
                    {
                        _logger.LogError($"{d.Location}: {d.GetMessage()}");
                    }

                    return null;
                }

                var a = Assembly.Load(dllStream.ToArray());
                WriteAssemblyInfoToConsole(a);
                return a;
            }
        }

        public void WriteAssemblyInfoToConsole(Assembly assembly)
        {
            foreach (Module m in assembly.GetModules())
            {
                foreach (Type t in m.GetTypes().Where(x => x.IsVisible))
                {
                    _logger.LogDebug(t.Name);

                    //foreach (MethodInfo mi in t.GetMethods().Where(x => x.IsVirtual))
                    //{
                    //    Console.WriteLine($"{t.Name}.{mi.Name}()");
                    //}
                }
            }
        }
    }
}
