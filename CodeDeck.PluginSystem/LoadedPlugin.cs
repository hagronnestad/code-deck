using CodeDeck.PluginAbstractions;
using System.Diagnostics;
using System.Reflection;

namespace CodeDeck.PluginSystem
{
    public class LoadedPlugin
    {
        public string Name => PluginType.Name;
        public Assembly Assembly { get; set; }

        public Type PluginType { get; set; }


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

            PluginType = pluginType;
        }

        public Tile? CreateTileInstance(string tileTypeName)
        {
            var tileType = PluginType
                .GetNestedTypes()
                .Where(x => x.BaseType == typeof(Tile))
                .Where(x => x.Name == tileTypeName)
                .FirstOrDefault();

            if (tileType is null) return null;

            var tileInstance = Activator.CreateInstance(tileType) as Tile;

            return tileInstance;
        }
    }
}
