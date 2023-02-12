using System.Text.Json.Serialization;

namespace CodeDeck.Plugins.Plugins.Weather.Models.Yr
{
    public class DataPeriod
    {
        [JsonPropertyName("summary")]
        public DataPeriodSummary? Summary { get; set; }

        [JsonPropertyName("details")]
        public DataPeriodDetails? Details { get; set; }
    }
}
