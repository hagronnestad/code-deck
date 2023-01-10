using System.Collections.Generic;

namespace CodeDeck.Models.Configuration
{
    /// <summary>
    /// Configuration class for a Page
    /// </summary>
    public class Page
    {
        public string? Name { get; set; }
        public List<Key> Keys { get; set; } = new();
    }
}
