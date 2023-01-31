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

        public Tile? CreateTileInstance(string tileTypeName, Dictionary<string, string>? settings)
        {
            var tileType = PluginType
                .GetNestedTypes()
                .Where(x => x.BaseType == typeof(Tile))
                .Where(x => x.Name == tileTypeName)
                .FirstOrDefault();

            if (tileType is null) return null;
            if (Activator.CreateInstance(tileType) is not Tile tileInstance) return null;

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

            return tileInstance;
        }
    }
}
