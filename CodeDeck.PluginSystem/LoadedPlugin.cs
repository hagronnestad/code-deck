using CodeDeck.PluginAbstractions;
using System.Diagnostics;
using System.Reflection;

namespace CodeDeck.PluginSystem
{
    public class LoadedPlugin
    {
        public CodeDeckPlugin? Instance { get; set; }

        public string Name { get; set; } = "";
        public Assembly Assembly { get; set; }

        public LoadedPlugin(Assembly assembly)
        {
            Assembly = assembly;

            var assemblyTypes = assembly.GetTypes();

            var pluginType = assemblyTypes
                .FirstOrDefault(x => x.BaseType != null && x.BaseType.Equals(typeof(CodeDeckPlugin)));

            if (pluginType == null)
            {
                Debug.WriteLine("Assembly does not contain a valid plugin!");
                return;
            }

            var pluginInstance = Activator.CreateInstance(pluginType) as CodeDeckPlugin;
            Instance = pluginInstance;

            Name = pluginType.Name;
        }
    }
}
