using System.Collections.Generic;

namespace CodeDeck.Models.Configuration
{
    /// <summary>
    /// Configuration class for a complete Deck
    /// </summary>
    public class StreamDeckConfiguration
    {
        public int Brightness { get; set; } = 75;

        public List<Profile> Profiles { get; set; } = new();
    }
}
