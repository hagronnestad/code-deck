using System.Text.Json.Serialization;

namespace CodeDeck.Plugins.Plugins.Weather.Models.Yr
{
    public class DataInstant
    {
        [JsonPropertyName("details")]
        public DataInstantDetails? Details { get; set; }
    }
}
