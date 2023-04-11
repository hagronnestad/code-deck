using System.Collections.Generic;

namespace CodeDeck.Models.Configuration
{
    public class PluginConfiguration
    {
        public string? Name { get; set; }

        public Dictionary<string, string>? Settings { get; set; }
    }
}
