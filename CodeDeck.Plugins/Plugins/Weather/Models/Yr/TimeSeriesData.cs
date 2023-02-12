using System.Text.Json.Serialization;

namespace CodeDeck.Plugins.Plugins.Weather.Models.Yr
{
    public class TimeSeriesData
    {
        [JsonPropertyName("instant")]
        public DataInstant? Instant { get; set; }

        [JsonPropertyName("next_1_hours")]
        public DataPeriod? Next1Hours { get; set; }

        [JsonPropertyName("next_6_hours")]
        public DataPeriod? Next6Hours { get; set; }

        [JsonPropertyName("next_12_hours")]
        public DataPeriod? Next12Hours { get; set; }

    }
}
