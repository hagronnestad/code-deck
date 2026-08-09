namespace CodeDeck.Plugins.Plugins.Weather.Models.Yr
{
    public class LocationForecast
    {
        [JsonPropertyName("properties")]
        public Properties? Properties { get; set; }

    }
}
