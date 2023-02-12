using System.Text.Json.Serialization;

namespace CodeDeck.Plugins.Plugins.Weather.Models.Yr
{
    public class DataPeriodDetails
    {
        [JsonPropertyName("precipitation_amount")]
        public double PrecipitationAmount { get; set; }
    }
}