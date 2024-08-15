namespace CodeDeck.PluginAbstractions
{
    /// <summary>
    /// The base class for all plugins
    /// </summary>
    public abstract class CodeDeckPlugin
    {
        public static string PluginPath { get; set; }

        public static Dictionary<string, string>? Settings { get; set; }
    }
}
