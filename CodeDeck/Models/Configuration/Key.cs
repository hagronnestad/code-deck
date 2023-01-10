using System.Collections.Generic;

namespace CodeDeck.Models.Configuration
{
    /// <summary>
    /// Configuration class for a Key
    /// </summary>
    public class Key
    {
        public const string KEY_TYPE_NORMAL = "normal";
        public const string KEY_TYPE_GO_BACK = "back";
        public const string KEY_TYPE_GOTO_PAGE = "page";

        /// <summary>
        /// The Key index on the deck
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? Text { get; set; }
        public string? TextColor { get; set; }
        public string? BackgroundColor { get; set; }
        public string? Font { get; set; }
        public float? FontSize { get; set; }

        public string? Image { get; set; }
        public int? ImagePadding { get; set; }

        /// <summary>
        /// Plugin reference
        /// </summary>
        public string? Plugin { get; set; }
        
        /// <summary>
        /// Tile reference
        /// </summary>
        public string? Tile { get; set; }

        public string KeyType { get; set; } = KEY_TYPE_NORMAL;
        public string? Profile { get; set; }
        public string? Page { get; set; }

        /// <summary>
        /// Settings for the Tile
        /// </summary>
        // TODO: Should this be a typed object, type defined by Tile
        public Dictionary<string, string>? Settings { get; set; }
    }
}
