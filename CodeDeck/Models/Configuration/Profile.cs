using System.Collections.Generic;

namespace CodeDeck.Models.Configuration
{
    /// <summary>
    /// Configuration class for a Profile
    /// </summary>
    public class Profile
    {
        public string? Name { get; set; }
        public List<Page> Pages { get; set; } = new();
    }
}
