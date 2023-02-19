using System.Collections.Generic;

namespace CodeDeck.Models.Configuration
{
    /// <summary>
    /// Configuration class for a complete Deck
    /// </summary>
    public class StreamDeckConfiguration
    {
        public string? DevicePath { get; set; }

        public int Brightness { get; set; } = 75;

        public string? FallbackFont { get; set; } = "Twemoji Mozilla";

        public List<Profile> Profiles { get; set; } = new();
    }
}
