using System.Collections.Generic;

namespace CodeDeck.Models.Configuration
{
    /// <summary>
    /// Configuration class for a Page
    /// </summary>
    public class Page
    {
        public const string PAGE_DEFAULT_NAME = "DefaultPage";

        public string Name { get; set; } = PAGE_DEFAULT_NAME;
        public List<Key> Keys { get; set; } = new();
    }
}
