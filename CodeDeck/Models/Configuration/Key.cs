using CodeDeck.PluginAbstractions;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CodeDeck.Models.Configuration
{
    /// <summary>
    /// Configuration class for a Key
    /// </summary>
    public class Key
    {
        public const string KEY_TYPE_NORMAL = "Normal";
        public const string KEY_TYPE_GO_BACK = "Back";
        public const string KEY_TYPE_GOTO_PAGE = "Page";

        /// <summary>
        /// The Key index on the deck
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? Text { get; set; }
        public string? TextColor { get; set; }
        public float? LineSpacing { get; set; }
        public string? Font { get; set; }
        public float? FontSize { get; set; }
        public bool? FontBold { get; set; }
        public bool? FontItalic { get; set; }

        public string? BackgroundColor { get; set; }
        public string? Image { get; set; }
        public int? ImagePadding { get; set; }

        public string? ActivityIndicatorColor { get; set; }

        public bool? ShowFolderIndicator { get; set; }
        public string? FolderIndicatorColor { get; set; }

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



        [JsonIgnore]
        public Color? TextColorAsColor
        {
            get
            {
                try
                {
                    if (TextColor != null) return Color.ParseHex(TextColor);
                }
                catch (Exception) { }

                return null;
            }
        }

        [JsonIgnore]
        public Color? BackgroundColorAsColor
        {
            get
            {
                try
                {
                    if (BackgroundColor != null) return Color.ParseHex(BackgroundColor);
                }
                catch (Exception) { }

                return null;
            }
        }

        [JsonIgnore]
        public Color? ActivityIndicatorColorAsColor
        {
            get
            {
                try
                {
                    if (ActivityIndicatorColor != null) return Color.ParseHex(ActivityIndicatorColor);
                }
                catch (Exception) { }

                return null;
            }
        }

        [JsonIgnore]
        public Color? FolderIndicatorColorAsColor
        {
            get
            {
                try
                {
                    if (FolderIndicatorColor != null) return Color.ParseHex(FolderIndicatorColor);
                }
                catch (Exception) { }

                return null;
            }
        }
    }
}
